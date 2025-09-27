using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace Mma.Configuration.Providers.SqlServer;

/// <summary>
/// Configuration provider that loads settings from SQL Server table with JSON columns
/// </summary>
public class SqlServerJsonConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly SqlServerJsonConfigurationSource _source;
    private readonly Timer? _reloadTimer;
    private  Timer? _retryTimer;
    private readonly ILogger? _logger;
    private int _retryAttempts = 0;
    private bool _isInitialized = false;

    public SqlServerJsonConfigurationProvider(SqlServerJsonConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        if (string.IsNullOrWhiteSpace(_source.ConnectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(source));

        // Setup reload timer if configured and provider is initialized
        if (_source.ReloadOnChange && _source.ReloadInterval > TimeSpan.Zero)
        {
            _reloadTimer = new Timer(
                callback: _ => TryLoad(),
                state: null,
                dueTime: _source.ReloadInterval,
                period: _source.ReloadInterval);
        }

    }

    public override void Load()
    {
        TryLoad();
    }

    private void TryLoad()
    {
        try
        {
            LoadInternal();
            _isInitialized = true;
            _retryAttempts = 0;
            _retryTimer?.Dispose();
        }
        catch (Exception ex)
        {
            HandleLoadError(ex);
        }
    }

    private void HandleLoadError(Exception ex)
    {
        _logger?.LogError(ex, "Failed to load configuration from SQL Server");

        if (_source.Optional)
        {
            _logger?.LogWarning("SQL Server configuration is optional, continuing without loading configuration");
            if (!_isInitialized)
            {
                Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                _isInitialized = true;
            }

            // Setup retry mechanism for optional providers
            SetupRetryTimer();
            return;
        }

        // For non-optional providers, check if we should auto-create table
        if (_source.AutoCreateTable && IsTableMissingError(ex))
        {
            _logger?.LogInformation("Attempting to auto-create configuration table");
            if (TryCreateTable())
            {
                _logger?.LogInformation("Configuration table created successfully, retrying load");
                TryLoad();
                return;
            }
        }

        // If database doesn't exist or other critical errors, setup retry for optional or throw for required
        if (IsDatabaseConnectionError(ex))
        {
            if (_source.Optional)
            {
                _logger?.LogWarning("Database connection failed for optional configuration, will retry periodically");
                SetupRetryTimer();
                return;
            }
        }

        // For non-optional providers, throw the exception
        throw new InvalidOperationException("Failed to load configuration from SQL Server", ex);
    }

    private void SetupRetryTimer()
    {
        if (_retryAttempts >= _source.MaxRetryAttempts)
        {
            _logger?.LogWarning("Maximum retry attempts ({MaxRetries}) reached for SQL Server configuration", _source.MaxRetryAttempts);
            return;
        }

        _retryAttempts++;
        var delay = TimeSpan.FromMilliseconds(_source.RetryDelay.TotalMilliseconds * Math.Pow(2, _retryAttempts - 1)); // Exponential backoff

        _logger?.LogInformation("Scheduling configuration retry {Attempt}/{MaxRetries} in {Delay}",
            _retryAttempts, _source.MaxRetryAttempts, delay);

        _retryTimer?.Dispose();
         _retryTimer = new Timer(
            callback: _ => TryLoad(),
            state: null,
            dueTime: delay,
            period: Timeout.InfiniteTimeSpan);
    }

    private void LoadInternal()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var connection = new SqlConnection(_source.ConnectionString);
        connection.Open();

        // Check if table exists
        if (!TableExists(connection))
        {
            if (_source.AutoCreateTable)
            {
                CreateTable(connection);
                _logger?.LogInformation("Created configuration table '{TableName}'", _source.TableName);
            }
            else
            {
                throw new InvalidOperationException($"Configuration table '{_source.TableName}' does not exist");
            }
        }

        var query = BuildQuery();
        using var command = new SqlCommand(query, connection);

        if (!string.IsNullOrEmpty(_source.Environment) && !string.IsNullOrEmpty(_source.EnvironmentColumn))
        {
            command.Parameters.AddWithValue("@Environment", _source.Environment);
        }

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var key = reader[_source.KeyColumn]?.ToString();
            var jsonValue = reader[_source.JsonColumn]?.ToString();

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(jsonValue))
                continue;

            // Parse JSON and flatten it into configuration keys
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonValue);
                FlattenJsonElement(data, key, jsonElement);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning("Failed to parse JSON for key '{Key}': {Error}", key, ex.Message);
                // Store as plain string if JSON parsing fails
                data[key] = jsonValue;
            }
        }

        Data = data;
        OnReload();
    }

    private bool TableExists(SqlConnection connection)
    {
        var query = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @TableName";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@TableName", _source.TableName);

        var count = (int)command.ExecuteScalar()!;
        return count > 0;
    }

    private void CreateTable(SqlConnection connection)
    {
        var createTableScript = GenerateCreateTableScript();
        using var command = new SqlCommand(createTableScript, connection);
        command.ExecuteNonQuery();
    }

    private bool TryCreateTable()
    {
        try
        {
            using var connection = new SqlConnection(_source.ConnectionString);
            connection.Open();
            CreateTable(connection);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to auto-create configuration table");
            return false;
        }
    }

    private string GenerateCreateTableScript()
    {
        var environmentColumnDefinition = !string.IsNullOrEmpty(_source.EnvironmentColumn)
            ? $"[{_source.EnvironmentColumn}] nvarchar(50) NULL,"
            : "";

        var uniqueConstraint = !string.IsNullOrEmpty(_source.EnvironmentColumn)
            ? $"CONSTRAINT UK_{_source.TableName}_Key_Environment UNIQUE ([{_source.KeyColumn}], [{_source.EnvironmentColumn}])"
            : $"CONSTRAINT UK_{_source.TableName}_Key UNIQUE ([{_source.KeyColumn}])";

        return $@"
                CREATE TABLE [{_source.TableName}] (
                    Id int IDENTITY(1,1) PRIMARY KEY,
                    [{_source.KeyColumn}] nvarchar(255) NOT NULL,
                    [{_source.JsonColumn}] nvarchar(max) NOT NULL,
                    {environmentColumnDefinition}
                    CreatedAt datetime2 DEFAULT GETDATE(),
                    UpdatedAt datetime2 DEFAULT GETDATE(),
                    {uniqueConstraint}
                );

                CREATE INDEX IX_{_source.TableName}_Key_Environment 
                ON [{_source.TableName}]([{_source.KeyColumn}]{(!string.IsNullOrEmpty(_source.EnvironmentColumn) ? $", [{_source.EnvironmentColumn}]" : "")}) 
                INCLUDE ([{_source.JsonColumn}]);";
    }

    private static bool IsDatabaseConnectionError(Exception ex)
    {
        return ex is SqlException sqlEx && (
            sqlEx.Number == 2 ||      // Timeout
            sqlEx.Number == 53 ||     // Network path not found  
            sqlEx.Number == 4060 ||   // Database doesn't exist
            sqlEx.Number == 18456 ||  // Login failed
            sqlEx.Number == 1225 ||   // Database principal doesn't exist
            sqlEx.Number == 233       // Connection init error
        );
    }

    private static bool IsTableMissingError(Exception ex)
    {
        return ex is SqlException sqlEx && sqlEx.Number == 208; // Invalid object name
    }

    private string BuildQuery()
    {
        var query = $"SELECT [{_source.KeyColumn}], [{_source.JsonColumn}] FROM [{_source.TableName}]";

        if (!string.IsNullOrEmpty(_source.Environment) && !string.IsNullOrEmpty(_source.EnvironmentColumn))
        {
            query += $" WHERE [{_source.EnvironmentColumn}] = @Environment OR [{_source.EnvironmentColumn}] IS NULL";
            query += $" ORDER BY CASE WHEN [{_source.EnvironmentColumn}] IS NULL THEN 0 ELSE 1 END"; // NULL (default) values first
        }

        return query;
    }

    private static void FlattenJsonElement(Dictionary<string, string?> data, string prefix, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    FlattenJsonElement(data, key, property.Value);
                }
                break;

            case JsonValueKind.Array:
                for (int i = 0; i < element.GetArrayLength(); i++)
                {
                    var key = $"{prefix}:{i}";
                    FlattenJsonElement(data, key, element[i]);
                }
                break;

            case JsonValueKind.String:
                data[prefix] = element.GetString();
                break;

            case JsonValueKind.Number:
                data[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                data[prefix] = element.GetBoolean().ToString();
                break;

            case JsonValueKind.Null:
                data[prefix] = null;
                break;

            default:
                data[prefix] = element.GetRawText();
                break;
        }
    }

    public void Dispose()
    {
        _reloadTimer?.Dispose();
        _retryTimer?.Dispose();
    }
}

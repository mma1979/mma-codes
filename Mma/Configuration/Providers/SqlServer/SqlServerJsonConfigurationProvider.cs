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
    private readonly ILogger? _logger;

    public SqlServerJsonConfigurationProvider(SqlServerJsonConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        if (string.IsNullOrWhiteSpace(_source.ConnectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(source));

        // Setup reload timer if configured
        if (_source.ReloadOnChange && _source.ReloadInterval > TimeSpan.Zero)
        {
            _reloadTimer = new Timer(
                callback: _ => Load(),
                state: null,
                dueTime: _source.ReloadInterval,
                period: _source.ReloadInterval);
        }
    }

    public override void Load()
    {
        try
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            using var connection = new SqlConnection(_source.ConnectionString);
            connection.Open();

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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load configuration from SQL Server");
            throw;
        }
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
    }
}


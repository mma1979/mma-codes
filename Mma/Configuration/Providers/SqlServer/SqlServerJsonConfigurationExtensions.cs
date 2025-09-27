using Microsoft.Extensions.Configuration;

using Mma.Configuration.Providers.SqlServer;

namespace Mma.Configuration.Providers.SqlServer;

// <summary>
/// Extension methods for adding SQL Server JSON configuration
/// </summary>
public static class SqlServerJsonConfigurationExtensions
{
    /// <summary>
    /// Adds SQL Server JSON configuration source
    /// </summary>
    public static IConfigurationBuilder AddSqlServerJson(
        this IConfigurationBuilder builder,
        string connectionString,
        string tableName = "AppSettings",
        string keyColumn = "Key",
        string jsonColumn = "JsonValue",
        string? environmentColumn = null,
        string? environment = null,
        bool reloadOnChange = false,
        TimeSpan reloadInterval = default,
        bool autoCreateTable = false,
        bool optional = false,
        TimeSpan retryDelay = default,
        int maxRetryAttempts = 5)
    {
        return builder.Add(new SqlServerJsonConfigurationSource
        {
            ConnectionString = connectionString,
            TableName = tableName,
            KeyColumn = keyColumn,
            JsonColumn = jsonColumn,
            EnvironmentColumn = environmentColumn,
            Environment = environment,
            ReloadOnChange = reloadOnChange,
            ReloadInterval = reloadInterval == default ? TimeSpan.FromMinutes(5) : reloadInterval,
            AutoCreateTable = autoCreateTable,
            Optional = optional,
            RetryDelay = retryDelay == default ? TimeSpan.FromSeconds(30) : retryDelay,
            MaxRetryAttempts = maxRetryAttempts
        });
    }

    /// <summary>
    /// Adds SQL Server JSON configuration source with configuration action
    /// </summary>
    public static IConfigurationBuilder AddSqlServerJson(
        this IConfigurationBuilder builder,
        Action<SqlServerJsonConfigurationSource> configureSource)
    {
        var source = new SqlServerJsonConfigurationSource();
        configureSource(source);
        return builder.Add(source);
    }
}

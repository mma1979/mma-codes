using Microsoft.Extensions.Configuration;

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
        TimeSpan reloadInterval = default)
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
            ReloadInterval = reloadInterval == default ? TimeSpan.FromMinutes(5) : reloadInterval
        });
    }

    // <summary>
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
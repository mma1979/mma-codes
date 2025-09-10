using Microsoft.Extensions.Configuration;

namespace Mma.Configuration.Providers.SqlServer;

/// <summary>
/// Configuration source for SQL Server JSON configuration provider
/// </summary>
public class SqlServerJsonConfigurationSource : IConfigurationSource
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "AppSettings";
    public string KeyColumn { get; set; } = "Key";
    public string JsonColumn { get; set; } = "JsonValue";
    public string? EnvironmentColumn { get; set; }
    public string? Environment { get; set; }
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.Zero;
    public bool ReloadOnChange { get; set; } = false;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SqlServerJsonConfigurationProvider(this);
    }
}

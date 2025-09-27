# Mma.Configuration

A powerful .NET configuration provider that loads application settings from SQL Server tables with JSON columns. This provider integrates seamlessly with the .NET configuration system and supports environment-specific configurations, automatic reloading, and complex JSON object storage.

## 🚀 Quick Start

### Installation

```bash
dotnet add package Mma.Configuration --version 9.0.3
```

Or via NuGet Package Manager:
```xml
<PackageReference Include="Mma.Configuration" Version="9.0.3" />
```

### Basic Usage

```csharp
using Mma.Configuration.Providers.SqlServer;

var configuration = new ConfigurationBuilder()
    .AddSqlServerJson(
        connectionString: "Server=localhost;Database=MyApp;Integrated Security=true;TrustServerCertificate=true;")
    .Build();

// Access configuration values
var dbConnectionString = configuration.GetConnectionString("Database");
var logLevel = configuration["Logging:LogLevel:Default"];
```

## 🏗️ Database Setup

Create your configuration table:

```sql
CREATE TABLE AppSettings (
    Id int IDENTITY(1,1) PRIMARY KEY,
    [Key] nvarchar(255) NOT NULL,
    JsonValue nvarchar(max) NOT NULL,
    Environment nvarchar(50) NULL,
    CreatedAt datetime2 DEFAULT GETDATE(),
    UpdatedAt datetime2 DEFAULT GETDATE(),
    UNIQUE([Key], Environment)
);
```

## ✨ Key Features

- **Environment-Specific Configuration**: Support for development, staging, production environments with fallback
- **JSON Object Storage**: Store complex configuration objects as JSON with automatic flattening
- **Automatic Reloading**: Optional configuration reloading at specified intervals
- **High Performance**: Optimized SQL queries with connection pooling support
- **Thread-Safe**: Safe for use in multi-threaded applications
- **Comprehensive Logging**: Built-in logging for debugging and monitoring
- **Flexible Schema**: Customizable table and column names
- **Error Resilience**: Graceful error handling with retry mechanisms

## 🔧 Advanced Configuration

```csharp
var configuration = new ConfigurationBuilder()
    .AddSqlServerJson(source =>
    {
        source.ConnectionString = "Server=localhost;Database=MyApp;Integrated Security=true;";
        source.TableName = "ApplicationSettings";
        source.KeyColumn = "SettingKey";
        source.JsonColumn = "SettingValue";
        source.EnvironmentColumn = "Env";
        source.Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        source.ReloadOnChange = true;
        source.ReloadInterval = TimeSpan.FromMinutes(10);
        source.AutoCreateTable = false;
        source.Optional = false;
        source.MaxRetryAttempts = 5;
        source.RetryDelay = TimeSpan.FromSeconds(30);
    })
    .Build();
```

## 📋 Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | *Required* | SQL Server connection string |
| `TableName` | `string` | `"AppSettings"` | Name of the configuration table |
| `KeyColumn` | `string` | `"Key"` | Name of the key column |
| `JsonColumn` | `string` | `"JsonValue"` | Name of the JSON value column |
| `EnvironmentColumn` | `string?` | `null` | Name of the environment column (optional) |
| `Environment` | `string?` | `null` | Current environment name |
| `ReloadOnChange` | `bool` | `false` | Enable automatic reloading |
| `ReloadInterval` | `TimeSpan` | `TimeSpan.Zero` | Reload interval when ReloadOnChange is true |
| `AutoCreateTable` | `bool` | `false` | Automatically create table if it doesn't exist |
| `Optional` | `bool` | `false` | Whether the configuration source is optional |
| `MaxRetryAttempts` | `int` | `5` | Maximum retry attempts on connection failure |
| `RetryDelay` | `TimeSpan` | `TimeSpan.FromSeconds(30)` | Delay between retry attempts |

## 🌍 ASP.NET Core Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add SQL Server configuration early in the pipeline
builder.Configuration.AddSqlServerJson(
    connectionString: builder.Configuration.GetConnectionString("ConfigDatabase")!,
    environment: builder.Environment.EnvironmentName,
    environmentColumn: "Environment",
    reloadOnChange: true,
    reloadInterval: TimeSpan.FromMinutes(5));

// Configure strongly-typed options
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

var app = builder.Build();
app.Run();
```

## 📊 JSON Flattening

The provider automatically flattens JSON objects into the standard .NET configuration format:

**Input JSON:**
```json
{
    "Database": {
        "ConnectionString": "Server=localhost;Database=MyApp;",
        "CommandTimeout": 30,
        "Retry": {
            "MaxAttempts": 3,
            "DelaySeconds": 5
        }
    },
    "Features": ["Feature1", "Feature2", "Feature3"]
}
```

**Flattened Keys:**
- `MySection:Database:ConnectionString`
- `MySection:Database:CommandTimeout`
- `MySection:Database:Retry:MaxAttempts`
- `MySection:Database:Retry:DelaySeconds`
- `MySection:Features:0` (Feature1)
- `MySection:Features:1` (Feature2)
- `MySection:Features:2` (Feature3)

## 🔄 Configuration Reloading

Enable automatic configuration reloading:

```csharp
var configuration = new ConfigurationBuilder()
    .AddSqlServerJson(
        connectionString: connectionString,
        reloadOnChange: true,
        reloadInterval: TimeSpan.FromMinutes(5))
    .Build();

// Register for change notifications
ChangeToken.OnChange(
    () => configuration.GetReloadToken(),
    () => {
        Console.WriteLine("Configuration reloaded!");
        // Handle configuration changes
    });
```

## 🏃‍♂️ Performance Tips

1. **Use Connection Pooling**: Include pooling settings in your connection string
2. **Create Indexes**: Add appropriate indexes on your configuration table
3. **Optimize Reload Interval**: Balance between freshness and performance
4. **Consider Caching**: Implement additional caching for frequently accessed values

## 🛡️ Security Best Practices

- Store sensitive connection strings in secure stores (Azure Key Vault, etc.)
- Use principle of least privilege for database access
- Consider encrypting sensitive JSON values
- Validate and sanitize configuration inputs

## 📦 NuGet Package

This project is packaged as a NuGet package and outputs to the `./nupkg` directory when built. The package includes:

- Main library assemblies
- Documentation files
- Package icon and metadata
- License information

**Current Version**: 9.0.3  
**Target Framework**: .NET 9.0  
**Package Output**: `./nupkg/Mma.Configuration.9.0.3.nupkg`

## 🔧 Building

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Create NuGet package (automatically done on build)
dotnet pack
```

The NuGet package will be generated in the `nupkg` folder.

## 📚 Documentation

For comprehensive documentation, examples, and advanced usage scenarios, see the [detailed documentation](docs/README.md).

## 🐛 Troubleshooting

### Common Issues:

1. **Connection String Issues**: Verify server name, database name, and credentials
2. **JSON Parsing Errors**: Validate JSON syntax and check for encoding issues
3. **Environment Configuration Not Loading**: Verify environment names match database values
4. **Configuration Not Reloading**: Ensure `ReloadOnChange` is true and connection is active

### Debug Configuration:

```csharp
// Enable detailed logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// List all configuration values
foreach (var kvp in configuration.AsEnumerable())
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
```

## 📄 License

This project is licensed under the MIT License - see the LICENSE.txt file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## 📞 Support

For support, please:
1. Check the troubleshooting section
2. Review the comprehensive documentation in the `docs` folder
3. Search existing issues on the project repository
4. Create a new issue with detailed information

---

**Project Repository**: https://github.com/mma1979/mma-configuration  
**Package Manager**: Available on NuGet  
**Framework**: .NET 9.0
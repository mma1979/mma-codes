# SQL Server JSON Configuration Provider

A .NET configuration provider that loads application settings from a SQL Server table with JSON columns. This provider integrates seamlessly with the .NET configuration system and can be used alongside other configuration sources like `appsettings.json`, environment variables, and more.

## 🚀 Features

- **JSON Configuration Storage**: Store complex configuration objects as JSON in SQL Server
- **Environment-Specific Settings**: Support for environment-specific configurations with fallback to defaults
- **Automatic Reloading**: Optional automatic configuration reloading at specified intervals
- **JSON Flattening**: Automatically flattens JSON objects into .NET configuration format
- **Flexible Schema**: Customizable table and column names
- **Error Handling**: Graceful handling of JSON parsing errors with logging
- **Thread-Safe**: Safe for use in multi-threaded applications
- **Performance Optimized**: Efficient querying and caching

## 📦 Installation

```xml
<PackageReference Include="Mma.Configuration" Version="9.0.1" />
```

## 🏗️ Database Setup

### 1. Create the Configuration Table

```sql
CREATE TABLE AppSettings (
    Id int IDENTITY(1,1) PRIMARY KEY,
    [Key] nvarchar(255) NOT NULL,
    JsonValue nvarchar(max) NOT NULL,
    Environment nvarchar(50) NULL, -- NULL means applies to all environments
    CreatedAt datetime2 DEFAULT GETDATE(),
    UpdatedAt datetime2 DEFAULT GETDATE(),
    UNIQUE([Key], Environment)
);

-- Create index for better performance
CREATE INDEX IX_AppSettings_Key_Environment ON AppSettings([Key], Environment);
```

### 2. Insert Sample Configuration Data

```sql
-- Connection strings for different environments
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('ConnectionStrings', '{
    "Database": "Server=prod-server;Database=MyApp;Integrated Security=true;TrustServerCertificate=true;",
    "Redis": "prod-redis:6379",
    "ServiceBus": "Endpoint=sb://prod-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=..."
}', 'Production');

INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('ConnectionStrings', '{
    "Database": "Server=dev-server;Database=MyApp_Dev;Integrated Security=true;TrustServerCertificate=true;",
    "Redis": "localhost:6379",
    "ServiceBus": "UseDevelopmentStorage=true"
}', 'Development');

-- Logging configuration (applies to all environments)
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('Logging', '{
    "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
    },
    "Console": {
        "IncludeScopes": true
    }
}', NULL);

-- API settings with different values per environment
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('ApiSettings', '{
    "BaseUrl": "https://api.example.com",
    "Timeout": 30,
    "RetryCount": 3,
    "Features": {
        "EnableCaching": true,
        "EnableRateLimiting": true,
        "MaxConcurrentRequests": 100
    }
}', 'Production');

INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('ApiSettings', '{
    "BaseUrl": "https://dev-api.example.com",
    "Timeout": 60,
    "RetryCount": 5,
    "Features": {
        "EnableCaching": false,
        "EnableRateLimiting": false,
        "MaxConcurrentRequests": 10
    }
}', 'Development');

-- JWT settings
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('JwtSettings', '{
    "SecretKey": "your-super-secret-key-here-at-least-32-characters",
    "Issuer": "MyApp",
    "Audience": "MyApp-Users",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
}', NULL);

-- Email configuration
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('EmailSettings', '{
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "FromName": "MyApp Notifications",
    "Templates": {
        "Welcome": "welcome-template.html",
        "PasswordReset": "password-reset-template.html"
    }
}', NULL);
```

## 🔧 Basic Usage

### 1. Simple Configuration

```csharp
using Configuration.SqlServer;

var configuration = new ConfigurationBuilder()
    .AddSqlServerJson(
        connectionString: "Server=localhost;Database=MyApp;Integrated Security=true;TrustServerCertificate=true;")
    .Build();

// Access configuration values
var dbConnectionString = configuration.GetConnectionString("Database");
var logLevel = configuration["Logging:LogLevel:Default"];
```

### 2. ASP.NET Core Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add SQL Server configuration early in the pipeline
builder.Configuration.AddSqlServerJson(
    connectionString: builder.Configuration.GetConnectionString("ConfigDatabase")!,
    environment: builder.Environment.EnvironmentName,
    environmentColumn: "Environment",
    reloadOnChange: true,
    reloadInterval: TimeSpan.FromMinutes(5));

// Register services
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var app = builder.Build();
app.Run();
```

### 3. Advanced Configuration with Options

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
| `ReloadInterval` | `TimeSpan` | `TimeSpan.FromMinutes(5)` | Reload interval when ReloadOnChange is true |

## 🎯 Real-World Examples

### Example 1: E-commerce Application

```csharp
// Startup.cs or Program.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add SQL Server configuration
        var configBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration) // Include existing config
            .AddSqlServerJson(
                connectionString: configuration.GetConnectionString("ConfigDatabase")!,
                environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                environmentColumn: "Environment",
                reloadOnChange: true);
        
        var config = configBuilder.Build();
        
        // Configure strongly-typed options
        services.Configure<PaymentSettings>(config.GetSection("PaymentSettings"));
        services.Configure<ShippingSettings>(config.GetSection("ShippingSettings"));
        services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
        
        // Use in services
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IShippingService, ShippingService>();
    }
}

// Configuration models
public class PaymentSettings
{
    public string StripeSecretKey { get; set; } = string.Empty;
    public string StripePublishableKey { get; set; } = string.Empty;
    public PayPalSettings PayPal { get; set; } = new();
    public bool EnableCryptocurrency { get; set; }
}

public class PayPalSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox"; // sandbox or live
}

// Service usage
public class PaymentService : IPaymentService
{
    private readonly PaymentSettings _paymentSettings;

    public PaymentService(IOptions<PaymentSettings> paymentSettings)
    {
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // Use _paymentSettings.StripeSecretKey, etc.
        // Configuration automatically reloads if changed in database
    }
}
```

### Example 2: Microservices Configuration

```csharp
// Microservice startup
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Each microservice can have its own configuration key
        builder.Configuration.AddSqlServerJson(
            connectionString: builder.Configuration.GetConnectionString("SharedConfig")!,
            environment: builder.Environment.EnvironmentName,
            environmentColumn: "Environment");
        
        // Service-specific configuration
        builder.Services.Configure<ServiceSettings>(
            builder.Configuration.GetSection($"Services:{builder.Environment.ApplicationName}"));
        
        // Shared configuration
        builder.Services.Configure<DatabaseSettings>(
            builder.Configuration.GetSection("SharedDatabase"));
        
        var app = builder.Build();
        app.Run();
    }
}

// Database entries for microservices
/*
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('Services', '{
    "UserService": {
        "Port": 5001,
        "HealthCheckInterval": "00:01:00",
        "Features": {
            "EnableUserRegistration": true,
            "RequireEmailConfirmation": true
        }
    },
    "OrderService": {
        "Port": 5002,
        "HealthCheckInterval": "00:01:00",
        "Features": {
            "EnableOrderTracking": true,
            "AllowCancellations": true
        }
    }
}', 'Production');
*/
```

### Example 3: Feature Flags and A/B Testing

```csharp
// Feature flags configuration
/*
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('FeatureFlags', '{
    "NewCheckoutFlow": {
        "Enabled": true,
        "Percentage": 50,
        "AllowedUsers": ["admin@example.com", "beta@example.com"]
    },
    "RecommendationEngine": {
        "Enabled": true,
        "Percentage": 100,
        "MinimumUserAge": 18
    },
    "PremiumFeatures": {
        "Enabled": false,
        "Percentage": 0,
        "RequiredPlan": "Premium"
    }
}', 'Production');
*/

// Feature flag service
public class FeatureFlagService
{
    private readonly IConfiguration _configuration;
    
    public FeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public bool IsFeatureEnabled(string featureName, string? userId = null)
    {
        var section = _configuration.GetSection($"FeatureFlags:{featureName}");
        
        if (!section.GetValue<bool>("Enabled"))
            return false;
            
        var percentage = section.GetValue<int>("Percentage");
        if (percentage < 100 && userId != null)
        {
            // Simple hash-based percentage rollout
            var hash = userId.GetHashCode();
            var userPercentage = Math.Abs(hash % 100);
            return userPercentage < percentage;
        }
        
        return percentage > 0;
    }
}
```

### Example 4: Multi-Tenant Configuration

```csharp
// Multi-tenant configuration provider
public class TenantConfigurationProvider
{
    private readonly IConfiguration _configuration;
    
    public TenantConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public T GetTenantSettings<T>(string tenantId, string section) where T : new()
    {
        // Try tenant-specific first, then fall back to default
        var tenantSection = _configuration.GetSection($"Tenants:{tenantId}:{section}");
        var defaultSection = _configuration.GetSection($"Tenants:Default:{section}");
        
        var settings = new T();
        defaultSection.Bind(settings);
        tenantSection.Bind(settings); // Override with tenant-specific values
        
        return settings;
    }
}

// Database configuration for tenants
/*
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('Tenants', '{
    "Default": {
        "BrandingSettings": {
            "PrimaryColor": "#007bff",
            "SecondaryColor": "#6c757d",
            "LogoUrl": "/images/default-logo.png"
        },
        "LimitsSettings": {
            "MaxUsers": 100,
            "MaxStorageGB": 10,
            "MaxApiCallsPerHour": 1000
        }
    },
    "tenant-123": {
        "BrandingSettings": {
            "PrimaryColor": "#dc3545",
            "LogoUrl": "/images/tenant-123-logo.png"
        },
        "LimitsSettings": {
            "MaxUsers": 500,
            "MaxStorageGB": 100,
            "MaxApiCallsPerHour": 10000
        }
    }
}', 'Production');
*/
```

## 📊 JSON Flattening Examples

The provider automatically flattens JSON objects into the standard .NET configuration format:

### Input JSON:
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
    "Features": ["Feature1", "Feature2", "Feature3"],
    "IsEnabled": true
}
```

### Flattened Configuration Keys:
- `MySection:Database:ConnectionString`
- `MySection:Database:CommandTimeout`
- `MySection:Database:Retry:MaxAttempts`
- `MySection:Database:Retry:DelaySeconds`
- `MySection:Features:0` (Feature1)
- `MySection:Features:1` (Feature2)
- `MySection:Features:2` (Feature3)
- `MySection:IsEnabled`

## 🔄 Configuration Reloading

The provider supports automatic reloading of configuration when changes are detected:

```csharp
var configuration = new ConfigurationBuilder()
    .AddSqlServerJson(
        connectionString: connectionString,
        reloadOnChange: true,
        reloadInterval: TimeSpan.FromMinutes(5)) // Check for changes every 5 minutes
    .Build();

// Register for change notifications
ChangeToken.OnChange(
    () => configuration.GetReloadToken(),
    () => {
        Console.WriteLine("Configuration reloaded!");
        // Handle configuration changes
    });
```

## 🏃‍♂️ Performance Considerations

### 1. Connection Pooling
Ensure your connection string includes connection pooling settings:

```csharp
var connectionString = "Server=localhost;Database=MyApp;Integrated Security=true;Pooling=true;Max Pool Size=100;Min Pool Size=5;";
```

### 2. Indexing
Create appropriate indexes on your configuration table:

```sql
-- Composite index for key and environment lookups
CREATE INDEX IX_AppSettings_Key_Environment ON AppSettings([Key], Environment) 
INCLUDE (JsonValue);

-- Index for environment-specific queries
CREATE INDEX IX_AppSettings_Environment ON AppSettings(Environment) 
WHERE Environment IS NOT NULL;
```

### 3. Caching Strategy
Consider implementing a caching layer for frequently accessed configurations:

```csharp
public class CachedSqlServerConfigurationProvider : IConfigurationProvider
{
    private readonly IMemoryCache _cache;
    private readonly SqlServerJsonConfigurationProvider _innerProvider;
    
    // Implementation details...
}
```

## 🛡️ Security Best Practices

### 1. Secure Connection Strings
Store sensitive connection strings securely:

```csharp
// Use Azure Key Vault or similar for production
builder.Configuration.AddAzureKeyVault(
    vaultUri: "https://your-keyvault.vault.azure.net/",
    credential: new DefaultAzureCredential());

// Then reference in configuration
builder.Configuration.AddSqlServerJson(
    connectionString: builder.Configuration["KeyVault:ConfigDatabaseConnectionString"]!);
```

### 2. Encrypt Sensitive JSON Values
For highly sensitive data, consider encrypting JSON values:

```sql
-- Example with encrypted values
INSERT INTO AppSettings ([Key], JsonValue, Environment) VALUES 
('SecretSettings', 
 ENCRYPTBYPASSPHRASE('YourPassphrase', '{"ApiKey": "secret-api-key", "PrivateKey": "private-key-data"}'),
 'Production');
```

### 3. Principle of Least Privilege
Grant minimal required permissions to the configuration database user:

```sql
-- Create dedicated user for configuration access
CREATE USER [ConfigReader] WITHOUT LOGIN;
GRANT SELECT ON AppSettings TO [ConfigReader];
-- Do not grant INSERT, UPDATE, DELETE unless necessary
```

## 🐛 Troubleshooting

### Common Issues:

1. **Connection String Issues**
   ```
   Error: A network-related or instance-specific error occurred
   ```
   - Verify server name, database name, and credentials
   - Ensure SQL Server is running and accessible
   - Check firewall settings

2. **JSON Parsing Errors**
   ```
   Warning: Failed to parse JSON for key 'MyKey'
   ```
   - Validate JSON syntax using online JSON validators
   - Check for special characters or encoding issues
   - Ensure proper escaping of quotes

3. **Environment Configuration Not Loading**
   - Verify `EnvironmentColumn` and `Environment` settings match database values
   - Check that environment names are case-sensitive matches
   - Ensure NULL environment records exist for fallback values

4. **Configuration Not Reloading**
   - Verify `ReloadOnChange` is set to `true`
   - Check that `ReloadInterval` is appropriate for your needs
   - Ensure database connection remains active

### Debug Configuration Loading:

```csharp
// Enable detailed logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add debug information
var config = builder.Configuration;
foreach (var kvp in config.AsEnumerable())
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
```

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📞 Support

If you encounter any issues or have questions, please:

1. Check the troubleshooting section above
2. Search existing GitHub issues
3. Create a new issue with detailed information about your problem

## 🔮 Roadmap

- [ ] Support for Azure SQL Database with managed identity
- [ ] Configuration validation and schema enforcement  
- [ ] Bulk configuration update APIs
- [ ] Configuration versioning and rollback
- [ ] Integration with popular caching solutions (Redis, etc.)
- [ ] Configuration change audit logging
- [ ] Support for configuration encryption at rest
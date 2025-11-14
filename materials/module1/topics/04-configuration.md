# Configuration & Secrets Management

## Configuration in Aspire

Aspire follows standard .NET configuration patterns but adds special features for distributed applications.

## Configuration Hierarchy

Configuration sources are applied in this order (later sources override earlier):

1. **appsettings.json** - Base configuration
2. **appsettings.{Environment}.json** - Environment-specific
3. **User Secrets** - Development secrets (never commit!)
4. **Environment Variables** - Runtime configuration
5. **Command Line Arguments** - Highest priority

```
Command Line  (highest priority)
     ↓
Environment Variables
     ↓
User Secrets
     ↓
appsettings.Development.json
     ↓
appsettings.json  (lowest priority)
```

## User Secrets (Development)

For sensitive data during development.

### Initialize User Secrets

```bash
cd MyApp.Api
dotnet user-secrets init
```

This adds a UserSecretsId to your .csproj:

```xml
<PropertyGroup>
  <UserSecretsId>abc123...</UserSecretsId>
</PropertyGroup>
```

### Set Secrets

```bash
# Connection string
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost;..."

# API key
dotnet user-secrets set "ExternalApi:ApiKey" "secret-key-here"

# Multiple values
dotnet user-secrets set "Email:Host" "smtp.example.com"
dotnet user-secrets set "Email:Port" "587"
```

### List Secrets

```bash
dotnet user-secrets list
```

### Remove Secrets

```bash
dotnet user-secrets remove "ConnectionStrings:Database"
dotnet user-secrets clear  # Remove all
```

### Access in Code

```csharp
var connectionString = builder.Configuration["ConnectionStrings:Database"];
var apiKey = builder.Configuration["ExternalApi:ApiKey"];
```

**Important:** User secrets are **only for development**. They're stored locally and never deployed.

## Environment Variables

Set configuration via environment variables.

### In AppHost

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("MaxRetries", "3")
    .WithEnvironment("LogLevel", "Debug")
    .WithEnvironment("FeatureFlags__NewUI", "true");
```

### Double Underscore for Nested Config

```csharp
// Sets FeatureFlags:NewUI in configuration
.WithEnvironment("FeatureFlags__NewUI", "true")

// Sets ConnectionStrings:Database
.WithEnvironment("ConnectionStrings__Database", connectionString)
```

### From System Environment

```bash
# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Production
export MaxRetries=5

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:MaxRetries = "5"
```

## Parameter Resources

For values that need to be provided when the AppHost runs.

### Define Parameters

```csharp
// Simple parameter
var apiKey = builder.AddParameter("api-key");

// Secret parameter (masked in dashboard)
var password = builder.AddParameter("db-password", secret: true);

// With default value
var maxRetries = builder.AddParameter("max-retries", defaultValue: "3");
```

### Use Parameters

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("ExternalApi:ApiKey", apiKey)
    .WithEnvironment("MaxRetries", maxRetries);
```

### Provide Values

**Option 1: Interactive Prompt**
When you run the AppHost, you'll be prompted:
```
Enter value for 'api-key': _
```

**Option 2: Configuration File**

In `appsettings.json`:
```json
{
  "Parameters": {
    "api-key": "your-key-here",
    "max-retries": "5"
  }
}
```

**Option 3: User Secrets**

```bash
dotnet user-secrets set "Parameters:api-key" "your-secret-key"
```

**Option 4: Environment Variables**

```bash
export Parameters__api-key="your-key"
```

## Connection Strings

### Automatic Injection with WithReference

```csharp
// In AppHost
var db = builder.AddPostgres("postgres").AddDatabase("mydb");
var cache = builder.AddRedis("cache");

var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(db)     // Injects ConnectionStrings__mydb
    .WithReference(cache); // Injects ConnectionStrings__cache
```

### Access in Service

```csharp
// Automatically available
var dbConnection = builder.Configuration.GetConnectionString("mydb");
var cacheConnection = builder.Configuration.GetConnectionString("cache");

// Or use Aspire components (recommended)
builder.AddNpgsqlDbContext<MyDbContext>("mydb");
builder.AddRedisClient("cache");
```

### Manual Connection Strings

If not using Aspire components:

```csharp
// In AppHost
var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment(
        "ConnectionStrings__mydb",
        "Server=localhost;Database=mydb;...");
```

## Configuration Sections

### Strongly-Typed Configuration

**appsettings.json:**
```json
{
  "EmailSettings": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "From": "noreply@example.com"
  }
}
```

**Configuration class:**
```csharp
public class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string From { get; set; } = "";
}
```

**Register and use:**
```csharp
// Register
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Use via dependency injection
public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

## Best Practices

### 1. Never Commit Secrets

```csharp
// ❌ Bad - secret in code
var apiKey = "secret-key-12345";

// ✅ Good - from configuration
var apiKey = builder.Configuration["ExternalApi:ApiKey"];
```

### 2. Use User Secrets for Development

```bash
# ✅ Good - secrets not in source control
dotnet user-secrets set "ApiKey" "dev-key"
```

### 3. Use Parameters for Deploy-Time Values

```csharp
// ✅ Good - provided at deployment
var adminEmail = builder.AddParameter("admin-email");
```

### 4. Use WithReference for Aspire Resources

```csharp
// ✅ Good - automatic connection string injection
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache)
    .WithReference(database);

// ❌ Bad - manual connection string
.WithEnvironment("ConnectionStrings__cache", "localhost:6379")
```

### 5. Environment-Specific Settings

```csharp
if (builder.Environment.IsDevelopment())
{
    // Development settings
    service.WithEnvironment("DetailedErrors", "true");
}
else
{
    // Production settings
    service.WithEnvironment("DetailedErrors", "false");
}
```

## Configuration Examples

### Example 1: External API Configuration

```csharp
// AppHost
var apiKey = builder.AddParameter("weather-api-key", secret: true);

var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("WeatherApi:BaseUrl", "https://api.weather.com")
    .WithEnvironment("WeatherApi:ApiKey", apiKey)
    .WithEnvironment("WeatherApi:Timeout", "30");
```

### Example 2: Feature Flags

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("FeatureFlags__NewUI", "true")
    .WithEnvironment("FeatureFlags__BetaFeatures", "false");
```

### Example 3: Database Configuration

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "myapp")
    .WithEnvironment("POSTGRES_USER", "appuser");

var db = postgres.AddDatabase("mydb");

var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(db);
```

## Quick Reference

### Setting Configuration

| Method | Use Case | Example |
|--------|----------|---------|
| `appsettings.json` | Non-sensitive defaults | `{ "PageSize": 20 }` |
| User Secrets | Development secrets | `dotnet user-secrets set "ApiKey" "..."` |
| Parameters | Deploy-time values | `builder.AddParameter("admin-email")` |
| WithEnvironment | Service-specific config | `.WithEnvironment("MaxRetries", "3")` |
| WithReference | Resource connections | `.WithReference(database)` |

### Reading Configuration

```csharp
// Simple value
var value = builder.Configuration["Key"];

// Nested value
var value = builder.Configuration["Section:SubSection:Key"];

// Connection string
var conn = builder.Configuration.GetConnectionString("mydb");

// Strongly-typed section
var settings = builder.Configuration
    .GetSection("EmailSettings")
    .Get<EmailSettings>();
```

## Next Steps

- **Next Topic:** [Dashboard](./05-dashboard.md)
- **Try Example:** [Configuration Example](../examples/04-database/)

## Official Documentation

- [External Parameters](https://learn.microsoft.com/dotnet/aspire/fundamentals/external-parameters)
- [AppHost Configuration](https://learn.microsoft.com/dotnet/aspire/app-host/configuration)
- [.NET Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)

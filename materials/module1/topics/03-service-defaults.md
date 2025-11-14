# ServiceDefaults

## What is ServiceDefaults?

**ServiceDefaults** is a shared library project that configures cross-cutting concerns for all services in your Aspire application. It ensures consistency and applies best practices automatically.

Think of it as a "template" that every service uses for common configuration.

## Why ServiceDefaults?

### Without ServiceDefaults

Every service needs to configure:
```csharp
// Repeat this in EVERY service:
builder.Services.AddOpenTelemetry()
    .WithTracing(...)
    .WithMetrics(...);

builder.Services.AddHealthChecks();
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(...);
// ... more boilerplate
```

**Problems:**
- Duplicated code in every service
- Easy to forget configuration
- Hard to update consistently
- Not DRY (Don't Repeat Yourself)

### With ServiceDefaults

```csharp
// In every service, just one line:
builder.AddServiceDefaults();
```

**Benefits:**
- ✅ One place to configure everything
- ✅ Consistent across all services
- ✅ Easy to update
- ✅ Best practices built-in

## Creating ServiceDefaults

### Using Templates

```bash
dotnet new aspire-servicedefaults -n MyApp.ServiceDefaults
```

### Project Structure

```
MyApp.ServiceDefaults/
├── Extensions.cs                    # Main configuration
└── MyApp.ServiceDefaults.csproj    # Package references
```

### Key Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
  <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
</ItemGroup>
```

## What ServiceDefaults Configures

### 1. OpenTelemetry (Observability)

Automatic instrumentation for:
- **Traces** - Distributed tracing across services
- **Metrics** - Performance and custom metrics
- **Logs** - Structured logging with correlation

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
    });
```

**What you get:**
- Every HTTP request is automatically traced
- Database calls are traced
- Performance metrics collected
- All visible in Aspire Dashboard

### 2. Health Checks

Standard health check endpoints:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
```

**Endpoints:**
- `/health` - Overall health status
- `/alive` - Liveness check (is the service running?)

### 3. Service Discovery

Services can find each other by name:

```csharp
builder.Services.AddServiceDiscovery();
```

**Usage:**
```csharp
// In your service
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    // "api" resolves to actual endpoint automatically
    client.BaseAddress = new Uri("http://api");
});
```

### 4. Resilience (Polly Policies)

Automatic retry, circuit breaker, and timeout:

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});
```

**Resilience patterns:**
- **Retry** - Retry transient failures
- **Circuit Breaker** - Prevent cascading failures
- **Timeout** - Prevent hanging requests
- **Bulkhead** - Limit concurrent requests

## ServiceDefaults Code

### Complete Extensions.cs

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Resilience (retry, circuit breaker, timeout)
            http.AddStandardResilienceHandler();
            
            // Service discovery
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(
        this IHostApplicationBuilder builder)
    {
        // Logging
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // Metrics and Tracing
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(
        this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry()
                .UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(
        this WebApplication app)
    {
        // Health check endpoints
        app.MapHealthChecks("/health");
        
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
```

## Using ServiceDefaults

### In Your Services

1. **Add Reference**
```bash
cd MyApp.Api
dotnet add reference ../MyApp.ServiceDefaults
```

2. **Add to Program.cs**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults - must be first!
builder.AddServiceDefaults();

// Your service configuration
builder.Services.AddControllers();
// ...

var app = builder.Build();

// Map health check endpoints
app.MapDefaultEndpoints();

// Your endpoints
app.MapControllers();

app.Run();
```

### Order Matters!

```csharp
// ✅ Correct order
builder.AddServiceDefaults();  // FIRST
builder.Services.AddControllers();
// ... other services

// ❌ Wrong - ServiceDefaults should be first
builder.Services.AddControllers();
builder.AddServiceDefaults();  // Too late!
```

## Customizing ServiceDefaults

### Add Your Own Configuration

```csharp
public static class MyExtensions
{
    public static IHostApplicationBuilder AddMyServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        // Standard Aspire defaults
        builder.AddServiceDefaults();

        // Your custom configuration
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        
        // Your logging configuration
        builder.Logging.AddConsole();
        
        return builder;
    }
}
```

### Environment-Specific Configuration

```csharp
public static IHostApplicationBuilder AddServiceDefaults(
    this IHostApplicationBuilder builder)
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();

    // Development-specific
    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddDebug();
    }

    // Production-specific
    if (builder.Environment.IsProduction())
    {
        // Add production monitoring
        builder.Services.AddApplicationInsightsTelemetry();
    }

    return builder;
}
```

## What You Get Automatically

### Traces

Every HTTP request automatically creates a trace:

```
Request: GET /api/users/123
  ├─ HTTP GET /api/users/123 (MyApi)
  │  ├─ Database Query: SELECT * FROM Users WHERE Id = 123
  │  └─ Cache Check: Redis GET user:123
  └─ Response: 200 OK (45ms)
```

### Metrics

Built-in metrics collected:
- HTTP request duration
- HTTP request count
- HTTP request size
- .NET runtime metrics (memory, GC, threads)
- CPU usage

### Logs

Structured logging with correlation:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Processing user request",
  "traceId": "abc123",
  "spanId": "def456",
  "service": "MyApi",
  "userId": "123"
}
```

## Health Checks

### Default Endpoints

```bash
# Overall health
curl http://localhost:5000/health

# Liveness (is service running?)
curl http://localhost:5000/alive
```

### Adding Custom Health Checks

```csharp
// In your service
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddRedis(connectionString, "redis")
    .AddCheck("external-api", () =>
    {
        // Check external API
        return HealthCheckResult.Healthy();
    });
```

## Service Discovery

### How It Works

1. **In AppHost:**
```csharp
var api = builder.AddProject<Projects.MyApi>("api");
var web = builder.AddProject<Projects.MyWeb>("web")
    .WithReference(api);
```

2. **In Web Service:**
```csharp
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // "api" automatically resolves to http://localhost:5001
    client.BaseAddress = new Uri("http://api");
});
```

3. **Magic!** ServiceDefaults handles resolution

### No Hardcoded URLs!

```csharp
// ❌ Bad - hardcoded
client.BaseAddress = new Uri("http://localhost:5001");

// ✅ Good - service discovery
client.BaseAddress = new Uri("http://api");
```

## Resilience Patterns

### Automatic Retry

Transient failures are retried automatically:

```csharp
// This automatically retries on transient failures
var response = await httpClient.GetAsync("http://api/users");
```

**Default behavior:**
- Retries: 3 attempts
- Backoff: Exponential (100ms, 200ms, 400ms)
- Jitter: Random delay to prevent thundering herd

### Circuit Breaker

Prevents cascading failures:

```
┌─────────────────────────────────────┐
│ Calls failing?                       │
│ Open circuit → Stop calling          │
│ Wait → Try again → Success?          │
│ Close circuit → Resume normal calls  │
└─────────────────────────────────────┘
```

### Timeout

Prevents hanging requests:

```csharp
// Automatically times out after 30 seconds
var response = await httpClient.GetAsync("http://slow-api");
```

## Best Practices

### 1. Always Use ServiceDefaults

```csharp
// In EVERY service
builder.AddServiceDefaults();
```

### 2. Don't Duplicate Configuration

```csharp
// ❌ Bad - duplicating OpenTelemetry setup
builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()...  // Already done!

// ✅ Good - customize in ServiceDefaults project
```

### 3. Map Health Endpoints

```csharp
// Always add these
app.MapDefaultEndpoints();
```

### 4. Reference ServiceDefaults, Not Individual Packages

```csharp
// ❌ Bad - adding packages directly
<PackageReference Include="OpenTelemetry..." />

// ✅ Good - reference ServiceDefaults
<ProjectReference Include="../MyApp.ServiceDefaults" />
```

## Example: Complete Service with ServiceDefaults

```csharp
// Program.cs
using MyApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults - one line for all the magic!
builder.AddServiceDefaults();

// Your service-specific configuration
builder.Services.AddControllers();
builder.AddNpgsqlDbContext<MyDbContext>("database");
builder.AddRedisClient("cache");

var app = builder.Build();

// Map health check endpoints
app.MapDefaultEndpoints();

// Your endpoints
app.MapControllers();

app.Run();
```

**That's it!** You now have:
- ✅ OpenTelemetry traces and metrics
- ✅ Health checks
- ✅ Service discovery
- ✅ Resilience patterns
- ✅ All best practices

## Next Steps

- **Next Topic:** [Configuration & Secrets](./04-configuration.md)
- **Try Example:** [Multi-Service with ServiceDefaults](../examples/02-multi-service/)

## Official Documentation

- [Service Defaults](https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults)
- [OpenTelemetry in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/telemetry)
- [Health Checks](https://learn.microsoft.com/dotnet/aspire/fundamentals/health-checks)

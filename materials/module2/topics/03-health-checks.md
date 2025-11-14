# Health Checks

## What Are Health Checks?

**Health checks** are endpoints that report whether your application is ready to serve traffic and operating correctly. They're essential for:

- **Load balancers** - Know which instances can receive traffic
- **Orchestrators** - Decide when to restart containers
- **Monitoring** - Alert when services are unhealthy
- **Deployment** - Wait for readiness before switching traffic

## Readiness vs Liveness

### Liveness Probe
**Question:** "Is the app running?"

**Purpose:** Detect if the app is stuck and needs restarting

**Example failures:**
- Deadlock
- Infinite loop
- Complete crash

**Action on failure:** Restart the container

### Readiness Probe
**Question:** "Is the app ready to handle requests?"

**Purpose:** Detect if the app can't process traffic temporarily

**Example failures:**
- Database connection lost
- External dependency unavailable
- Starting up / warming up

**Action on failure:** Stop sending traffic (but don't restart)

## Built-in Health Checks in Aspire

ServiceDefaults adds a basic health check automatically:

```csharp
// In ServiceDefaults/Extensions.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
```

**Endpoints added:**
- `/health` - Overall health (readiness)
- `/alive` - Liveness check

### Usage

```bash
# Check overall health
curl http://localhost:5000/health

# Check liveness
curl http://localhost:5000/alive
```

**Response:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123",
  "entries": {
    "self": {
      "status": "Healthy",
      "duration": "00:00:00.0012"
    }
  }
}
```

## Adding Custom Health Checks

### Database Health Check

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("database")!,
        name: "database",
        tags: new[] { "db", "sql", "postgres" });
```

### Redis Health Check

```csharp
builder.Services.AddHealthChecks()
    .AddRedis(
        builder.Configuration.GetConnectionString("cache")!,
        name: "cache",
        tags: new[] { "cache", "redis" });
```

### HTTP Endpoint Health Check

```csharp
builder.Services.AddHealthChecks()
    .AddUrlGroup(
        new Uri("https://api.external.com/health"),
        name: "external-api",
        tags: new[] { "external" });
```

### Custom Health Check Class

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly MyDbContext _dbContext;

    public DatabaseHealthCheck(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try a simple query
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            
            // Check row count (optional)
            var orderCount = await _dbContext.Orders.CountAsync(cancellationToken);
            
            return HealthCheckResult.Healthy(
                $"Database is healthy. Orders: {orderCount}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed",
                exception: ex);
        }
    }
}

// Register it
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db" });
```

## Health Check States

### Healthy
Everything is working correctly.

```csharp
return HealthCheckResult.Healthy("All systems operational");
```

### Degraded
Service is working but not optimally.

```csharp
if (cacheAvailable)
{
    return HealthCheckResult.Healthy();
}
else
{
    return HealthCheckResult.Degraded("Cache unavailable, using database");
}
```

### Unhealthy
Service cannot function properly.

```csharp
return HealthCheckResult.Unhealthy("Database connection failed");
```

## Filtering Health Checks

### By Tags

```csharp
// Liveness - just check if app is alive
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Readiness - check dependencies
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// All checks
app.MapHealthChecks("/health");
```

### Register with Tags

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddNpgSql(connectionString, tags: new[] { "ready", "db" })
    .AddRedis(cacheConnection, tags: new[] { "ready", "cache" });
```

## Health Check UI

View health status in a web UI:

```bash
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage
```

```csharp
// Configure health checks UI
builder.Services
    .AddHealthChecksUI(setup =>
    {
        setup.AddHealthCheckEndpoint("api", "http://localhost:5001/health");
        setup.AddHealthCheckEndpoint("web", "http://localhost:5000/health");
    })
    .AddInMemoryStorage();

// Add UI endpoint
app.MapHealthChecksUI(options => options.UIPath = "/health-ui");
```

Access at: `http://localhost:5000/health-ui`

## Health Checks in Aspire Dashboard

The Aspire Dashboard shows health check status in the **Resources** tab:

- ✅ **Green** - Healthy
- ⚠️ **Yellow** - Degraded
- ❌ **Red** - Unhealthy

Click a resource to see detailed health check results.

## Best Practices

### 1. Don't Check Too Much in Liveness

```csharp
// ✅ Good - simple liveness
.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })

// ❌ Bad - liveness checking dependencies
.AddNpgSql(connectionString, tags: new[] { "live" })  // Don't do this!
```

**Why?** If DB is down, liveness fails → container restarts → still can't connect → restart loop!

### 2. Check Critical Dependencies in Readiness

```csharp
// ✅ Good - readiness checks dependencies
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddNpgSql(connectionString, tags: new[] { "ready" })
    .AddRedis(cacheConnection, tags: new[] { "ready" });
```

### 3. Use Timeouts

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        timeout: TimeSpan.FromSeconds(3),  // Don't wait forever
        tags: new[] { "ready" });
```

### 4. Return Useful Information

```csharp
// ✅ Good - helpful data
return HealthCheckResult.Healthy($"Database responsive. Latency: {latency}ms");

// ❌ Bad - no context
return HealthCheckResult.Healthy();
```

### 5. Consider Degraded State

```csharp
public async Task<HealthCheckResult> CheckHealthAsync(...)
{
    var primaryDbAvailable = await CheckPrimaryDatabase();
    var replicaDbAvailable = await CheckReplicaDatabase();

    if (primaryDbAvailable)
    {
        return HealthCheckResult.Healthy("Primary database available");
    }
    else if (replicaDbAvailable)
    {
        return HealthCheckResult.Degraded("Using replica, primary unavailable");
    }
    else
    {
        return HealthCheckResult.Unhealthy("No database available");
    }
}
```

## Complete Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add health checks
builder.Services.AddHealthChecks()
    // Liveness - just check if alive
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    
    // Readiness - check dependencies
    .AddNpgSql(
        builder.Configuration.GetConnectionString("database")!,
        name: "database",
        tags: new[] { "ready", "db" })
    .AddRedis(
        builder.Configuration.GetConnectionString("cache")!,
        name: "cache",
        tags: new[] { "ready", "cache" })
    .AddUrlGroup(
        new Uri("https://api.payment.com/health"),
        name: "payment-api",
        timeout: TimeSpan.FromSeconds(3),
        tags: new[] { "ready", "external" });

var app = builder.Build();

// Map health check endpoints
app.MapHealthChecks("/health");  // All checks (readiness)
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

## Kubernetes/Container Apps Configuration

```yaml
livenessProbe:
  httpGet:
    path: /alive
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

## Next Steps

- **Next Topic:** [Deployment Manifests](./04-deployment-manifests.md)
- **Try Example:** [Health Checks Deep Dive](../examples/02-health-checks/)
- **Official Docs:** [Health Checks in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/health-checks)

## Further Reading

- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [Health Checks Library](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)

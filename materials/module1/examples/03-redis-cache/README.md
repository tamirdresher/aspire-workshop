# Redis Cache Example

## What This Example Shows

Adding infrastructure components (Redis) to an Aspire application:
- **API Service** - Uses Redis for caching
- **Redis Container** - Automatically started by Aspire
- **ServiceDefaults** - OpenTelemetry integration

Demonstrates how Aspire orchestrates both services and infrastructure.

## Running the Example

```bash
cd RedisCache.AppHost
dotnet run
```

Requirements:
- Docker Desktop or Podman running
- .NET 8 SDK with Aspire workload

## What You'll See

1. **Dashboard opens** - Resources tab shows API and Redis
2. **Redis starts automatically** - No manual docker commands needed
3. **API uses cache** - Check logs to see cache hits/misses
4. **Traces** - See Redis operations in distributed traces

## Architecture

```
┌─────────────┐
│  API (5001) │─────┐
└─────────────┘     │
                    │ Connection string injected
                    ↓
             ┌──────────────┐
             │ Redis (6379) │
             │  (Container) │
             └──────────────┘
```

## Project Structure

```
03-redis-cache/
├── RedisCache.AppHost/          # Orchestrator
│   ├── Program.cs               # Defines API + Redis
│   └── RedisCache.AppHost.csproj
├── RedisCache.Api/              # API service
│   ├── Program.cs               # Uses Redis client
│   ├── WeatherController.cs     # Cached endpoints
│   └── RedisCache.Api.csproj
└── RedisCache.ServiceDefaults/  # Shared config
    └── Extensions.cs
```

## Key Concepts Demonstrated

1. **Adding Infrastructure** - `AddRedis()` starts Redis container
2. **WithReference** - Injects connection string automatically
3. **Aspire Components** - `AddRedisClient()` configures client
4. **No Manual Config** - Connection strings handled by Aspire
5. **Observability** - Redis operations appear in traces

## How It Works

### AppHost (Orchestrator)

```csharp
// Define Redis
var cache = builder.AddRedis("cache");

// Reference it in API
var api = builder.AddProject<Projects.RedisCache_Api>("api")
    .WithReference(cache);  // Connection string injected
```

### API Service

```csharp
// ServiceDefaults enables service discovery
builder.AddServiceDefaults();

// Add Redis client - connection string from AppHost
builder.AddRedisClient("cache");

// Use in code
app.MapGet("/weather", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    
    // Try cache first
    var cached = await db.StringGetAsync("weather");
    if (cached.HasValue)
    {
        return Results.Ok(cached.ToString());
    }
    
    // Cache miss - generate and cache
    var weather = GenerateWeather();
    await db.StringSetAsync("weather", weather, TimeSpan.FromSeconds(30));
    return Results.Ok(weather);
});
```

## Try It

1. **Run the app**
   ```bash
   dotnet run --project RedisCache.AppHost
   ```

2. **Call the API**
   - Open dashboard, click API endpoint
   - Go to `/weather`
   - Call again - should be cached!

3. **Watch in Dashboard**
   - **Logs tab** - See "Cache hit" or "Cache miss" logs
   - **Traces tab** - See Redis GET/SET operations
   - **Resources tab** - Redis container status

## Experiment

### Change Cache Duration

In `WeatherController.cs`:
```csharp
await db.StringSetAsync("weather", weather, TimeSpan.FromSeconds(10));  // Shorter cache
```

### Add More Endpoints

```csharp
app.MapGet("/products/{id}", async (int id, IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var key = $"product:{id}";
    
    var cached = await db.StringGetAsync(key);
    if (cached.HasValue)
    {
        return Results.Ok(JsonSerializer.Deserialize<Product>(cached));
    }
    
    var product = await FetchProduct(id);
    await db.StringSetAsync(key, JsonSerializer.Serialize(product));
    return Results.Ok(product);
});
```

### Use Data Volumes

In AppHost, persist Redis data:
```csharp
var cache = builder.AddRedis("cache")
    .WithDataVolume();  // Data persists between runs
```

## Common Issues

### Redis Container Won't Start

**Check:**
- Docker is running: `docker ps`
- Port 6379 not in use: `netstat -an | grep 6379`
- No other Redis instances running

### Connection Errors

**Check:**
1. Dashboard Resources tab - is Redis running?
2. API logs - connection string correct?
3. ServiceDefaults added in API?

## Next Steps

- **Add Database:** [Database Example](../04-database/)
- **Learn More:** [Configuration Topic](../../topics/04-configuration.md)
- **Official Docs:** [Redis Integration](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-integration)

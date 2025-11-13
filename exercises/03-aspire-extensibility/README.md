# Exercise 3: Aspire Extensibility

## Overview

In this exercise, you'll learn how to extend .NET Aspire with custom components, integrations, and resources to enhance your application's capabilities.

## Learning Objectives

By the end of this exercise, you will be able to:
- Add Aspire component integrations (Redis, PostgreSQL, etc.)
- Create custom resources for your AppHost
- Build custom health checks
- Extend the Aspire Dashboard with custom metrics
- Implement custom telemetry and logging

## Prerequisites

- Completed Exercise 1: System Topology
- Docker Desktop installed and running (for Redis, PostgreSQL containers)

## Steps

### Part 1: Adding Redis Cache Integration

#### Step 1: Install Aspire Redis Component

Add the Aspire Redis hosting package to your AppHost:

```bash
cd ECommerce.AppHost
dotnet add package Aspire.Hosting.Redis
```

Add the Aspire Redis client to your API project:

```bash
cd ../ECommerce.Api
dotnet add package Aspire.StackExchange.Redis
```

#### Step 2: Add Redis to AppHost

Update `ECommerce.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var redis = builder.AddRedis("cache");

// Add the API project with Redis reference
var api = builder.AddProject<Projects.ECommerce_Api>("api")
    .WithReference(redis);

// Add the Web project
builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

#### Step 3: Use Redis in the API

Update `ECommerce.Api/Program.cs`:

```csharp
using ECommerce.Api.Services;
using ECommerce.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Redis distributed cache
builder.AddRedisClient("cache");

// Add services
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<OrderService>();

// ... rest of configuration
```

Create a cached product service:

```csharp
// ECommerce.Api/Services/CachedProductService.cs
using StackExchange.Redis;
using System.Text.Json;
using ECommerce.Shared.Models;

namespace ECommerce.Api.Services;

public class CachedProductService
{
    private readonly ProductService _productService;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CachedProductService> _logger;

    public CachedProductService(
        ProductService productService,
        IConnectionMultiplexer redis,
        ILogger<CachedProductService> logger)
    {
        _productService = productService;
        _redis = redis;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        var db = _redis.GetDatabase();
        var cacheKey = "products:all";

        // Try to get from cache
        var cachedData = await db.StringGetAsync(cacheKey);
        if (!cachedData.IsNullOrEmpty)
        {
            _logger.LogInformation("Products retrieved from cache");
            return JsonSerializer.Deserialize<List<Product>>(cachedData!)!;
        }

        // Get from service
        var products = await _productService.GetAllProductsAsync();

        // Store in cache
        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(products),
            TimeSpan.FromMinutes(5));

        _logger.LogInformation("Products cached for 5 minutes");
        return products;
    }
}
```

### Part 2: Adding PostgreSQL Database

#### Step 1: Install Aspire PostgreSQL Component

```bash
cd ../ECommerce.AppHost
dotnet add package Aspire.Hosting.PostgreSQL
```

#### Step 2: Add PostgreSQL to AppHost

Update `ECommerce.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();  // Adds PgAdmin for database management

var catalogDb = postgres.AddDatabase("catalogdb");

// Add Redis cache
var redis = builder.AddRedis("cache");

// Add the API project with database and cache references
var api = builder.AddProject<Projects.ECommerce_Api>("api")
    .WithReference(catalogDb)
    .WithReference(redis);

// Add the Web project
builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

#### Step 3: Add Entity Framework Support

Install EF Core packages:

```bash
cd ../ECommerce.Api
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Create a DbContext:

```csharp
// ECommerce.Api/Data/ECommerceDbContext.cs
using Microsoft.EntityFrameworkCore;
using ECommerce.Shared.Models;

namespace ECommerce.Api.Data;

public class ECommerceDbContext : DbContext
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        // Configure Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.OwnsMany(e => e.Items);
        });
    }
}
```

Update Program.cs to use DbContext:

```csharp
// Add database context
builder.AddNpgsqlDbContext<ECommerceDbContext>("catalogdb");
```

### Part 3: Creating Custom Health Checks

Create a custom health check:

```csharp
// ECommerce.Api/HealthChecks/ProductServiceHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ECommerce.Api.Services;

namespace ECommerce.Api.HealthChecks;

public class ProductServiceHealthCheck : IHealthCheck
{
    private readonly ProductService _productService;

    public ProductServiceHealthCheck(ProductService productService)
    {
        _productService = productService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await _productService.GetAllProductsAsync();
            
            if (products.Count == 0)
            {
                return HealthCheckResult.Degraded("No products available");
            }

            return HealthCheckResult.Healthy($"{products.Count} products available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Product service is unavailable", ex);
        }
    }
}
```

Register the health check:

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<ProductServiceHealthCheck>("product_service");
```

### Part 4: Custom Metrics

Add custom metrics using OpenTelemetry:

```csharp
// ECommerce.Api/Metrics/OrderMetrics.cs
using System.Diagnostics.Metrics;

namespace ECommerce.Api.Metrics;

public class OrderMetrics
{
    private readonly Counter<int> _ordersCreated;
    private readonly Histogram<double> _orderValue;
    
    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("ECommerce.Api");
        _ordersCreated = meter.CreateCounter<int>("orders.created", "orders", "Number of orders created");
        _orderValue = meter.CreateHistogram<double>("order.value", "USD", "Order value in USD");
    }

    public void RecordOrderCreated(double value)
    {
        _ordersCreated.Add(1);
        _orderValue.Record(value);
    }
}
```

Register and use metrics:

```csharp
// In Program.cs
builder.Services.AddSingleton<OrderMetrics>();

// In order creation endpoint
app.MapPost("/api/orders", async (Order order, OrderService orderService, OrderMetrics metrics) =>
{
    var createdOrder = await orderService.CreateOrderAsync(order);
    metrics.RecordOrderCreated((double)createdOrder.TotalAmount);
    return Results.Created($"/api/orders/{createdOrder.Id}", createdOrder);
});
```

### Part 5: Custom Aspire Component

Create a reusable Aspire component:

```csharp
// Create a new project: ECommerce.Aspire.Hosting
// Add package reference: Aspire.Hosting

// CustomResource.cs
using Aspire.Hosting;

namespace ECommerce.Aspire.Hosting;

public static class CustomResourceExtensions
{
    public static IResourceBuilder<T> WithCustomHealthCheck<T>(
        this IResourceBuilder<T> builder,
        string endpoint = "/health")
        where T : IResource
    {
        return builder.WithAnnotation(new CustomHealthCheckAnnotation(endpoint));
    }
}

public class CustomHealthCheckAnnotation : IResourceAnnotation
{
    public string Endpoint { get; }

    public CustomHealthCheckAnnotation(string endpoint)
    {
        Endpoint = endpoint;
    }
}
```

### Part 6: Environment-Specific Configuration

Add different configurations for different environments:

```csharp
// ECommerce.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Use environment-specific resources
if (builder.Environment.IsDevelopment())
{
    // Use containers in development
    var postgres = builder.AddPostgres("postgres").WithPgAdmin();
    var redis = builder.AddRedis("cache");
}
else
{
    // Use managed services in production
    var postgres = builder.AddConnectionString("postgres");
    var redis = builder.AddConnectionString("cache");
}
```

## Running the Extended Application

1. Start Docker Desktop
2. Run the AppHost:
   ```bash
   cd ECommerce.AppHost
   dotnet run
   ```

3. Open the Aspire Dashboard
4. Observe the new resources (Redis, PostgreSQL, PgAdmin)
5. Check the Metrics tab for custom metrics
6. Verify health checks in the Resources tab

## Verification

1. **Redis Cache**: Check logs to see cache hits and misses
2. **PostgreSQL**: Access PgAdmin through the Dashboard and view tables
3. **Health Checks**: View health status in the Dashboard
4. **Custom Metrics**: See order metrics in the Metrics tab
5. **Distributed Tracing**: Verify traces include Redis and PostgreSQL calls

## Key Concepts Learned

- **Component Integrations**: Using pre-built Aspire components
- **Resource Extensions**: Creating reusable resource configurations
- **Health Checks**: Implementing custom health monitoring
- **Custom Metrics**: Adding application-specific metrics
- **Environment Configuration**: Managing different environments
- **Database Integration**: Using Entity Framework with Aspire

## Available Aspire Components

Popular Aspire components include:
- `Aspire.Hosting.Redis` - Redis cache
- `Aspire.Hosting.PostgreSQL` - PostgreSQL database
- `Aspire.Hosting.SqlServer` - SQL Server
- `Aspire.Hosting.RabbitMQ` - RabbitMQ message broker
- `Aspire.Hosting.MongoDB` - MongoDB
- `Aspire.Hosting.Azure.*` - Azure services
- `Aspire.Hosting.Kafka` - Apache Kafka

## Common Issues

### Issue: Redis container fails to start
**Solution**: Ensure Docker Desktop is running and you have sufficient resources allocated

### Issue: Database migrations not running
**Solution**: Use `dotnet ef migrations add Initial` and apply with `dotnet ef database update`

### Issue: Custom metrics not showing
**Solution**: Verify the meter name matches what OpenTelemetry is configured to collect

## Clean Up

Stop all containers from the Aspire Dashboard or:

```bash
docker stop $(docker ps -q)
```

## Additional Resources

- [Aspire Components](https://learn.microsoft.com/dotnet/aspire/fundamentals/components-overview)
- [Redis in Aspire](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-component)
- [PostgreSQL in Aspire](https://learn.microsoft.com/dotnet/aspire/database/postgresql-component)
- [Custom Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [OpenTelemetry Metrics](https://opentelemetry.io/docs/instrumentation/net/metrics/)

## Congratulations!

You've completed all three exercises and learned how to:
1. ✅ Create a system topology with Aspire
2. ✅ Deploy Aspire applications
3. ✅ Extend Aspire with custom components and integrations

You're now ready to build production-ready distributed applications with .NET Aspire!

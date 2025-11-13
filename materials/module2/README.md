# Module 2: Production Time Orchestration (Outer Loop)

## Overview
This module focuses on preparing your .NET Aspire application for production deployment. You'll learn about OpenTelemetry integration, health checks, deployment manifests, publishing options, and resource customization for production environments.

## Learning Objectives
By the end of this module, you will:
- Understand OpenTelemetry integration in Aspire for production observability
- Implement comprehensive health checks for services and dependencies
- Generate and understand Aspire deployment manifests
- Use Aspire publishers to deploy to various environments
- Customize resources for production requirements
- Apply best practices for production deployments

## Prerequisites
- Completed Module 1
- Understanding of containerization concepts
- Basic knowledge of cloud deployment
- Access to Azure subscription (optional, for cloud deployment)

## OpenTelemetry in Aspire

### What is OpenTelemetry?
OpenTelemetry (OTel) is a vendor-neutral observability framework for collecting traces, metrics, and logs. .NET Aspire has built-in OpenTelemetry support configured through ServiceDefaults.

### Three Pillars of Observability

#### 1. Traces
Distributed tracing shows the path of a request through your system.

**What's Included Automatically:**
- HTTP client and server requests
- Database calls (Entity Framework, ADO.NET)
- Redis operations
- Message queue operations
- gRPC calls

**Adding Custom Traces:**

```csharp
using System.Diagnostics;

public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("OrderService");

    public async Task<Order> ProcessOrder(int orderId)
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            // Business logic
            var order = await GetOrderAsync(orderId);
            activity?.SetTag("order.amount", order.TotalAmount);
            
            await ValidateOrder(order);
            await ProcessPayment(order);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return order;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

**Registering Custom Activity Source:**

In `Program.cs`:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("OrderService");
    });
```

#### 2. Metrics
Metrics provide numerical data about your application's performance and behavior.

**Built-in Metrics:**
- HTTP request duration and count
- HTTP request size
- CPU usage
- Memory usage
- .NET runtime metrics
- Database connection pool metrics

**Adding Custom Metrics:**

```csharp
using System.Diagnostics.Metrics;

public class OrderService
{
    private static readonly Meter Meter = new("OrderService");
    private static readonly Counter<long> OrdersProcessed = 
        Meter.CreateCounter<long>("orders.processed", "orders", "Number of orders processed");
    private static readonly Histogram<double> OrderAmount = 
        Meter.CreateHistogram<double>("order.amount", "USD", "Order amount in USD");
    private static readonly ObservableGauge<int> PendingOrders = 
        Meter.CreateObservableGauge<int>("orders.pending", GetPendingOrderCount, "orders");

    public async Task ProcessOrder(Order order)
    {
        // Process order
        await SaveOrder(order);
        
        // Record metrics
        OrdersProcessed.Add(1, new KeyValuePair<string, object?>("order.type", order.Type));
        OrderAmount.Record(order.TotalAmount, new KeyValuePair<string, object?>("currency", "USD"));
    }

    private static int GetPendingOrderCount() => 
        // Logic to get pending count
        42;
}
```

**Registering Custom Meter:**

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("OrderService");
    });
```

#### 3. Logs
Structured logging with correlation across services.

**Using ILogger with Enrichment:**

```csharp
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> _logger;

    public OrderController(ILogger<OrderController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = request.OrderId,
            ["CustomerId"] = request.CustomerId,
            ["TraceId"] = Activity.Current?.TraceId.ToString()
        }))
        {
            _logger.LogInformation("Processing order creation");
            
            try
            {
                var order = await ProcessOrderAsync(request);
                _logger.LogInformation("Order created successfully with total {Amount}", order.TotalAmount);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order");
                return StatusCode(500);
            }
        }
    }
}
```

### Exporting to External Systems

By default, Aspire sends telemetry to the dashboard. For production, export to your observability platform:

**Application Insights (Azure):**
```csharp
// In ServiceDefaults
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
```

**Prometheus & Grafana:**
```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
    });

app.MapPrometheusScrapingEndpoint(); // Exposes /metrics endpoint
```

**Jaeger:**
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://jaeger:4317");
        });
    });
```

## Health Checks and Monitoring

### Built-in Health Checks
ServiceDefaults automatically adds health checks for:
- Application liveness
- Readiness (all dependencies are ready)

**Accessing Health Endpoints:**
- Liveness: `/health`
- Readiness: `/alive`

### Adding Custom Health Checks

**Database Health Check:**
```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly TaskDbContext _context;

    public DatabaseHealthCheck(TaskDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable", ex);
        }
    }
}
```

**Registration:**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddRedis(builder.Configuration.GetConnectionString("cache")!, name: "redis")
    .AddUrlGroup(new Uri("https://external-api.com/health"), name: "external-api");
```

**Custom Health Check Endpoint:**
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});
```

### Health Check Policies

**Configure health check intervals:**
```csharp
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(5);
    options.Period = TimeSpan.FromSeconds(30);
});
```

## The Manifest File

The deployment manifest is a JSON description of your application's topology, used by deployment tools.

### Generating the Manifest

```bash
cd MyApp.AppHost
dotnet run --publisher manifest --output-path ../manifest.json
```

### Manifest Structure

```json
{
  "resources": {
    "cache": {
      "type": "container.v0",
      "connectionString": "...",
      "image": "redis:7.2"
    },
    "postgres": {
      "type": "container.v0",
      "connectionString": "...",
      "image": "postgres:16"
    },
    "taskdb": {
      "type": "value.v0",
      "connectionString": "{postgres.connectionString};Database=taskdb"
    },
    "apiservice": {
      "type": "project.v0",
      "path": "../MyApp.Api/MyApp.Api.csproj",
      "env": {
        "ConnectionStrings__cache": "{cache.connectionString}",
        "ConnectionStrings__taskdb": "{taskdb.connectionString}",
        "OTEL_EXPORTER_OTLP_ENDPOINT": "..."
      },
      "bindings": {
        "http": {
          "scheme": "http",
          "protocol": "tcp",
          "transport": "http"
        },
        "https": {
          "scheme": "https",
          "protocol": "tcp",
          "transport": "http"
        }
      }
    }
  }
}
```

### What's in the Manifest?

1. **Resources**: All services and infrastructure
2. **Connection Strings**: Parameterized connection information
3. **Environment Variables**: Configuration for each service
4. **Bindings**: Network endpoints and protocols
5. **Dependencies**: Relationships between resources
6. **Container Images**: Infrastructure component images

### Using the Manifest

The manifest is consumed by:
- Azure Developer CLI (`azd`)
- Custom deployment tools
- CI/CD pipelines
- Infrastructure as Code tools

## Aspire Publishers

Publishers are tools that take your Aspire application and deploy it to a target environment.

### Built-in Publishers

#### 1. Manifest Publisher
Generates the deployment manifest:
```bash
dotnet run --publisher manifest --output-path manifest.json
```

#### 2. Azure Developer CLI (azd)
Deploys to Azure Container Apps:

**Initialize:**
```bash
azd init
```

**Deploy:**
```bash
azd up
```

**What it does:**
- Provisions Azure resources (Container Apps, databases, etc.)
- Builds and pushes container images to Azure Container Registry
- Deploys services to Azure Container Apps
- Configures networking and environment variables

### Deployment Workflow

```
Local Development
       ↓
Generate Manifest
       ↓
Build Containers
       ↓
Deploy to Environment
       ↓
Production Running
```

### Azure Deployment Example

**Step 1: Install Prerequisites**
```bash
# Install Azure Developer CLI
curl -fsSL https://aka.ms/install-azd.sh | bash

# Or on Windows
winget install microsoft.azd
```

**Step 2: Initialize Project**
```bash
azd init
# Select: Use code in current directory
# Enter environment name: prod
# Select location: eastus
```

**Step 3: Configure Azure Resources**

Edit `infra/main.bicep` if needed for customization.

**Step 4: Deploy**
```bash
# Provision and deploy
azd up

# Or step by step
azd provision  # Creates Azure resources
azd deploy     # Deploys application
```

**Step 5: Monitor**
```bash
# View environment
azd show

# View logs
azd logs

# Open dashboard
azd dashboard
```

### Container Registry Configuration

**For Azure Container Registry:**
```bash
azd env set AZURE_CONTAINER_REGISTRY_NAME myregistry
```

**For Docker Hub:**
```csharp
// In AppHost
var api = builder.AddProject<Projects.MyApi>("api")
    .PublishAsDockerFile();
```

## Resource Customization

### Container Customization

**Custom Image:**
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "16-alpine")
    .WithImageTag("16.1");
```

**Environment Variables:**
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "scram-sha-256");
```

**Data Persistence:**
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()  // Named volume
    .WithDataBindMount("./data");  // Bind mount to local directory
```

**Resource Limits:**
```csharp
var redis = builder.AddRedis("cache")
    .WithAnnotation(new ContainerResourceAnnotation
    {
        Memory = 512 * 1024 * 1024,  // 512 MB
        CPULimit = 0.5  // 50% of one core
    });
```

### Service Customization

**Build Arguments:**
```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithBuildArg("BUILD_CONFIGURATION", "Release");
```

**Multiple Endpoints:**
```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithHttpEndpoint(port: 8080, name: "http")
    .WithHttpEndpoint(port: 8081, name: "metrics")
    .WithHttpsEndpoint(port: 8443, name: "https");
```

**Replicas for High Availability:**
```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReplicas(3);  // Run 3 instances
```

### Production-Ready Patterns

**Separate Databases per Environment:**
```csharp
var postgres = builder.AddPostgres("postgres");

var database = builder.Environment.IsProduction()
    ? postgres.AddDatabase("prod-db")
    : postgres.AddDatabase("dev-db");
```

**External Dependencies:**
```csharp
if (builder.Environment.IsProduction())
{
    // Use Azure Redis in production
    var cache = builder.AddConnectionString("cache");
}
else
{
    // Use local Redis in development
    var cache = builder.AddRedis("cache");
}
```

**Configuration by Environment:**
```csharp
var api = builder.AddProject<Projects.MyApi>("api");

if (builder.Environment.IsProduction())
{
    api.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
       .WithHttpsEndpoint(port: 443);
}
else
{
    api.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
       .WithHttpEndpoint(port: 5000);
}
```

## Resilience Patterns

ServiceDefaults includes resilience patterns using Polly:

### Retry Policy
Automatically configured for transient failures:

```csharp
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
});
```

### Circuit Breaker
Prevents cascading failures:

```csharp
builder.Services.AddHttpClient<MyApiClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.CircuitBreaker.FailureRatio = 0.5;  // Break at 50% failure
        options.CircuitBreaker.MinimumThroughput = 10;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    });
```

### Timeout Policy
Prevents hanging requests:

```csharp
builder.Services.AddHttpClient<MyApiClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
    });
```

## Best Practices for Production

### 1. Security
- Use secrets management (Azure Key Vault, Kubernetes secrets)
- Enable HTTPS in production
- Implement authentication and authorization
- Regular security scans

### 2. Observability
- Export telemetry to external systems
- Set up alerting based on metrics
- Implement comprehensive logging
- Use correlation IDs

### 3. Reliability
- Implement health checks for all dependencies
- Use retry and circuit breaker patterns
- Configure appropriate timeouts
- Plan for graceful degradation

### 4. Performance
- Set resource limits
- Use connection pooling
- Implement caching strategies
- Monitor and optimize slow queries

### 5. Deployment
- Use infrastructure as code
- Automate deployments with CI/CD
- Implement blue-green or canary deployments
- Have rollback procedures

## Troubleshooting Production Issues

### Viewing Logs in Azure
```bash
azd logs --service apiservice
```

### Accessing Metrics
Use Azure Monitor, Application Insights, or your observability platform.

### Remote Debugging
Consider using remote profiling tools and diagnostic dumps rather than interactive debugging.

### Health Check Failures
Review health check endpoints and verify dependencies are accessible.

## Additional Resources
- [.NET Aspire Deployment](https://learn.microsoft.com/dotnet/aspire/deployment/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Polly Resilience](https://www.pollydocs.org/)

## Next Steps
Proceed to the hands-on exercise where you'll prepare an application for deployment, generate manifests, and deploy to Azure Container Apps.

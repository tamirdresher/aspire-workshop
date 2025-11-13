# Module 2: Hands-On Exercise - Deploying Your App

## Exercise Overview
In this exercise, you'll take an existing Aspire application and prepare it for production deployment. You'll add custom telemetry, implement health checks, generate deployment manifests, and optionally deploy to Azure Container Apps.

## Scenario
Continue with the Task Manager application from Module 1. You'll:
- Add custom traces, metrics, and logs
- Implement comprehensive health checks
- Generate a deployment manifest
- Containerize the application
- Deploy to Azure Container Apps (optional)

## Time Required
60-75 minutes (90+ minutes with Azure deployment)

## Prerequisites
- Completed Module 1 exercise
- Task Manager application from Module 1
- Docker Desktop running
- Azure subscription (optional, for cloud deployment)
- Azure Developer CLI installed (for Azure deployment)

## Step 1: Add Custom OpenTelemetry Instrumentation

### 1.1 Create a Task Service with Custom Telemetry

Create `TaskManager.ApiService/Services/TaskService.cs`:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace TaskManager.ApiService.Services;

public class TaskService
{
    private static readonly ActivitySource ActivitySource = new("TaskManager.Api");
    private static readonly Meter Meter = new("TaskManager.Api");
    
    private static readonly Counter<long> TasksCreated = 
        Meter.CreateCounter<long>("tasks.created", "tasks", "Number of tasks created");
    
    private static readonly Counter<long> TasksCompleted = 
        Meter.CreateCounter<long>("tasks.completed", "tasks", "Number of tasks completed");
    
    private static readonly Histogram<double> TaskProcessingDuration = 
        Meter.CreateHistogram<double>("task.processing.duration", "ms", "Time to process a task");

    private readonly TaskDbContext _context;
    private readonly IDatabase _cache;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        TaskDbContext context,
        IConnectionMultiplexer redis,
        ILogger<TaskService> logger)
    {
        _context = context;
        _cache = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<TaskItem> CreateTaskAsync(string title, string? description)
    {
        using var activity = ActivitySource.StartActivity("CreateTask");
        var startTime = DateTime.UtcNow;
        
        try
        {
            activity?.SetTag("task.title", title);
            
            _logger.LogInformation("Creating new task: {Title}", title);

            var task = new TaskItem
            {
                Title = title,
                Description = description,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Record metrics
            TasksCreated.Add(1, new KeyValuePair<string, object?>("priority", "normal"));
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            TaskProcessingDuration.Record(duration);

            activity?.SetTag("task.id", task.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation("Task created successfully with ID: {TaskId}", task.Id);

            return task;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Failed to create task: {Title}", title);
            throw;
        }
    }

    public async Task<TaskItem?> GetTaskAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetTask");
        activity?.SetTag("task.id", id);

        try
        {
            // Try cache first
            var cacheKey = $"task:{id}";
            var cachedTask = await _cache.StringGetAsync(cacheKey);
            
            if (cachedTask.HasValue)
            {
                _logger.LogInformation("Task {TaskId} retrieved from cache", id);
                activity?.AddEvent(new ActivityEvent("CacheHit"));
                return System.Text.Json.JsonSerializer.Deserialize<TaskItem>(cachedTask!);
            }

            // Not in cache, get from database
            _logger.LogInformation("Task {TaskId} not in cache, retrieving from database", id);
            activity?.AddEvent(new ActivityEvent("CacheMiss"));
            
            var task = await _context.Tasks.FindAsync(id);
            
            if (task != null)
            {
                // Store in cache for 5 minutes
                var json = System.Text.Json.JsonSerializer.Serialize(task);
                await _cache.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(5));
                activity?.AddEvent(new ActivityEvent("CacheUpdated"));
            }

            return task;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to get task: {TaskId}", id);
            throw;
        }
    }

    public async Task<bool> CompleteTaskAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("CompleteTask");
        activity?.SetTag("task.id", id);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TaskId"] = id,
            ["Operation"] = "CompleteTask"
        }))
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                {
                    _logger.LogWarning("Task not found: {TaskId}", id);
                    return false;
                }

                task.IsCompleted = true;
                await _context.SaveChangesAsync();

                // Invalidate cache
                var cacheKey = $"task:{id}";
                await _cache.KeyDeleteAsync(cacheKey);

                // Record metric
                TasksCompleted.Add(1);

                _logger.LogInformation("Task completed: {TaskId}", id);
                activity?.SetStatus(ActivityStatusCode.Ok);

                return true;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Failed to complete task: {TaskId}", id);
                throw;
            }
        }
    }

    public async Task<List<TaskItem>> GetAllTasksAsync()
    {
        using var activity = ActivitySource.StartActivity("GetAllTasks");

        try
        {
            var tasks = await _context.Tasks
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            activity?.SetTag("task.count", tasks.Count);
            _logger.LogInformation("Retrieved {Count} tasks", tasks.Count);

            return tasks;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to get all tasks");
            throw;
        }
    }
}
```

### 1.2 Register Custom Telemetry Sources

Update `TaskManager.ApiService/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register custom telemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("TaskManager.Api");
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("TaskManager.Api");
    });

// Add database and cache
builder.AddNpgsqlDbContext<TaskDbContext>("taskdb");
builder.AddRedisClient("cache");

// Register services
builder.Services.AddScoped<TaskService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    await context.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 1.3 Create API Endpoints

Create `TaskManager.ApiService/Controllers/TasksController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using TaskManager.ApiService.Services;

namespace TaskManager.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly TaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(TaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskItem>>> GetAll()
    {
        _logger.LogInformation("Getting all tasks");
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> Get(int id)
    {
        _logger.LogInformation("Getting task {TaskId}", id);
        var task = await _taskService.GetTaskAsync(id);
        
        if (task == null)
            return NotFound();
        
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create([FromBody] CreateTaskRequest request)
    {
        _logger.LogInformation("Creating task: {Title}", request.Title);
        var task = await _taskService.CreateTaskAsync(request.Title, request.Description);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id}/complete")]
    public async Task<ActionResult> Complete(int id)
    {
        _logger.LogInformation("Completing task {TaskId}", id);
        var result = await _taskService.CompleteTaskAsync(id);
        
        if (!result)
            return NotFound();
        
        return NoContent();
    }
}

public record CreateTaskRequest(string Title, string? Description);
```

## Step 2: Implement Health Checks

### 2.1 Create Custom Health Checks

Create `TaskManager.ApiService/HealthChecks/DatabaseHealthCheck.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskManager.ApiService.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly TaskDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(TaskDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            var taskCount = await _context.Tasks.CountAsync(cancellationToken);
            
            return HealthCheckResult.Healthy(
                "Database is healthy",
                new Dictionary<string, object>
                {
                    ["task_count"] = taskCount
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
        }
    }
}
```

Create `TaskManager.ApiService/HealthChecks/CacheHealthCheck.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace TaskManager.ApiService.HealthChecks;

public class CacheHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheHealthCheck> _logger;

    public CacheHealthCheck(IConnectionMultiplexer redis, ILogger<CacheHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            
            var endpoint = _redis.GetEndPoints().FirstOrDefault();
            var server = endpoint != null ? _redis.GetServer(endpoint) : null;
            
            return HealthCheckResult.Healthy(
                "Redis cache is healthy",
                new Dictionary<string, object>
                {
                    ["endpoint"] = endpoint?.ToString() ?? "unknown",
                    ["connected"] = _redis.IsConnected
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache health check failed");
            return HealthCheckResult.Unhealthy("Redis cache is unhealthy", ex);
        }
    }
}
```

### 2.2 Register Health Checks

Update `TaskManager.ApiService/Program.cs`:

```csharp
// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<CacheHealthCheck>("cache");

// ... rest of the configuration ...

var app = builder.Build();

// Add detailed health check endpoint
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString(),
                data = e.Value.Data
            })
        };
        
        await context.Response.WriteAsJsonAsync(response);
    }
});
```

## Step 3: Generate Deployment Manifest

### 3.1 Generate the Manifest

```bash
cd TaskManager.AppHost
dotnet run --publisher manifest --output-path ../manifest.json
```

### 3.2 Examine the Manifest

```bash
cd ..
cat manifest.json
```

**Expected output structure:**
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
      "image": "postgres:16"
    },
    "taskdb": {
      "type": "value.v0",
      "connectionString": "{postgres.connectionString};Database=taskdb"
    },
    "apiservice": {
      "type": "project.v0",
      "path": "../TaskManager.ApiService/TaskManager.ApiService.csproj",
      "env": {
        "ConnectionStrings__cache": "{cache.connectionString}",
        "ConnectionStrings__taskdb": "{taskdb.connectionString}"
      }
    }
  }
}
```

### 3.3 Customize for Production

Update `TaskManager.AppHost/Program.cs` for production customization:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithDataVolume();

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithEnvironment("POSTGRES_PASSWORD", builder.AddParameter("postgres-password", secret: true));

var database = postgres.AddDatabase("taskdb");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(database);

if (builder.Environment.IsProduction())
{
    // Production-specific configuration
    apiService.WithReplicas(3);
}

var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database);

builder.AddProject<Projects.TaskManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

## Step 4: Containerize the Application

### 4.1 Add Dockerfile Support

Add `TaskManager.ApiService/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TaskManager.ApiService/TaskManager.ApiService.csproj", "TaskManager.ApiService/"]
COPY ["TaskManager.ServiceDefaults/TaskManager.ServiceDefaults.csproj", "TaskManager.ServiceDefaults/"]
RUN dotnet restore "TaskManager.ApiService/TaskManager.ApiService.csproj"
COPY . .
WORKDIR "/src/TaskManager.ApiService"
RUN dotnet build "TaskManager.ApiService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskManager.ApiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskManager.ApiService.dll"]
```

### 4.2 Build Container Images

```bash
# Build API service
cd TaskManager.ApiService
docker build -t taskmanager-api:latest -f Dockerfile ..

# Verify image
docker images | grep taskmanager
```

### 4.3 Test Container Locally

```bash
# Run the API container
docker run -d -p 8080:8080 \
  --name taskmanager-api \
  -e ConnectionStrings__cache="localhost:6379" \
  taskmanager-api:latest

# Test the endpoint
curl http://localhost:8080/health

# Stop and remove
docker stop taskmanager-api
docker rm taskmanager-api
```

## Step 5: Deploy to Azure Container Apps (Optional)

### 5.1 Install Azure Developer CLI

**On Linux/macOS:**
```bash
curl -fsSL https://aka.ms/install-azd.sh | bash
```

**On Windows:**
```powershell
winget install microsoft.azd
```

### 5.2 Login to Azure

```bash
azd auth login
```

### 5.3 Initialize for Azure

```bash
cd TaskManager
azd init

# Select: Use code in current directory
# Environment name: prod
# Location: eastus (or your preferred region)
```

### 5.4 Provision and Deploy

```bash
# This will:
# 1. Create Azure resources (Container Apps, Container Registry, etc.)
# 2. Build container images
# 3. Push to Azure Container Registry
# 4. Deploy to Azure Container Apps
azd up
```

**This process takes 5-10 minutes.**

### 5.5 Verify Deployment

```bash
# Show environment details
azd show

# View logs
azd logs --service apiservice

# Get service endpoints
azd env get-values
```

### 5.6 Access Application

The output will include URLs like:
- API: `https://apiservice.{random}.eastus.azurecontainerapps.io`
- Web: `https://webfrontend.{random}.eastus.azurecontainerapps.io`

### 5.7 Monitor in Azure

```bash
# Open Azure portal
azd dashboard
```

Or visit the Azure Portal and navigate to:
- Resource Group: `rg-{environment-name}`
- Container Apps
- Application Insights

## Step 6: Verify and Test

### 6.1 Test Health Endpoints

```bash
# Get API URL from azd
API_URL=$(azd env get-values | grep APISERVICE_URL | cut -d'=' -f2)

# Test health endpoint
curl $API_URL/health/detailed
```

### 6.2 Test API Functionality

```bash
# Create a task
curl -X POST $API_URL/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Deploy to Azure","description":"Complete Azure deployment"}'

# Get all tasks
curl $API_URL/api/tasks

# Get specific task
curl $API_URL/api/tasks/1

# Complete task
curl -X PUT $API_URL/api/tasks/1/complete
```

### 6.3 View Telemetry

**In Azure Portal:**
1. Navigate to Application Insights
2. Click "Logs" and run queries:

```kusto
// View recent traces
traces
| where timestamp > ago(1h)
| order by timestamp desc
| take 50

// View custom metrics
customMetrics
| where name == "tasks.created"
| summarize sum(value) by bin(timestamp, 5m)

// View dependencies
dependencies
| where timestamp > ago(1h)
| project timestamp, name, type, duration, success
| order by timestamp desc
```

3. Click "Transaction search" to see end-to-end traces
4. Click "Metrics" to view charts

## Step 7: Clean Up (Optional)

### 7.1 Delete Azure Resources

```bash
# Delete all provisioned resources
azd down

# Confirm deletion when prompted
```

This removes all Azure resources and cleans up the deployment.

## Challenge Tasks

### Challenge 1: Add Application Insights Export
Configure OpenTelemetry to export directly to Application Insights:

```csharp
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
```

### Challenge 2: Implement Resilience Patterns
Add retry and circuit breaker policies for external dependencies.

### Challenge 3: Add Prometheus Endpoint
Export metrics in Prometheus format:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
    });

app.MapPrometheusScrapingEndpoint();
```

### Challenge 4: Multi-Region Deployment
Deploy to multiple Azure regions for high availability.

### Challenge 5: Custom Dashboard
Create a Grafana dashboard for your custom metrics.

## Verification Checklist

- [ ] Custom traces appear in the Aspire Dashboard
- [ ] Custom metrics are recorded and visible
- [ ] Structured logs include proper context
- [ ] Health checks return detailed status
- [ ] Manifest file generated successfully
- [ ] Container images build without errors
- [ ] Application runs in containers locally
- [ ] (Optional) Deployment to Azure succeeds
- [ ] (Optional) Application is accessible via public URL
- [ ] (Optional) Telemetry appears in Application Insights

## Key Takeaways

1. **Custom Telemetry**: Activity sources and meters provide deep visibility
2. **Health Checks**: Essential for orchestration and monitoring
3. **Manifest**: Bridge between development and deployment
4. **Containers**: Standard packaging for cloud deployment
5. **Azure Integration**: Seamless deployment with `azd`

## Next Steps
- Explore advanced telemetry features
- Implement custom publishers for other platforms
- Prepare for Module 3: Aspire Extensibility

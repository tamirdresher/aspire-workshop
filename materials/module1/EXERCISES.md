# Module 1: Hands-On Exercise - Creating a System Topology

## Exercise Overview
In this exercise, you'll create a multi-service application using .NET Aspire. You'll set up an AppHost, add multiple services and infrastructure components, and wire them together to create a cohesive distributed system.

## Scenario
You're building a simple task management system with:
- A Web API backend for task operations
- A Web frontend for user interaction
- A Redis cache for performance
- A PostgreSQL database for persistence
- A background worker for notifications

## Time Required
45-60 minutes

## Step 1: Create the Solution Structure

### 1.1 Create a New Aspire Application

```bash
# Create a new directory
mkdir TaskManager
cd TaskManager

# Create the Aspire starter solution
dotnet new aspire-starter -n TaskManager
```

This creates:
- `TaskManager.AppHost` - Orchestration project
- `TaskManager.ServiceDefaults` - Shared configuration
- `TaskManager.ApiService` - Sample API
- `TaskManager.Web` - Sample web frontend

### 1.2 Explore the Generated Projects

**TaskManager.AppHost/Program.cs:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice");

builder.AddProject<Projects.TaskManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

**TaskManager.ServiceDefaults/Extensions.cs:**
```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();
    // ... more configuration
}
```

### 1.3 Run the Application

```bash
cd TaskManager.AppHost
dotnet run
```

**What to observe:**
- The Aspire Dashboard opens automatically
- Two services appear in the Resources tab
- Logs stream from both services
- The web frontend is accessible via the displayed endpoint

## Step 2: Add Infrastructure Components

### 2.1 Add Redis Cache

Update `TaskManager.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache); // Reference the cache

builder.AddProject<Projects.TaskManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

### 2.2 Add PostgreSQL Database

Update `TaskManager.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Add PostgreSQL with a specific database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(); // Persist data between runs

var database = postgres.AddDatabase("taskdb");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(database); // Reference the database

builder.AddProject<Projects.TaskManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

### 2.3 Install Required Packages in API Service

```bash
cd ../TaskManager.ApiService

# Add Aspire components
dotnet add package Aspire.StackExchange.Redis
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
```

### 2.4 Configure the API Service to Use Infrastructure

Update `TaskManager.ApiService/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add services
builder.AddRedisClient("cache");
builder.AddNpgsqlDbContext<TaskDbContext>("taskdb");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

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

### 2.5 Create the Database Context

Create `TaskManager.ApiService/TaskDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IsCompleted).IsRequired();
        });
    }
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## Step 3: Add a Background Worker

### 3.1 Create a Worker Service

```bash
cd ..
dotnet new worker -n TaskManager.Worker
dotnet sln add TaskManager.Worker
```

### 3.2 Add ServiceDefaults Reference

```bash
cd TaskManager.Worker
dotnet add reference ../TaskManager.ServiceDefaults
```

### 3.3 Update Worker to Use ServiceDefaults

Update `TaskManager.Worker/Program.cs`:

```csharp
using TaskManager.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

### 3.4 Register Worker in AppHost

Update `TaskManager.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("taskdb");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(database);

// Add the worker service
var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database);

builder.AddProject<Projects.TaskManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

## Step 4: Configuration and Secrets

### 4.1 Add a Parameter for Configuration

Update `TaskManager.AppHost/Program.cs` to add a configurable parameter:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a parameter
var notificationInterval = builder.AddParameter("notification-interval");

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("taskdb");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(database);

var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database)
    .WithEnvironment("NotificationInterval", notificationInterval);

builder.AddProject<Projects.TaskManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

### 4.2 Set Parameter Value

When running, you'll be prompted or can set via:

**Option 1 - Interactive Prompt:**
The dashboard will prompt for the value when starting.

**Option 2 - appsettings.json:**
Update `TaskManager.AppHost/appsettings.json`:
```json
{
  "Parameters": {
    "notification-interval": "30"
  }
}
```

**Option 3 - User Secrets:**
```bash
cd TaskManager.AppHost
dotnet user-secrets init
dotnet user-secrets set "Parameters:notification-interval" "30"
```

### 4.3 Add a Secret Parameter

For sensitive data:

```csharp
var apiKey = builder.AddParameter("external-api-key", secret: true);

var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database)
    .WithEnvironment("NotificationInterval", notificationInterval)
    .WithEnvironment("ExternalApi:ApiKey", apiKey);
```

## Step 5: Explore and Verify

### 5.1 Run the Complete Application

```bash
cd TaskManager.AppHost
dotnet run
```

### 5.2 Verify in Dashboard

**Resources Tab:**
- [ ] Verify all 5 resources are running (web, api, worker, cache, postgres)
- [ ] Check endpoint URLs are displayed
- [ ] Confirm all show "Running" status

**Logs Tab:**
- [ ] Filter logs by resource
- [ ] Observe startup messages from each service
- [ ] Watch real-time log streaming

**Traces Tab:**
- [ ] Make a request to the web frontend
- [ ] Observe the trace spanning web -> api
- [ ] Click on individual spans to see details

**Metrics Tab:**
- [ ] View CPU and memory usage
- [ ] Observe HTTP request metrics
- [ ] Check per-resource metrics

### 5.3 Test Service Communication

Access the API via the web frontend or directly:

```bash
# Get the API endpoint from dashboard (e.g., http://localhost:5234)
curl http://localhost:5234/weatherforecast
```

Observe in dashboard:
- New trace appears showing the request
- Logs show the API handling the request
- Metrics update with request count

## Step 6: Experiment with Configuration

### 6.1 Add Environment-Specific Settings

Create `TaskManager.AppHost/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "Parameters": {
    "notification-interval": "10"
  }
}
```

### 6.2 Modify Resource Configuration

Try different configurations:

```csharp
// Run multiple replicas of the worker
var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database)
    .WithReplicas(3); // Run 3 instances

// Change exposed ports
var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithHttpEndpoint(port: 5100, name: "http")
    .WithReference(cache)
    .WithReference(database);
```

### 6.3 Add Custom Environment Variables

```csharp
var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithEnvironment("FeatureFlags:EnableNotifications", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(cache)
    .WithReference(database);
```

## Step 7: Challenge Tasks

Try these additional tasks to deepen your understanding:

### Challenge 1: Add RabbitMQ
Add RabbitMQ messaging between the API and Worker:
```csharp
var messaging = builder.AddRabbitMQ("messaging");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(messaging);

var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(messaging);
```

### Challenge 2: Add Azure Storage Emulator
Add Azurite for blob storage:
```csharp
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(blobs);
```

### Challenge 3: Add a MongoDB Database
```csharp
var mongo = builder.AddMongoDB("mongodb")
    .AddDatabase("tasklogs");

var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(mongo);
```

## Verification Checklist

- [ ] All services start without errors
- [ ] Infrastructure containers are running in Docker
- [ ] Dashboard shows all resources as "Running"
- [ ] Web frontend is accessible and can call the API
- [ ] Logs appear in the dashboard from all services
- [ ] Traces show end-to-end request flow
- [ ] Configuration parameters are correctly injected
- [ ] Services can access Redis and PostgreSQL
- [ ] Changes trigger automatic rebuilds

## Common Issues and Solutions

### Issue: Port Already in Use
**Solution:** Stop other instances or change the port:
```csharp
.WithHttpEndpoint(port: 5200, name: "http")
```

### Issue: Docker Containers Not Starting
**Solution:** 
- Ensure Docker Desktop is running
- Check Docker daemon: `docker ps`
- Clear old containers: `docker system prune`

### Issue: Connection Strings Not Injected
**Solution:**
- Verify `.WithReference()` is called
- Check ServiceDefaults is registered
- Confirm correct connection name matches

### Issue: Dashboard Not Opening
**Solution:**
- Check console output for dashboard URL
- Manually navigate to `http://localhost:15888`
- Verify port 15888 isn't in use

## Key Takeaways

1. **AppHost is the Orchestrator**: Defines your entire system topology in code
2. **References Create Dependencies**: `.WithReference()` injects connection info
3. **ServiceDefaults Standardizes**: Common configuration in one place
4. **Dashboard Provides Visibility**: Real-time observability out of the box
5. **Fast Inner Loop**: Quick iterations with automatic rebuilds

## Next Steps
- Experiment with different infrastructure components
- Add your own custom configuration
- Explore the dashboard features in depth
- Prepare for Module 2: Production Time Orchestration

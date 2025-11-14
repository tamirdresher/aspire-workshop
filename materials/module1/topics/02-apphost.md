# AppHost Fundamentals

## What is the AppHost?

The **AppHost** is the orchestration layer of your .NET Aspire application. It's a special console application that:

1. **Defines** your distributed application's topology
2. **Manages** the lifecycle of all resources (services, databases, caches, etc.)
3. **Provides** configuration and service discovery
4. **Launches** the Aspire Dashboard for observability

Think of it as the "conductor" of your distributed orchestra.

## Creating an AppHost

### Using Templates

```bash
# Create just an AppHost
dotnet new aspire-apphost -n MyApp.AppHost

# Or create a complete starter (AppHost + ServiceDefaults + sample services)
dotnet new aspire-starter -n MyApp
```

### Project Structure

```
MyApp.AppHost/
├── Program.cs              # Application topology definition
├── appsettings.json        # Configuration
├── appsettings.Development.json
└── MyApp.AppHost.csproj    # Project file with IsAspireHost=true
```

### Key Project Properties

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <IsAspireHost>true</IsAspireHost>  <!-- Important! -->
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="8.0.0" />
  </ItemGroup>
</Project>
```

## The DistributedApplicationBuilder

The core API for defining your application.

### Basic Structure

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add resources here
// ...

builder.Build().Run();
```

### Builder Configuration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Access configuration
var setting = builder.Configuration["MySetting"];

// Access environment
if (builder.Environment.IsDevelopment())
{
    // Development-specific setup
}

// Access services (for advanced scenarios)
builder.Services.AddSingleton<IMyService, MyService>();
```

## Adding Resources

Resources are the building blocks of your application.

### 1. Project Resources (.NET Services)

Add your .NET projects as services:

```csharp
// Add a project by reference
var api = builder.AddProject<Projects.MyApi>("api");

// Add a project from a path
var web = builder.AddProject("web", "../MyWeb/MyWeb.csproj");
```

**What happens:**
- Project is built automatically
- Service is started when AppHost runs
- Endpoints are registered
- Service appears in dashboard

### 2. Container Resources (Infrastructure)

Add infrastructure components that run in Docker:

```csharp
// Redis cache
var redis = builder.AddRedis("cache");

// PostgreSQL database
var postgres = builder.AddPostgres("postgres");

// SQL Server
var sql = builder.AddSqlServer("sql");

// RabbitMQ
var rabbitmq = builder.AddRabbitMQ("messaging");
```

**What happens:**
- Docker image is pulled (if needed)
- Container is started automatically
- Connection strings are generated
- Resource appears in dashboard

### 3. Database Resources

Logical databases within database servers:

```csharp
var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("mydb");

// Now reference the database in services
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(database);
```

### 4. Executable Resources

Run external executables:

```csharp
var npm = builder.AddExecutable("frontend", "npm", ".")
    .WithArgs("run", "dev");
```

## Resource References

Connect resources to services using `.WithReference()`.

### Basic References

```csharp
var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres").AddDatabase("mydb");

var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache)      // Injects ConnectionStrings__cache
    .WithReference(db);        // Injects ConnectionStrings__mydb
```

### What WithReference Does

1. Creates a dependency (cache starts before api)
2. Injects connection string as environment variable
3. Enables service discovery
4. Appears in dashboard topology view

### Service-to-Service References

```csharp
var api = builder.AddProject<Projects.MyApi>("api");

var web = builder.AddProject<Projects.MyWeb>("web")
    .WithReference(api);  // Web can call API
```

**In the web service**, you can now use:

```csharp
// Automatically discovers the API endpoint
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    // Service discovery resolves "api" to actual endpoint
    client.BaseAddress = new Uri("http://api");
});
```

## Configuring Resources

### Environment Variables

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("MaxRetries", "3")
    .WithEnvironment("LogLevel", "Debug");
```

### HTTP Endpoints

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithHttpsEndpoint(port: 5001, name: "https");

// External endpoints (accessible outside Docker network)
var web = builder.AddProject<Projects.MyWeb>("web")
    .WithExternalHttpEndpoints();  // Can be accessed from browser
```

### Replicas (Multiple Instances)

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReplicas(3);  // Run 3 instances for load balancing
```

### Data Volumes (Persistence)

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();  // Data persists between runs

var redis = builder.AddRedis("cache")
    .WithDataVolume("redis-data");  // Named volume
```

## Complete Example

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var cache = builder.AddRedis("cache")
    .WithDataVolume();

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("mydb");

var messaging = builder.AddRabbitMQ("messaging");

// Backend Services
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(messaging)
    .WithEnvironment("MaxConnections", "100")
    .WithReplicas(2);

// Background Worker
var worker = builder.AddProject<Projects.MyWorker>("worker")
    .WithReference(database)
    .WithReference(messaging);

// Frontend
var web = builder.AddProject<Projects.MyWeb>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api);

builder.Build().Run();
```

## Resource Lifecycle

When you run the AppHost:

1. **Dependency Resolution**
   - Analyzes all `.WithReference()` calls
   - Builds dependency graph

2. **Startup Order**
   - Infrastructure starts first (databases, caches)
   - Services start in dependency order
   - Web frontends start last

3. **Health Monitoring**
   - Checks if resources are healthy
   - Restarts failed resources (configurable)
   - Updates dashboard status

4. **Shutdown**
   - Graceful shutdown in reverse order
   - Containers are stopped (but not removed)
   - Data volumes persist

## Best Practices

### 1. Meaningful Names

```csharp
// ✅ Good - clear and descriptive
var userDb = postgres.AddDatabase("userdb");
var productCache = builder.AddRedis("product-cache");

// ❌ Bad - unclear
var db1 = postgres.AddDatabase("db1");
var r = builder.AddRedis("r");
```

### 2. Organize by Layer

```csharp
// Infrastructure
var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres").AddDatabase("mydb");

// Services
var api = builder.AddProject<Projects.Api>("api")...;
var worker = builder.AddProject<Projects.Worker>("worker")...;

// Frontends
var web = builder.AddProject<Projects.Web>("web")...;
```

### 3. Use Configuration

```csharp
// Read from appsettings.json
var replicaCount = builder.Configuration.GetValue<int>("Api:Replicas", 1);

var api = builder.AddProject<Projects.Api>("api")
    .WithReplicas(replicaCount);
```

### 4. Environment-Specific Setup

```csharp
var db = builder.AddPostgres("postgres");

if (builder.Environment.IsDevelopment())
{
    db.WithDataVolume();  // Persist data in dev
}
else
{
    // In production, use managed database
    db.WithDataBindMount("./init-scripts");
}
```

## Running the AppHost

### Command Line

```bash
# From AppHost directory
dotnet run

# With specific configuration
dotnet run --configuration Release

# With environment variable
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

### Visual Studio

- Set AppHost as startup project
- Press F5 or click Run
- Dashboard opens automatically

### VS Code

- Use "Run and Debug" panel
- Select AppHost configuration
- Press F5

## Dashboard Access

By default: `http://localhost:15888`

Configure in `appsettings.json`:

```json
{
  "ASPIRE_DASHBOARD_PORT": "18888"
}
```

## Next Steps

- **Next Topic:** [ServiceDefaults](./03-service-defaults.md)
- **Try Example:** [Multi-Service App](../examples/02-multi-service/)

## Official Documentation

- [AppHost Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview)
- [Resources in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/orchestrate-resources)
- [Aspire Architecture](https://learn.microsoft.com/dotnet/aspire/architecture/overview)

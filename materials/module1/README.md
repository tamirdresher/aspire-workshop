# Module 1: Dev Time Orchestration (Inner Loop)

## Overview
This module introduces .NET Aspire's core concepts and demonstrates how it streamlines local development of distributed applications. You'll learn how to orchestrate services and infrastructure components, manage configuration, and leverage the built-in dashboard for an enhanced inner-loop development experience.

## Learning Objectives
By the end of this module, you will:
- Understand the core concepts and building blocks of .NET Aspire
- Create and configure an AppHost to orchestrate distributed applications
- Use ServiceDefaults for consistent service configuration
- Manage configuration and secrets effectively
- Navigate and utilize the Aspire Dashboard for development

## Prerequisites
- .NET SDK 8.0 or later with Aspire workload installed
- Docker Desktop or Podman running
- Visual Studio 2022 or VS Code with C# extensions
- Basic understanding of .NET and C#

## Concepts & Building Blocks

### Why .NET Aspire?
Modern applications are increasingly distributed, involving multiple services, databases, message queues, and other infrastructure components. Managing these dependencies during development can be challenging:
- **Complex Setup**: Setting up local development environments with all dependencies
- **Configuration Management**: Keeping track of connection strings, ports, and settings
- **Observability**: Understanding what's happening across multiple services
- **Consistency**: Ensuring all team members have similar environments

.NET Aspire addresses these challenges by providing:
- **Orchestration**: Simple APIs to define and run your application's services and dependencies
- **Built-in Observability**: Integrated OpenTelemetry support with a beautiful dashboard
- **Standardization**: ServiceDefaults for consistent configuration across services
- **Developer Experience**: Fast inner-loop with automatic resource provisioning

### Core Building Blocks

#### 1. AppHost
The orchestration layer of your application. It defines:
- Services (APIs, web apps, worker services)
- Infrastructure components (databases, caches, message brokers)
- Dependencies and relationships between resources
- Configuration and environment variables

**Key Responsibilities:**
- Resource definition and lifecycle management
- Service discovery and binding
- Configuration injection
- Development environment orchestration

#### 2. ServiceDefaults
A shared project that configures common cross-cutting concerns:
- OpenTelemetry (traces, metrics, logs)
- Service discovery
- Health checks
- Resilience patterns
- HTTP client defaults

**Benefits:**
- Consistent configuration across all services
- Reduced boilerplate code
- Easy to update common settings
- Best practices built-in

#### 3. Aspire Dashboard
An interactive web-based dashboard that provides:
- Real-time view of all running resources
- Logs aggregation and filtering
- Distributed tracing visualization
- Metrics and performance data
- Environment variable inspection

## DistributedApplicationBuilder: Wiring Services and Infrastructure

The `DistributedApplicationBuilder` is the main API for defining your application topology.

### Basic Service Registration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a .NET project as a service
var apiService = builder.AddProject<Projects.MyApi>("api");

// Add another service with a reference to the first
var webApp = builder.AddProject<Projects.MyWebApp>("webapp")
    .WithReference(apiService);

builder.Build().Run();
```

### Adding Infrastructure Components

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var cache = builder.AddRedis("cache");

// Add SQL Server database
var db = builder.AddSqlServer("sql")
    .AddDatabase("mydb");

// Add a service that uses both
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache)
    .WithReference(db);

builder.Build().Run();
```

### Resource Methods

Common methods for configuring resources:

- **`.WithReference(resource)`**: Creates a dependency and injects connection information
- **`.WithEnvironment(key, value)`**: Adds environment variables
- **`.WithHttpEndpoint(port, name)`**: Exposes an HTTP endpoint
- **`.WithHttpsEndpoint(port, name)`**: Exposes an HTTPS endpoint
- **`.WithReplicas(count)`**: Runs multiple instances
- **`.WithExternalHttpEndpoints()`**: Makes endpoints accessible outside the application

## Configuration & Secrets Management

### Configuration Sources (Priority Order)
1. Command-line arguments
2. Environment variables
3. User secrets (development only)
4. appsettings.{Environment}.json
5. appsettings.json

### User Secrets
For sensitive data during development:

```bash
# Initialize user secrets
dotnet user-secrets init --project ./MyApi

# Set a secret
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost;..." --project ./MyApi
```

In AppHost:
```csharp
var db = builder.AddConnectionString("Database");
```

### Environment Variables
Pass configuration to services:

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("MaxRetries", "3")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");
```

### Parameter Resources
For values that need to be provided at runtime:

```csharp
var apiKey = builder.AddParameter("api-key", secret: true);

var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("ExternalApi:ApiKey", apiKey);
```

### Binding Configuration
Services automatically receive connection information for referenced resources:

```csharp
// In AppHost
var cache = builder.AddRedis("cache");
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache);
```

```csharp
// In MyApi - configuration is automatically bound
builder.AddRedisClient("cache");
```

## Solution Structure

A typical Aspire solution structure:

```
MySolution/
├── MySolution.sln
├── MySolution.AppHost/              # Orchestration layer
│   ├── Program.cs                   # Application topology definition
│   ├── appsettings.json
│   └── MySolution.AppHost.csproj
├── MySolution.ServiceDefaults/      # Shared configuration
│   ├── Extensions.cs                # ServiceDefaults setup
│   └── MySolution.ServiceDefaults.csproj
├── MySolution.Api/                  # API service
│   ├── Program.cs
│   ├── appsettings.json
│   └── MySolution.Api.csproj
└── MySolution.Web/                  # Web frontend
    ├── Program.cs
    ├── appsettings.json
    └── MySolution.Web.csproj
```

### Project References
- **AppHost**: References all service projects
- **Service Projects**: Reference ServiceDefaults
- **ServiceDefaults**: No references to other projects (only NuGet packages)

## Developer Inner-Loop with `dotnet aspire run`

### Starting the Application

```bash
# From the AppHost directory
cd MySolution.AppHost
dotnet run

# Or using the new command (coming in .NET 9)
dotnet aspire run
```

### What Happens?
1. AppHost starts and reads the application topology
2. Required infrastructure containers are pulled and started (if not running)
3. All services are built and launched
4. Service discovery and configuration injection occurs
5. Aspire Dashboard opens in your browser (typically at `http://localhost:15888`)

### Development Workflow

1. **Make Changes**: Edit your code in any service
2. **Automatic Rebuild**: Aspire detects changes and rebuilds affected services
3. **Quick Restart**: Only modified services restart, not the entire system
4. **Observe**: Use the dashboard to monitor logs, traces, and metrics
5. **Iterate**: Repeat the cycle with fast feedback

### Dashboard Navigation

**Resources Tab:**
- Lists all services and infrastructure
- Shows status (running, stopped, failed)
- Provides quick actions (restart, view logs)

**Logs Tab:**
- Aggregated logs from all resources
- Filtering by resource, log level, and text
- Real-time streaming

**Traces Tab:**
- Distributed tracing visualization
- End-to-end request flow
- Performance bottleneck identification

**Metrics Tab:**
- CPU, memory, and custom metrics
- Time-series graphs
- Per-resource breakdown

## Best Practices

1. **Organize by Feature**: Group related services and resources
2. **Use ServiceDefaults**: Don't duplicate configuration across services
3. **Meaningful Names**: Use clear, descriptive names for resources
4. **Environment Parity**: Keep development close to production
5. **Resource Naming**: Use consistent naming conventions
6. **Configuration Hierarchy**: Leverage the configuration precedence
7. **Secret Management**: Always use user-secrets or parameters for sensitive data

## Common Patterns

### Multi-Service Application
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres").AddDatabase("appdb");
var messaging = builder.AddRabbitMQ("messaging");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(cache)
    .WithReference(db)
    .WithReference(messaging);

var worker = builder.AddProject<Projects.Worker>("worker")
    .WithReference(cache)
    .WithReference(db)
    .WithReference(messaging);

var web = builder.AddProject<Projects.Web>("web")
    .WithReference(api);

builder.Build().Run();
```

### Environment-Specific Configuration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddSqlServer("sql");

if (builder.Environment.IsDevelopment())
{
    db.WithDataVolume(); // Persist data in development
}

var mydb = db.AddDatabase("mydb");

builder.Build().Run();
```

## Troubleshooting

### Services Won't Start
- Check Docker is running
- Verify port conflicts
- Review logs in the dashboard
- Ensure all dependencies are referenced

### Configuration Not Applied
- Check configuration precedence
- Verify user-secrets are initialized
- Confirm environment variable naming
- Review ServiceDefaults registration

### Dashboard Not Opening
- Check if port 15888 is available
- Look for URL in console output
- Try accessing manually: `http://localhost:15888`

## Additional Resources
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [AppHost API Reference](https://learn.microsoft.com/dotnet/api/aspire.hosting)
- [ServiceDefaults Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults)
- [Aspire Samples](https://github.com/dotnet/aspire-samples)

## Next Steps
Proceed to the hands-on exercise where you'll create your first Aspire application and define a system topology with multiple services and infrastructure components.

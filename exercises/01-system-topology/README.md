# Exercise 1: Creating a System Topology with .NET Aspire

## Overview

In this exercise, you'll migrate the existing brownfield ecommerce application to .NET Aspire by creating an AppHost project and setting up the system topology.

## Learning Objectives

By the end of this exercise, you will be able to:
- Create an Aspire AppHost project
- Add existing projects to the Aspire orchestration
- Configure service discovery between services
- Use the Aspire Dashboard for observability
- Understand the benefits of Aspire orchestration

## Prerequisites

- .NET 9.0 SDK or later
- .NET Aspire workload installed (`dotnet workload install aspire`)
- Completed start project (located in `../../start-project`)

## Steps

### Step 1: Install .NET Aspire Workload

First, ensure you have the .NET Aspire workload installed:

```bash
dotnet workload install aspire
```

### Step 2: Create the AppHost Project

Navigate to the start-project directory and create a new Aspire AppHost project:

```bash
cd ../../start-project
dotnet new aspire-apphost -n ECommerce.AppHost
dotnet sln add ECommerce.AppHost/ECommerce.AppHost.csproj
```

This creates an orchestrator project that will manage your application's services.

### Step 3: Create the ServiceDefaults Project

Create a ServiceDefaults project for shared configurations:

```bash
dotnet new aspire-servicedefaults -n ECommerce.ServiceDefaults
dotnet sln add ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj
```

### Step 4: Add Project References to AppHost

Add references to the projects you want to orchestrate:

```bash
cd ECommerce.AppHost
dotnet add reference ../ECommerce.Api/ECommerce.Api.csproj
dotnet add reference ../ECommerce.Web/ECommerce.Web.csproj
```

### Step 5: Configure the AppHost

Open `ECommerce.AppHost/Program.cs` and configure your application topology:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add the API project
var api = builder.AddProject<Projects.ECommerce_Api>("api");

// Add the Web project with a reference to the API
builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

### Step 6: Add ServiceDefaults to Your Projects

Add the ServiceDefaults package to both the API and Web projects:

```bash
cd ../ECommerce.Api
dotnet add reference ../ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj

cd ../ECommerce.Web
dotnet add reference ../ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj
```

### Step 7: Update API Program.cs

Update the API's `Program.cs` to use ServiceDefaults:

```csharp
// Add this at the top
using ECommerce.Api.Services;
using ECommerce.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults (includes OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Rest of your existing code...
```

Add this before `app.Run()`:

```csharp
app.MapDefaultEndpoints();
app.Run();
```

### Step 8: Update Web Program.cs

Update the Web's `Program.cs` similarly:

```csharp
using ECommerce.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults
builder.AddServiceDefaults();

// Update the HttpClient configuration to use service discovery
builder.Services.AddHttpClient<ApiClient>(client => 
{
    // The service name matches what we defined in AppHost
    client.BaseAddress = new Uri("https+http://api");
});

// Rest of your existing code...
```

Update the ApiClient to remove the hardcoded URL:

```csharp
public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
{
    _httpClient = httpClient;
    _logger = logger;
    // BaseAddress is now set through DI configuration
}
```

Add before `app.Run()`:

```csharp
app.MapDefaultEndpoints();
app.Run();
```

### Step 9: Run Your Application with Aspire

Set the AppHost as the startup project and run:

```bash
cd ../ECommerce.AppHost
dotnet run
```

Or if using Visual Studio, set `ECommerce.AppHost` as the startup project and press F5.

### Step 10: Explore the Aspire Dashboard

When the application starts, the Aspire Dashboard will open automatically. Explore:

1. **Resources Tab**: View all running services and their status
2. **Logs Tab**: See aggregated logs from all services
3. **Traces Tab**: View distributed traces across services
4. **Metrics Tab**: Monitor performance metrics

## Verification

1. Open the Aspire Dashboard (typically at `http://localhost:15888`)
2. Verify both `api` and `web` services are running
3. Click on the `web` service endpoint to open the application
4. Navigate to the Products page
5. Verify products are loading (service discovery is working)
6. Check the Traces tab to see the HTTP call from Web to API

## Key Concepts Learned

- **AppHost Project**: The orchestrator that defines your application's topology
- **ServiceDefaults**: Shared configuration for observability, health checks, and service discovery
- **Service Discovery**: Automatic resolution of service URLs using service names
- **Aspire Dashboard**: Centralized dashboard for monitoring and debugging
- **Distributed Tracing**: Automatic tracing across service boundaries

## Common Issues

### Issue: Service discovery not working
**Solution**: Ensure you've:
- Added `builder.AddServiceDefaults()` to both services
- Used the correct service name in the HttpClient BaseAddress (`https+http://api`)
- Added `app.MapDefaultEndpoints()` before `app.Run()`

### Issue: Dashboard not opening
**Solution**: Check the console output for the Dashboard URL, or navigate to `http://localhost:15888`

## Next Steps

Proceed to [Exercise 2: Deploying Your App](../02-deploying-app/README.md) to learn about deployment options.

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Service Discovery in Aspire](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)
- [Aspire Dashboard Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard)

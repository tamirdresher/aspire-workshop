# Exercise 1 Solution - System Topology with .NET Aspire

This is the completed solution for Exercise 1, showing the brownfield ecommerce application migrated to use .NET Aspire orchestration.

## What's Changed

This solution demonstrates the first step in migrating to Aspire:

### Added Projects

1. **ECommerce.AppHost** - Aspire orchestration project
   - Manages all microservices
   - Configures Azure resource emulators (Cosmos DB, Azure Storage)
   - Sets up service discovery
   - Provides the Aspire Dashboard

2. **ECommerce.ServiceDefaults** - Shared Aspire configuration
   - OpenTelemetry for distributed tracing
   - Health checks
   - Service discovery
   - HTTP resilience patterns

### Modified Projects

All API projects (Catalog, Basket, Ordering, AIAssistant) now:
- Reference ServiceDefaults
- Call `builder.AddServiceDefaults()` for automatic telemetry
- Call `app.MapDefaultEndpoints()` for health checks

## Key Benefits

With Aspire orchestration, you now get:

✅ **Single Command Start**: Run all services with `dotnet run --project src/ECommerce.AppHost`
✅ **Automatic Service Discovery**: Services find each other automatically
✅ **Centralized Dashboard**: View all services at https://localhost:15888
✅ **Built-in Observability**: Distributed tracing, metrics, and logs
✅ **Azure Resource Emulators**: Cosmos DB and Azure Storage run locally
✅ **Health Monitoring**: Automatic health checks for all services

## Running the Solution

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop (for emulators)

### Start the Application

```bash
# From the solution directory
dotnet run --project src/ECommerce.AppHost
```

This single command will:
1. Start the Aspire Dashboard
2. Launch Cosmos DB emulator container
3. Launch Azure Storage (Azurite) emulator container
4. Start all 4 microservices
5. Start the React frontend
6. Configure service discovery between all components

### Access the Application

- **Aspire Dashboard**: https://localhost:15888
- **React Frontend**: Will be shown in the dashboard
- **All APIs**: Accessible through the dashboard or service discovery

## What's Different from Start Project

### Before (Start Project)
```csharp
// Manual configuration
var connectionString = configuration["CosmosDb:ConnectionString"];
var cosmosClient = new CosmosClient(connectionString);
```

### After (Exercise 1 Solution)
```csharp
// Aspire AppHost
var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(cosmos);
```

### Service Configuration

**Before**: Each service configured independently with hardcoded URLs
**After**: Services use `builder.AddServiceDefaults()` for automatic configuration

## Architecture

```
ECommerce.AppHost (Orchestrator)
├── Azure Cosmos DB Emulator
├── Azure Storage Emulator (Azurite)
│   ├── Queue Storage
│   └── Blob Storage
├── Catalog.API → Cosmos DB
├── Basket.API → Queue Storage
├── Ordering.API → Queue Storage
├── AIAssistant.API → (OpenAI placeholder)
└── React Frontend → All APIs (via service discovery)
```

## Exploring the Dashboard

Once running, open https://localhost:15888 and explore:

1. **Resources**: See all services and their status
2. **Console Logs**: View aggregated logs from all services
3. **Structured Logs**: Search and filter logs
4. **Traces**: View distributed traces across services
5. **Metrics**: Monitor performance metrics

## Next Steps

Continue to [Exercise 2](../../02-deploying-app/) to learn about deployment.

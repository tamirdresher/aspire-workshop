# Multi-Service Application Example

## What This Example Shows

A complete multi-service Aspire application with:
- **Web Frontend** - Blazor web app
- **API Backend** - Web API with sample endpoints
- **ServiceDefaults** - Shared configuration

This demonstrates service-to-service communication and the full Aspire developer experience.

## Running the Example

```bash
# From this directory
cd MultiService.AppHost
dotnet run
```

The dashboard opens and shows:
- Both services running
- Service dependencies
- Logs from both services
- HTTP endpoints

## Architecture

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │ HTTP
       ↓
┌─────────────┐
│  Web (5000) │──────┐
└─────────────┘      │
                     │ Service Discovery
                     ↓
              ┌─────────────┐
              │ API (5001)  │
              └─────────────┘
```

## Project Structure

```
02-multi-service/
├── MultiService.AppHost/          # Orchestrator
│   ├── Program.cs
│   └── MultiService.AppHost.csproj
├── MultiService.ServiceDefaults/  # Shared config
│   ├── Extensions.cs
│   └── MultiService.ServiceDefaults.csproj
├── MultiService.Api/              # Backend API
│   ├── Program.cs
│   ├── Controllers/
│   └── MultiService.Api.csproj
└── MultiService.Web/              # Frontend
    ├── Program.cs
    ├── Pages/
    └── MultiService.Web.csproj
```

## Key Concepts Demonstrated

1. **Multi-Service Orchestration** - AppHost manages multiple services
2. **Service References** - Web discovers and calls API automatically
3. **ServiceDefaults** - Shared OpenTelemetry and health checks
4. **Automatic Configuration** - No hardcoded URLs needed

## Try It

1. **View the web app** - Click the endpoint in the dashboard
2. **Make API calls** - Web app calls the API automatically
3. **Watch traces** - See requests flow from Web → API in the Traces tab
4. **Check logs** - Aggregated logs from both services

## Modify and Experiment

### Add a New Endpoint

In `MultiService.Api/Program.cs`:

```csharp
app.MapGet("/hello", () => "Hello from API!");
```

### Call It from Web

In `MultiService.Web/Program.cs`:

```csharp
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://api");
});
```

### Watch It in Dashboard

- Restart the app
- Make a request
- See the new trace in the dashboard

## Next Steps

- Add Redis: [Redis Cache Example](../03-redis-cache/)
- Learn about: [ServiceDefaults](../../topics/03-service-defaults.md)

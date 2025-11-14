# Aspire Dashboard

## What is the Aspire Dashboard?

The **Aspire Dashboard** is a web-based UI that provides real-time observability for your distributed application during development. It's your command center for understanding what's happening across all services.

## Automatic Launch

When you run your AppHost:

```bash
cd MyApp.AppHost
dotnet run
```

The dashboard automatically opens at: **`http://localhost:15888`**

## Dashboard Tabs

### 1. Resources Tab

Shows all resources in your application:

**What you see:**
- All services (APIs, web apps, workers)
- Infrastructure components (databases, caches, message brokers)
- Status (Starting, Running, Finished, Failed)
- Resource dependencies
- Endpoints (clickable links)

**Example view:**
```
Resource          | Type      | State   | Endpoints
------------------|-----------|---------|-------------------------
web               | Project   | Running | http://localhost:5000
api               | Project   | Running | http://localhost:5001
cache             | Container | Running | localhost:6379
postgres          | Container | Running | localhost:5432
```

**Actions:**
- Click endpoints to open in browser
- View environment variables
- See startup logs
- Restart resources

### 2. Logs Tab

Aggregated logs from all services in one place.

**Features:**
- **Real-time streaming** - Logs appear as they're generated
- **Filtering** - By resource, level, or text search
- **Color coding** - Different log levels (Info, Warning, Error)
- **Timestamps** - Precise timing information
- **Correlation** - Linked to traces

**Example:**
```
[12:34:56] [api] [Info] Processing order 123
[12:34:56] [api] [Debug] Fetching user data
[12:34:57] [database] [Info] Query executed in 45ms
[12:34:57] [api] [Info] Order 123 completed
```

**Filters:**
```
Resource: [All ▼]
Level: [Information ▼]
Search: [🔍 order 123]
```

### 3. Traces Tab

Distributed tracing across all services.

**What you see:**
- Request flow through multiple services
- Each operation (HTTP call, database query, cache check)
- Duration of each operation
- Parent-child relationships
- Errors and exceptions

**Example trace:**
```
GET /api/orders/123                     [200ms total]
  ├─ HTTP GET /api/orders/123 (api)    [180ms]
  │  ├─ Database Query (postgres)      [45ms]
  │  ├─ Cache Check (redis)            [5ms]
  │  └─ HTTP GET /api/users/456 (api)  [80ms]
  │     └─ Database Query (postgres)   [30ms]
  └─ Response                           [200 OK]
```

**Features:**
- Click a trace to see details
- View timing breakdown
- See all logs for that request
- Identify slow operations
- Debug failures

### 4. Metrics Tab

Performance metrics from all services.

**Built-in metrics:**
- HTTP request rate
- HTTP request duration
- HTTP error rate
- Memory usage
- CPU usage
- Garbage collection stats
- ThreadPool statistics

**Views:**
- Graphs over time
- Multiple metrics on one chart
- Per-service breakdowns
- Custom metrics from your code

**Example:**
```
[Graph showing HTTP requests/second over last 5 minutes]
api:  █████████████ (avg: 150 req/s)
web:  ████ (avg: 45 req/s)
```

## Key Features

### 1. Resource Dependencies

See how resources relate to each other:

```
web
 └─► api
      ├─► cache (redis)
      ├─► database (postgres)
      └─► messaging (rabbitmq)
```

### 2. Environment Variables

View all environment variables for each resource:

```
Resource: api
Environment:
  ASPNETCORE_ENVIRONMENT = Development
  ConnectionStrings__cache = localhost:6379
  ConnectionStrings__database = Server=localhost;...
  OTEL_EXPORTER_OTLP_ENDPOINT = http://localhost:4317
```

### 3. Structured Logs

Logs are structured and searchable:

```json
{
  "Timestamp": "2024-01-15T12:34:56Z",
  "Level": "Information",
  "Message": "Processing order",
  "OrderId": 123,
  "UserId": 456,
  "TraceId": "abc123",
  "SpanId": "def456"
}
```

Search by any field!

### 4. Trace Correlation

Click a log entry to see:
- The full trace for that request
- All related logs
- Performance breakdown

### 5. Real-Time Updates

Everything updates live:
- New logs appear immediately
- Metrics update every few seconds
- Resource status changes reflected instantly

## Using the Dashboard for Debugging

### Scenario 1: Slow Request

**Problem:** API is slow

**Debug steps:**
1. Go to **Traces** tab
2. Find slow traces (sort by duration)
3. Click the trace to see breakdown
4. Identify the slow operation (database query? external API?)
5. Fix the bottleneck

**Example:**
```
Total: 5000ms
  ├─ HTTP call: 10ms
  ├─ Database query: 4950ms ⚠️ SLOW!
  └─ Processing: 40ms
```

### Scenario 2: Service Crash

**Problem:** Service keeps failing

**Debug steps:**
1. Go to **Resources** tab
2. See which service is failing
3. Click "View Logs"
4. Look for error messages
5. Check **Traces** for failed requests

**Example log:**
```
[12:34:56] [api] [Error] Unhandled exception
   NullReferenceException: Object reference not set
   at MyApi.Controllers.OrdersController.Get(Int32 id)
```

### Scenario 3: Missing Configuration

**Problem:** Service can't connect to database

**Debug steps:**
1. Go to **Resources** tab
2. Click on the failing service
3. View **Environment Variables**
4. Check if connection string is present and correct

### Scenario 4: Understanding Request Flow

**Problem:** Want to understand how a request flows

**Debug steps:**
1. Make a request to your app
2. Go to **Traces** tab
3. Find your request (search by URL or time)
4. Click to see the full trace
5. See every service call, database query, cache check

## Configuration

### Change Dashboard Port

In AppHost `appsettings.json`:

```json
{
  "ASPIRE_DASHBOARD_PORT": "18888"
}
```

Dashboard will now be at `http://localhost:18888`

### Disable Auto-Open

```json
{
  "ASPIRE_DASHBOARD_AUTO_OPEN": "false"
}
```

### Authentication

For production-like scenarios:

```json
{
  "ASPIRE_DASHBOARD_AUTHENTICATION": "OpenIdConnect"
}
```

## Standalone Dashboard

You can run the dashboard standalone (without AppHost) to observe any .NET application.

### Install

```bash
dotnet tool install -g Aspire.Hosting.Dashboard
```

### Run

```bash
aspire-dashboard
```

Then configure your apps to send telemetry to it.

**Learn more:** [Standalone Dashboard Mode](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone)

## Tips and Tricks

### 1. Filter Logs Efficiently

```
# Filter by resource
Resource: api

# Filter by level
Level: Warning

# Search text
🔍 exception

# Combine filters
Resource: api, Level: Error, Search: "database"
```

### 2. Use Trace Search

```
# Search by HTTP method
GET

# Search by status code
500

# Search by duration
>1000ms

# Search by service
api
```

### 3. Export Traces

Click "Export" to save traces as JSON for:
- Sharing with team
- Further analysis
- Bug reports

### 4. Custom Metrics

Add your own metrics in code:

```csharp
var meter = new Meter("MyApp.Orders");
var orderCounter = meter.CreateCounter<int>("orders_processed");

// Increment when processing an order
orderCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
```

These appear in the **Metrics** tab!

### 5. Structured Logging

Use structured logging for better searchability:

```csharp
logger.LogInformation(
    "Processing order {OrderId} for user {UserId}",
    orderId, userId);
```

Now you can search by `OrderId` or `UserId` in the dashboard!

## Dashboard Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `/` | Focus search |
| `r` | Refresh |
| `Esc` | Close details |
| `←` `→` | Navigate tabs |

## Common Issues

### Dashboard Won't Open

**Check:**
1. Port 15888 not in use: `netstat -an | grep 15888`
2. AppHost is running
3. No firewall blocking localhost

### Logs Not Appearing

**Check:**
1. Service has `AddServiceDefaults()` called
2. OTEL_EXPORTER_OTLP_ENDPOINT is set (automatic with Aspire)
3. Service is actually writing logs

### Traces Not Showing

**Check:**
1. ServiceDefaults included
2. OpenTelemetry configured
3. HTTP calls are being made (traces need activity)

## Next Steps

- **Next Topic:** [Service Discovery](./06-service-discovery.md)
- **Try It:** Run any example and explore the dashboard
- **Official Docs:** [Dashboard Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview)

## Further Reading

- [Dashboard Features](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/explore)
- [Standalone Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone)
- [Dashboard Configuration](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/configuration)
- [OpenTelemetry in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/telemetry)

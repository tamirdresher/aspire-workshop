# OpenTelemetry in Aspire

## What is OpenTelemetry?

**OpenTelemetry (OTel)** is an open-source observability framework for collecting traces, metrics, and logs from your applications. It's the industry standard for observability.

**In Aspire:** OpenTelemetry is built-in and automatically configured through ServiceDefaults.

## The Three Pillars of Observability

### 1. Traces (What Happened?)

Distributed tracing shows the path of a request through your system:

```
User Request → Web → API → Database
                 │
                 └─→ Cache
```

**What you see:**
- Which services were called
- How long each operation took
- Parent-child relationships
- Errors and exceptions

**Example trace:**
```
GET /orders/123                          [250ms total]
  ├─ HTTP GET /api/orders/123 (API)     [200ms]
  │  ├─ SELECT FROM orders (Database)   [120ms]
  │  ├─ GET user:456 (Redis)            [10ms]
  │  └─ HTTP GET /api/users/456 (API)   [50ms]
  │     └─ SELECT FROM users (Database) [30ms]
  └─ Render response                     [50ms]
```

### 2. Metrics (How Much?)

Quantitative measurements over time:

- **Counters** - How many times? (requests, orders, errors)
- **Gauges** - What's the current value? (memory, CPU, queue length)
- **Histograms** - Distribution of values? (request duration, response sizes)

**Examples:**
```
http_requests_total: 15,234
http_request_duration_ms (p50): 45ms
http_request_duration_ms (p95): 180ms
memory_usage_bytes: 256MB
active_connections: 42
```

### 3. Logs (What Details?)

Structured log entries with context:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Processing order",
  "orderId": 123,
  "userId": 456,
  "traceId": "abc123",  // Links to trace!
  "spanId": "def456"
}
```

**Key feature:** Logs are automatically correlated with traces.

## Automatic Instrumentation in Aspire

When you add ServiceDefaults, you get automatic instrumentation for:

### HTTP Calls
```csharp
// Automatically traced
var response = await httpClient.GetAsync("http://api/orders");
```

**Captured automatically:**
- Request method (GET, POST, etc.)
- URL
- Status code (200, 404, etc.)
- Duration
- Headers (selective)

### Database Queries
```csharp
// Automatically traced
var orders = await dbContext.Orders.Where(o => o.UserId == userId).ToListAsync();
```

**Captured automatically:**
- SQL command
- Database name
- Duration
- Connection info
- Parameters (sanitized)

### Cache Operations
```csharp
// Automatically traced
var value = await cache.StringGetAsync("key");
```

**Captured automatically:**
- Command (GET, SET, etc.)
- Key
- Duration
- Server info

## Built-in Metrics

ServiceDefaults configures these metrics automatically:

### ASP.NET Core Metrics
- `http.server.request.duration` - Request duration histogram
- `http.server.active_requests` - Current active requests
- `http.server.request.body.size` - Request body sizes
- `http.server.response.body.size` - Response body sizes

### HTTP Client Metrics
- `http.client.request.duration` - Outgoing request duration
- `http.client.active_requests` - Concurrent outgoing requests

### .NET Runtime Metrics
- `process.runtime.dotnet.gc.collections.count` - GC collections
- `process.runtime.dotnet.gc.heap.size` - Heap size
- `process.runtime.dotnet.threadpool.thread.count` - Thread pool threads
- `process.runtime.dotnet.monitor.lock_contention.count` - Lock contentions

### Custom Metrics (Your App)
```csharp
var meter = new Meter("MyApp.Orders");
var orderCounter = meter.CreateCounter<int>("orders_processed");
var orderDuration = meter.CreateHistogram<double>("order_duration_ms");

// Increment counter
orderCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));

// Record duration
orderDuration.Record(duration.TotalMilliseconds);
```

## Viewing in Aspire Dashboard

### Traces Tab

**Features:**
- Search by URL, service, status code
- Filter by duration (find slow requests)
- Sort by time or duration
- Click to see detailed waterfall view

**Use cases:**
- Find slow operations
- Debug errors
- Understand request flow
- Identify bottlenecks

### Metrics Tab

**Features:**
- Graph any metric over time
- Multiple metrics on one chart
- Filter by dimensions
- Zoom and pan

**Use cases:**
- Monitor performance trends
- Detect anomalies
- Compare services
- Capacity planning

### Logs Tab

**Features:**
- Real-time streaming
- Filter by service, level, or text
- Correlated with traces
- Structured log fields

**Use cases:**
- Debug specific issues
- Monitor errors
- Audit activity
- Troubleshoot problems

## Configuration

### OpenTelemetry Endpoint

Aspire automatically configures the endpoint:

```bash
# Environment variable set by AppHost
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

### Sampling

Control which traces are collected:

```csharp
// In ServiceDefaults
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetSampler(new TraceIdRatioBasedSampler(0.1));  // 10% sampling
    });
```

**Sampling strategies:**
- **Always on** - Record everything (development)
- **Always off** - Record nothing
- **Ratio-based** - Record X% of traces
- **Parent-based** - Follow parent's decision

### Resource Attributes

Add identifying information:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("my-api", "1.0.0");
        resource.AddAttributes(new[]
        {
            new KeyValuePair<string, object>("deployment.environment", "production"),
            new KeyValuePair<string, object>("service.team", "platform")
        });
    });
```

## Exporting to Production Systems

### Application Insights (Azure)

```csharp
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = "InstrumentationKey=...";
    });
```

### Other Exporters

```csharp
// Jaeger
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddJaegerExporter());

// Prometheus
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddPrometheusExporter());

// Console (debugging)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddConsoleExporter());
```

## Best Practices

### 1. Use Structured Logging

```csharp
// ✅ Good - structured
logger.LogInformation("Order {OrderId} processed for user {UserId}", orderId, userId);

// ❌ Bad - string concatenation
logger.LogInformation($"Order {orderId} processed for user {userId}");
```

### 2. Add Business Context

```csharp
using var activity = Activity.Current;
activity?.SetTag("order.id", orderId);
activity?.SetTag("user.id", userId);
activity?.SetTag("order.total", orderTotal);
```

### 3. Record Meaningful Metrics

```csharp
// ✅ Good - actionable metrics
orderCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
orderDuration.Record(duration.TotalMilliseconds, 
    new KeyValuePair<string, object?>("payment_method", "credit_card"));

// ❌ Bad - too granular or not useful
debugCounter.Add(1);
```

### 4. Don't Over-Instrument

```csharp
// ❌ Bad - too many custom spans
using var span1 = Activity.Current;
using var span2 = Activity.Current;  // Unnecessary
using var span3 = Activity.Current;  // Unnecessary
```

Automatic instrumentation handles most cases!

### 5. Use Log Levels Appropriately

```csharp
logger.LogDebug("Detailed debugging info");        // Verbose
logger.LogInformation("Order processed");          // Normal
logger.LogWarning("Retry failed, will try again"); // Attention
logger.LogError(ex, "Failed to process order");    // Error
logger.LogCritical("Database unavailable");        // Critical
```

## Troubleshooting

### Traces Not Appearing

**Check:**
1. ServiceDefaults added? `builder.AddServiceDefaults()`
2. OTEL_EXPORTER_OTLP_ENDPOINT set?
3. HTTP calls actually being made?
4. Dashboard running?

### Metrics Not Showing

**Check:**
1. Metric name correct?
2. Is activity happening? (metrics need data)
3. Check Metrics tab filters
4. Refresh dashboard

### High Overhead

**Solutions:**
1. Reduce sampling rate
2. Disable verbose logs in production
3. Limit custom spans
4. Use async logging

## Next Steps

- **Next Topic:** [Advanced Observability](./02-advanced-observability.md)
- **Try Example:** [Custom Metrics](../examples/01-custom-metrics/)
- **Official Docs:** [OpenTelemetry in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/telemetry)

## Further Reading

- [OpenTelemetry Official Site](https://opentelemetry.io/)
- [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Semantic Conventions](https://opentelemetry.io/docs/concepts/semantic-conventions/)

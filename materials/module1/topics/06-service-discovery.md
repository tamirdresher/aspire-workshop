# Service Discovery

## What is Service Discovery?

**Service Discovery** is the automatic process of locating services in a distributed application. Instead of hardcoding URLs, services find each other dynamically.

## The Problem

### Without Service Discovery

```csharp
// ❌ Hardcoded URLs - breaks in different environments
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
});
```

**Problems:**
- Different ports in dev, test, prod
- Doesn't work in containers
- Doesn't work in cloud
- Can't run multiple instances
- Hard to configure

### With Service Discovery

```csharp
// ✅ Service discovery - works everywhere
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://api");
});
```

**Benefits:**
- Works in any environment
- Scales to multiple instances
- Automatic load balancing
- No configuration needed

## How It Works in Aspire

### Step 1: Define in AppHost

```csharp
var api = builder.AddProject<Projects.MyApi>("api");

var web = builder.AddProject<Projects.MyWeb>("web")
    .WithReference(api);  // Creates service discovery entry
```

### Step 2: ServiceDefaults Enables Discovery

```csharp
// In both services
builder.AddServiceDefaults();  // Includes service discovery
```

### Step 3: Use Service Name

```csharp
// In web service
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // "api" resolves to actual endpoint
    client.BaseAddress = new Uri("http://api");
});
```

### Step 4: Magic!

When web calls API:
1. "http://api" is resolved
2. Actual endpoint is looked up (http://localhost:5001)
3. Request is sent to the real address
4. It just works!

## Service Names

The name you use in `AddProject` becomes the service name:

```csharp
// Name is "api"
var api = builder.AddProject<Projects.MyApi>("api");

// Name is "orders-api"
var ordersApi = builder.AddProject<Projects.OrdersApi>("orders-api");

// Name is "users-svc"
var usersSvc = builder.AddProject<Projects.UsersService>("users-svc");
```

Use these names in your code:
- `http://api`
- `http://orders-api`
- `http://users-svc`

## Using Service Discovery

### HTTP Clients

```csharp
// Register HTTP client
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    client.BaseAddress = new Uri("http://api");
});

// Use in your code
public class MyService
{
    private readonly MyApiClient _apiClient;

    public MyService(MyApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Order> GetOrderAsync(int id)
    {
        // Service discovery resolves "api" automatically
        return await _apiClient.GetOrderAsync(id);
    }
}
```

### Direct HTTP Calls

```csharp
public class OrderService
{
    private readonly HttpClient _httpClient;

    public OrderService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("http://api");
    }

    public async Task<string> GetOrderStatusAsync(int orderId)
    {
        // Resolves to actual endpoint
        var response = await _httpClient.GetAsync($"/orders/{orderId}/status");
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Specific Endpoints

Services can have multiple endpoints:

```csharp
// In AppHost
var api = builder.AddProject<Projects.MyApi>("api")
    .WithHttpEndpoint(port: 5001, name: "http")
    .WithHttpsEndpoint(port: 5002, name: "https");
```

Access specific endpoints:

```csharp
// HTTP endpoint
client.BaseAddress = new Uri("http://api");

// HTTPS endpoint (if configured)
client.BaseAddress = new Uri("https://api");
```

## Connection String Injection

Infrastructure resources also use service discovery via connection strings.

### Database Example

```csharp
// In AppHost
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("mydb");

var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(db);  // Injects connection string
```

**What happens:**
- Environment variable is set: `ConnectionStrings__mydb`
- Connection string contains actual endpoint
- Service can access it via configuration

```csharp
// In API service
var connectionString = builder.Configuration.GetConnectionString("mydb");
// Returns: Server=localhost;Port=5432;Database=mydb;...

// Or use Aspire component (recommended)
builder.AddNpgsqlDbContext<MyDbContext>("mydb");
```

### Cache Example

```csharp
// In AppHost
var cache = builder.AddRedis("cache");

var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache);
```

```csharp
// In API service
builder.AddRedisClient("cache");  // Connection string injected automatically

// Use in code
public class CacheService
{
    private readonly IConnectionMultiplexer _redis;

    public CacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;  // Already connected!
    }
}
```

## Environment-Specific Resolution

Service discovery adapts to different environments:

### Development (Local)
```
http://api → http://localhost:5001
```

### Docker Compose
```
http://api → http://api:8080
```

### Kubernetes
```
http://api → http://api.default.svc.cluster.local
```

### Azure Container Apps
```
http://api → http://api.internal.xxx.azurecontainerapps.io
```

**You write the same code everywhere!**

## Load Balancing

When you run multiple instances:

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReplicas(3);  // 3 instances
```

Service discovery automatically load balances:
```
http://api → Round-robin between 3 instances
  Instance 1: http://localhost:5001
  Instance 2: http://localhost:5002
  Instance 3: http://localhost:5003
```

## Advanced Patterns

### Named HTTP Clients

```csharp
// Register multiple clients
builder.Services.AddHttpClient("orders-api", client =>
{
    client.BaseAddress = new Uri("http://orders-api");
});

builder.Services.AddHttpClient("users-api", client =>
{
    client.BaseAddress = new Uri("http://users-api");
});

// Use in code
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task DoWorkAsync()
    {
        var ordersClient = _httpClientFactory.CreateClient("orders-api");
        var usersClient = _httpClientFactory.CreateClient("users-api");

        // Both use service discovery
        var orders = await ordersClient.GetAsync("/orders");
        var users = await usersClient.GetAsync("/users");
    }
}
```

### Typed HTTP Clients

```csharp
// Strongly-typed client
public class OrdersApiClient
{
    private readonly HttpClient _httpClient;

    public OrdersApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Order> GetOrderAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Order>($"/orders/{id}");
    }

    public async Task CreateOrderAsync(Order order)
    {
        await _httpClient.PostAsJsonAsync("/orders", order);
    }
}

// Register with service discovery
builder.Services.AddHttpClient<OrdersApiClient>(client =>
{
    client.BaseAddress = new Uri("http://orders-api");
});
```

### Conditional References

```csharp
// Development: use real API
// Production: use mock or different API

if (builder.Environment.IsDevelopment())
{
    var api = builder.AddProject<Projects.MyApi>("api");
    var web = builder.AddProject<Projects.MyWeb>("web")
        .WithReference(api);
}
else
{
    var externalApi = builder.AddConnectionString("api");
    var web = builder.AddProject<Projects.MyWeb>("web")
        .WithEnvironment("Services__api", externalApi);
}
```

## Best Practices

### 1. Always Use Service Names

```csharp
// ✅ Good
client.BaseAddress = new Uri("http://api");

// ❌ Bad
client.BaseAddress = new Uri("http://localhost:5001");
```

### 2. Use Descriptive Names

```csharp
// ✅ Good - clear purpose
var ordersApi = builder.AddProject<Projects.OrdersApi>("orders-api");
var usersApi = builder.AddProject<Projects.UsersApi>("users-api");

// ❌ Bad - unclear
var api1 = builder.AddProject<Projects.Api1>("api1");
var svc = builder.AddProject<Projects.Service>("svc");
```

### 3. Use WithReference for Dependencies

```csharp
// ✅ Good - explicit dependency
var api = builder.AddProject<Projects.Api>("api");
var web = builder.AddProject<Projects.Web>("web")
    .WithReference(api);

// ❌ Bad - manual configuration
var web = builder.AddProject<Projects.Web>("web")
    .WithEnvironment("ApiUrl", "http://localhost:5001");
```

### 4. Use Aspire Components

```csharp
// ✅ Good - Aspire component handles discovery
builder.AddRedisClient("cache");
builder.AddNpgsqlDbContext<MyDbContext>("database");

// ❌ Bad - manual connection string
var connStr = builder.Configuration["ConnectionStrings:cache"];
builder.Services.AddSingleton(ConnectionMultiplexer.Connect(connStr));
```

## Troubleshooting

### Service Not Found

**Error:** `Unable to resolve service 'api'`

**Causes:**
1. Service name mismatch
2. No `WithReference` in AppHost
3. ServiceDefaults not added

**Fix:**
```csharp
// In AppHost - ensure names match
var api = builder.AddProject<Projects.MyApi>("api");
var web = builder.AddProject<Projects.MyWeb>("web")
    .WithReference(api);  // Don't forget this!

// In both services
builder.AddServiceDefaults();  // Required!
```

### Connection Refused

**Error:** `Connection refused to localhost:5001`

**Causes:**
1. Service not started
2. Wrong port
3. Service crashed

**Check:**
1. Dashboard Resources tab - is service running?
2. Dashboard Logs tab - any errors?
3. Correct service name in code?

### Wrong Endpoint

**Symptoms:** Calls go to wrong service

**Causes:**
1. Service name typo
2. Multiple services with similar names

**Fix:**
```csharp
// Verify exact names in AppHost
var ordersApi = builder.AddProject<Projects.OrdersApi>("orders-api");
var productsApi = builder.AddProject<Projects.ProductsApi>("products-api");

// Use exact names in code
client.BaseAddress = new Uri("http://orders-api");  // Not "orders" or "ordersapi"
```

## Complete Example

**AppHost:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres").AddDatabase("mydb");

// Services
var ordersApi = builder.AddProject<Projects.OrdersApi>("orders-api")
    .WithReference(db)
    .WithReference(cache);

var productsApi = builder.AddProject<Projects.ProductsApi>("products-api")
    .WithReference(db)
    .WithReference(cache);

var web = builder.AddProject<Projects.Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(ordersApi)
    .WithReference(productsApi);

builder.Build().Run();
```

**Web Service:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();  // Enables service discovery

// Register API clients with service discovery
builder.Services.AddHttpClient<OrdersApiClient>(client =>
{
    client.BaseAddress = new Uri("http://orders-api");
});

builder.Services.AddHttpClient<ProductsApiClient>(client =>
{
    client.BaseAddress = new Uri("http://products-api");
});

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
```

**Usage in Controller:**
```csharp
public class ShoppingController : ControllerBase
{
    private readonly OrdersApiClient _ordersClient;
    private readonly ProductsApiClient _productsClient;

    public ShoppingController(
        OrdersApiClient ordersClient,
        ProductsApiClient productsClient)
    {
        _ordersClient = ordersClient;
        _productsClient = productsClient;
    }

    [HttpGet("cart")]
    public async Task<CartViewModel> GetCartAsync()
    {
        // Both clients use service discovery automatically
        var orders = await _ordersClient.GetOrdersAsync();
        var products = await _productsClient.GetProductsAsync();

        return new CartViewModel(orders, products);
    }
}
```

## Next Steps

- **Complete Module:** You've learned all core concepts!
- **Practice:** [Complete System Example](../examples/05-complete-system/)
- **Lab:** [Guided Lab: Task Manager](../exercises/lab-task-manager.md)

## Official Documentation

- [Service Discovery Overview](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)
- [Networking in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/networking-overview)
- [HTTP Client Configuration](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)

# Aspire Resource Model

## Understanding Resources

In Aspire, everything in your application is a **resource**:
- Services (.NET projects)
- Containers (Redis, PostgreSQL, etc.)
- Executables (npm, Python scripts)
- Cloud resources (Azure Storage, etc.)

## The IResource Interface

All resources implement `IResource`:

```csharp
public interface IResource
{
    string Name { get; }
    ResourceAnnotationCollection Annotations { get; }
}
```

**Key properties:**
- **Name** - Unique identifier (e.g., "api", "cache", "database")
- **Annotations** - Metadata about the resource

## Resource Types

### 1. Project Resources

.NET projects in your solution:

```csharp
public interface IProjectMetadata
{
    string ProjectPath { get; }
}

// Usage
var api = builder.AddProject<Projects.MyApi>("api");
```

### 2. Container Resources

Docker containers:

```csharp
public interface IResourceWithConnectionString
{
    string? ConnectionStringExpression { get; }
}

// Usage
var redis = builder.AddRedis("cache");
var postgres = builder.AddPostgres("db");
```

### 3. Executable Resources

External processes:

```csharp
public interface IResourceWithCommand
{
    string Command { get; }
    string? WorkingDirectory { get; }
    IEnumerable<string> Args { get; }
}

// Usage
var npm = builder.AddExecutable("frontend", "npm", ".")
    .WithArgs("run", "dev");
```

### 4. Cloud Resources

Azure/cloud services:

```csharp
// Azure Storage
var storage = builder.AddAzureStorage("storage");

// Azure Service Bus
var serviceBus = builder.AddAzureServiceBus("messaging");
```

## Resource Lifecycle

### 1. Definition Phase

```csharp
// Resources are defined in AppHost
var cache = builder.AddRedis("cache");
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache);
```

### 2. Dependency Resolution

Aspire analyzes `.WithReference()` calls to build a dependency graph:

```
cache (no dependencies)
  └─► api (depends on cache)
```

### 3. Startup Phase

Resources start in dependency order:
1. Infrastructure (databases, caches) starts first
2. Services start after their dependencies are ready
3. Web frontends start last

### 4. Running Phase

- Resources are monitored for health
- Logs are collected
- Metrics are gathered
- Failures trigger restarts (if configured)

### 5. Shutdown Phase

Graceful shutdown in reverse dependency order:
1. Frontends stop first
2. Services stop
3. Infrastructure stops last

## Annotations

Annotations add metadata to resources.

### Common Annotations

#### Endpoint Annotations

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithHttpEndpoint(port: 5001, name: "http");

// Creates EndpointAnnotation
```

#### Environment Annotations

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("LogLevel", "Debug");

// Creates EnvironmentCallbackAnnotation
```

#### Connection String Annotations

```csharp
var db = postgres.AddDatabase("mydb");

// Creates ConnectionStringReferenceAnnotation
```

### Custom Annotations

You can create your own:

```csharp
public class CustomAnnotation : IResourceAnnotation
{
    public string CustomProperty { get; set; } = "";
}

// Add to resource
var api = builder.AddProject<Projects.MyApi>("api");
api.Annotations.Add(new CustomAnnotation { CustomProperty = "value" });

// Read annotation
var annotation = api.Annotations.OfType<CustomAnnotation>().FirstOrDefault();
```

## Resource Builders

Resources are created by **builder methods**:

```csharp
public static IResourceBuilder<RedisResource> AddRedis(
    this IDistributedApplicationBuilder builder,
    string name,
    int? port = null)
{
    var redis = new RedisResource(name);
    
    return builder.AddResource(redis)
        .WithImage("redis")
        .WithImageRegistry("docker.io")
        .WithImageTag("latest")
        .WithEndpoint(port: port ?? 6379, name: "tcp");
}
```

### IResourceBuilder<T>

The builder provides a fluent API:

```csharp
public interface IResourceBuilder<T> where T : IResource
{
    string Name { get; }
    T Resource { get; }
    IDistributedApplicationBuilder ApplicationBuilder { get; }
}
```

**Common operations:**
- `WithReference()` - Add dependencies
- `WithEnvironment()` - Set environment variables
- `WithEndpoint()` - Define network endpoints
- `WithAnnotation()` - Add custom metadata

## Resource References

### What is a Reference?

A reference creates a dependency and provides configuration:

```csharp
var cache = builder.AddRedis("cache");
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache);  // Reference!
```

**What happens:**
1. **Dependency** - cache must start before api
2. **Connection string** - Injected as `ConnectionStrings__cache`
3. **Service discovery** - api can resolve "cache"

### Reference Implementation

```csharp
public static IResourceBuilder<T> WithReference<T>(
    this IResourceBuilder<T> builder,
    IResourceBuilder<IResourceWithConnectionString> resource)
{
    return builder.WithEnvironment(
        $"ConnectionStrings__{resource.Resource.Name}",
        resource.Resource.ConnectionStringExpression);
}
```

## Resource State

Resources have different states:

### Starting
Resource is being launched.

### Running
Resource is operational.

### Finished
Resource completed successfully.

### Failed
Resource encountered an error.

### Hidden
Resource exists but isn't shown in dashboard.

## Creating Custom Resources

### Step 1: Define Resource Class

```csharp
public class MongoDbResource : ContainerResource, IResourceWithConnectionString
{
    public MongoDbResource(string name) : base(name)
    {
    }

    public string? ConnectionStringExpression =>
        $"mongodb://{{{Name}.bindings.tcp.host}}:{{{Name}.bindings.tcp.port}}";
}
```

### Step 2: Create Builder Extension

```csharp
public static class MongoDbResourceBuilderExtensions
{
    public static IResourceBuilder<MongoDbResource> AddMongoDb(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var mongodb = new MongoDbResource(name);
        
        return builder.AddResource(mongodb)
            .WithImage("mongo")
            .WithImageTag("latest")
            .WithEndpoint(port: port ?? 27017, targetPort: 27017, name: "tcp");
    }

    public static IResourceBuilder<MongoDbResource> WithDataVolume(
        this IResourceBuilder<MongoDbResource> builder)
    {
        return builder.WithVolume("mongodb-data", "/data/db");
    }
}
```

### Step 3: Use It

```csharp
var mongodb = builder.AddMongoDb("mongodb")
    .WithDataVolume();

var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(mongodb);
```

## Best Practices

### 1. Implement Appropriate Interfaces

```csharp
// ✅ Good - implements connection string interface
public class MyResource : ContainerResource, IResourceWithConnectionString
{
    public string ConnectionStringExpression => "...";
}

// ❌ Bad - no connection string interface
public class MyResource : ContainerResource
{
}
```

### 2. Use Descriptive Names

```csharp
// ✅ Good
public class ElasticsearchResource : ContainerResource { }

// ❌ Bad
public class ESRes : ContainerResource { }
```

### 3. Follow Naming Conventions

```csharp
// ✅ Good - consistent naming
public static IResourceBuilder<RedisResource> AddRedis(...)
public static IResourceBuilder<PostgresServerResource> AddPostgres(...)

// ❌ Bad - inconsistent
public static IResourceBuilder<RedisResource> CreateRedis(...)
public static IResourceBuilder<PostgresServerResource> MakePostgres(...)
```

### 4. Support Configuration

```csharp
// ✅ Good - configurable
public static IResourceBuilder<MyResource> AddMyService(
    this IDistributedApplicationBuilder builder,
    string name,
    int? port = null,
    string? version = null)
{
    // ...
}

// ❌ Bad - hardcoded
public static IResourceBuilder<MyResource> AddMyService(
    this IDistributedApplicationBuilder builder)
{
    // Always uses port 8080, version "latest"
}
```

## Next Steps

- **Next Topic:** [Custom Hosting Integrations](./02-custom-hosting-integrations.md)
- **Try Example:** [Custom Container Resource](../examples/01-custom-container/)
- **Official Docs:** [Resource Annotations](https://learn.microsoft.com/dotnet/aspire/fundamentals/annotations-overview)

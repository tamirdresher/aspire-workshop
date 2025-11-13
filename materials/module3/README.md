# Module 3: Aspire Extensibility

## Overview
This module explores the extensibility mechanisms of .NET Aspire, enabling you to create custom resources, understand the resource lifecycle, and test your Aspire applications. You'll learn how to extend Aspire beyond its built-in capabilities to meet specific requirements.

## Learning Objectives
By the end of this module, you will:
- Understand Aspire's resource structure and lifecycle
- Create custom resource types for specialized infrastructure
- Implement custom resource builders and extensions
- Write unit tests for Aspire applications
- Apply best practices for extensible Aspire solutions

## Prerequisites
- Completed Modules 1 and 2
- Understanding of C# generics and extension methods
- Familiarity with the builder pattern
- Basic knowledge of dependency injection

## Aspire Resources Structure and Lifecycle

### Resource Anatomy

Every Aspire resource implements core interfaces that define its behavior:

```csharp
public interface IResource
{
    string Name { get; }
    ResourceAnnotation[] Annotations { get; }
}

public interface IResourceWithConnectionString : IResource
{
    string? ConnectionStringExpression { get; }
}

public interface IResourceWithEndpoints : IResource
{
    IEnumerable<EndpointAnnotation> GetEndpoints();
}
```

### Resource Types

#### 1. Container Resources
Resources that run as Docker containers:

```csharp
public interface IContainerResource : IResource
{
    string ImageName { get; }
    string? ImageTag { get; }
}
```

#### 2. Project Resources
.NET projects that compile and run:

```csharp
public interface IProjectResource : IResource
{
    string ProjectPath { get; }
}
```

#### 3. Executable Resources
Arbitrary executables:

```csharp
public interface IExecutableResource : IResource
{
    string Command { get; }
    string WorkingDirectory { get; }
}
```

### Resource Lifecycle

Resources go through several phases:

1. **Definition**: Resources are defined in the AppHost
2. **Validation**: Aspire validates the resource configuration
3. **Preparation**: Dependencies are resolved, containers pulled
4. **Startup**: Resources start in dependency order
5. **Running**: Resources are monitored and observable
6. **Shutdown**: Graceful shutdown when stopped

### Annotations

Annotations attach metadata and behavior to resources:

```csharp
public abstract class ResourceAnnotation
{
    // Base class for all annotations
}

// Common annotations
public class EndpointAnnotation : ResourceAnnotation
{
    public string Name { get; }
    public int? Port { get; }
    public string Protocol { get; }
    public string Scheme { get; }
}

public class EnvironmentVariableAnnotation : ResourceAnnotation
{
    public string Name { get; }
    public string Value { get; }
}

public class ContainerImageAnnotation : ResourceAnnotation
{
    public string Registry { get; }
    public string Image { get; }
    public string Tag { get; }
}
```

## Creating Custom Resources

### Scenario 1: Custom MongoDB with Replica Set

Create a MongoDB resource that configures a replica set automatically.

#### Step 1: Define the Resource

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace CustomResources;

public interface IMongoDBReplicaSetResource : IResourceWithConnectionString
{
    string ReplicaSetName { get; }
    int ReplicaCount { get; }
}

internal class MongoDBReplicaSetResource : ContainerResource, IMongoDBReplicaSetResource
{
    public MongoDBReplicaSetResource(string name, string replicaSetName, int replicaCount)
        : base(name)
    {
        ReplicaSetName = replicaSetName;
        ReplicaCount = replicaCount;
    }

    public string ReplicaSetName { get; }
    public int ReplicaCount { get; }
    
    public string? ConnectionStringExpression =>
        $"mongodb://localhost:{PrimaryEndpoint?.Port}/?replicaSet={ReplicaSetName}";

    private EndpointAnnotation? PrimaryEndpoint =>
        Annotations.OfType<EndpointAnnotation>().FirstOrDefault(e => e.Name == "primary");
}
```

#### Step 2: Create Resource Builder

```csharp
public class MongoDBReplicaSetResourceBuilder : 
    IResourceBuilder<IMongoDBReplicaSetResource>
{
    private readonly IResourceBuilder<MongoDBReplicaSetResource> _inner;

    public MongoDBReplicaSetResourceBuilder(
        IResourceBuilder<MongoDBReplicaSetResource> inner)
    {
        _inner = inner;
    }

    public IMongoDBReplicaSetResource Resource => _inner.Resource;
    public IDistributedApplicationBuilder ApplicationBuilder => _inner.ApplicationBuilder;

    public IResourceBuilder<IMongoDBReplicaSetResource> WithAnnotation(
        ResourceAnnotation annotation)
    {
        _inner.WithAnnotation(annotation);
        return this;
    }

    public IResourceBuilder<IMongoDBReplicaSetResource> WithReference(
        IResourceBuilder<IResourceWithConnectionString> resource)
    {
        _inner.WithReference(resource);
        return this;
    }
}
```

#### Step 3: Create Extension Methods

```csharp
public static class MongoDBReplicaSetExtensions
{
    public static IResourceBuilder<IMongoDBReplicaSetResource> AddMongoDBReplicaSet(
        this IDistributedApplicationBuilder builder,
        string name,
        string replicaSetName = "rs0",
        int replicaCount = 3,
        int? port = null)
    {
        var resource = new MongoDBReplicaSetResource(name, replicaSetName, replicaCount);
        
        var resourceBuilder = builder.AddResource(resource)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = "docker.io",
                Image = "mongo",
                Tag = "7.0"
            })
            .WithHttpEndpoint(port: port, name: "primary", targetPort: 27017)
            .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "admin")
            .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "password")
            .WithEnvironment("MONGO_REPLICA_SET_NAME", replicaSetName);

        // Add init script to configure replica set
        resourceBuilder.WithBindMount(
            GetInitScriptPath(),
            "/docker-entrypoint-initdb.d/init-replica-set.js");

        return new MongoDBReplicaSetResourceBuilder(resourceBuilder);
    }

    public static IResourceBuilder<IMongoDBReplicaSetResource> WithMongoExpress(
        this IResourceBuilder<IMongoDBReplicaSetResource> builder,
        string? name = null,
        int? port = null)
    {
        var expressName = name ?? $"{builder.Resource.Name}-express";
        
        builder.ApplicationBuilder.AddContainer(expressName, "mongo-express")
            .WithHttpEndpoint(port: port, targetPort: 8081, name: "http")
            .WithEnvironment("ME_CONFIG_MONGODB_URL", builder.Resource.ConnectionStringExpression!)
            .WithReference(builder);

        return builder;
    }

    private static string GetInitScriptPath()
    {
        // Return path to init script
        return Path.Combine(AppContext.BaseDirectory, "scripts", "init-replica-set.js");
    }
}
```

#### Step 4: Usage

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDBReplicaSet("mongodb", replicaSetName: "myapp-rs", replicaCount: 3)
    .WithMongoExpress(port: 8081);

var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(mongo);

builder.Build().Run();
```

### Scenario 2: Custom Kafka Resource with Topics

Create a Kafka resource that automatically creates topics on startup.

#### Define the Resource

```csharp
public interface IKafkaResource : IResourceWithConnectionString
{
    IReadOnlyList<string> Topics { get; }
}

internal class KafkaResource : ContainerResource, IKafkaResource
{
    private readonly List<string> _topics = new();

    public KafkaResource(string name) : base(name)
    {
    }

    public IReadOnlyList<string> Topics => _topics.AsReadOnly();

    public void AddTopic(string topic)
    {
        _topics.Add(topic);
    }

    public string? ConnectionStringExpression
    {
        get
        {
            var endpoint = Annotations
                .OfType<EndpointAnnotation>()
                .FirstOrDefault(e => e.Name == "kafka");
            
            return endpoint != null 
                ? $"localhost:{endpoint.Port}" 
                : null;
        }
    }
}
```

#### Create Extension Methods

```csharp
public static class KafkaExtensions
{
    public static IResourceBuilder<IKafkaResource> AddKafka(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var kafka = new KafkaResource(name);
        
        var resourceBuilder = builder.AddResource(kafka)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Image = "confluentinc/cp-kafka",
                Tag = "7.5.0"
            })
            .WithHttpEndpoint(port: port, targetPort: 9092, name: "kafka")
            .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
            .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1");

        // Add Zookeeper dependency
        var zookeeper = builder.AddContainer($"{name}-zookeeper", "confluentinc/cp-zookeeper")
            .WithHttpEndpoint(targetPort: 2181, name: "zookeeper")
            .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181");

        resourceBuilder.WithReference(zookeeper);

        return resourceBuilder;
    }

    public static IResourceBuilder<IKafkaResource> WithTopic(
        this IResourceBuilder<IKafkaResource> builder,
        string topicName)
    {
        if (builder.Resource is KafkaResource kafka)
        {
            kafka.AddTopic(topicName);
        }

        return builder;
    }

    public static IResourceBuilder<IKafkaResource> WithKafkaUI(
        this IResourceBuilder<IKafkaResource> builder,
        int? port = null)
    {
        builder.ApplicationBuilder.AddContainer($"{builder.Resource.Name}-ui", "provectuslabs/kafka-ui")
            .WithHttpEndpoint(port: port, targetPort: 8080, name: "http")
            .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local")
            .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", builder.Resource.ConnectionStringExpression!)
            .WithReference(builder);

        return builder;
    }
}
```

#### Usage

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("messaging")
    .WithTopic("orders")
    .WithTopic("notifications")
    .WithTopic("analytics")
    .WithKafkaUI(port: 8080);

var api = builder.AddProject<Projects.OrderApi>("orderapi")
    .WithReference(kafka);

builder.Build().Run();
```

### Scenario 3: Custom External API Resource

Create a resource type for external APIs with health checking and configuration.

```csharp
public interface IExternalApiResource : IResourceWithConnectionString
{
    string BaseUrl { get; }
    string? ApiKey { get; }
    string? HealthEndpoint { get; }
}

internal class ExternalApiResource : Resource, IExternalApiResource
{
    public ExternalApiResource(string name, string baseUrl) : base(name)
    {
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }
    public string? ApiKey { get; set; }
    public string? HealthEndpoint { get; set; }
    
    public string? ConnectionStringExpression => BaseUrl;
}

public static class ExternalApiExtensions
{
    public static IResourceBuilder<IExternalApiResource> AddExternalApi(
        this IDistributedApplicationBuilder builder,
        string name,
        string baseUrl)
    {
        var resource = new ExternalApiResource(name, baseUrl);
        return builder.AddResource(resource);
    }

    public static IResourceBuilder<IExternalApiResource> WithApiKey(
        this IResourceBuilder<IExternalApiResource> builder,
        string apiKey)
    {
        if (builder.Resource is ExternalApiResource resource)
        {
            resource.ApiKey = apiKey;
        }

        return builder;
    }

    public static IResourceBuilder<IExternalApiResource> WithHealthCheck(
        this IResourceBuilder<IExternalApiResource> builder,
        string healthEndpoint)
    {
        if (builder.Resource is ExternalApiResource resource)
        {
            resource.HealthEndpoint = healthEndpoint;
        }

        // Add health check annotation
        builder.WithAnnotation(new HealthCheckAnnotation
        {
            Endpoint = healthEndpoint,
            Interval = TimeSpan.FromSeconds(30)
        });

        return builder;
    }
}

// Usage
var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddExternalApi("weather-api", "https://api.weather.com")
    .WithApiKey(builder.AddParameter("weather-api-key", secret: true))
    .WithHealthCheck("/health");

var api = builder.AddProject<Projects.MyApi>("api")
    .WithEnvironment("WeatherApi:BaseUrl", weatherApi.Resource.ConnectionStringExpression!)
    .WithEnvironment("WeatherApi:ApiKey", weatherApi.Resource.ApiKey!);
```

## Advanced Customization Patterns

### Resource Lifecycle Hooks

Implement lifecycle hooks for custom behavior:

```csharp
public class LifecycleHookAnnotation : ResourceAnnotation
{
    public Func<IResource, CancellationToken, Task>? BeforeStart { get; init; }
    public Func<IResource, CancellationToken, Task>? AfterStart { get; init; }
    public Func<IResource, CancellationToken, Task>? BeforeStop { get; init; }
}

// Usage
var database = builder.AddPostgres("db")
    .WithAnnotation(new LifecycleHookAnnotation
    {
        AfterStart = async (resource, ct) =>
        {
            // Run migrations after database starts
            Console.WriteLine("Running database migrations...");
            await RunMigrationsAsync(ct);
        }
    });
```

### Dynamic Configuration

Create resources that adapt based on environment:

```csharp
public static class SmartResourceExtensions
{
    public static IResourceBuilder<T> WithEnvironmentAdaptation<T>(
        this IResourceBuilder<T> builder) where T : IResource
    {
        var environment = builder.ApplicationBuilder.Environment;

        if (environment.IsDevelopment())
        {
            // Development settings
            builder.WithEnvironment("LOG_LEVEL", "Debug");
        }
        else if (environment.IsStaging())
        {
            // Staging settings
            builder.WithEnvironment("LOG_LEVEL", "Information")
                   .WithReplicas(2);
        }
        else if (environment.IsProduction())
        {
            // Production settings
            builder.WithEnvironment("LOG_LEVEL", "Warning")
                   .WithReplicas(5);
        }

        return builder;
    }
}
```

### Resource Groups

Group related resources together:

```csharp
public class ResourceGroup
{
    private readonly List<IResource> _resources = new();

    public string Name { get; }
    public IReadOnlyList<IResource> Resources => _resources.AsReadOnly();

    public ResourceGroup(string name)
    {
        Name = name;
    }

    public void Add(IResource resource)
    {
        _resources.Add(resource);
    }
}

public static class ResourceGroupExtensions
{
    public static ResourceGroup AddResourceGroup(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        var group = new ResourceGroup(name);
        // Store group in builder context
        return group;
    }

    public static IResourceBuilder<T> InGroup<T>(
        this IResourceBuilder<T> builder,
        ResourceGroup group) where T : IResource
    {
        group.Add(builder.Resource);
        return builder;
    }
}

// Usage
var builder = DistributedApplication.CreateBuilder(args);

var dataGroup = builder.AddResourceGroup("data-layer");

var postgres = builder.AddPostgres("db").InGroup(dataGroup);
var redis = builder.AddRedis("cache").InGroup(dataGroup);
var mongo = builder.AddMongoDB("docs").InGroup(dataGroup);
```

## Aspire Unit Testing

### Testing Resource Configuration

```csharp
using Aspire.Hosting;
using Xunit;

public class AppHostTests
{
    [Fact]
    public void AppHost_Includes_AllRequiredResources()
    {
        // Arrange
        var appHost = CreateAppHost();

        // Act
        var resources = appHost.Resources.ToList();

        // Assert
        Assert.Contains(resources, r => r.Name == "cache");
        Assert.Contains(resources, r => r.Name == "database");
        Assert.Contains(resources, r => r.Name == "apiservice");
    }

    [Fact]
    public void ApiService_References_Cache_And_Database()
    {
        // Arrange
        var appHost = CreateAppHost();

        // Act
        var apiService = appHost.Resources
            .First(r => r.Name == "apiservice");

        var references = apiService.Annotations
            .OfType<ResourceReferenceAnnotation>()
            .Select(r => r.Resource.Name)
            .ToList();

        // Assert
        Assert.Contains("cache", references);
        Assert.Contains("database", references);
    }

    [Fact]
    public void Redis_Uses_Correct_Image()
    {
        // Arrange
        var appHost = CreateAppHost();

        // Act
        var redis = appHost.Resources.First(r => r.Name == "cache");
        var imageAnnotation = redis.Annotations
            .OfType<ContainerImageAnnotation>()
            .FirstOrDefault();

        // Assert
        Assert.NotNull(imageAnnotation);
        Assert.Equal("redis", imageAnnotation.Image);
        Assert.Equal("7.2", imageAnnotation.Tag);
    }

    private static IDistributedApplication CreateAppHost()
    {
        var builder = DistributedApplication.CreateBuilder();
        
        var cache = builder.AddRedis("cache");
        var db = builder.AddPostgres("postgres").AddDatabase("database");
        
        builder.AddProject<Projects.ApiService>("apiservice")
            .WithReference(cache)
            .WithReference(db);

        return builder.Build();
    }
}
```

### Testing Custom Resources

```csharp
public class MongoDBReplicaSetTests
{
    [Fact]
    public void MongoDBReplicaSet_Creates_WithCorrectConfiguration()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var mongo = builder.AddMongoDBReplicaSet("mongodb", 
            replicaSetName: "testrs", 
            replicaCount: 3);

        // Assert
        Assert.Equal("mongodb", mongo.Resource.Name);
        Assert.Equal("testrs", mongo.Resource.ReplicaSetName);
        Assert.Equal(3, mongo.Resource.ReplicaCount);
    }

    [Fact]
    public void MongoDBReplicaSet_ConnectionString_IsCorrect()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var mongo = builder.AddMongoDBReplicaSet("mongodb", replicaSetName: "rs0");

        // Act
        var connectionString = mongo.Resource.ConnectionStringExpression;

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("replicaSet=rs0", connectionString);
    }

    [Fact]
    public void MongoDBReplicaSet_WithMongoExpress_Adds_UIContainer()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var mongo = builder.AddMongoDBReplicaSet("mongodb")
            .WithMongoExpress();

        var app = builder.Build();
        var resources = app.Resources.ToList();

        // Assert
        Assert.Contains(resources, r => r.Name == "mongodb-express");
    }
}
```

### Integration Testing

```csharp
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class IntegrationTests : IClassFixture<AspireAppHostFixture>
{
    private readonly AspireAppHostFixture _fixture;

    public IntegrationTests(AspireAppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApiService_Returns_HealthyStatus()
    {
        // Arrange
        var httpClient = _fixture.CreateHttpClient("apiservice");

        // Act
        var response = await httpClient.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task Redis_IsAccessible_FromApiService()
    {
        // Arrange
        var httpClient = _fixture.CreateHttpClient("apiservice");

        // Act
        var response = await httpClient.GetAsync("/api/cache/test");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}

public class AspireAppHostFixture : IAsyncLifetime
{
    private IDistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = DistributedApplication.CreateBuilder();
        // Configure your app
        _app = builder.Build();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    public HttpClient CreateHttpClient(string resourceName)
    {
        var resource = _app!.Resources.First(r => r.Name == resourceName);
        var endpoint = resource.Annotations
            .OfType<EndpointAnnotation>()
            .First();
        
        return new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{endpoint.Port}")
        };
    }
}
```

## Best Practices

### 1. Resource Naming
- Use descriptive, lowercase names
- Follow consistent naming conventions
- Avoid special characters except hyphens

### 2. Configuration
- Use parameters for sensitive data
- Provide sensible defaults
- Document required configuration

### 3. Dependencies
- Explicitly declare dependencies with `.WithReference()`
- Order resources appropriately
- Consider startup order

### 4. Error Handling
- Validate configuration early
- Provide clear error messages
- Fail fast on misconfiguration

### 5. Documentation
- Document custom resources
- Provide usage examples
- Include troubleshooting guides

### 6. Testing
- Test resource configuration
- Validate connection strings
- Test dependency resolution
- Include integration tests

## Common Patterns

### Pattern 1: Resource Factory

```csharp
public static class ResourceFactory
{
    public static IResourceBuilder<IRedisResource> CreateCache(
        this IDistributedApplicationBuilder builder,
        string name,
        CacheConfiguration config)
    {
        var redis = builder.AddRedis(name);

        if (config.PersistData)
        {
            redis.WithDataVolume();
        }

        if (config.MaxMemory.HasValue)
        {
            redis.WithEnvironment("REDIS_MAXMEMORY", config.MaxMemory.Value.ToString());
        }

        return redis;
    }
}
```

### Pattern 2: Conditional Resources

```csharp
public static IDistributedApplicationBuilder AddObservability(
    this IDistributedApplicationBuilder builder)
{
    if (builder.Configuration.GetValue<bool>("EnableJaeger"))
    {
        builder.AddContainer("jaeger", "jaegertracing/all-in-one")
            .WithHttpEndpoint(port: 16686, name: "ui")
            .WithHttpEndpoint(port: 4317, name: "otlp");
    }

    if (builder.Configuration.GetValue<bool>("EnablePrometheus"))
    {
        builder.AddContainer("prometheus", "prom/prometheus")
            .WithHttpEndpoint(port: 9090, name: "ui")
            .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml");
    }

    return builder;
}
```

### Pattern 3: Resource Validation

```csharp
public static class ResourceValidation
{
    public static IResourceBuilder<T> Validate<T>(
        this IResourceBuilder<T> builder,
        Action<T> validation) where T : IResource
    {
        try
        {
            validation(builder.Resource);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Resource '{builder.Resource.Name}' validation failed: {ex.Message}",
                ex);
        }

        return builder;
    }
}

// Usage
builder.AddPostgres("db")
    .Validate(db =>
    {
        if (string.IsNullOrEmpty(db.ConnectionStringExpression))
        {
            throw new InvalidOperationException("Connection string is required");
        }
    });
```

## Additional Resources
- [Aspire Hosting GitHub Repository](https://github.com/dotnet/aspire)
- [Custom Resource Examples](https://github.com/dotnet/aspire-samples)
- [Aspire Community Resources](https://github.com/topics/dotnet-aspire)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)

## Next Steps
Proceed to the hands-on exercise where you'll create custom resources, implement lifecycle hooks, and write tests for your Aspire application.

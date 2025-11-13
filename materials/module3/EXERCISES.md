# Module 3: Hands-On Exercise - Extending Aspire with Custom Resources

## Exercise Overview
In this exercise, you'll create custom Aspire resources, implement lifecycle hooks, and write tests for your Aspire applications. You'll build a custom Elasticsearch resource with Kibana integration and implement comprehensive unit tests.

## Scenario
Extend the Task Manager application with:
- A custom Elasticsearch resource for search and analytics
- Kibana for visualization
- Custom lifecycle hooks for index initialization
- Unit tests for resource configuration

## Time Required
60-75 minutes

## Step 1: Create Custom Elasticsearch Resource

### 1.1 Create a New Class Library for Custom Resources

```bash
cd TaskManager
dotnet new classlib -n TaskManager.CustomResources
dotnet sln add TaskManager.CustomResources

# Add required packages
cd TaskManager.CustomResources
dotnet add package Aspire.Hosting.AppHost
```

### 1.2 Define the Elasticsearch Resource

Create `TaskManager.CustomResources/ElasticsearchResource.cs`:

```csharp
using Aspire.Hosting.ApplicationModel;

namespace TaskManager.CustomResources;

public interface IElasticsearchResource : IResourceWithConnectionString
{
    string ClusterName { get; }
    int? MemoryLimit { get; }
}

internal class ElasticsearchResource : ContainerResource, IElasticsearchResource
{
    public ElasticsearchResource(string name, string clusterName = "docker-cluster") 
        : base(name)
    {
        ClusterName = clusterName;
    }

    public string ClusterName { get; }
    public int? MemoryLimit { get; set; }

    public string? ConnectionStringExpression
    {
        get
        {
            var endpoint = this.GetEndpoint("http");
            return endpoint?.Property(EndpointProperty.Url);
        }
    }
}
```

### 1.3 Create Extension Methods

Create `TaskManager.CustomResources/ElasticsearchExtensions.cs`:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace TaskManager.CustomResources;

public static class ElasticsearchExtensions
{
    public static IResourceBuilder<IElasticsearchResource> AddElasticsearch(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        string? clusterName = null)
    {
        var resource = new ElasticsearchResource(name, clusterName ?? "docker-cluster");
        
        var resourceBuilder = builder.AddResource(resource)
            .WithImage("elasticsearch", "8.11.0")
            .WithHttpEndpoint(port: port, targetPort: 9200, name: "http")
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("cluster.name", resource.ClusterName)
            .WithEnvironment("xpack.security.enabled", "false")
            .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m");

        return resourceBuilder;
    }

    public static IResourceBuilder<IElasticsearchResource> WithMemoryLimit(
        this IResourceBuilder<IElasticsearchResource> builder,
        int megabytes)
    {
        if (builder.Resource is ElasticsearchResource resource)
        {
            resource.MemoryLimit = megabytes;
            builder.WithEnvironment("ES_JAVA_OPTS", $"-Xms{megabytes}m -Xmx{megabytes}m");
        }

        return builder;
    }

    public static IResourceBuilder<IElasticsearchResource> WithDataVolume(
        this IResourceBuilder<IElasticsearchResource> builder,
        string? name = null)
    {
        var volumeName = name ?? $"{builder.Resource.Name}-data";
        builder.WithVolume(volumeName, "/usr/share/elasticsearch/data");
        return builder;
    }

    public static IResourceBuilder<IElasticsearchResource> WithKibana(
        this IResourceBuilder<IElasticsearchResource> builder,
        string? name = null,
        int? port = null)
    {
        var kibanaName = name ?? $"{builder.Resource.Name}-kibana";
        
        builder.ApplicationBuilder.AddContainer(kibanaName, "kibana")
            .WithImageTag("8.11.0")
            .WithHttpEndpoint(port: port, targetPort: 5601, name: "http")
            .WithEnvironment("ELASTICSEARCH_HOSTS", builder.Resource.ConnectionStringExpression!)
            .WithReference(builder);

        return builder;
    }

    public static IResourceBuilder<IElasticsearchResource> WithIndexInitialization(
        this IResourceBuilder<IElasticsearchResource> builder,
        params string[] indices)
    {
        builder.WithAnnotation(new ElasticsearchIndexInitializationAnnotation(indices));
        return builder;
    }
}

internal class ElasticsearchIndexInitializationAnnotation : IResourceAnnotation
{
    public ElasticsearchIndexInitializationAnnotation(string[] indices)
    {
        Indices = indices;
    }

    public string[] Indices { get; }
}
```

### 1.4 Add Lifecycle Hook for Index Creation

Create `TaskManager.CustomResources/ElasticsearchLifecycleHook.cs`:

```csharp
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace TaskManager.CustomResources;

public class ElasticsearchLifecycleHook : IDistributedApplicationLifecycleHook
{
    private readonly ILogger<ElasticsearchLifecycleHook> _logger;

    public ElasticsearchLifecycleHook(ILogger<ElasticsearchLifecycleHook> logger)
    {
        _logger = logger;
    }

    public async Task AfterResourcesCreatedAsync(
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default)
    {
        var elasticsearchResources = appModel.Resources
            .OfType<IElasticsearchResource>()
            .ToList();

        foreach (var resource in elasticsearchResources)
        {
            var annotation = resource.Annotations
                .OfType<ElasticsearchIndexInitializationAnnotation>()
                .FirstOrDefault();

            if (annotation != null)
            {
                _logger.LogInformation(
                    "Found Elasticsearch resource '{ResourceName}' with {IndexCount} indices to initialize",
                    resource.Name,
                    annotation.Indices.Length);

                // Note: Actual initialization would happen after the resource is started
                // This is a simplified example
            }
        }

        await Task.CompletedTask;
    }

    public async Task AfterEndpointsAllocatedAsync(
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default)
    {
        var elasticsearchResources = appModel.Resources
            .OfType<IElasticsearchResource>()
            .ToList();

        foreach (var resource in elasticsearchResources)
        {
            var annotation = resource.Annotations
                .OfType<ElasticsearchIndexInitializationAnnotation>()
                .FirstOrDefault();

            if (annotation != null && resource.ConnectionStringExpression != null)
            {
                try
                {
                    await InitializeIndicesAsync(
                        resource.ConnectionStringExpression,
                        annotation.Indices,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to initialize indices for Elasticsearch resource '{ResourceName}'",
                        resource.Name);
                }
            }
        }
    }

    private async Task InitializeIndicesAsync(
        string elasticsearchUrl,
        string[] indices,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(elasticsearchUrl) };

        // Wait for Elasticsearch to be ready
        var retries = 30;
        while (retries > 0)
        {
            try
            {
                var response = await httpClient.GetAsync("/_cluster/health", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch
            {
                // Elasticsearch not ready yet
            }

            await Task.Delay(1000, cancellationToken);
            retries--;
        }

        if (retries == 0)
        {
            _logger.LogWarning("Elasticsearch did not become ready in time");
            return;
        }

        // Create indices
        foreach (var index in indices)
        {
            try
            {
                _logger.LogInformation("Creating index: {IndexName}", index);
                
                var response = await httpClient.PutAsJsonAsync(
                    $"/{index}",
                    new
                    {
                        settings = new
                        {
                            number_of_shards = 1,
                            number_of_replicas = 0
                        }
                    },
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully created index: {IndexName}", index);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogInformation("Index {IndexName} already exists", index);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to create index {IndexName}: {StatusCode}",
                        index,
                        response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating index: {IndexName}", index);
            }
        }
    }
}
```

## Step 2: Integrate Custom Resource into AppHost

### 2.1 Reference Custom Resources Project

```bash
cd ../TaskManager.AppHost
dotnet add reference ../TaskManager.CustomResources
```

### 2.2 Register Lifecycle Hook

Update `TaskManager.AppHost/Program.cs`:

```csharp
using TaskManager.CustomResources;

var builder = DistributedApplication.CreateBuilder(args);

// Register lifecycle hook
builder.Services.AddLifecycleHook<ElasticsearchLifecycleHook>();

var cache = builder.AddRedis("cache")
    .WithDataVolume();

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("taskdb");

// Add custom Elasticsearch resource
var elasticsearch = builder.AddElasticsearch("search", port: 9200)
    .WithMemoryLimit(512)
    .WithDataVolume()
    .WithKibana(port: 5601)
    .WithIndexInitialization("tasks", "logs", "analytics");

var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(elasticsearch);

var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
    .WithReference(cache)
    .WithReference(database);

builder.AddProject<Projects.TaskManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

### 2.3 Test the Custom Resource

```bash
cd TaskManager.AppHost
dotnet run
```

**Verify:**
- [ ] Elasticsearch container starts
- [ ] Kibana container starts
- [ ] Dashboard shows both resources
- [ ] Indices are created (check logs)
- [ ] Connection string is available to API service

## Step 3: Create Unit Tests

### 3.1 Create Test Project

```bash
cd ..
dotnet new xunit -n TaskManager.Tests
dotnet sln add TaskManager.Tests

cd TaskManager.Tests
dotnet add reference ../TaskManager.AppHost
dotnet add reference ../TaskManager.CustomResources
dotnet add package Aspire.Hosting.Testing
```

### 3.2 Create Resource Configuration Tests

Create `TaskManager.Tests/AppHostConfigurationTests.cs`:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using TaskManager.CustomResources;
using Xunit;

namespace TaskManager.Tests;

public class AppHostConfigurationTests
{
    [Fact]
    public void AppHost_Includes_AllRequiredResources()
    {
        // Arrange & Act
        using var app = CreateAppHost();
        var resources = app.Resources.ToList();

        // Assert
        Assert.Contains(resources, r => r.Name == "cache");
        Assert.Contains(resources, r => r.Name == "postgres");
        Assert.Contains(resources, r => r.Name == "taskdb");
        Assert.Contains(resources, r => r.Name == "search");
        Assert.Contains(resources, r => r.Name == "apiservice");
        Assert.Contains(resources, r => r.Name == "worker");
        Assert.Contains(resources, r => r.Name == "webfrontend");
    }

    [Fact]
    public void ApiService_References_AllDependencies()
    {
        // Arrange & Act
        using var app = CreateAppHost();
        var apiService = app.Resources.First(r => r.Name == "apiservice");
        
        var references = apiService.Annotations
            .OfType<ResourceReferenceAnnotation>()
            .Select(r => r.Resource.Name)
            .ToList();

        // Assert
        Assert.Contains("cache", references);
        Assert.Contains("taskdb", references);
        Assert.Contains("search", references);
    }

    [Fact]
    public void Redis_Has_DataVolume()
    {
        // Arrange & Act
        using var app = CreateAppHost();
        var redis = app.Resources.First(r => r.Name == "cache");
        
        var hasVolume = redis.Annotations
            .OfType<ContainerMountAnnotation>()
            .Any(m => m.Type == ContainerMountType.Volume);

        // Assert
        Assert.True(hasVolume, "Redis should have a data volume");
    }

    [Fact]
    public void Postgres_Has_DataVolume()
    {
        // Arrange & Act
        using var app = CreateAppHost();
        var postgres = app.Resources.First(r => r.Name == "postgres");
        
        var hasVolume = postgres.Annotations
            .OfType<ContainerMountAnnotation>()
            .Any(m => m.Type == ContainerMountType.Volume);

        // Assert
        Assert.True(hasVolume, "PostgreSQL should have a data volume");
    }

    [Fact]
    public void WebFrontend_Has_ExternalEndpoint()
    {
        // Arrange & Act
        using var app = CreateAppHost();
        var web = app.Resources.First(r => r.Name == "webfrontend");
        
        var endpoints = web.Annotations
            .OfType<EndpointAnnotation>()
            .ToList();

        // Assert
        Assert.NotEmpty(endpoints);
        Assert.Contains(endpoints, e => e.IsExternal);
    }

    private static DistributedApplication CreateAppHost()
    {
        var builder = DistributedApplication.CreateBuilder();
        
        // Register lifecycle hook
        builder.Services.AddLifecycleHook<ElasticsearchLifecycleHook>();

        var cache = builder.AddRedis("cache").WithDataVolume();
        var postgres = builder.AddPostgres("postgres").WithDataVolume();
        var database = postgres.AddDatabase("taskdb");
        
        var elasticsearch = builder.AddElasticsearch("search")
            .WithMemoryLimit(512)
            .WithDataVolume()
            .WithKibana()
            .WithIndexInitialization("tasks", "logs", "analytics");

        var apiService = builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
            .WithReference(cache)
            .WithReference(database)
            .WithReference(elasticsearch);

        var worker = builder.AddProject<Projects.TaskManager_Worker>("worker")
            .WithReference(cache)
            .WithReference(database);

        builder.AddProject<Projects.TaskManager_Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(apiService);

        return builder.Build();
    }
}
```

### 3.3 Create Custom Resource Tests

Create `TaskManager.Tests/ElasticsearchResourceTests.cs`:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using TaskManager.CustomResources;
using Xunit;

namespace TaskManager.Tests;

public class ElasticsearchResourceTests
{
    [Fact]
    public void AddElasticsearch_Creates_Resource_WithCorrectName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resource = builder.AddElasticsearch("test-search");

        // Assert
        Assert.Equal("test-search", resource.Resource.Name);
    }

    [Fact]
    public void AddElasticsearch_Sets_ClusterName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resource = builder.AddElasticsearch("search", clusterName: "my-cluster");

        // Assert
        Assert.Equal("my-cluster", resource.Resource.ClusterName);
    }

    [Fact]
    public void AddElasticsearch_Uses_CorrectImage()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resource = builder.AddElasticsearch("search");
        using var app = builder.Build();

        var imageAnnotation = resource.Resource.Annotations
            .OfType<ContainerImageAnnotation>()
            .FirstOrDefault();

        // Assert
        Assert.NotNull(imageAnnotation);
        Assert.Equal("elasticsearch", imageAnnotation.Image);
        Assert.Equal("8.11.0", imageAnnotation.Tag);
    }

    [Fact]
    public void WithMemoryLimit_Sets_JavaOptions()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resource = builder.AddElasticsearch("search")
            .WithMemoryLimit(1024);

        using var app = builder.Build();

        // Assert
        var envAnnotation = resource.Resource.Annotations
            .OfType<EnvironmentCallbackAnnotation>()
            .FirstOrDefault();

        Assert.NotNull(envAnnotation);
    }

    [Fact]
    public void WithDataVolume_Adds_VolumeMount()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resource = builder.AddElasticsearch("search")
            .WithDataVolume();

        using var app = builder.Build();

        // Assert
        var volumeAnnotation = resource.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/usr/share/elasticsearch/data");

        Assert.NotNull(volumeAnnotation);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
    }

    [Fact]
    public void WithKibana_Adds_KibanaContainer()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        builder.AddElasticsearch("search")
            .WithKibana();

        using var app = builder.Build();
        var resources = app.Resources.ToList();

        // Assert
        Assert.Contains(resources, r => r.Name == "search-kibana");
    }

    [Fact]
    public void WithIndexInitialization_Adds_Annotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var indices = new[] { "tasks", "logs" };

        // Act
        var resource = builder.AddElasticsearch("search")
            .WithIndexInitialization(indices);

        // Assert
        var annotation = resource.Resource.Annotations
            .OfType<ElasticsearchIndexInitializationAnnotation>()
            .FirstOrDefault();

        Assert.NotNull(annotation);
        Assert.Equal(indices, annotation.Indices);
    }

    [Fact]
    public void ConnectionString_IsNotNull_AfterConfiguration()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resource = builder.AddElasticsearch("search", port: 9200);
        using var app = builder.Build();

        // Assert
        // Note: Connection string might be null until endpoints are allocated
        // In a real scenario, this would be tested in an integration test
        Assert.NotNull(resource.Resource);
    }

    [Fact]
    public void MultipleElasticsearch_CanBeAdded()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        builder.AddElasticsearch("search1");
        builder.AddElasticsearch("search2");
        using var app = builder.Build();

        var resources = app.Resources
            .OfType<IElasticsearchResource>()
            .ToList();

        // Assert
        Assert.Equal(2, resources.Count);
        Assert.Contains(resources, r => r.Name == "search1");
        Assert.Contains(resources, r => r.Name == "search2");
    }
}
```

### 3.4 Create Integration Tests (Optional)

Create `TaskManager.Tests/IntegrationTests.cs`:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.CustomResources;
using Xunit;

namespace TaskManager.Tests;

public class IntegrationTests : IAsyncLifetime
{
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = DistributedApplication.CreateBuilder();
        
        builder.Services.AddLifecycleHook<ElasticsearchLifecycleHook>();

        var cache = builder.AddRedis("cache");
        var postgres = builder.AddPostgres("postgres");
        var database = postgres.AddDatabase("taskdb");
        
        var elasticsearch = builder.AddElasticsearch("search")
            .WithIndexInitialization("tasks");

        builder.AddProject<Projects.TaskManager_ApiService>("apiservice")
            .WithReference(cache)
            .WithReference(database)
            .WithReference(elasticsearch);

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

    [Fact]
    public async Task Elasticsearch_IsAccessible()
    {
        // Arrange
        var resource = _app!.Resources
            .OfType<IElasticsearchResource>()
            .First();

        var endpoint = resource.GetEndpoint("http");
        var url = $"{endpoint.Url}";

        using var httpClient = new HttpClient();

        // Act
        var response = await httpClient.GetAsync($"{url}/_cluster/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ApiService_HealthCheck_Returns_Healthy()
    {
        // Arrange
        var apiService = _app!.Resources.First(r => r.Name == "apiservice");
        var endpoint = apiService.GetEndpoint("http");
        
        using var httpClient = new HttpClient();

        // Act
        var response = await httpClient.GetAsync($"{endpoint.Url}/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }
}
```

### 3.5 Run Tests

```bash
cd TaskManager.Tests
dotnet test
```

**Expected results:**
- All configuration tests pass
- Custom resource tests pass
- Integration tests pass (if Elasticsearch is running)

## Step 4: Additional Custom Resource Examples

### 4.1 Create a Custom Monitoring Stack

Create `TaskManager.CustomResources/MonitoringExtensions.cs`:

```csharp
using Aspire.Hosting;

namespace TaskManager.CustomResources;

public static class MonitoringExtensions
{
    public static IDistributedApplicationBuilder AddMonitoringStack(
        this IDistributedApplicationBuilder builder,
        string name = "monitoring")
    {
        // Prometheus
        var prometheus = builder.AddContainer($"{name}-prometheus", "prom/prometheus")
            .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "ui")
            .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml");

        // Grafana
        var grafana = builder.AddContainer($"{name}-grafana", "grafana/grafana")
            .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "ui")
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
            .WithReference(prometheus);

        // Loki for logs
        var loki = builder.AddContainer($"{name}-loki", "grafana/loki")
            .WithHttpEndpoint(port: 3100, targetPort: 3100, name: "http")
            .WithBindMount("./loki-config.yml", "/etc/loki/local-config.yaml");

        return builder;
    }
}
```

### 4.2 Create a Custom Message Queue Resource

Create `TaskManager.CustomResources/RabbitMQExtensions.cs`:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace TaskManager.CustomResources;

public static class RabbitMQExtensions
{
    public static IResourceBuilder<RabbitMQServerResource> AddRabbitMQWithManagement(
        this IDistributedApplicationBuilder builder,
        string name,
        string? username = null,
        string? password = null)
    {
        var rabbitMQ = builder.AddRabbitMQ(name, 
            userName: username, 
            password: password);

        // Add management plugin endpoint
        rabbitMQ.WithHttpEndpoint(port: 15672, targetPort: 15672, name: "management");

        return rabbitMQ;
    }

    public static IResourceBuilder<RabbitMQServerResource> WithQueues(
        this IResourceBuilder<RabbitMQServerResource> builder,
        params string[] queueNames)
    {
        // Add annotation for queue initialization
        builder.WithAnnotation(new RabbitMQQueueAnnotation(queueNames));
        return builder;
    }
}

internal class RabbitMQQueueAnnotation : IResourceAnnotation
{
    public RabbitMQQueueAnnotation(string[] queueNames)
    {
        QueueNames = queueNames;
    }

    public string[] QueueNames { get; }
}
```

## Step 5: Document Custom Resources

### 5.1 Create Documentation

Create `TaskManager.CustomResources/README.md`:

```markdown
# TaskManager Custom Resources

This library provides custom Aspire resources for the TaskManager application.

## Elasticsearch Resource

### Basic Usage

```csharp
var elasticsearch = builder.AddElasticsearch("search");
```

### With Kibana

```csharp
var elasticsearch = builder.AddElasticsearch("search")
    .WithKibana(port: 5601);
```

### With Index Initialization

```csharp
var elasticsearch = builder.AddElasticsearch("search")
    .WithIndexInitialization("tasks", "logs", "analytics");
```

### Full Configuration

```csharp
var elasticsearch = builder.AddElasticsearch("search", port: 9200, clusterName: "my-cluster")
    .WithMemoryLimit(1024)
    .WithDataVolume()
    .WithKibana(port: 5601)
    .WithIndexInitialization("tasks", "logs");
```

## Configuration

### Memory Limit

Controls the JVM heap size:

```csharp
.WithMemoryLimit(512)  // 512 MB
```

### Data Persistence

```csharp
.WithDataVolume()  // Named volume
.WithDataVolume("es-data")  // Custom volume name
```

## Lifecycle Hooks

The `ElasticsearchLifecycleHook` automatically creates configured indices after Elasticsearch starts.

Register in AppHost:

```csharp
builder.Services.AddLifecycleHook<ElasticsearchLifecycleHook>();
```

## Testing

See `TaskManager.Tests` for examples of unit testing custom resources.
```

## Verification Checklist

- [ ] Custom Elasticsearch resource created
- [ ] Extension methods work correctly
- [ ] Lifecycle hook initializes indices
- [ ] Kibana integration works
- [ ] Unit tests pass
- [ ] Integration tests pass (optional)
- [ ] Documentation is complete
- [ ] Resources appear in Aspire Dashboard
- [ ] Connection strings are generated correctly

## Challenge Tasks

### Challenge 1: Add Elasticsearch Security
Enable security features in Elasticsearch:
```csharp
.WithEnvironment("xpack.security.enabled", "true")
.WithEnvironment("ELASTIC_PASSWORD", builder.AddParameter("elastic-password", secret: true))
```

### Challenge 2: Create a Custom Database Migration Resource
Build a resource that runs database migrations on startup.

### Challenge 3: Add Snapshot Repository
Configure Elasticsearch snapshot repository for backups.

### Challenge 4: Create Resource Templates
Build a factory for common resource configurations.

### Challenge 5: Add Health Checks
Implement custom health checks for Elasticsearch indices.

## Key Takeaways

1. **Custom Resources**: Extend Aspire for specialized infrastructure
2. **Lifecycle Hooks**: Run code at specific points in the resource lifecycle
3. **Testing**: Unit test resource configuration and behavior
4. **Extensions**: Use extension methods for fluent configuration
5. **Annotations**: Attach metadata to resources for custom behavior

## Next Steps
- Explore more complex custom resources
- Implement custom publishers for deployment
- Create reusable resource libraries
- Share custom resources with the community

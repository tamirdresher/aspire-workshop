#:sdk Aspire.AppHost.Sdk@13.1.0

#:package Aspire.Hosting.Redis@13.1.0
#:package Aspire.Hosting.PostgreSQL@13.1.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("db")
    .AddDatabase("mydb");

var cache = builder.AddRedis("cache")
    .WithoutHttpsCertificate();

var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithReference(cache)
    .WaitFor(cache);

builder.Eventing.Subscribe<BeforeStartEvent>(
    static (evt, cancellationToken) =>
    {
        var logger = evt.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("0. BeforeStartEvent");

        return Task.CompletedTask;
    });


// This event fires **after a resource is added**, but **before endpoints are allocated**. It's especially useful for custom resources that don't have a built-in lifecycle (like containers or executables), giving you a clean place to kick off background logic, set default state, or wire up behavior.
builder.Eventing.Subscribe<InitializeResourceEvent>(
    static (evt, cancellationToken) =>
    {
        var logger = evt.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("\"{ResourceName}\" 1. InitializeResourceEvent", evt.Resource.Name);
        return Task.CompletedTask;
    });

// This event fires once a resource's endpoints have been assigned (e.g., after port resolution or container allocation). It's scoped per resource, so you can safely get an <xref:Aspire.Hosting.ApplicationModel.EndpointReference> and build derived URLs or diagnostics.
builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(
    static (evt, cancellationToken) =>
    {
        var logger = evt.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("\"{ResourceName}\" 2.  ResourceEndpointsAllocatedEvent", evt.Resource.Name);
        if (evt.Resource is IResourceWithEndpoints resource &&
            resource.TryGetEndpoints(out var endpoints))
        {
            foreach(var ep in endpoints)
            {
                logger.LogInformation($"\tEndpoint {ep.Name} - target {ep.TargetHost}, Port: {ep.Port}");
            }
        }
        return Task.CompletedTask;
    });



// Raised when a connection string is ready and enables dependent resources to be wired dynamically.    
builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(
    static async (evt, cancellationToken) =>
    {
        var logger = evt.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("\"{ResourceName}\" 3. ConnectionStringAvailableEvent", evt.Resource.Name);
        if(evt.Resource is IResourceWithConnectionString resourceWithConnectionString)
        {
            var connectionString = await resourceWithConnectionString.GetConnectionStringAsync();
            logger.LogInformation("\tConnection string {connectionString}",connectionString);
        }
    });

// This event fires **after a resource is added**, but **before endpoints are allocated**. It's especially useful for custom resources that don't have a built-in lifecycle (like containers or executables), giving you a clean place to kick off background logic, set default state, or wire up behavior.
builder.Eventing.Subscribe<BeforeResourceStartedEvent>(
    static (evt, cancellationToken) =>
    {
        var logger = evt.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("\"{ResourceName}\" 4. BeforeResourceStartedEvent", evt.Resource.Name);
        return Task.CompletedTask;
    });

// Raised when the resource is considered "ready." which Unblocks dependents waiting for readiness. 
builder.Eventing.Subscribe<ResourceReadyEvent>(
    static (evt, cancellationToken) =>
    {
        var logger = evt.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("\"{ResourceName}\" 5. ResourceReadyEvent", evt.Resource.Name);
        return Task.CompletedTask;
    });

//Raised after the AppHost created resources
builder.Eventing.Subscribe<AfterResourcesCreatedEvent>(
    static (evt, cancellationToken) =>
    {        
        var logger = evt.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("100. AfterResourcesCreatedEvent");

        return Task.CompletedTask;
    });




builder.Build().Run();
#:sdk Aspire.AppHost.Sdk@13.4.6

#:package Aspire.Hosting.Redis@13.4.6

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIRECERTIFICATES001

var builder = DistributedApplication.CreateBuilder(args);

builder.OnBeforeStart(static (@event, cancellationToken) =>
{
    var logger = @event.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("0. AppHost is starting.");
    return Task.CompletedTask;
});

var cache = builder.AddRedis("cache")
    .WithoutHttpsCertificate();

cache.OnInitializeResource(static (resource, @event, cancellationToken) =>
{
    var logger = @event.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("1. Initializing {ResourceName}.", resource.Name);
    return Task.CompletedTask;
});

cache.OnResourceEndpointsAllocated(static (resource, @event, cancellationToken) =>
{
    var logger = @event.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("2. Endpoints allocated for {ResourceName}.", resource.Name);

    if (resource.TryGetEndpoints(out var endpoints))
    {
        foreach (var endpoint in endpoints)
        {
            logger.LogInformation(
                "Endpoint {EndpointName}: {Host}:{Port}.",
                endpoint.Name,
                endpoint.TargetHost,
                endpoint.Port);
        }
    }

    return Task.CompletedTask;
});

cache.OnConnectionStringAvailable(static (resource, @event, cancellationToken) =>
{
    var logger = @event.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("3. Connection string available for {ResourceName}.", resource.Name);
    return Task.CompletedTask;
});

// Startup waits for this callback, so keep blocking initialization work bounded.
cache.OnBeforeResourceStarted(static (resource, @event, cancellationToken) =>
{
    var logger = @event.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("4. Starting {ResourceName}.", resource.Name);
    return Task.CompletedTask;
});

cache.OnResourceReady(static (resource, @event, cancellationToken) =>
{
    var logger = @event.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("5. {ResourceName} is ready.", resource.Name);
    return Task.CompletedTask;
});

var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithReference(cache)
    .WaitFor(cache);

builder.Eventing.Subscribe<AfterResourcesCreatedEvent>(static (@event, cancellationToken) =>
{
    var logger = @event.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("6. AppHost resources were created.");
    return Task.CompletedTask;
});

builder.Build().Run();

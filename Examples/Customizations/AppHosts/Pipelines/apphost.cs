#:sdk Aspire.AppHost.Sdk@13.1.0

#:package Aspire.Hosting.Redis@13.1.0

using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var builder = DistributedApplication.CreateBuilder(args);

// Add custom deployment validation
builder.Pipeline.AddStep("generate-certificates", async context =>
    {
        var logger = context.Services.GetRequiredService<ILogger<Program>>();

        await Task.Delay(1000);
        logger.LogInformation("Generated certificates");
    }, 
    requiredBy: WellKnownPipelineSteps.Deploy,
    dependsOn: new[]{WellKnownPipelineSteps.Publish});

builder.Pipeline.AddStep("write-path", async context =>
    {
        var logger = context.Services.GetRequiredService<ILogger<Program>>();

        await Task.Delay(1000);
        logger.LogInformation($"PATH: {Environment.GetEnvironmentVariable("PATH")}");
    });



var cache = builder.AddRedis("cache")
    .WithoutHttpsCertificate();   

var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithReference(cache)
    .WaitFor(cache);

var web = builder.AddCSharpApp("frontend", "../../../Services/AspireCustomResource.Web/")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
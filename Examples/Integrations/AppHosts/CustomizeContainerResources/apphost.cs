#:sdk Aspire.AppHost.Sdk@13.4.6
#:property ManagePackageVersionsCentrally=false

#:package Aspire.Hosting.PostgreSQL@13.4.6
#:package Aspire.Hosting.Redis@13.4.6


using System.Reflection;

var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithBindMount("ollama", "/root/.ollama")
    .WithBindMount("./ollamaconfig", "/usr/config")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama")
    //.WithEntrypoint("/usr/config/entrypoint.sh")
    .WithContainerRuntimeArgs("--gpus=all");

// Create a Redis container derived from ContainerResource
var redis = builder.AddRedis("redis")
    // Change container name and image version
    .WithImageTag("latest")    
    .WithArgs("--save", "60", "1", "--loglevel", "warning")   
    // Add volume mounts (host → container)
    .WithVolume("redis-data", "/data", isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent);

builder.Build().Run();
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis container - runs automatically in Docker
var cache = builder.AddRedis("cache");

// Add API service with Redis reference
var api = builder.AddProject<Projects.RedisCache_Api>("api")
    .WithExternalHttpEndpoints()
    .WithReference(cache);  // Injects ConnectionStrings__cache

builder.Build().Run();

// That's it! Redis starts automatically and API can use it.
// Connection string is injected via environment variable.
// No manual docker commands or configuration needed!

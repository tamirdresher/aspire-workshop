var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var dbPassword = builder.AddParameter("dbPassword", secret: true);
var db = builder.AddPostgres("db", password: dbPassword)
    .AddDatabase("complexdb");

var messaging = builder.AddRabbitMQ("messaging");

var backend = builder.AddProject<Projects.Backend>("backend")
    .WithReference(cache)
    .WithReference(db)
    .WithReference(messaging);

var aiService = builder.AddPythonApp("ai-service", "../../src/ai-service", "main.py")
    .WithHttpEndpoint(env: "PORT", port: 8000)
    .WithExternalHttpEndpoints();

builder.AddNpmApp("frontend", "../../src/frontend")
    .WithReference(backend)
    .WithReference(aiService)
    .WithHttpEndpoint(targetPort: 3000, name: "http");

builder.Build().Run();

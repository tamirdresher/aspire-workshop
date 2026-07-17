using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Aspire runs `go run .` and supplies the dynamically allocated port through PORT.
builder.AddGoApp("go-api", "../go-api")
    .WithHttpEndpoint(env: "PORT")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.Build().Run();

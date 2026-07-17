using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Aspire runs `bun index.ts` and supplies the dynamically allocated port through PORT.
builder.AddBunApp("bun-api", "../bun-api", "index.ts")
    .WithHttpEndpoint(env: "PORT")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.Build().Run();

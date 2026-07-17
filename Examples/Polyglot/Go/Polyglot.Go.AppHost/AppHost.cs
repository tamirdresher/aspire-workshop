using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// AddGoApp runs `go run .` from the given directory (it must contain a go.mod).
// WithHttpEndpoint's `env` parameter tells Aspire to pass the assigned port to the
// process via the PORT environment variable, which go-api/main.go reads at startup.
var api = builder.AddGoApp("go-api", "../go-api")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter();

builder.Build().Run();

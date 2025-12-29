var builder = DistributedApplication.CreateBuilder(args);

// Add API project
var api = builder.AddProject<Projects.Bookstore_API>("api");

// Add Customer Web app with service discovery
builder.AddProject<Projects.Bookstore_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

// Add Admin React app (Node.js)
var admin = builder.AddJavaScriptApp("admin", "../Bookstore.Admin")
    .WithReference(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// Add Worker service for book descriptions
builder.AddProject<Projects.Bookstore_Worker>("worker")
    .WithReference(api);

builder.Build().Run();

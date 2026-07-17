var builder = DistributedApplication.CreateBuilder(args);

// Add API project
var api = builder.AddProject<Projects.Bookstore_API>("api");

// Add Customer Web app with service discovery
builder.AddProject<Projects.Bookstore_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

// Add Admin React app (Node.js)
builder.AddViteApp("admin", "../Bookstore.Admin")
    .WithReference(api)
    .WithExternalHttpEndpoints();

// Add Worker service for book descriptions
builder.AddProject<Projects.Bookstore_Worker>("worker")
    .WithReference(api);

builder.Build().Run();

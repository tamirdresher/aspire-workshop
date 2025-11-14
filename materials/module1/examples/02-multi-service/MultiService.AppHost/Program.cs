var builder = DistributedApplication.CreateBuilder(args);

// Add the API service
var api = builder.AddProject<Projects.MultiService_Api>("api");

// Add the Web frontend with a reference to the API
var web = builder.AddProject<Projects.MultiService_Web>("web")
    .WithExternalHttpEndpoints()  // Make it accessible from browser
    .WithReference(api);           // Web can discover and call API

// Build and run
builder.Build().Run();

// That's it! Both services start automatically.
// The Web service can call the API using http://api (service discovery)
// The dashboard shows both services and their dependencies

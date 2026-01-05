#:sdk Aspire.AppHost.Sdk@13.1.0


var builder = DistributedApplication.CreateBuilder(args);

var nonSecretParameter = builder.AddParameter("non-secret-parameter", secret: false);

var parameter = builder.AddParameter("example-parameter-name", secret: true);

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var externalServiceUrl = builder.AddParameter("external-service-url")
    .WithDescription("The URL of the external service.")
    .WithCustomInput(p => new()
    {
        InputType = InputType.Choice,
        Options =  [new("https://www.nuget.org/", "https://www.nuget.org/"), new("https://www.bing.com/", "https://www.bing.com/")],        
        Name = p.Name,
        Placeholder = $"Enter value for {p.Name}",
        Description = p.Description
    });

var externalService = builder.AddConnectionString("external-service");

var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithEnvironment("URL", externalServiceUrl)
    .WithReference(externalService)
     // Create an additional endpoint named "admin"
    // - port: null => host/proxy port allocated dynamically
    // - targetPort: null => service listening port allocated dynamically and injected to ADMIN_PORT
    .WithEndpoint(
        name: "admin",
        scheme: "http",
        env: "ADMIN_PORT",
        port: null,
        targetPort: null,
        isProxied: true,
        isExternal: true);

builder.Build().Run();
#:sdk Aspire.AppHost.Sdk@13.1.0


var builder = DistributedApplication.CreateBuilder(args);

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

#pragma warning restore ASPIREINTERACTION001

var externalService = builder.AddExternalService("external-service", externalServiceUrl)
    .WithHttpHealthCheck(path: "/", statusCode: 200);

var externalService2 = builder.AddExternalService("external-service2", externalServiceUrl)
    .WithUrl("https://www.github.com/")
    .WithUrl("https://www.stackoverflow.com/");



var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithReference(externalService2);


builder.Build().Run();
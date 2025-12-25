using Aspire.Hosting;
using AspireCustomResource.AppHost;
using Microsoft.Extensions.DependencyInjection;
using Projects;
using static System.Net.WebRequestMethods;

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
var externalService = builder.AddExternalService("external-service", externalServiceUrl)
    .WithHttpHealthCheck(path: "/", statusCode: 200);
#pragma warning restore ASPIREINTERACTION001

var externalService2 = builder.AddExternalService("external-service2", externalServiceUrl)
    .WithUrl("https://www.github.com/")
    .WithUrl("https://www.stackoverflow.com/");


var redis = builder.AddConnectionString("redis");

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var cache = builder.AddRedis("cache")
    .WithoutHttpsCertificate()
    .WithCommand("clear-cache", "Clear Cache",
        async context => {
            var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
            if (interactionService.IsAvailable)
            {
                var result = await interactionService.PromptConfirmationAsync(
                    title: "Clear confirmation",
                    message: "Are you sure you want to delete the data?");

                if (result.Data)
                {
                    // Run your resource/command logic.
                }
            }
            return new ExecuteCommandResult { Success = true, ErrorMessage = "" };
           },
        commandOptions: new CommandOptions
        {
            UpdateState = updateCtx=>ResourceCommandState.Enabled,
            IconName = "AnimalRabbitOff", // Specify the icon name
            IconVariant = IconVariant.Filled // Specify the icon variant
        });
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.





var mocksJson = """
{
  "mocks": [
    {
      "request": { "url": "http://example.com/data", "method": "GET" },
      "response": {
        "statusCode": 200,
        "headers": [ 
          {
            "name": "content-type",
            "value": "application/json; odata.metadata=minimal"
          }
        ],
        "body": { "message": "hello from inline mocks" }
      }
    }
  ]
}
""";

var devProxy = builder.AddMicrosoftDevProxy(
    name: "devproxy",
    options: new DevProxyOptions
    {
        WorkingDirectory = builder.AppHostDirectory,
        BaseConfigFile = "devproxyrc.json", // optional; will be merged if exists
        Port = 18000,
        ApiPort = 8897,
        Mocks = new DevProxyMocksOptions
        {
            JsonContent = mocksJson,
            FileName = "mocks.json"
        }
    });
var example= devProxy.AddUrlMock(    "example",
    urlPattern: "http://example.com/*",
    url: "http://example.com");

var apiService = builder.AddProject<Projects.AspireCustomResource_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(redis)
    .WithHttpHealthCheck("/health")
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
        isExternal: true)
    .WaitFor(devProxy)
    .WithReference(example)
    .WithReference(devProxy)
    .WithDevProxy(devProxy) ; 

builder.AddProject<Projects.AspireCustomResource_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(externalService2)
    .WithReference(devProxy);

builder.Build().Run();

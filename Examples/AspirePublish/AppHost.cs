#:package Aspire.Hosting.Docker@13.1.0-preview.1.25616.3
#:package Aspire.Hosting.Python@13.1.0

#:sdk Aspire.AppHost.Sdk@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

builder.AddUvicornApp("python-service", "./python-service", "main.py")
    .WithHttpEndpoint(name: "main", targetPort: 8000, env: "UVICORN_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        // Customizations go here
        service.Labels["target_env"] = "production";
    });

builder.Build().Run();

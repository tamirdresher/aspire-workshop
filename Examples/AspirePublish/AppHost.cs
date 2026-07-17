#:sdk Aspire.AppHost.Sdk@13.4.6
#:property ManagePackageVersionsCentrally=false

#:package Aspire.Hosting.Docker@13.4.6
#:package Aspire.Hosting.Kubernetes@13.4.6-preview.1.26319.6
#:package Aspire.Hosting.Python@13.4.6

using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
{
    ["--target"] = "Deployment:Target",
    ["--registry"] = "Deployment:Registry"
});

var target = builder.Configuration["Deployment:Target"]?.ToLowerInvariant() ?? "compose";

switch (target)
{
    case "compose":
        builder.AddDockerComposeEnvironment("env");
        break;

    case "k8s":
    case "kubernetes":
    {
        var registryHost = builder.Configuration["Deployment:Registry"];
        if (string.IsNullOrWhiteSpace(registryHost))
        {
            throw new InvalidOperationException(
                "Kubernetes deployment requires --registry <host>, reachable by this machine and the cluster.");
        }

#pragma warning disable ASPIRECOMPUTE003
        var registry = builder.AddContainerRegistry("registry", registryHost);
        builder.AddKubernetesEnvironment("env")
            .WithContainerRegistry(registry)
            .WithHelm(helm =>
            {
                helm.WithNamespace("aspire-publish");
                helm.WithReleaseName("aspire-publish");
            });
#pragma warning restore ASPIRECOMPUTE003
        break;
    }

    default:
        throw new InvalidOperationException(
            $"Unsupported deployment target '{target}'. Use 'compose' or 'k8s'.");
}

var pythonService = builder.AddUvicornApp("python-service", "./python-service", "main:app")
    .WithHttpEndpoint(name: "main", targetPort: 8000, env: "UVICORN_PORT")
    .WithExternalHttpEndpoints();

if (target == "compose")
{
    pythonService.PublishAsDockerComposeService((resource, service) =>
    {
        service.Labels["target_env"] = "production";
    });
}

builder.Build().Run();

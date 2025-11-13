# Exercise 2: Deploying Your .NET Aspire Application

## Overview

In this exercise, you'll learn how to deploy your Aspire application to different environments using Aspire's deployment features, including containerization and manifest generation.

## Learning Objectives

By the end of this exercise, you will be able to:
- Generate deployment manifests from your Aspire application
- Containerize your Aspire application
- Understand deployment options for Aspire apps
- Deploy to Azure Container Apps (or other container orchestrators)

## Prerequisites

- Completed Exercise 1: System Topology
- Docker Desktop installed and running
- Azure CLI (optional, for Azure deployment)

## Steps

### Step 1: Add Container Support

First, ensure your projects have Dockerfile support. Create a Dockerfile for the API project:

```dockerfile
# ECommerce.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ECommerce.Api/ECommerce.Api.csproj", "ECommerce.Api/"]
COPY ["ECommerce.Shared/ECommerce.Shared.csproj", "ECommerce.Shared/"]
COPY ["ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj", "ECommerce.ServiceDefaults/"]
RUN dotnet restore "ECommerce.Api/ECommerce.Api.csproj"
COPY . .
WORKDIR "/src/ECommerce.Api"
RUN dotnet build "ECommerce.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
```

Create a similar Dockerfile for the Web project:

```dockerfile
# ECommerce.Web/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ECommerce.Web/ECommerce.Web.csproj", "ECommerce.Web/"]
COPY ["ECommerce.Shared/ECommerce.Shared.csproj", "ECommerce.Shared/"]
COPY ["ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj", "ECommerce.ServiceDefaults/"]
RUN dotnet restore "ECommerce.Web/ECommerce.Web.csproj"
COPY . .
WORKDIR "/src/ECommerce.Web"
RUN dotnet build "ECommerce.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.Web.dll"]
```

### Step 2: Update AppHost for Container Support

Update your `ECommerce.AppHost/Program.cs` to use containers:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// For local development, use projects
var api = builder.AddProject<Projects.ECommerce_Api>("api");

builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(api);

// Alternative: For container deployment, use this instead:
// var api = builder.AddContainer("api", "ecommerce-api")
//     .WithHttpEndpoint(port: 8080, targetPort: 8080);
//
// builder.AddContainer("web", "ecommerce-web")
//     .WithHttpEndpoint(port: 8081, targetPort: 8080)
//     .WithEnvironment("ApiUrl", api.GetEndpoint("http"));

builder.Build().Run();
```

### Step 3: Generate Deployment Manifest

Aspire can generate deployment manifests for various platforms. Generate a manifest:

```bash
cd ECommerce.AppHost
dotnet run --publisher manifest --output-path ../manifest.json
```

This creates a `manifest.json` file that describes your application's deployment requirements.

### Step 4: Build Container Images

Build container images for your services:

```bash
cd ..
docker build -f ECommerce.Api/Dockerfile -t ecommerce-api:latest .
docker build -f ECommerce.Web/Dockerfile -t ecommerce-web:latest .
```

### Step 5: Test Locally with Docker Compose

Create a `docker-compose.yml` file to test your containers:

```yaml
version: '3.8'

services:
  api:
    image: ecommerce-api:latest
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080

  web:
    image: ecommerce-web:latest
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ApiUrl=http://api:8080
    depends_on:
      - api
```

Run with Docker Compose:

```bash
docker-compose up
```

Access the application at `http://localhost:5002`

### Step 6: Deploy to Azure Container Apps (Optional)

If you have an Azure subscription, you can deploy using the Azure Developer CLI:

```bash
# Install Azure Developer CLI if not already installed
# https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd

# Initialize the Azure deployment
azd init

# Provision infrastructure and deploy
azd up
```

Follow the prompts to:
1. Select your Azure subscription
2. Choose a region
3. Deploy the application

### Step 7: Using Aspire with Azure

Update your AppHost to include Azure resources:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Azure resources (requires Aspire.Hosting.Azure NuGet packages)
var insights = builder.AddAzureApplicationInsights("insights");

var api = builder.AddProject<Projects.ECommerce_Api>("api")
    .WithReference(insights);

builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(api)
    .WithReference(insights);

builder.Build().Run();
```

Install the required package:

```bash
cd ECommerce.AppHost
dotnet add package Aspire.Hosting.Azure.ApplicationInsights
```

## Deployment Manifest Structure

The generated manifest includes:

```json
{
  "resources": {
    "api": {
      "type": "project.v0",
      "path": "../ECommerce.Api/ECommerce.Api.csproj",
      "env": {
        "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
        "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true"
      },
      "bindings": {
        "http": {
          "scheme": "http",
          "protocol": "tcp",
          "transport": "http"
        },
        "https": {
          "scheme": "https",
          "protocol": "tcp",
          "transport": "http"
        }
      }
    }
  }
}
```

## Key Concepts Learned

- **Container Images**: Packaging your Aspire services as Docker containers
- **Deployment Manifests**: Declarative descriptions of your application topology
- **Azure Container Apps**: Serverless container platform for Aspire apps
- **Azure Developer CLI**: Tool for deploying Aspire apps to Azure
- **Multi-environment Configuration**: Managing different configs for dev/prod

## Deployment Options

### 1. Azure Container Apps
- Fully managed serverless containers
- Auto-scaling and built-in load balancing
- Native Aspire support

### 2. Kubernetes
- Use the manifest to generate Kubernetes YAML
- Full control over deployment
- Requires more infrastructure management

### 3. Docker Compose
- Simple local or server deployment
- Good for development and small-scale production
- Easy to understand and debug

### 4. Azure Kubernetes Service (AKS)
- Managed Kubernetes on Azure
- Enterprise-grade features
- Integrates with Azure services

## Verification

1. Verify containers are running:
   ```bash
   docker ps
   ```

2. Check logs:
   ```bash
   docker logs <container-id>
   ```

3. Test the application endpoints:
   ```bash
   curl http://localhost:5001/api/products
   ```

## Common Issues

### Issue: Container build fails
**Solution**: Ensure all project references are correctly specified in the Dockerfile COPY commands

### Issue: Service discovery not working in containers
**Solution**: Use environment variables to pass service URLs when not using Aspire orchestration

### Issue: Azure deployment fails
**Solution**: Check Azure CLI is logged in (`az login`) and you have appropriate permissions

## Clean Up

Remove Docker containers and images:

```bash
docker-compose down
docker rmi ecommerce-api:latest ecommerce-web:latest
```

## Next Steps

Proceed to [Exercise 3: Aspire Extensibility](../03-aspire-extensibility/README.md) to learn about extending Aspire with custom components.

## Additional Resources

- [Deploy Aspire apps to Azure Container Apps](https://learn.microsoft.com/dotnet/aspire/deployment/azure/container-apps)
- [Aspire Deployment Overview](https://learn.microsoft.com/dotnet/aspire/deployment/overview)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Docker Documentation](https://docs.docker.com/)

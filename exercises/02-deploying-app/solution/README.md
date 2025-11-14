# Exercise 2 Solution - Deployment with .NET Aspire

This solution extends Exercise 1 with deployment-ready configurations for containerization and cloud deployment.

## What's New in This Solution

This solution builds on Exercise 1 and adds:

### Containerization Support

Each microservice now includes:
- Dockerfile for containerization
- Multi-stage build optimizations
- Minimal base images for security

### Deployment Manifests

The AppHost can generate deployment manifests for:
- Azure Container Apps
- Kubernetes
- Docker Compose

### Azure Resource Configuration

Enhanced Azure resource configuration:
- Production-ready Cosmos DB connection
- Azure Storage with proper authentication
- Azure OpenAI service configuration

## Running the Solution

### Local Development (Same as Exercise 1)

```bash
dotnet run --project src/ECommerce.AppHost
```

### Generate Deployment Manifest

```bash
cd src/ECommerce.AppHost
dotnet run --publisher manifest --output-path ../../manifest.json
```

### Deploy to Azure Container Apps

Using Azure Developer CLI (azd):

```bash
azd init
azd up
```

## Next Steps

Continue to [Exercise 3](../../03-aspire-extensibility/) to learn about extending Aspire.

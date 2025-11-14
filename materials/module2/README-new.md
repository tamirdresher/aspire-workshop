# Module 2: Production Time Orchestration (Outer Loop)

## Overview
This module covers preparing your .NET Aspire application for production deployment. Learn about observability with OpenTelemetry, health checks, deployment manifests, and various deployment targets.

## Learning Objectives
- Understand OpenTelemetry integration (traces, metrics, logs)
- Implement comprehensive health checks
- Generate and customize deployment manifests
- Deploy to Azure Container Apps
- Use Aspire publishers for different targets
- Configure production-ready resource settings

## Prerequisites
- Completed Module 1 or equivalent Aspire knowledge
- .NET SDK 8.0+ with Aspire workload
- Docker Desktop or Podman
- Azure subscription (optional, for cloud deployment exercises)

## Module Structure

### Topics (Read in Order)

1. **[OpenTelemetry Basics](./topics/01-opentelemetry.md)**
   - What is OpenTelemetry?
   - Traces, metrics, and logs
   - Automatic instrumentation
   - Custom instrumentation

2. **[Advanced Observability](./topics/02-advanced-observability.md)**
   - Custom metrics
   - Custom activity sources
   - Log correlation
   - Performance tuning

3. **[Health Checks](./topics/03-health-checks.md)**
   - Built-in health checks
   - Custom health checks
   - Readiness vs liveness
   - Health check UI

4. **[Deployment Manifests](./topics/04-deployment-manifests.md)**
   - What are manifests?
   - Generating manifests
   - Manifest structure
   - Customizing manifests

5. **[Azure Deployment](./topics/05-azure-deployment.md)**
   - Azure Container Apps overview
   - Deploying with Aspire CLI
   - Deploying with azd
   - Resource provisioning

6. **[Resource Customization](./topics/06-resource-customization.md)**
   - Container settings
   - Environment-specific config
   - Production vs development
   - Advanced scenarios

### Hands-On Examples

Each example can be run independently:

1. **[Custom Metrics](./examples/01-custom-metrics/)** - Add application-specific metrics
2. **[Health Checks Deep Dive](./examples/02-health-checks/)** - Comprehensive health monitoring
3. **[Manifest Generation](./examples/03-manifest-generation/)** - Generate and explore manifests
4. **[Local Container Build](./examples/04-container-build/)** - Build and test containers locally
5. **[Azure Deployment](./examples/05-azure-deployment/)** - Deploy to Azure Container Apps

### Practice Exercises

- **[Guided Lab: E-Commerce Observability](./exercises/lab-ecommerce-observability.md)**
  - Add comprehensive observability to an e-commerce system
  - Custom metrics, traces, and health checks
  - Prepare for production deployment
  - ~90-120 minutes

## Time Estimate
- Reading topics: 90-120 minutes
- Running examples: 45-60 minutes
- Practice exercises: 90-120 minutes
- **Total: 3.5-5 hours**

## Getting Started

1. **Read the topics** in order (01 through 06)
2. **Run the examples** as you learn each concept
3. **Complete the guided lab** to reinforce learning

## Quick Start

To run any example:

```bash
cd examples/01-custom-metrics
dotnet run --project CustomMetrics.AppHost
```

The Aspire Dashboard will open at `http://localhost:15888`.

## Official Documentation References

- [OpenTelemetry in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/telemetry)
- [Health Checks](https://learn.microsoft.com/dotnet/aspire/fundamentals/health-checks)
- [Deployment Overview](https://learn.microsoft.com/dotnet/aspire/deployment/overview)
- [Azure Deployment](https://learn.microsoft.com/dotnet/aspire/deployment/aspire-deploy/aca-deployment-aspire-cli)
- [Manifest Format](https://learn.microsoft.com/dotnet/aspire/deployment/manifest-format)

## Previous/Next Modules

- **Previous:** [Module 1: Dev Time Orchestration](../module1/README.md)
- **Next:** [Module 3: Aspire Extensibility](../module3/README.md)

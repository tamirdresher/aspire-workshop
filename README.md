# Building Distributed Apps with .NET Aspire - Workshop

A comprehensive 3-day workshop for building cloud-native, distributed applications using .NET Aspire.

## 📚 Workshop Overview

This workshop teaches you how to build production-ready distributed applications using .NET Aspire - an opinionated, cloud-ready stack for building observable, production-ready distributed applications.

**What you'll learn:**
- Model cloud-based applications with code
- Orchestrate multiple services and their dependencies locally and in the cloud
- Implement service discovery for dynamic communication
- Integrate with databases, caches, and message queues
- Build custom resources for specialized scenarios
- Test distributed applications effectively
- Deploy to Azure with minimal configuration

## 🎯 3-Day Workshop Agenda

### 📅 Day 1: Aspire Fundamentals
**Topics:**
- Aspire concepts & building blocks
- System topology and the App Host
- Service discovery and references
- Basic integrations

**[📖 Start with Lesson 1: Getting Started with .NET Aspire](Exercise/workshop/Lesson-01/README.md)**

Learn how to add Aspire to an existing application, set up service defaults, create an AppHost for orchestration, and implement service discovery.

---

### 📅 Day 2: Customizations & Integrations
**Topics:**
- Aspire customizations & extensions
- Working with Aspire integrations (Redis, Cosmos DB, PostgreSQL, etc.)
- Parameters and secrets management
- Publish mode - publishers and resource customizations
- Custom commands and URL customizations

**[📖 Continue with Lesson 2: Integrations and Cloud Services](Exercise/workshop/Lesson-02/README.md)** 

---

### 📅 Day 3: Advanced Topics
**Topics:**
- Aspire internals and resource model
- Building custom resources from scratch
- Aspire distributed testing strategies
- Integration testing with Playwright

**[📖 Advance to Lesson 3: Custom Resources and Testing](Exercise/workshop/Lesson-03/README.md)**

Master the Aspire resource model, build a custom "Talking Clock" resource, and implement comprehensive integration tests.

---

## 💡 Code Examples

This repository includes practical examples demonstrating various Aspire capabilities:

### 🔧 Customizations
Advanced AppHost customization techniques:

- **[Annotations](Examples/Customizations/AppHosts/Annotations/)** - Using annotations for resource extensibility
- **[Commands](Examples/Customizations/AppHosts/Commands/)** - Custom commands with interaction service
- **[Eventing](Examples/Customizations/AppHosts/Eventing/)** - Resource lifecycle events
- **[Parameters](Examples/Customizations/AppHosts/Parameters/)** - Parameter management and custom inputs
- **[Pipelines](Examples/Customizations/AppHosts/Pipelines/)** - Resource processing pipelines
- **[URL Customizations](Examples/Customizations/AppHosts/UrlCustomizations/)** - Custom URL configurations for the dashboard

### 🔌 Integrations
Working with cloud services and emulators:

- **[All Emulators](Examples/Integrations/AppHosts/AllEmulators/)** - Running Azure emulators locally (Cosmos DB, Storage, etc.)
- **[Infrastructure Configuration](Examples/Integrations/AppHosts/ConfigureInfrastructure/)** - Configuring Azure infrastructure programmatically
- **[Custom Bicep](Examples/Integrations/AppHosts/CustomBicep/)** - Using custom Bicep templates for Azure resources
- **[Container Customizations](Examples/Integrations/AppHosts/CustomizeContainerResources/)** - Advanced container configuration
- **[External Resources](Examples/Integrations/AppHosts/ExternalResources/)** - Connecting to external services

### 🏗️ Custom Resources
Building your own Aspire resources:

- **[AspireCustomResource](Examples/AspireCustomResource/)** - Complete example application with custom resources
- **[DevProxy Integration](Examples/AspireCustomResource/AspireCustomResource.AppHost/DevProxyResource.cs)** - Custom resource for Microsoft Dev Proxy

### 🚀 Service Orchestration
Multi-service application examples:

- **[Services Example](Examples/Services/)** - Complete multi-service app with API, Web frontend, and Worker service
- **[Integrations Services](Examples/Integrations/Services/)** - Service integration patterns

### 🧪 Testing
Comprehensive testing strategies:

- **[NoteTaker Test Suite](Examples/Testing/)** - Full integration testing example
  - Backend API with Entity Framework Core, Redis, and RabbitMQ
  - Frontend Node.js application
  - Python AI service
  - [Integration Tests](Examples/Testing/src/NoteTaker.Tests/IntegrationTests.cs) with xUnit
  - [Playwright E2E Tests](Examples/Testing/src/NoteTaker.Tests/PlaywrightIntegrationTests.cs)

### 📦 Deployment
Publishing and deployment examples:

- **[Aspire Publish](Examples/AspirePublish/)** - Deployment scenarios and manifest generation
- **[Python Service](Examples/AspirePublish/python-service/)** - Orchestrating Python services with .NET Aspire

---

## 🚀 Quick Start

### Prerequisites

- **.NET 10 SDK** or later - [Download](https://dotnet.microsoft.com/download)
- **Visual Studio 2026**  or **Visual Studio Code** with C# Dev Kit
- **Aspire CLI** 
- **Docker Desktop** (for container resources) - [Download](https://www.docker.com/products/docker-desktop)
- **Node.js 18+** and npm (for JavaScript examples) - [Download](https://nodejs.org/)
- **Azure Subscription** (optional, for cloud deployment)

### Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/aspire-workshop.git
   cd aspire-workshop
   ```

2. **Install .NET Aspire workload**:
   ```bash
   dotnet new install Aspire.ProjectTemplates
   ```

3. **Verify installation**:
   ```bash
   dotnet new list
   ```
   You should see `aspire` in the installed templates list.

### Running the Workshop

#### Start with the Hands-On Exercise

Follow the progressive lessons to build the Bookstore application:

```bash
cd Exercise/start
dotnet restore
```

**Then proceed to [Lesson 1](Exercise/workshop/Lesson-01/README.md)** for step-by-step instructions.

#### Explore Examples

Run any example to see Aspire in action:

```bash
# Run the service orchestration example
cd Examples/Services
dotnet run --project AspireCustomResource.AppHost

# Run the testing example
cd Examples/Testing/src
dotnet run --project NoteTaker.AppHost

# Run integration tests
cd Examples/Testing
dotnet test
```

The Aspire Dashboard will open automatically at `http://localhost:15888` (or similar).

---

## 📖 Key Concepts

### Service Defaults
Opinionated configuration providing:
- **OpenTelemetry** - Metrics, traces, and logging
- **Health Checks** - Liveness and readiness endpoints
- **Service Discovery** - Configuration-based endpoint resolution
- **Resilience** - HTTP retry policies and circuit breakers

### App Host
The orchestrator project that:
- Defines your application model
- Manages service lifecycle
- Provides the developer dashboard
- Configures service-to-service communication
- Generates deployment manifests

### Service Discovery
Reference services by name instead of hardcoded URLs:
```csharp
// In AppHost
var api = builder.AddProject<Projects.Bookstore_API>("api");
var web = builder.AddProject<Projects.Bookstore_Web>("web")
    .WithReference(api);

// In the consuming service
client.BaseAddress = new("https+http://api");
```

### Integrations
Easily integrate with cloud services:
```csharp
// Redis
var cache = builder.AddRedis("cache");

// Cosmos DB
var database = builder.AddAzureCosmosDB("cosmos")
    .AddDatabase("bookstore");

// PostgreSQL
var db = builder.AddPostgres("postgres")
    .AddDatabase("catalogdb");
```

---

## 📖 Repository Structure

```
aspire-workshop/
├── Exercise/                      # Workshop materials
│   ├── start/                    # Starting Bookstore application
│   └── workshop/                 # Lesson guides and solutions
│       ├── Lesson-01/            # Day 1: Getting started
│       ├── Lesson-02/            # Day 2: Integrations (coming soon)
│       └── Lesson-03/            # Day 3: Custom resources & testing
├── Examples/                      # Reference implementations
│   ├── Customizations/           # AppHost customization examples
│   ├── Integrations/             # Cloud integration examples
│   ├── Services/                 # Service orchestration examples
│   ├── AspireCustomResource/     # Custom resource examples
│   ├── Testing/                  # Testing strategies
│   └── AspirePublish/            # Deployment examples
└── README.md                     # This file
```

---

## 🤝 Contributing

This workshop is designed to be a living resource. Contributions, issues, and feature requests are welcome!

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

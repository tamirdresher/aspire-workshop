# Building Distributed Apps with Aspire - Workshop

A comprehensive 3-day workshop for building cloud-native, distributed applications using Aspire.

## 📚 Workshop Overview

This workshop teaches you how to build observable, production-ready distributed applications with Aspire.

**What you'll learn:**
- Model cloud-based applications with code
- Orchestrate multiple services and their dependencies locally and in the cloud
- Implement service discovery for dynamic communication
- Integrate with databases, caches, and message queues
- Build custom resources for specialized scenarios
- Test distributed applications effectively
- Deploy to Azure with minimal configuration
- Use AI coding agents with Aspire-aware lifecycle and diagnostics workflows

## 🎯 3-Day Workshop Agenda

### 📅 Day 1: Aspire Fundamentals
**Topics:**
- Aspire concepts & building blocks
- System topology and the App Host
- Service discovery and references
- Basic integrations

**[📖 Start with Lesson 1: Getting Started with Aspire](Exercise/workshop/Lesson-01/README.md)**

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

## AI-assisted Aspire workflow

Aspire 13.4 includes first-class support for AI coding agents. The official
[`microsoft/aspire-skills`](https://github.com/microsoft/aspire-skills) bundle teaches agents
how to initialize, run, observe, deploy, and wire Aspire applications without falling back to
ad hoc `dotnet`, Docker, or port-polling workflows.

Follow **[AI coding agents and Aspire skills](docs/ai-agents-and-aspire-skills.md)** to install
the six workflow skills, configure GitHub Copilot CLI or another supported agent, and use the
agent-safe `aspire start` → `aspire wait` → `aspire describe` workflow.

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
- **[Python Service](Examples/AspirePublish/python-service/)** - Orchestrating Python services with Aspire

---

## 🚀 Quick Start

### Prerequisites

- **.NET 10 SDK** or later - [Download](https://dotnet.microsoft.com/download)
- **Visual Studio 2026** or **Visual Studio Code** with C# Dev Kit and the [Aspire extension](https://aspire.dev/get-started/aspire-vscode-extension/)
- **Aspire CLI 13.4** or later - [Install the Aspire CLI](https://aspire.dev/get-started/install-cli/)
- **Docker Desktop** (for container resources) - [Download](https://www.docker.com/products/docker-desktop)
- **Current Node.js LTS** and npm (for JavaScript examples) - [Download](https://nodejs.org/)
- **Azure Subscription** (optional, for cloud deployment)

### Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/tamirdresher/aspire-workshop.git
   cd aspire-workshop
   ```

2. **Install the Aspire CLI** (example using the .NET global tool):
   ```bash
   dotnet tool install -g Aspire.Cli
   ```
   npm, WinGet, Homebrew, mise, and install-script options are documented in the
   [official installation guide](https://aspire.dev/get-started/install-cli/). Do not install
   the retired Aspire workload.

3. **Verify the environment**:
   ```bash
   aspire --version
   aspire doctor
   ```

4. **Configure an AI coding agent** (optional):
   ```bash
   aspire agent init
   ```
   See [AI coding agents and Aspire skills](docs/ai-agents-and-aspire-skills.md) for
   deterministic non-interactive setup and the recommended runtime workflow.

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
aspire run --apphost Examples/Services/AspireCustomResource.AppHost

# Run the testing example
aspire run --apphost Examples/Testing/src/NoteTaker.AppHost

# Run integration tests
dotnet test Examples/Testing/src/NoteTaker.Tests/NoteTaker.Tests.csproj
```

Use the dashboard URL printed by the Aspire CLI. Dashboard and OTLP ports are assigned
dynamically for AppHost runs; do not hardcode them in scripts or tests.

> **Using an AI coding agent?** Agents should use background execution with `aspire start`
> (and `--isolated` in a worktree), then call `aspire wait` before interacting with a resource.
> See the [agent workflow guide](docs/ai-agents-and-aspire-skills.md).

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
│       ├── Lesson-02/            # Day 2: Integrations and data
│       └── Lesson-03/            # Day 3: Custom resources & testing
├── docs/                          # Cross-cutting workshop guidance
│   └── ai-agents-and-aspire-skills.md
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

## 📚 Current Aspire resources

- [Aspire documentation](https://aspire.dev/)
- [Aspire CLI command reference](https://aspire.dev/reference/cli/commands/aspire/)
- [Use AI coding agents](https://aspire.dev/get-started/ai-coding-agents/)
- [Aspire skills](https://aspire.dev/get-started/aspire-skills/)
- [`microsoft/aspire-skills`](https://github.com/microsoft/aspire-skills)

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

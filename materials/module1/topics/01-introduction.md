# Introduction to .NET Aspire

## What is .NET Aspire?

.NET Aspire is an opinionated, cloud-ready stack for building **observable, production-ready distributed applications**. It provides:

- **Orchestration** - Define and run your distributed app locally with one command
- **Components** - Pre-built integrations for common services (databases, caches, messaging)
- **Tooling** - Visual Studio/VS Code support and CLI tools
- **Observability** - Built-in OpenTelemetry support with a powerful dashboard

## The Problem: Complex Distributed Development

### Before Aspire

Developing a typical microservices application required:

```bash
# Terminal 1: Start Redis
docker run -p 6379:6379 redis

# Terminal 2: Start SQL Server
docker run -p 1433:1433 -e SA_PASSWORD=... mcr.microsoft.com/mssql/server

# Terminal 3: Start RabbitMQ
docker run -p 5672:5672 -p 15672:15672 rabbitmq:management

# Terminal 4: Update config files
# Edit appsettings.json with connection strings

# Terminal 5: Run API
cd MyApi && dotnet run

# Terminal 6: Run Web
cd MyWeb && dotnet run

# Terminal 7: Run Worker
cd MyWorker && dotnet run
```

**Issues:**
- 😫 Many manual steps
- 🔧 Configuration scattered across files
- 🐛 Hard to debug across services
- ⏱️ 30+ minutes setup time
- 🤷 "Works on my machine" problems

### After Aspire

```bash
cd MyApp.AppHost
dotnet run
```

**That's it!** Everything starts automatically:
- ✅ All infrastructure (Redis, SQL, RabbitMQ)
- ✅ All services (API, Web, Worker)
- ✅ Dashboard with logs, traces, metrics
- ✅ < 2 minutes to running application

## Core Concepts

### 1. AppHost (Orchestrator)

The **AppHost** is a special .NET project that defines your application's topology:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres").AddDatabase("mydb");

// Services
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(cache)
    .WithReference(db);

var web = builder.AddProject<Projects.MyWeb>("web")
    .WithReference(api);

builder.Build().Run();
```

**What it does:**
- Defines what runs (services, databases, caches, etc.)
- Manages dependencies between resources
- Handles service discovery automatically
- Provides configuration to services

### 2. ServiceDefaults (Standard Configuration)

**ServiceDefaults** is a shared library that configures cross-cutting concerns:

```csharp
// In every service's Program.cs
builder.AddServiceDefaults();
```

**Provides:**
- OpenTelemetry (traces, metrics, logs)
- Health checks
- Service discovery
- Resilience (retry, circuit breaker, timeout)
- HTTP client configuration

### 3. Dashboard (Observability)

A web-based dashboard that shows:
- **Resources** - All running services and infrastructure
- **Logs** - Aggregated logs from all services
- **Traces** - End-to-end request tracing
- **Metrics** - Performance data and custom metrics

Automatically opens at `http://localhost:15888` when you run the AppHost.

### 4. Integrations (Components)

Pre-built packages for common infrastructure:

```csharp
// In AppHost
var redis = builder.AddRedis("cache");
var postgres = builder.AddPostgres("db");
var rabbitmq = builder.AddRabbitMQ("messaging");

// In your service
builder.AddRedisClient("cache");           // Auto-configured
builder.AddNpgsqlDbContext<MyDbContext>("db");  // Connection injected
builder.AddRabbitMQClient("messaging");    // Ready to use
```

No manual connection strings or configuration needed!

## When to Use .NET Aspire

### ✅ Great For

- **Microservices architectures** - Orchestrate multiple services
- **Cloud-native apps** - Built with cloud deployment in mind
- **Team development** - Consistent development environments
- **New projects** - Start with best practices built-in
- **Modernizing apps** - Incrementally add to existing solutions

### ⚠️ Consider Alternatives

- **Single service apps** - Aspire might be overkill
- **Non-.NET services** - While you can orchestrate them, benefits are reduced
- **Simple CRUD apps** - Traditional approaches may be simpler

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│           Developer Machine                      │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │     AppHost (Orchestrator)                │  │
│  │  - Defines topology                       │  │
│  │  - Manages resources                      │  │
│  │  - Provides configuration                 │  │
│  └──────────────────────────────────────────┘  │
│                    │                             │
│        ┌───────────┼───────────┐                │
│        ↓           ↓           ↓                │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐           │
│  │   API   │ │   Web   │ │ Worker  │           │
│  │ Service │ │   App   │ │ Service │           │
│  └─────────┘ └─────────┘ └─────────┘           │
│        │           │           │                │
│        └───────────┼───────────┘                │
│                    ↓                             │
│  ┌──────────────────────────────────────────┐  │
│  │   Infrastructure (Docker)                 │  │
│  │  - Redis                                  │  │
│  │  - PostgreSQL                             │  │
│  │  - RabbitMQ                               │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │   Aspire Dashboard                        │  │
│  │  - Logs, Traces, Metrics                  │  │
│  │  - http://localhost:15888                 │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

## Key Benefits

### 1. Faster Inner Loop

**Before:** 30+ minutes to set up  
**After:** < 2 minutes with one command

### 2. Built-in Observability

- Automatic distributed tracing
- Aggregated logs from all services
- Real-time metrics
- No additional configuration needed

### 3. Consistent Environments

- Same setup for all developers
- Infrastructure as code
- No "works on my machine" issues

### 4. Production-Ready

- Deploy to Azure Container Apps with one command
- Kubernetes manifest generation
- Best practices built-in

### 5. Easier Debugging

- See request flow across services
- Correlate logs automatically
- Performance insights out of the box

## Getting Started

### Installation

```bash
# Install .NET Aspire workload
dotnet workload install aspire

# Verify installation
dotnet workload list
```

### Your First Aspire App

```bash
# Create a new Aspire app
dotnet new aspire-starter -n MyFirstAspire
cd MyFirstAspire

# Run it!
cd MyFirstAspire.AppHost
dotnet run
```

The dashboard will open automatically showing your running application!

## Next Steps

Now that you understand what Aspire is and why it's useful, let's dive into the details:

- **Next:** [AppHost Fundamentals](./02-apphost.md)
- **Example:** Run [Hello Aspire](../examples/01-hello-aspire/) to see it in action

## Official Documentation

- [Aspire Overview](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
- [Build Your First Aspire App](https://learn.microsoft.com/dotnet/aspire/get-started/build-your-first-aspire-app)
- [Aspire Architecture](https://learn.microsoft.com/dotnet/aspire/architecture/overview)

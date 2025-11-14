# Module 1: Dev Time Orchestration (Inner Loop)

## Overview
This module introduces .NET Aspire's core concepts for local development and orchestration of distributed applications. Learn how to use AppHost to orchestrate services, configure them with ServiceDefaults, and leverage the Dashboard for enhanced developer experience.

## Learning Objectives
- Understand .NET Aspire core concepts and architecture
- Create and configure an AppHost for orchestration
- Use ServiceDefaults for consistent service configuration
- Manage configuration and secrets effectively
- Navigate and utilize the Aspire Dashboard

## Prerequisites
- .NET SDK 8.0 or later with Aspire workload installed
- Docker Desktop or Podman running
- Visual Studio 2022 or VS Code with C# extensions
- Basic understanding of .NET and C#

## Module Structure

### Topics (Read in Order)

1. **[Introduction to .NET Aspire](./topics/01-introduction.md)**
   - Why Aspire? Problem statement and solutions
   - Core concepts overview
   - When to use Aspire

2. **[AppHost Fundamentals](./topics/02-apphost.md)**
   - What is AppHost?
   - DistributedApplicationBuilder API
   - Defining services and dependencies
   - Resource lifecycle

3. **[ServiceDefaults](./topics/03-service-defaults.md)**
   - Purpose and benefits
   - OpenTelemetry integration
   - Health checks
   - Resilience patterns

4. **[Configuration & Secrets](./topics/04-configuration.md)**
   - Configuration hierarchy
   - User secrets for development
   - Environment variables
   - Parameter resources

5. **[Dashboard](./topics/05-dashboard.md)**
   - Dashboard features and navigation
   - Logs, traces, and metrics
   - Resource monitoring
   - Debugging workflows

6. **[Service Discovery](./topics/06-service-discovery.md)**
   - How services find each other
   - Connection string injection
   - HTTP service references

### Hands-On Examples

Each example can be run independently with `dotnet run`:

1. **[Hello Aspire](./examples/01-hello-aspire/)** - Simplest possible Aspire app
2. **[Multi-Service App](./examples/02-multi-service/)** - Web + API orchestration
3. **[Adding Redis](./examples/03-redis-cache/)** - Infrastructure component integration
4. **[Database Integration](./examples/04-database/)** - PostgreSQL with EF Core
5. **[Complete System](./examples/05-complete-system/)** - Full multi-service application

### Practice Exercises

- **[Guided Lab: Task Manager System](./exercises/lab-task-manager.md)**
  - Step-by-step building a multi-service task management system
  - Covers all topics in the module
  - ~60-90 minutes

## Time Estimate
- Reading topics: 60-90 minutes
- Running examples: 30-45 minutes
- Practice exercises: 60-90 minutes
- **Total: 2.5-3.5 hours**

## Getting Started

1. **Read the topics** in order (01 through 06)
2. **Run the examples** as you learn each concept
3. **Complete the guided lab** to reinforce learning

## Quick Start

To run any example:

```bash
cd examples/01-hello-aspire
dotnet run
```

The Aspire Dashboard will open automatically at `http://localhost:15888`.

## Official Documentation References

- [AppHost Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview)
- [Service Defaults](https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults)
- [Dashboard Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview)

## Next Module

After completing Module 1, proceed to [Module 2: Production Time Orchestration](../module2/README.md).

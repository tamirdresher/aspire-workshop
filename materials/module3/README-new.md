# Module 3: Aspire Extensibility

## Overview
This module covers extending .NET Aspire with custom resources, integrations, and advanced patterns. Learn how to create your own hosting integrations, customize resource behavior, and test Aspire applications.

## Learning Objectives
- Understand Aspire resource model and lifecycle
- Create custom hosting integrations
- Build client integrations for your services
- Implement resource builders and extensions
- Write effective tests for Aspire applications
- Apply advanced extensibility patterns

## Prerequisites
- Completed Modules 1 & 2 or equivalent Aspire experience
- .NET SDK 8.0+ with Aspire workload
- Understanding of C# generics and extension methods
- Familiarity with dependency injection

## Module Structure

### Topics (Read in Order)

1. **[Resource Model](./topics/01-resource-model.md)**
   - IResource interface
   - Resource types
   - Resource lifecycle
   - Annotations

2. **[Custom Hosting Integrations](./topics/02-custom-hosting-integrations.md)**
   - Creating resource builders
   - Container-based resources
   - Executable resources
   - Connection string providers

3. **[Client Integrations](./topics/03-client-integrations.md)**
   - Creating client libraries
   - Configuration binding
   - Health checks
   - OpenTelemetry integration

4. **[Resource Builders](./topics/04-resource-builders.md)**
   - Builder pattern
   - Fluent APIs
   - Method chaining
   - Common patterns

5. **[Testing Aspire Apps](./topics/05-testing.md)**
   - Testing fundamentals
   - DistributedApplication in tests
   - Resource testing
   - Integration testing

6. **[Advanced Patterns](./topics/06-advanced-patterns.md)**
   - Custom annotations
   - Resource hooks
   - Environment customization
   - Production scenarios

### Hands-On Examples

Each example demonstrates extensibility concepts:

1. **[Custom Container Resource](./examples/01-custom-container/)** - Elasticsearch integration
2. **[Executable Resource](./examples/02-executable-resource/)** - Python script orchestration
3. **[Client Integration](./examples/03-client-integration/)** - Custom service client
4. **[Testing Example](./examples/04-testing/)** - Test-driven Aspire development
5. **[Complete Custom Integration](./examples/05-complete-integration/)** - Kafka integration

### Practice Exercises

- **[Guided Lab: Build a Custom Integration](./exercises/lab-custom-integration.md)**
  - Create a complete custom resource (MongoDB)
  - Implement hosting and client integrations
  - Add tests
  - ~120-150 minutes

## Time Estimate
- Reading topics: 90-120 minutes
- Running examples: 60-90 minutes
- Practice exercises: 120-150 minutes
- **Total: 4.5-6 hours**

## Getting Started

1. **Read the topics** in order (01 through 06)
2. **Run the examples** as you learn each concept
3. **Complete the guided lab** to build your own integration

## Quick Start

To run any example:

```bash
cd examples/01-custom-container
dotnet run --project CustomContainer.AppHost
```

## Official Documentation References

- [Custom Hosting Integrations](https://learn.microsoft.com/dotnet/aspire/extensibility/custom-hosting-integration)
- [Custom Client Integrations](https://learn.microsoft.com/dotnet/aspire/extensibility/custom-client-integration)
- [Testing Overview](https://learn.microsoft.com/dotnet/aspire/testing/overview)
- [Resource Annotations](https://learn.microsoft.com/dotnet/aspire/fundamentals/annotations-overview)

## Previous Module

- **Previous:** [Module 2: Production Time Orchestration](../module2/README.md)

## What You'll Build

By the end of this module, you'll be able to:
- Create custom resources for any technology (databases, message brokers, etc.)
- Build reusable integrations that others can use
- Test Aspire applications effectively
- Customize resource behavior for specific needs
- Contribute to the Aspire ecosystem

# Building Distributed Apps with .NET Aspire - Workshop

Welcome to the comprehensive workshop on building distributed applications with .NET Aspire! This hands-on course will guide you through migrating a brownfield ecommerce application to .NET Aspire and mastering its powerful features.

## 🎯 Workshop Overview

.NET Aspire is an opinionated, cloud-ready stack for building observable, production-ready distributed applications. It provides a curated set of components and tools that simplify the development of cloud-native applications.

In this workshop, you'll learn by doing - starting with a traditional .NET ecommerce application and progressively transforming it using Aspire's capabilities.

## 📚 Course Structure

### Start Project
Located in the [`start-project/`](./start-project/) directory, this is a brownfield ecommerce application built with:
- **ECommerce.Api** - ASP.NET Core Web API for product and order management
- **ECommerce.Web** - Razor Pages web frontend
- **ECommerce.Shared** - Shared models and contracts

This application runs without Aspire and has typical challenges like manual service configuration, no centralized observability, and manual orchestration requirements.

### Hands-On Exercises

#### [Exercise 1: Creating a System Topology](./exercises/01-system-topology/)
**Duration**: 45-60 minutes

Learn to:
- Install and configure .NET Aspire
- Create an AppHost project for orchestration
- Set up ServiceDefaults for shared configuration
- Implement service discovery
- Explore the Aspire Dashboard

**Key Takeaway**: Transform manual service configuration into automatic orchestration with built-in observability.

#### [Exercise 2: Deploying Your App](./exercises/02-deploying-app/)
**Duration**: 60-75 minutes

Learn to:
- Containerize your Aspire application
- Generate deployment manifests
- Deploy with Docker Compose
- Deploy to Azure Container Apps
- Manage multi-environment configurations

**Key Takeaway**: Understand various deployment options and how Aspire simplifies the deployment process.

#### [Exercise 3: Aspire Extensibility](./exercises/03-aspire-extensibility/)
**Duration**: 60-90 minutes

Learn to:
- Add component integrations (Redis, PostgreSQL)
- Create custom health checks
- Implement custom metrics and telemetry
- Build reusable Aspire components
- Configure environment-specific resources

**Key Takeaway**: Extend Aspire with components and custom functionality to meet your application's needs.

## 🚀 Getting Started

### Prerequisites

Before starting the workshop, ensure you have:

1. **.NET 9.0 SDK** or later
   ```bash
   dotnet --version
   ```

2. **.NET Aspire Workload**
   ```bash
   dotnet workload install aspire
   ```

3. **Docker Desktop** (for exercises 2 and 3)
   - [Download Docker Desktop](https://www.docker.com/products/docker-desktop)

4. **Your preferred IDE**
   - Visual Studio 2022 (17.12+) with ASP.NET workload
   - Visual Studio Code with C# extension
   - JetBrains Rider 2024.3+

5. **Optional: Azure CLI** (for cloud deployment in Exercise 2)
   ```bash
   az --version
   ```

### Quick Start

1. **Clone this repository**
   ```bash
   git clone https://github.com/tamirdresher/aspire-workshop.git
   cd aspire-workshop
   ```

2. **Test the start project**
   ```bash
   cd start-project
   dotnet build
   ```

3. **Begin Exercise 1**
   ```bash
   cd ../exercises/01-system-topology
   # Follow the README.md instructions
   ```

## 📖 Learning Path

```
Start Project (Traditional App)
    ↓
Exercise 1: System Topology
    ↓ Learn orchestration & observability
Exercise 2: Deployment
    ↓ Learn deployment strategies
Exercise 3: Extensibility
    ↓ Learn custom components
Production-Ready Aspire App! 🎉
```

## 🎓 What You'll Learn

By completing this workshop, you will:

- ✅ Understand the benefits of .NET Aspire for distributed applications
- ✅ Create and configure Aspire orchestration projects
- ✅ Implement automatic service discovery
- ✅ Use the Aspire Dashboard for monitoring and debugging
- ✅ Deploy Aspire applications to various platforms
- ✅ Integrate popular components (Redis, PostgreSQL, etc.)
- ✅ Create custom components and extensions
- ✅ Implement observability with OpenTelemetry
- ✅ Build production-ready cloud-native applications

## 🏗️ Workshop Structure

Each exercise follows this pattern:

1. **Overview** - What you'll learn
2. **Learning Objectives** - Specific skills you'll gain
3. **Prerequisites** - What you need before starting
4. **Step-by-Step Instructions** - Detailed guide with code samples
5. **Verification** - How to validate your work
6. **Key Concepts** - Summary of important points
7. **Common Issues** - Troubleshooting guide
8. **Additional Resources** - Links for deeper learning

## 💡 Best Practices

Throughout the workshop, you'll learn these best practices:

- **Observability First**: Built-in distributed tracing, metrics, and logging
- **Service Discovery**: Automatic service resolution without hardcoded URLs
- **Configuration Management**: Environment-specific settings without code changes
- **Health Checks**: Proactive monitoring of service health
- **Resource Lifecycle**: Proper management of dependencies and resources
- **Development Experience**: Fast inner loop with hot reload and dashboard

## 🤝 Contributing

Found an issue or want to improve the workshop? Contributions are welcome!

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📝 Additional Resources

### Official Documentation
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Aspire Samples](https://github.com/dotnet/aspire-samples)
- [.NET Aspire GitHub](https://github.com/dotnet/aspire)

### Community
- [.NET Aspire Discord](https://aka.ms/aspire/discord)
- [.NET Blog](https://devblogs.microsoft.com/dotnet/)
- [Stack Overflow - aspire tag](https://stackoverflow.com/questions/tagged/dotnet-aspire)

### Related Technologies
- [OpenTelemetry](https://opentelemetry.io/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Docker](https://docs.docker.com/)

## 📄 License

This workshop is provided as-is for educational purposes.

## 🙏 Acknowledgments

This workshop was created to help developers learn .NET Aspire through practical, hands-on experience. Special thanks to the .NET team at Microsoft for creating Aspire and the comprehensive documentation.

---

**Ready to build amazing distributed applications?** Start with [Exercise 1: Creating a System Topology](./exercises/01-system-topology/)! 🚀
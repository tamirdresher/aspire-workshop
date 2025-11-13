# .NET Aspire Workshop

Welcome to the comprehensive .NET Aspire workshop! This hands-on course provides an end-to-end introduction to building robust, distributed applications with .NET Aspire.

> **📖 Official Documentation**: This workshop is aligned with the [official .NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/). See [OFFICIAL-DOCS-REFERENCE.md](./OFFICIAL-DOCS-REFERENCE.md) for detailed mapping to official resources.

## 🎯 Workshop Overview

This workshop empowers developers to build distributed applications with confidence through:
- **Orchestration**: Model your system architecture using AppHost resources
- **Observability**: Built-in OpenTelemetry integration with Aspire Dashboard
- **Production-Ready**: Configuration, secrets management, health checks, and resilience
- **Extensibility**: Create custom resources and extend Aspire capabilities

## 📚 Course Structure

### Module 1: Dev Time Orchestration (Inner Loop)
**Focus**: Local development and the inner loop

**Topics**:
- Why Aspire? Core concepts and building blocks (AppHost, ServiceDefaults, Dashboard)
- DistributedApplicationBuilder for wiring services and infrastructure
- Configuration & secrets management
- Developer inner-loop with `aspire run` or `dotnet run`

**Materials**: [Module 1 Content](./materials/module1/)
- [Teaching Materials](./materials/module1/README.md)
- [Hands-On Exercise](./materials/module1/EXERCISES.md)

**Official Docs**: [AppHost Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview)

### Module 2: Production Time Orchestration (Outer Loop)
**Focus**: Preparing for production deployment

**Topics**:
- OpenTelemetry integration (traces, metrics, logs)
- Health checks and monitoring
- Deployment manifests
- Aspire publishers and deployment options
- Resource customization

**Materials**: [Module 2 Content](./materials/module2/)
- [Teaching Materials](./materials/module2/README.md)
- [Hands-On Exercise](./materials/module2/EXERCISES.md)

**Official Docs**: [Deployment Overview](https://learn.microsoft.com/dotnet/aspire/deployment/overview)

### Module 3: Aspire Extensibility
**Focus**: Extending and customizing Aspire

**Topics**:
- Aspire resources structure and lifecycle
- Creating custom resources
- Resource builders and extensions
- Aspire unit testing

**Materials**: [Module 3 Content](./materials/module3/)
- [Teaching Materials](./materials/module3/README.md)
- [Hands-On Exercise](./materials/module3/EXERCISES.md)

**Official Docs**: [Custom Integrations](https://learn.microsoft.com/dotnet/aspire/extensibility/custom-hosting-integration)

## 🛠️ Comprehensive Exercise: eCommerce Conversion

Transform a traditional eCommerce application ("ShopHub") into an Aspire-orchestrated system through an 8-step guided conversion.

**What You'll Build**:
- Multi-service eCommerce application
- Complete infrastructure orchestration (SQL Server, Redis, RabbitMQ)
- Full observability with distributed tracing
- Production-ready deployment configuration

**Exercise Materials**: [eCommerce Conversion Guide](./exercises/ecommerce-conversion/)
- [Overview & Architecture](./exercises/ecommerce-conversion/README.md)
- [Step-by-Step Guide](./exercises/ecommerce-conversion/STEPS-SUMMARY.md)
- [Detailed Instructions](./exercises/ecommerce-conversion/step-01-current-application.md)

**Time Required**: 2.5-3.5 hours

**Official Reference**: [Migration from Docker Compose](https://learn.microsoft.com/dotnet/aspire/get-started/migrate-from-docker-compose)

## 🎓 Learning Objectives

By completing this workshop, you will be able to:

✅ **Model Architecture**: Define your system's resource graph using AppHost resources  
✅ **Local Observability**: Run distributed systems locally with end-to-end observability  
✅ **Apply Best Practices**: Implement resilience, health checks, and secure configuration  
✅ **Deploy Confidently**: Package and verify solutions for cloud deployment  

## 💻 Prerequisites

### Required Tools
- **.NET SDK 8.0+** with Aspire workload ([Setup Guide](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling))
- **Docker Desktop** or Podman (with WSL2 on Windows)
- **Visual Studio 2022** (latest) or **VS Code** with C# extensions
- **Git**
- **Internet access**

### Optional Tools
- SQL Server Management Studio
- Azure subscription (for cloud deployment exercises)
- Node.js (if working with SPA frontends)

### Installation

#### Install .NET Aspire Workload
```bash
dotnet workload install aspire
```

#### Verify Installation
```bash
dotnet workload list
# Should show 'aspire' in the installed workloads

aspire --version  # For Aspire CLI (9.0+)
```

#### Check Docker
```bash
docker --version
docker ps
```

**Official Setup Guide**: [Setup and Tooling](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)

## 🚀 Getting Started

### Quick Start
1. Clone this repository
2. Navigate to Module 1 materials
3. Follow the teaching guide and exercises
4. Progress through modules sequentially

### Workshop Flow
```
Module 1 (2-3 hours)
    ↓
Module 2 (2-3 hours)
    ↓
Module 3 (2-3 hours)
    ↓
eCommerce Exercise (2.5-3.5 hours)
```

**Total Time**: 8-12 hours (can be split across multiple sessions)

## 📖 Using This Workshop

### For Self-Paced Learning
1. Start with Module 1 README
2. Complete the hands-on exercise
3. Progress through each module
4. Finish with the comprehensive eCommerce conversion

### For Instructors
- Each module has detailed teaching materials
- Exercises include solution code and expected outcomes
- Time estimates help with scheduling
- Troubleshooting sections address common issues

### For Teams
- Can be delivered as a multi-day workshop
- Modules are independent and can be customized
- Includes both teaching materials and practical exercises
- Real-world scenarios prepare for production use

## 📁 Repository Structure

```
aspire-workshop/
├── README.md                           # This file
├── OFFICIAL-DOCS-REFERENCE.md          # Mapping to official documentation
├── materials/                          # Teaching materials
│   ├── module1/                        # Dev time orchestration
│   │   ├── README.md                   # Teaching content
│   │   └── EXERCISES.md                # Hands-on exercises
│   ├── module2/                        # Production orchestration
│   │   ├── README.md
│   │   └── EXERCISES.md
│   └── module3/                        # Aspire extensibility
│       ├── README.md
│       └── EXERCISES.md
└── exercises/                          # Practical exercises
    └── ecommerce-conversion/           # Comprehensive project
        ├── README.md                   # Exercise overview
        ├── STEPS-SUMMARY.md            # Quick reference
        ├── step-01-current-application.md
        ├── step-02-aspire-structure.md
        └── ...                         # Additional steps
```

## 🎯 Learning Outcomes

### Technical Skills
- Aspire AppHost and ServiceDefaults configuration
- Service orchestration and dependency management
- Infrastructure as code with Aspire
- OpenTelemetry and distributed tracing
- Service discovery and configuration management
- Custom resource development
- Production deployment strategies

### Development Practices
- Improving inner-loop development experience
- Implementing observability in distributed systems
- Managing configuration and secrets securely
- Testing distributed applications
- Deploying to cloud platforms

## 🔗 Additional Resources

### Official Documentation
- **[.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)** - Primary resource
- **[Aspire Samples Repository](https://github.com/dotnet/aspire-samples)** - Official code samples
- **[eShop Reference Application](https://github.com/dotnet/eshop)** - Comprehensive example
- **[What's New in Aspire](https://learn.microsoft.com/dotnet/aspire/whats-new/)** - Latest features

### Workshop-Specific
- **[OFFICIAL-DOCS-REFERENCE.md](./OFFICIAL-DOCS-REFERENCE.md)** - Maps workshop to official docs
- Provides version information and migration guidance
- Links to related official tutorials

### Community
- **[.NET Aspire GitHub](https://github.com/dotnet/aspire)** - Source code and issues
- **[Official Discord](https://aka.ms/aspire/discord)** - Community support
- **[Stack Overflow](https://stackoverflow.com/questions/tagged/dotnet-aspire)** - Q&A
- **[.NET Blog](https://devblogs.microsoft.com/dotnet/)** - Announcements and articles

## 🤝 Contributing

This workshop is designed to be comprehensive and practical. If you find issues or have suggestions:
- Open an issue with feedback
- Suggest improvements to exercises
- Share your experience

## 📄 License

This workshop is provided for educational purposes.

## 🙏 Acknowledgments

This workshop is aligned with [official .NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/) and incorporates best practices from the .NET team and community. Special thanks to the Aspire team at Microsoft for creating excellent documentation and samples.

---

**Ready to start?** Begin with [Module 1: Dev Time Orchestration](./materials/module1/README.md)

**Need help aligning with official docs?** See [OFFICIAL-DOCS-REFERENCE.md](./OFFICIAL-DOCS-REFERENCE.md)
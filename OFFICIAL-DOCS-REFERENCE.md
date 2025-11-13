# Official .NET Aspire Documentation Reference

This document maps the workshop content to the official .NET Aspire documentation on Microsoft Learn and provides additional context based on the latest official guidance.

## 📚 Official Documentation Sources

- **Primary Documentation**: [Microsoft Learn - .NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- **Source Repository**: [dotnet/docs-aspire on GitHub](https://github.com/dotnet/docs-aspire)
- **API Reference**: [.NET Aspire API Documentation](https://learn.microsoft.com/dotnet/api?view=dotnet-aspire-9.0)
- **Samples**: [dotnet/aspire-samples](https://github.com/dotnet/aspire-samples)

## 🎯 Workshop Module Alignment with Official Docs

### Module 1: Dev Time Orchestration (Inner Loop)

Our workshop content aligns with these official documentation sections:

#### Core Concepts
| Workshop Topic | Official Documentation |
|---------------|------------------------|
| AppHost Overview | [AppHost Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview) |
| ServiceDefaults | [Service Defaults](https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults) |
| Dashboard Usage | [Dashboard Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview) |
| Resource Orchestration | [Resources in Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/orchestrate-resources) |
| Configuration & Secrets | [External Parameters](https://learn.microsoft.com/dotnet/aspire/fundamentals/external-parameters) |

#### Getting Started
| Workshop Exercise | Official Tutorial |
|-------------------|-------------------|
| Creating Your First Aspire App | [Build Your First Aspire Solution](https://learn.microsoft.com/dotnet/aspire/get-started/build-your-first-aspire-app) |
| System Topology Exercise | [Architecture Overview](https://learn.microsoft.com/dotnet/aspire/architecture/overview) |

#### Key Updates from Official Docs

**Aspire CLI (New in 9.0/13.0)**:
- The workshop uses `dotnet run` but students should know about the new `aspire run` command
- Official reference: [Aspire CLI Overview](https://learn.microsoft.com/dotnet/aspire/cli/overview)

**Terminology Alignment**:
- ✅ "AppHost" - correct (official term)
- ✅ "ServiceDefaults" - correct (official term)
- ✅ "DistributedApplicationBuilder" - correct (official API)
- ⚠️ "Inner loop" - official docs use "locally orchestrate" and "dev-time"

### Module 2: Production Time Orchestration (Outer Loop)

#### Deployment & Publishing
| Workshop Topic | Official Documentation |
|---------------|------------------------|
| Deployment Overview | [Deployment Overview](https://learn.microsoft.com/dotnet/aspire/deployment/overview) |
| Deployment Manifests | [Tool-builder Manifest Schemas](https://learn.microsoft.com/dotnet/aspire/deployment/manifest-format) |
| Azure Container Apps | [Deploy to ACA using Aspire CLI](https://learn.microsoft.com/dotnet/aspire/deployment/aspire-deploy/aca-deployment-aspire-cli) |
| Azure Developer CLI | [Deploy to ACA using azd](https://learn.microsoft.com/dotnet/aspire/deployment/azd/aca-deployment) |

#### Observability
| Workshop Topic | Official Documentation |
|---------------|------------------------|
| OpenTelemetry Integration | [Telemetry](https://learn.microsoft.com/dotnet/aspire/fundamentals/telemetry) |
| Health Checks | [Health Checks](https://learn.microsoft.com/dotnet/aspire/fundamentals/health-checks) |
| Dashboard Features | [Explore Dashboard Features](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/explore) |

#### Key Updates from Official Docs

**New Deployment Tools**:
- `aspire deploy` command (Preview in 13.0)
- `aspire publish` command (Preview in 13.0)
- Official reference: [aspire deploy](https://learn.microsoft.com/dotnet/aspire/cli-reference/aspire-deploy)

**Azure Best Practices**:
- Security best practices now documented
- Official reference: [Azure Security Best Practices](https://learn.microsoft.com/dotnet/aspire/deployment/aspire-deploy/azure-security-best-practices)

### Module 3: Aspire Extensibility

#### Custom Resources
| Workshop Topic | Official Documentation |
|---------------|------------------------|
| Creating Hosting Integrations | [Create Hosting Integrations](https://learn.microsoft.com/dotnet/aspire/extensibility/custom-hosting-integration) |
| Creating Client Integrations | [Create Client Integrations](https://learn.microsoft.com/dotnet/aspire/extensibility/custom-client-integration) |
| Resource Annotations | [Resource Annotations Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/annotations-overview) |

#### Testing
| Workshop Topic | Official Documentation |
|---------------|------------------------|
| Testing Overview | [Testing Overview](https://learn.microsoft.com/dotnet/aspire/testing/overview) |
| Writing Tests | [Write Your First Aspire Test](https://learn.microsoft.com/dotnet/aspire/testing/write-your-first-test) |
| Managing AppHost in Tests | [Managing the AppHost](https://learn.microsoft.com/dotnet/aspire/testing/manage-app-host) |

#### Key Updates from Official Docs

**Testing Infrastructure**:
- `Aspire.Hosting.Testing` namespace is the official approach
- New testing patterns documented with concrete examples

**Community Toolkit**:
- Many community-created integrations now available
- Official reference: [Community Toolkit Overview](https://learn.microsoft.com/dotnet/aspire/community-toolkit/overview)

## 🔄 Version Alignment

### Current Official Version: Aspire 9.0 / 13.0

**Major Features in Aspire 9.0/13.0**:
- Aspire CLI (`aspire` command)
- Improved dashboard with GitHub Copilot integration
- Enhanced deployment state caching
- Azure Container App Jobs support
- Standalone dashboard mode improvements
- Python and Node.js orchestration

**Workshop Content Status**:
- ✅ Based on Aspire 8.0+ concepts (compatible with 9.0)
- ⚠️ Some examples may need updating for 9.0/13.0 features
- 📌 Students should be aware of version-specific features

### Migration Path
For students working with different versions:
- [What's New in Aspire 13](https://aspire.dev/whats-new/aspire-13/)
- [Upgrade to Aspire 13.0](https://learn.microsoft.com/dotnet/aspire/get-started/upgrade-to-aspire-13)

## 🎯 Integration Components

### Workshop vs Official Integrations

Our workshop covers these integrations (aligned with official docs):

#### Databases
| Integration | Workshop Module | Official Docs |
|------------|----------------|---------------|
| SQL Server | Module 1 & 2 | [SQL Server Integration](https://learn.microsoft.com/dotnet/aspire/database/sql-server-integration) |
| PostgreSQL | Module 1 | [PostgreSQL Integration](https://learn.microsoft.com/dotnet/aspire/database/postgresql-integration) |
| MongoDB | Module 3 (Custom) | [MongoDB Integration](https://learn.microsoft.com/dotnet/aspire/database/mongodb-integration) |

#### Caching
| Integration | Workshop Module | Official Docs |
|------------|----------------|---------------|
| Redis | Module 1 & 2 | [Redis Caching Overview](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-caching-overview) |
| Distributed Cache | Module 2 | [Redis Distributed Cache](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-distributed-caching-integration) |

#### Messaging
| Integration | Workshop Module | Official Docs |
|------------|----------------|---------------|
| RabbitMQ | Module 1 & eCommerce | [RabbitMQ Integration](https://learn.microsoft.com/dotnet/aspire/messaging/rabbitmq-integration) |
| Azure Service Bus | Referenced | [Azure Service Bus](https://learn.microsoft.com/dotnet/aspire/messaging/azure-service-bus-integration) |
| Kafka | Module 3 (Custom) | [Apache Kafka](https://learn.microsoft.com/dotnet/aspire/messaging/kafka-integration) |

#### Custom Examples (Workshop Only)
- Elasticsearch (Module 3) - demonstrates custom resource creation
- Monitoring Stack (Module 3) - learning extensibility patterns

**Note**: These custom examples teach extensibility concepts; official integrations exist for many services in the [Community Toolkit](https://learn.microsoft.com/dotnet/aspire/community-toolkit/overview).

## 📖 Additional Official Resources

### Beyond the Workshop

These official topics extend beyond workshop scope but are valuable:

#### Advanced Topics
- [Azure Functions with Aspire](https://learn.microsoft.com/dotnet/aspire/serverless/functions)
- [Kubernetes Integration](https://learn.microsoft.com/dotnet/aspire/deployment/kubernetes-integration)
- [Orleans Framework](https://learn.microsoft.com/dotnet/aspire/frameworks/orleans)
- [YARP Reverse Proxy](https://learn.microsoft.com/dotnet/aspire/proxies/yarp-integration)
- [Azure AI Integrations](https://learn.microsoft.com/dotnet/aspire/azureai/azureai-foundry-integration)

#### Security & Authentication
- [Keycloak Integration](https://learn.microsoft.com/dotnet/aspire/authentication/keycloak-integration)
- [Azure Key Vault](https://learn.microsoft.com/dotnet/aspire/security/azure-security-key-vault-integration)
- [Secure Communication Between Integrations](https://learn.microsoft.com/dotnet/aspire/extensibility/secure-communication-between-integrations)

#### Development Tools
- [Visual Studio Code Extension](https://learn.microsoft.com/dotnet/aspire/fundamentals/aspire-vscode-extension)
- [GitHub Codespaces](https://learn.microsoft.com/dotnet/aspire/get-started/github-codespaces)
- [Dev Containers](https://learn.microsoft.com/dotnet/aspire/get-started/dev-containers)

## 🔧 Hands-On Labs (Official)

Microsoft provides official tutorials that complement this workshop:

1. **Caching Tutorial**: [Caching using Redis integrations](https://learn.microsoft.com/dotnet/aspire/caching/caching-integrations)
2. **Database Tutorial**: [Connect to SQL Server with EF Core](https://learn.microsoft.com/dotnet/aspire/database/sql-server-integrations)
3. **Storage Tutorial**: [Connect to storage](https://learn.microsoft.com/dotnet/aspire/storage/azure-storage-integrations)
4. **Messaging Tutorial**: [Messaging using Aspire integrations](https://learn.microsoft.com/dotnet/aspire/messaging/messaging-integrations)

## ⚠️ Important Differences & Clarifications

### Terminology
- **Official**: "Locally orchestrate" → **Workshop**: "Inner loop"
- **Official**: "AppHost" → **Workshop**: "AppHost" ✅
- **Official**: "Integrations" → **Workshop**: "Components" (we use both)

### Command Line Tools
- **Workshop uses**: `dotnet run --project AppHost`
- **Official 9.0+**: `aspire run` (new CLI)
- Both are valid; CLI is the modern approach

### API Versions
- Workshop examples target .NET 8.0
- Official docs reference .NET 9.0 APIs
- Core concepts remain the same

## 🎓 Learning Path Recommendation

### For Workshop Participants

**Suggested Learning Sequence**:

1. **Complete Workshop Modules 1-3** (8-12 hours)
   - Hands-on foundation with practical exercises

2. **Read Official Getting Started** (2-3 hours)
   - [Aspire Overview](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
   - [Build Your First Aspire App](https://learn.microsoft.com/dotnet/aspire/get-started/build-your-first-aspire-app)
   - [Setup and Tooling](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)

3. **Complete eCommerce Exercise** (2.5-3.5 hours)
   - Real-world application scenario

4. **Explore Official Integrations** (varies)
   - [Integrations Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/integrations-overview)
   - Deep dive into specific integrations needed for your projects

5. **Study Deployment Options** (3-4 hours)
   - [Deployment Overview](https://learn.microsoft.com/dotnet/aspire/deployment/overview)
   - Platform-specific guides (Azure, Kubernetes, etc.)

6. **Advanced Topics** (as needed)
   - Testing, Security, Performance tuning
   - Custom integrations for your scenarios

### Recommended Official Samples

After completing the workshop, explore these official samples:

1. **[eShop](https://github.com/dotnet/eshop)** - Comprehensive reference application
2. **[Orleans Voting Sample](https://github.com/dotnet/aspire-samples/tree/main/samples/Orleans)** - Advanced patterns
3. **[AspireYouTube](https://github.com/dotnet/aspire-samples/tree/main/samples/AspireYouTube)** - Multi-tier application
4. **Browse all samples**: [Aspire Samples Browser](https://learn.microsoft.com/samples/browse?expanded=dotnet&terms=aspire)

## 🔗 Quick Reference Links

### Essential Documentation
- [Aspire Overview](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
- [AppHost Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview)
- [Dashboard Overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview)
- [Service Defaults](https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults)

### API References
- [Aspire.Hosting API](https://learn.microsoft.com/dotnet/api?term=Aspire.Hosting&view=dotnet-aspire-9.0)
- [Aspire.Hosting.Azure API](https://learn.microsoft.com/dotnet/api?term=Aspire.Hosting.Azure&view=dotnet-aspire-9.0)
- [Full API Reference](https://learn.microsoft.com/dotnet/api?view=dotnet-aspire-9.0)

### Community & Support
- [Official Discord](https://aka.ms/aspire/discord)
- [Stack Overflow - dotnet-aspire tag](https://stackoverflow.com/questions/tagged/dotnet-aspire)
- [GitHub Issues](https://github.com/dotnet/aspire/issues)
- [GitHub Discussions](https://github.com/dotnet/aspire/discussions)

## 📝 Notes for Instructors

### Teaching with Official Documentation

**When to Reference Official Docs**:
- During concept introduction: Show official overview pages
- For API details: Reference official API documentation
- For troubleshooting: Use official troubleshooting guides
- For updates: Point to "What's New" documentation

**How to Bridge Content**:
1. Start with workshop hands-on exercises (practical skills)
2. Reference official docs for deeper understanding
3. Use official samples for additional practice
4. Encourage exploration of official integration catalog

### Version Management

**For Workshops**:
- Verify Aspire version students will use
- Note version-specific features
- Provide upgrade guidance if needed
- Keep workshop materials version-agnostic where possible

## 🔄 Keeping Current

### Official Release Information

**Stay Updated**:
- [What's New](https://learn.microsoft.com/dotnet/aspire/whats-new/)
- [Breaking Changes](https://learn.microsoft.com/dotnet/aspire/compatibility/breaking-changes)
- [Official Support Policy](https://dotnet.microsoft.com/platform/support/policy/aspire)

### Contributing

The official documentation is open source:
- Repository: https://github.com/dotnet/docs-aspire
- Contributions welcome via pull requests
- Follow .NET Foundation Code of Conduct

---

## Summary

This workshop provides hands-on, practical learning that aligns with official .NET Aspire documentation. Use this reference to:

✅ **Connect workshop concepts to official documentation**  
✅ **Find authoritative resources for deeper learning**  
✅ **Stay current with latest Aspire features**  
✅ **Extend beyond workshop scope with official guides**  

The workshop emphasizes practical skills through exercises, while official documentation provides comprehensive reference material. Together, they create a complete learning experience.

**Questions or Suggestions?**  
Open an issue or contribute improvements to align workshop content with evolving official documentation.

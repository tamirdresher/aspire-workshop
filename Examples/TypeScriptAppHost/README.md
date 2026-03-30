# TypeScript AppHost Example ⭐ NEW in Aspire 13.2

This example demonstrates the **TypeScript AppHost** feature introduced in Aspire 13.2 - write your application orchestration in TypeScript instead of C#!

## Overview

TypeScript AppHost provides the same powerful orchestration capabilities as C# AppHost, but with TypeScript syntax and tooling. This is perfect for:

- **JavaScript/TypeScript-first teams** who prefer working in TypeScript
- **Polyglot projects** orchestrating multiple language services
- **Rapid prototyping** with TypeScript's flexible type system
- **Cross-platform development** without .NET SDK dependency

## Key Features

- **Auto-generated Integration SDKs** - TypeScript type definitions generated from .NET integration packages
- **Service Discovery** - Same powerful service references as C# AppHost
- **Unified Configuration** - `aspire.config.json` for all settings
- **Full Dashboard Support** - All dashboard features work identically

## Getting Started

### Prerequisites

- Node.js 18+ with npm
- Aspire CLI 13.2+
- Docker Desktop (for container resources)

### Quick Start

1. **Create a new TypeScript AppHost**:
   ```bash
   aspire init --language typescript
   ```

2. **Review the generated structure**:
   ```
   TypeScriptAppHost/
   ├── apphost.ts              # Main orchestration file
   ├── aspire.config.json      # Aspire configuration
   ├── package.json            # Node.js dependencies
   └── tsconfig.json           # TypeScript configuration
   ```

3. **Install dependencies**:
   ```bash
   npm install
   ```

4. **Restore Aspire integrations** (generates TypeScript SDKs):
   ```bash
   aspire restore
   ```
   
   This creates a `.modules/` directory with TypeScript definitions for all Aspire integrations.

5. **Run the AppHost**:
   ```bash
   aspire run
   ```

## TypeScript AppHost Example

Here's a simple TypeScript AppHost that orchestrates an API service with Redis:

```typescript
// apphost.ts
import { createBuilder } from "@aspire/hosting";

const builder = createBuilder();

// Add Redis cache
const cache = builder.addRedis("cache");

// Add API service with Redis reference
const api = builder.addProject("api", "./services/api/api.csproj")
    .withReference(cache);

// Add Web frontend with API reference
const web = builder.addProject("web", "./services/web/web.csproj")
    .withReference(api);

await builder.build().runAsync();
```

## Integration SDK Generation

When you run `aspire restore`, Aspire generates TypeScript SDKs in the `.modules/` folder:

```typescript
// Auto-generated from Aspire.Hosting.Redis
declare module "@aspire/hosting" {
    interface IDistributedApplicationBuilder {
        addRedis(name: string): IResourceBuilder<RedisResource>;
    }
}
```

These provide full IntelliSense and type safety in your TypeScript AppHost.

## Configuration: aspire.config.json

TypeScript apphosts use a unified configuration file:

```json
{
  "appHost": {
    "path": "apphost.ts",
    "language": "typescript/nodejs"
  },
  "sdk": {
    "version": "13.2.0"
  },
  "channel": "stable",
  "profiles": {
    "default": {
      "applicationUrl": "https://localhost:17000;http://localhost:15000"
    }
  }
}
```

## Comparison: TypeScript vs C# AppHost

| Feature | TypeScript AppHost | C# AppHost |
|---------|-------------------|------------|
| **Service Discovery** | ✅ Fully supported | ✅ Fully supported |
| **Integration Packages** | ✅ Auto-generated SDKs | ✅ NuGet packages |
| **Dashboard** | ✅ Full support | ✅ Full support |
| **Custom Resources** | ✅ Via TypeScript | ✅ Via C# |
| **Type Safety** | ✅ TypeScript types | ✅ C# types |
| **Requires .NET SDK** | ❌ No (self-contained) | ✅ Yes |
| **Build Performance** | ⚡ Fast (no compilation) | 🔄 Standard .NET build |
| **IDE Support** | VS Code, WebStorm | Visual Studio, VS Code |

## When to Use TypeScript AppHost

**Choose TypeScript AppHost when:**
- Your team primarily works in JavaScript/TypeScript
- You're orchestrating polyglot services (Node.js, Python, Go, .NET)
- You want faster iteration without .NET build times
- You prefer TypeScript tooling and ecosystem

**Stick with C# AppHost when:**
- Your team is .NET-focused
- You're building custom resource types requiring .NET libraries
- You want full IntelliSense in Visual Studio
- You prefer static compilation and C# type system

## Advanced Features

### Adding Integrations

```typescript
// Redis
const redis = builder.addRedis("cache");

// PostgreSQL
const postgres = builder.addPostgres("postgres")
    .addDatabase("mydb");

// Azure CosmosDB
const cosmos = builder.addAzureCosmosDb("cosmos")
    .addDatabase("catalogdb");
```

### Service References

```typescript
const api = builder.addProject("api", "./api/api.csproj");
const worker = builder.addProject("worker", "./worker/worker.csproj")
    .withReference(api);  // Worker can discover API service
```

### Container Resources

```typescript
const nginx = builder.addContainer("webserver", "nginx", "latest")
    .withHttpEndpoint(port: 8080);
```

### Environment Variables

```typescript
const api = builder.addProject("api", "./api/api.csproj")
    .withEnvironment("LOG_LEVEL", "Debug")
    .withEnvironment("FEATURE_FLAGS", "EnableCaching");
```

## Learn More

- [TypeScript AppHost Documentation](https://aspire.dev/fundamentals/typescript-apphost/)
- [Aspire 13.2 Release Notes](https://aspire.dev/whats-new/aspire-13-2/)
- [Complete Example with Running Code](https://github.com/tamirdresher/aspire-session-2026-03/tree/main/02-typescript-apphost)

## Related Examples

- [Basic AppHost (C#)](../AspireCustomResource/) - Compare with C# implementation
- [Service Discovery](../Services/) - Service communication patterns
- [Testing](../Testing/) - Integration and E2E testing strategies

## Next Steps

1. Create your own TypeScript AppHost with `aspire init --language typescript`
2. Add integrations with `aspire add <integration>`
3. Explore the generated `.modules/` SDK code
4. Compare side-by-side with C# AppHost in other examples
5. Build a polyglot app orchestrating TypeScript, Python, and .NET services together!

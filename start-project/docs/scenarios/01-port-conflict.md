# Scenario: Port Conflict Management

## The Problem

You're on a development team with multiple developers working on the e-commerce application. Everyone needs to run services locally for debugging.

## Current Architecture: Fixed Port Allocation

| Service | Port | Configurable In |
|---------|------|-----------------|
| Catalog.API | 7001 | [`launchSettings.json`](../../src/Catalog.API/Properties/launchSettings.json) |
| Basket.API | 7002 | [`launchSettings.json`](../../src/Basket.API/Properties/launchSettings.json) |
| Ordering.API | 7003 | [`launchSettings.json`](../../src/Ordering.API/Properties/launchSettings.json) |
| AIAssistant.API | 7004 | [`launchSettings.json`](../../src/AIAssistant.API/Properties/launchSettings.json) |
| React Frontend | 5173 | Vite default |
| Cosmos DB Emulator | 8081 | Emulator default |
| Azurite (Blob) | 10000 | Azurite default |
| Azurite (Queue) | 10001 | Azurite default |
| Azurite (Table) | 10002 | Azurite default |

**Total ports in use:** 9 (plus OS ephemeral ports)

## Scenario Walkthrough

### Monday 9:00 AM - Developer A Starts Work

```bash
# Developer A
$ cd src/Catalog.API
$ dotnet run

# ✅ Success
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
```

All services start successfully. Developer A is running the full stack.

### Monday 10:00 AM - Developer B Joins

Developer B wants to debug a specific issue in Basket.API:

```bash
# Developer B
$ cd src/Basket.API
$ dotnet run

# ❌ Error!
crit: Microsoft.AspNetCore.Server.Kestrel[0]
      Unable to start Kestrel.
System.IO.IOException: Failed to bind to address https://127.0.0.1:7002:
address already in use.
```

**Problem:** Developer A is already using port 7002!

## Current "Solution": Manual Port Reconfiguration

Developer B must now:

### Step 1: Change Basket.API Port

Edit [`src/Basket.API/Properties/launchSettings.json`](../../src/Basket.API/Properties/launchSettings.json):

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "openapi/v1.json",
      "applicationUrl": "https://localhost:7012;http://localhost:5012",  // ← Changed from 7002
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Step 2: Update Ordering.API Configuration

Ordering.API calls Basket.API, so its configuration must be updated:

Edit [`src/Ordering.API/appsettings.json`](../../src/Ordering.API/appsettings.json):

```json
{
  "ServiceUrls": {
    "BasketApi": "https://localhost:7012"  // ← Changed from 7002
  }
}
```

### Step 3: Update Frontend Configuration

Edit [`src/ecommerce-web/.env.local`](../../src/ecommerce-web/.env.local):

```env
VITE_BASKET_API=https://localhost:7012  # Changed from 7002
```

### Step 4: Restart Basket.API

```bash
cd src/Basket.API
dotnet run

# ✅ Now works on port 7012
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7012
```

### Step 5: Restart Ordering.API

```bash
cd src/Ordering.API
dotnet run

# ✅ Now points to Basket.API on 7012
```

### Step 6: Restart Frontend

```bash
cd src/ecommerce-web
npm run dev

# ✅ Now points to Basket.API on 7012
```

## The Cost

**Files Modified:** 3  
**Services Restarted:** 3  
**Time Spent:** 5-10 minutes  
**Risk of Errors:** High (easy to forget one config file)  
**Git Complications:** Must remember NOT to commit these changes!

## What Happens Next

### Developer B Finishes, Goes Home

Developer B must now **revert all changes**:
1. Restore `launchSettings.json` to port 7002
2. Restore `appsettings.json` in Ordering.API
3. Restore `.env.local` in frontend

If Developer B forgets and commits these changes:
- Developer A's environment breaks
- CI/CD pipeline might break
- Staging environment might break
- Code review detects "unnecessary config changes"

## Real-World Complexity Multiplier

### Scenario: 3 Developers, 4 Services

**Morning:**
- Developer A: Running all 4 services (ports 7001-7004)
- Developer B: Wants to debug Basket.API
- Developer C: Wants to debug Ordering.API

**Port Assignments:**

| Service | Dev A | Dev B | Dev C |
|---------|-------|-------|-------|
| Catalog.API | 7001 | - | - |
| Basket.API | 7002 | 7012 | - |
| Ordering.API | 7003 | - | 7013 |
| AIAssistant.API | 7004 | - | - |

**Configuration Updates Required:**
- Dev B: 3 files (launchSettings + 2 dependent services)
- Dev C: 2 files (launchSettings + frontend)
- If Dev C's Ordering calls Dev B's Basket: Another config change!

**Total:** 7-10 configuration files modified just to avoid port conflicts!

## The Aspire Solution

With .NET Aspire, ports are **dynamically allocated** at runtime:

```csharp
// In AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(catalogApi);  // Automatic service discovery - no ports needed!
var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(basketApi);

builder.Build().Run();
```

### What Happens:

1. **First Run (Dev A):**
   - Catalog.API: Port 7001 (dynamically allocated)
   - Basket.API: Port 7002 (dynamically allocated)
   - Ordering.API: Port 7003 (dynamically allocated)

2. **Second Run (Dev B, same time):**
   - Catalog.API: Port 7101 (automatically different!)
   - Basket.API: Port 7102 (automatically different!)
   - Ordering.API: Port 7103 (automatically different!)

3. **Service Discovery:**
   - Ordering.API doesn't care what port Basket.API is on
   - Uses `https+http://basket-api` instead of hardcoded URL
   - Aspire resolves to actual port at runtime

### Results:

| Metric | Without Aspire | With Aspire |
|--------|---------------|-------------|
| **Files to Edit** | 3-10 per developer | 0 |
| **Port Conflicts** | Frequent | Never |
| **Time to Resolve** | 5-10 minutes | 0 seconds |
| **Risk of Errors** | High | None |
| **Git Noise** | Port config changes | None |
| **Team Coordination** | "Who's using port 7002?" | Not needed |

## Try It Yourself

### Experience the Pain

1. Start the application following the [Running Locally guide](../../README.md#running-locally)
2. In a different terminal, try to start the same service again
3. Watch it fail with "address already in use"
4. Try the manual port reconfiguration process above

### Then See the Relief

Complete [Exercise 1](../../../exercises/01-system-topology/README.md) and:
1. Start the application with Aspire (F5)
2. Note which ports are allocated
3. Stop and start again
4. Note how ports can change, but everything still works!
5. Run multiple instances - no conflicts!

## Conclusion

Port conflict management is a **hidden time sink** in microservices development. 

- **Conservative estimate:** 10 minutes per conflict
- **Frequency:** 2-3 times per day per developer
- **Annual cost:** 100+ hours per team of 5 developers

.NET Aspire **eliminates this entirely** with dynamic port allocation and automatic service discovery.

Next: [Environment Configuration Chaos](02-environment-config.md)
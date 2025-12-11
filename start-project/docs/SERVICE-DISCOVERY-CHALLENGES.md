# Service Discovery Challenges Without .NET Aspire

This document explains the problems you'll encounter when managing service-to-service communication manually in a microservices architecture.

## The Problem: Manual Service Discovery

In this brownfield e-commerce application, we have **4 microservices** that need to communicate with each other:

```
Frontend (React) → Catalog API → Cosmos DB
                 → Basket API → Catalog API → Cosmos DB
                              → Queue Storage
                 → Ordering API → Basket API → Queue Storage
                 → AI Assistant API → OpenAI
```

### Current Architecture Pain Points

#### 1. **Hardcoded URLs Everywhere**

**Problem:** Every service dependency requires a hardcoded URL configuration.

**Evidence in Code:**
- [`Basket.API/appsettings.json`](../src/Basket.API/appsettings.json:10) - Contains `CatalogApi: "https://localhost:7001"`
- [`Ordering.API/appsettings.json`](../src/Ordering.API/appsettings.json:10) - Contains `BasketApi: "https://localhost:7002"`
- [`ecommerce-web/src/services/apiService.ts`](../src/ecommerce-web/src/services/apiService.ts:3-8) - All 4 API URLs hardcoded

**Consequences:**
- **12+ configuration entries** across all files for just 4 services
- Each environment (dev, staging, production) needs separate configurations
- Port changes require updates in multiple places
- New services multiply the configuration burden

#### 2. **Port Conflict Hell**

**Scenario:** You're on a team of 5 developers.

**Current Port Allocation:**
- Catalog.API: `7001`
- Basket.API: `7002`
- Ordering.API: `7003`
- AIAssistant.API: `7004`
- React Frontend: `5173`
- Cosmos DB Emulator: `8081`
- Azurite (Storage): `10000-10002`

**What Happens:**
1. Developer A runs the full stack → Occupies ports 5173, 7001-7004, 8081, 10000-10002
2. Developer B wants to debug just Basket.API → Port 7002 is taken
3. Developer B must:
   - Edit [`launchSettings.json`](../src/Basket.API/Properties/launchSettings.json)
   - Change port to 7012
   - Update [`appsettings.json`](../src/Ordering.API/appsettings.json) in Ordering.API to point to 7012
   - Update frontend [`apiService.ts`](../src/ecommerce-web/src/services/apiService.ts) if testing UI
   - Remember to change it back before committing

**With Aspire:** Ports are automatically allocated. No conflicts, ever.

#### 3. **Environment Configuration Matrix**

**The Math:**
- 4 microservices with inter-service dependencies
- 3 environments (Development, Staging, Production)
- Each service can call 1-2 other services

**Configuration Entries Needed:**
```
Frontend config: 4 API URLs × 3 environments = 12 entries
Basket.API config: 1 dependency × 3 environments = 3 entries
Ordering.API config: 1 dependency × 3 environments = 3 entries
Total: 18+ configuration entries to maintain
```

**Current State (Development):**
```json
// Frontend - .env.development
VITE_CATALOG_API=https://localhost:7001
VITE_BASKET_API=https://localhost:7002
VITE_ORDERING_API=https://localhost:7003
VITE_AI_ASSISTANT_API=https://localhost:7004

// Frontend - .env.staging
VITE_CATALOG_API=https://staging-catalog.company.com
VITE_BASKET_API=https://staging-basket.company.com
VITE_ORDERING_API=https://staging-ordering.company.com
VITE_AI_ASSISTANT_API=https://staging-ai.company.com

// And so on for production...
```

**With Aspire:** One AppHost configuration. Service names resolve automatically in all environments.

#### 4. **HTTPS/HTTP Multi-Scheme Complexity**

**Problem:** Different environments use different schemes.

**Current Reality:**
- **Local Development:** HTTP only (no certificates)
  - Frontend: `http://localhost:5173`
  - APIs: `https://localhost:700x` (self-signed certs)
  
- **Staging:** HTTPS with staging certificates
  - `https://staging-api.company.com`
  
- **Production:** HTTPS with production certificates
  - `https://api.company.com`

**Code Impact:**
```typescript
// In apiService.ts - which scheme do we use?
const API_BASE = {
  catalog: import.meta.env.VITE_CATALOG_API || 'https://localhost:7001',
  // But localhost might not have HTTPS working...
  // Should we try HTTP first, then HTTPS?
  // Do we need different configurations for development?
}
```

**Certificate Issues:**
- Self-signed certificates in development → Browser warnings
- Different certificate chains per environment
- Certificate rotation requires config updates

**With Aspire:** Use `https+http://service-name` - tries HTTPS first, falls back to HTTP automatically.

#### 5. **Startup Order Dependencies**

**The Challenge:** Services depend on each other. You must start them in the correct order.

**Current Manual Process:**
```bash
# Terminal 1 - Start Cosmos DB Emulator
docker run -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator

# Terminal 2 - Start Azurite (Storage Emulator)
azurite --silent

# Terminal 3 - Start Catalog.API (required by Basket)
cd src/Catalog.API
dotnet run

# Terminal 4 - Start Basket.API (required by Ordering)
cd src/Basket.API
dotnet run

# Terminal 5 - Start Ordering.API
cd src/Ordering.API
dotnet run

# Terminal 6 - Start AI Assistant (optional)
cd src/AIAssistant.API
dotnet run

# Terminal 7 - Start Frontend
cd src/ecommerce-web
npm run dev
```

**Total:** 7 terminals/windows to manage!

**What Goes Wrong:**
- Start Ordering before Basket → Fails with "Basket API unavailable"
- Start Basket before Catalog → Fails with "Catalog API unavailable"
- Forget to start Azurite → Queue operations fail silently
- Cosmos DB not ready → Catalog API crashes on startup

**With Aspire:** Press F5 once. Everything starts in the correct order automatically.

#### 6. **No Centralized Observability**

**Current State:** To debug an issue, you need to:

1. **Check 7 different log outputs:**
   - Terminal 1: Cosmos DB logs
   - Terminal 2: Azurite logs
   - Terminal 3: Catalog.API console
   - Terminal 4: Basket.API console
   - Terminal 5: Ordering.API console
   - Terminal 6: AI Assistant console
   - Terminal 7: React dev server logs

2. **Correlate logs manually:**
   ```
   [Catalog.API] GET /api/catalog/123
   [Basket.API] Validating product 123...
   [Catalog.API] Returning product 123
   [Basket.API] Product validated
   ```
   - Good luck finding these across 4 different terminal windows!

3. **No distributed tracing:**
   - Can't see the full request flow: Frontend → Basket → Catalog → Cosmos DB
   - Can't measure performance across services
   - Can't identify bottlenecks in the call chain

**With Aspire:** Single dashboard shows:
- All service logs aggregated
- Distributed traces across all services
- Performance metrics
- Health status

#### 7. **Service Discovery at Scale**

**Current System:** 4 services = Manageable (barely)

**Real-World Scenario:** 20+ services

**Configuration Explosion:**
```
Service A needs: B, C, D (3 dependencies × 3 environments = 9 configs)
Service E needs: A, F, G, H (4 dependencies × 3 environments = 12 configs)
Service I needs: E, J (2 dependencies × 3 environments = 6 configs)
... and so on

Total: 100+ configuration entries to maintain
```

**Maintenance Nightmare:**
- Service B changes port → Update configurations in A, C, D, E, F
- New environment added → Multiply all configurations by 4
- Service renamed → Find and replace in 20+ files
- Onboarding new developer → "Here's a 50-line README on how to configure everything"

**With Aspire:** Add service once in AppHost. All dependencies resolve automatically.

## Real-World Failure Scenarios

### Scenario 1: "Works on My Machine"

**Situation:** Developer A's setup works perfectly. Developer B clones the repo.

**Developer B's Experience:**
```bash
$ cd src/ecommerce-web
$ npm run dev

# Frontend loads, shows empty products
# Browser console: "Failed to fetch products"
# Check: http://localhost:7001 → "Connection refused"

# Oh, need to start Catalog.API
$ cd src/Catalog.API
$ dotnet run
# Error: "Cannot connect to Cosmos DB"

# Ah, need Cosmos DB Emulator
$ docker run -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
# Wait 2 minutes for emulator to start...

# Try again...
$ cd src/Catalog.API
$ dotnet run
# Now it works!

# Back to frontend... Products appear!
# Try to add to basket...
# Error: "Failed to update basket"

# Repeat process for Basket.API, Azurite, etc.
```

**Time to first successful run:** 30-45 minutes

**With Aspire:** Clone, F5, running in 2 minutes.

### Scenario 2: Environment Configuration Drift

**Problem:** Staging config gets out of sync with development.

**What Happens:**
1. Team adds new `Payment.API` service
2. Update local `appsettings.Development.json` files
3. Push to staging
4. Forget to update `appsettings.Staging.json`
5. Staging deployment succeeds but payment features broken
6. Debug for 2 hours before realizing config is wrong

**With Aspire:** Config drift impossible - single source of truth in AppHost.

### Scenario 3: The Debug Session from Hell

**Situation:** User reports: "Checkout is failing in production"

**Current Debugging Process:**
1. Check frontend logs → Request sent to Basket.API ✓
2. SSH into Basket.API server → Check logs
3. See: "Catalog.API returned 500"
4. SSH into Catalog.API server → Check logs  
5. See: "Cosmos DB connection timeout"
6. Check Cosmos DB metrics
7. Total time across 4 systems: 45 minutes

**With Aspire Dashboard:** See the entire trace in one view in 2 minutes.

## The Solution: .NET Aspire Service Discovery

### How Aspire Solves This

**Instead of:**
```csharp
// Manual configuration
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    var url = configuration["ServiceUrls:CatalogApi"] 
        ?? throw new Exception("Config missing!");
    client.BaseAddress = new Uri(url);
});
```

**You get:**
```csharp
// AppHost - declare once
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(catalogApi);  // Automatic service discovery!

// In Basket.API - use by name
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https+http://catalog-api");
    // Aspire resolves "catalog-api" to the actual URL automatically
});
```

### Benefits Summary

| Challenge | Without Aspire | With Aspire |
|-----------|---------------|-------------|
| **Configuration** | 18+ manual entries across files | Single AppHost declaration |
| **Port Management** | Manual allocation, conflicts common | Automatic, conflict-free |
| **Startup** | 7 terminals, manual ordering | F5, automatic orchestration |
| **Environment Sync** | Copy/paste configs × environments | Automatic in all environments |
| **Observability** | 7 separate log streams | Unified dashboard |
| **Debugging** | Hunt across terminals | Distributed tracing |
| **Onboarding** | 50-line setup guide | "Clone and F5" |
| **Service Discovery** | Hardcoded URLs | Automatic resolution |

## Next Steps

Ready to see how Aspire transforms this chaos into simplicity?

→ [Exercise 1: Creating a System Topology](../../exercises/01-system-topology/README.md)

You'll migrate this exact brownfield application to .NET Aspire and experience the difference firsthand!
# Scenario: Environment Configuration Matrix

## The Problem

Your e-commerce application needs to run in multiple environments with different service URLs. Without Aspire, you must manually maintain configurations for each environment.

## The Configuration Matrix

### Environments
1. **Local Development** - Developer machines (localhost)
2. **Integration** - Shared dev environment (dev.company.com)
3. **Staging** - Pre-production (staging.company.com)
4. **Production** - Live system (company.com)

### Services Requiring Configuration
1. Frontend (calls 4 backend APIs)
2. Basket.API (calls Catalog.API)
3. Ordering.API (calls Basket.API)

## Current State: Manual Configuration Hell

### Configuration Entries Required

| Environment | Frontend | Basket.API | Ordering.API | **Total** |
|-------------|----------|------------|--------------|-----------|
| Development | 4 URLs | 1 URL | 1 URL | **6** |
| Integration | 4 URLs | 1 URL | 1 URL | **6** |
| Staging | 4 URLs | 1 URL | 1 URL | **6** |
| Production | 4 URLs | 1 URL | 1 URL | **6** |
| **TOTAL** | **16** | **4** | **4** | **24 URLs!** |

## Detailed Breakdown

### Development Environment

#### Frontend: `.env.development`
```env
VITE_CATALOG_API=https://localhost:7001
VITE_BASKET_API=https://localhost:7002
VITE_ORDERING_API=https://localhost:7003
VITE_AI_ASSISTANT_API=https://localhost:7004
```

#### Basket.API: `appsettings.Development.json`
```json
{
  "ServiceUrls": {
    "CatalogApi": "https://localhost:7001"
  }
}
```

#### Ordering.API: `appsettings.Development.json`
```json
{
  "ServiceUrls": {
    "BasketApi": "https://localhost:7002"
  }
}
```

### Integration Environment

#### Frontend: `.env.integration`
```env
VITE_CATALOG_API=https://catalog-api.dev.company.com
VITE_BASKET_API=https://basket-api.dev.company.com
VITE_ORDERING_API=https://ordering-api.dev.company.com
VITE_AI_ASSISTANT_API=https://ai-api.dev.company.com
```

#### Basket.API: `appsettings.Integration.json`
```json
{
  "ServiceUrls": {
    "CatalogApi": "https://catalog-api.dev.company.com"
  }
}
```

#### Ordering.API: `appsettings.Integration.json`
```json
{
  "ServiceUrls": {
    "BasketApi": "https://basket-api.dev.company.com"
  }
}
```

### Staging Environment

#### Frontend: `.env.staging`
```env
VITE_CATALOG_API=https://catalog-api.staging.company.com
VITE_BASKET_API=https://basket-api.staging.company.com
VITE_ORDERING_API=https://ordering-api.staging.company.com
VITE_AI_ASSISTANT_API=https://ai-api.staging.company.com
```

#### Basket.API: `appsettings.Staging.json`
```json
{
  "ServiceUrls": {
    "CatalogApi": "https://catalog-api.staging.company.com"
  }
}
```

#### Ordering.API: `appsettings.Staging.json`
```json
{
  "ServiceUrls": {
    "BasketApi": "https://basket-api.staging.company.com"
  }
}
```

### Production Environment

#### Frontend: `.env.production`
```env
VITE_CATALOG_API=https://catalog-api.company.com
VITE_BASKET_API=https://basket-api.company.com
VITE_ORDERING_API=https://ordering-api.company.com
VITE_AI_ASSISTANT_API=https://ai-api.company.com
```

#### Basket.API: `appsettings.Production.json`
```json
{
  "ServiceUrls": {
    "CatalogApi": "https://catalog-api.company.com"
  }
}
```

#### Ordering.API: `appsettings.Production.json`
```json
{
  "ServiceUrls": {
    "BasketApi": "https://basket-api.company.com"
  }
}
```

## Real-World Scenario: Adding a New Service

Your team decides to add a **Notifications.API** service.

### Changes Required Without Aspire

#### 1. Create Service Configuration Files

**Notifications.API/appsettings.Development.json:**
```json
{
  "ServiceUrls": {
    "OrderingApi": "https://localhost:7003"  // Needs to call Ordering
  }
}
```

**Repeat for:** Integration, Staging, Production = **4 files**

#### 2. Update Frontend

**Add to ALL environment files** (4 × 1 entry = 4 changes):
```env
VITE_NOTIFICATIONS_API=https://localhost:7005        # Development
VITE_NOTIFICATIONS_API=https://notif-api.dev...     # Integration
VITE_NOTIFICATIONS_API=https://notif-api.staging... # Staging
VITE_NOTIFICATIONS_API=https://notif-api.company... # Production
```

#### 3. Update Ordering.API

**Add to ALL environment files** (4 × 1 entry = 4 changes):
```json
{
  "ServiceUrls": {
    "BasketApi": "...",
    "NotificationsApi": "https://localhost:7005"  // New!
  }
}
```

#### 4. Update Documentation

- Deployment runbook
- Developer onboarding guide
- Architecture diagrams
- Port allocation spreadsheet

### Total for One New Service:
- **Configuration files:** 12 new/modified
- **URL entries:** 12
- **Documentation:** 4 pages
- **Time:** 2-3 hours
- **Error risk:** HIGH (easy to miss one environment)

## Common Problems

### Problem 1: Configuration Drift

**Scenario:** Someone updates staging configuration but forgets production.

```json
// Staging - Updated to new domain
{
  "ServiceUrls": {
    "CatalogApi": "https://catalog-api-v2.staging.company.com"  // ✅ Updated
  }
}

// Production - Still uses old domain
{
  "ServiceUrls": {
    "CatalogApi": "https://catalog-api.company.com"  // ❌ Outdated
  }
}
```

**Result:** Production breaks when old domain is decommissioned.

### Problem 2: Copy-Paste Errors

**Scenario:** Copy staging config to production, forget to update URLs.

```json
// Production file (appsettings.Production.json)
// But accidentally has staging URLs! 😱
{
  "ServiceUrls": {
    "CatalogApi": "https://catalog-api.staging.company.com"  // ❌ WRONG!
  }
}
```

**Result:** Production calls staging services! Data corruption risk!

### Problem 3: Case Sensitivity

**Scenario:** Different casing in different environments.

```json
// Development
"CatalogApi": "https://localhost:7001"  // ✅

// Production  
"catalogApi": "https://catalog-api.company.com"  // ❌ Different casing!
```

**Result:** Configuration not found, cryptic errors.

### Problem 4: Missing Environment

**Scenario:** New developer creates local config from template.

```json
// Developer copies from .env.example
VITE_CATALOG_API=https://your-catalog-api-here
VITE_BASKET_API=https://your-basket-api-here
```

**Result:** "your-catalog-api-here is not a valid URL" errors.

## Maintenance Burden

### Annual Changes (Conservative Estimate)

| Change Type | Frequency | Files Affected | Time/Change | Annual Hours |
|-------------|-----------|----------------|-------------|--------------|
| Service rename | 2/year | 12 | 30 min | 12 hrs |
| New service | 4/year | 12 | 2 hrs | 32 hrs |
| Domain change | 1/year | 24 | 1 hr | 24 hrs |
| Port change | 3/year | 8 | 20 min | 3 hrs |
| Bug fixes (wrong URL) | 10/year | 1-3 | 15 min | 5 hrs |
| **TOTAL** | **20/year** | - | - | **76 hours** |

**Cost for team of 5:** 76 hours × 5 = **380 developer hours per year**

At $100/hour = **$38,000 annual cost** just managing service URLs!

## The Aspire Solution

### Single Source of Truth: AppHost

```csharp
// ECommerce.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Define services once - works in ALL environments
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(catalogApi);  // Declares dependency
var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(basketApi);   // Declares dependency

builder.Build().Run();
```

### What This Provides

#### In Development (Localhost)
```
catalog-api  → Resolved to https://localhost:7001 (dynamic)
basket-api   → Resolved to https://localhost:7002 (dynamic)
ordering-api → Resolved to https://localhost:7003 (dynamic)
```

#### In Staging (Azure Container Apps)
```
catalog-api  → Resolved to https://catalog-api.staging.company.com (automatic)
basket-api   → Resolved to https://basket-api.staging.company.com (automatic)
ordering-api → Resolved to https://ordering-api.staging.company.com (automatic)
```

#### In Production (Azure Container Apps)
```
catalog-api  → Resolved to https://catalog-api.company.com (automatic)
basket-api   → Resolved to https://basket-api.company.com (automatic)
ordering-api → Resolved to https://ordering-api.company.com (automatic)
```

### Service Code Doesn't Change!

```csharp
// Basket.API - Same code in ALL environments
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    // "catalog-api" resolves automatically based on environment
    client.BaseAddress = new Uri("https+http://catalog-api");
});
```

### Adding New Service

**Before Aspire:** 12 config files, 2-3 hours, high error risk

**With Aspire:**
```csharp
// Add ONE line to AppHost
var notificationsApi = builder.AddProject<Projects.Notifications_API>("notifications-api")
    .WithReference(orderingApi);

// That's it! Works in all environments automatically.
```

**Time:** 30 seconds  
**Files changed:** 1  
**Error risk:** Near zero

## Comparison Summary

| Metric | Without Aspire | With Aspire |
|--------|---------------|-------------|
| **Config files per environment** | 6 | 0 (AppHost only) |
| **Total config entries** | 24 URLs | 0 URLs |
| **New service setup** | 12 files, 2 hrs | 1 line, 30 sec |
| **Environment drift risk** | High | Zero |
| **Copy-paste errors** | Common | Impossible |
| **Documentation updates** | Every change | Rare |
| **Annual maintenance** | 76 hours | ~5 hours |
| **Annual cost (5 devs)** | $38,000 | $2,500 |

**Savings:** $35,500/year + reduced errors + faster development

## See It In Action

### Step 1: Experience the Pain

Try manually creating configuration for a 5th environment (e.g., "UAT"):

1. Create `.env.uat` in frontend
2. Add 4 service URLs
3. Create `appsettings.UAT.json` in each API
4. Add service dependencies
5. Test to ensure no typos
6. Document in README

Time yourself! ⏱️

### Step 2: Experience the Relief

Complete [Exercise 1](../../../exercises/01-system-topology/README.md) and add a new environment:

1. Deploy to a new Azure Container Apps environment
2. That's it - Aspire handles all service URLs automatically

Time yourself again! ⏱️

## Conclusion

Environment configuration is a **multiplication disaster** in microservices:

```
Services × Dependencies × Environments = Configuration Files
   4     ×      2        ×      4       =      32 entries
```

Every service added: +8 config entries  
Every environment added: +8 config entries  
Every dependency added: +4 config entries

.NET Aspire **collapses this to 1** with centralized service orchestration.

Next: [Startup Order Dependencies](03-startup-order.md)
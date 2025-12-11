# Troubleshooting Guide: Running Without .NET Aspire

This guide helps you solve common issues when running the e-commerce application manually, without Aspire orchestration.

> **💡 Spoiler Alert:** Every issue in this guide is automatically prevented by .NET Aspire. This document exists to show you why Aspire matters!

## Table of Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Service Not Found Errors](#service-not-found-errors)
- [Connection Refused](#connection-refused)
- [CORS Errors](#cors-errors)
- [Certificate/HTTPS Errors](#certificatehttps-errors)
- [Azure Resource Errors](#azure-resource-errors)
- [Performance Issues](#performance-issues)
- [The Nuclear Option](#the-nuclear-option)

---

## Quick Diagnostics

### Checklist: Are All Services Running?

Run this checklist BEFORE debugging anything else:

```bash
# 1. Check Cosmos DB Emulator
curl https://localhost:8081/_explorer/index.html
# Expected: HTML response
# If fails: Cosmos DB not running

# 2. Check Azurite (Storage Emulator)
curl http://127.0.0.1:10000/devstoreaccount1?comp=list
# Expected: XML response
# If fails: Azurite not running

# 3. Check Catalog.API
curl https://localhost:7001/api/catalog
# Expected: JSON array (may be empty)
# If fails: Catalog.API not running or wrong port

# 4. Check Basket.API
curl https://localhost:7002/api/basket/test-user
# Expected: JSON object with empty basket
# If fails: Basket.API not running

# 5. Check Ordering.API
curl https://localhost:7003/api/orders
# Expected: JSON array (may be empty)
# If fails: Ordering.API not running

# 6. Check AI Assistant.API
curl https://localhost:7004/api/chat -X POST -H "Content-Type: application/json" -d '{"userId":"test","message":"hello"}'
# Expected: JSON response (or config error)
# If fails: AI Assistant not running

# 7. Check Frontend
curl http://localhost:5173
# Expected: HTML response
# If fails: React dev server not running
```

**If ANY check fails, that service needs to be started!**

---

## Service Not Found Errors

### Error: "Failed to load products. Make sure the Catalog API is running"

**Symptom:** Frontend shows this error message when trying to load products.

**Root Cause:** Frontend can't reach Catalog.API at `https://localhost:7001`

**Solutions (try in order):**

#### Solution 1: Start Catalog.API
```bash
cd src/Catalog.API
dotnet run
```

Wait for: `Now listening on: https://localhost:7001`

#### Solution 2: Check Port Configuration
```bash
# Check if something else is using port 7001
netstat -ano | findstr :7001   # Windows
lsof -i :7001                  # macOS/Linux
```

If port is taken:
1. Kill the process using that port, OR
2. Change port in [`launchSettings.json`](../src/Catalog.API/Properties/launchSettings.json)
3. Update [`apiService.ts`](../src/ecommerce-web/src/services/apiService.ts) with new port

#### Solution 3: Check Cosmos DB Dependency
Catalog.API requires Cosmos DB. Ensure it's running:

```bash
# Start Cosmos DB Emulator (Windows)
# Or use Docker:
docker run -p 8081:8081 -p 10250-10255:10250-10255 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
```

#### Solution 4: SSL Certificate Issues
If Catalog.API is running but connection fails:

```bash
# Trust the development certificate
dotnet dev-certs https --trust
```

---

### Error: "Cannot validate product - Catalog API unavailable"

**Symptom:** Basket.API logs show this error when trying to add items

**Root Cause:** Basket.API can't reach Catalog.API for product validation

**Check:**
1. Is Catalog.API running? `curl https://localhost:7001/api/catalog`
2. Is URL correct in [`Basket.API/appsettings.json`](../src/Basket.API/appsettings.json)?
   ```json
   "ServiceUrls": {
     "CatalogApi": "https://localhost:7001"  // ← Check this!
   }
   ```

**Fix:**
```bash
# Terminal 1: Start Catalog.API
cd src/Catalog.API
dotnet run

# Terminal 2: Restart Basket.API
cd src/Basket.API
dotnet run
```

---

### Error: "Cannot retrieve basket - Basket API unavailable"

**Symptom:** Ordering.API fails when creating orders

**Root Cause:** Ordering.API can't reach Basket.API at `https://localhost:7002`

**Check:**
1. Is Basket.API running? `curl https://localhost:7002/api/basket/test`
2. Is URL correct in [`Ordering.API/appsettings.json`](../src/Ordering.API/appsettings.json)?

**Fix:** Start services in order:
```bash
# 1. Catalog.API (required by Basket)
cd src/Catalog.API && dotnet run &

# 2. Basket.API (required by Ordering)
cd src/Basket.API && dotnet run &

# 3. Ordering.API
cd src/Ordering.API && dotnet run
```

---

## Connection Refused

### Error: "ECONNREFUSED" or "Connection refused" in browser console

**Symptom:** Frontend can't connect to any API

**Common Causes:**

#### Cause 1: Service Not Started
**Solution:** Start all services (see [Startup Order](#proper-startup-order))

#### Cause 2: Wrong Port in Environment Variable
**Check:**
```bash
# In src/ecommerce-web
cat .env.local
# or
cat .env.development
```

**Fix:**
```bash
# Create/update .env.local
echo "VITE_CATALOG_API=https://localhost:7001" > .env.local
echo "VITE_BASKET_API=https://localhost:7002" >> .env.local
echo "VITE_ORDERING_API=https://localhost:7003" >> .env.local
echo "VITE_AI_ASSISTANT_API=https://localhost:7004" >> .env.local

# Restart frontend
npm run dev
```

#### Cause 3: Firewall Blocking Localhost
**Solution (Windows):**
```powershell
# Run as Administrator
New-NetFirewallRule -DisplayName "ASP.NET Core Dev" -Direction Inbound -LocalPort 7001-7004 -Protocol TCP -Action Allow
```

**Solution (macOS/Linux):**
```bash
# Check if firewall is blocking
sudo ufw status
# If active, allow ports
sudo ufw allow 7001:7004/tcp
```

---

## CORS Errors

### Error: "CORS policy: No 'Access-Control-Allow-Origin' header"

**Symptom:** Browser console shows CORS error when frontend calls API

**Root Cause:** API's CORS configuration doesn't allow frontend origin

**Check CORS Configuration:**

In [`Catalog.API/Program.cs`](../src/Catalog.API/Program.cs):
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()      // ← Should allow any origin
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

**Common Issues:**

#### Issue 1: CORS Middleware Not Added
```csharp
// Missing this line before app.Run()
app.UseCors();
```

#### Issue 2: Wrong Origin Specified
If you changed to specific origins:
```csharp
policy.WithOrigins("http://localhost:5173")  // ← Must match frontend URL
```

#### Issue 3: HTTPS vs HTTP Mismatch
```
Frontend: http://localhost:5173
API:      https://localhost:7001  ← HTTPS!
```

**Solutions:**
1. Use `AllowAnyOrigin()` for development
2. Or match exact frontend URL in `WithOrigins()`
3. Ensure frontend uses same scheme (HTTP/HTTPS) as API

---

## Certificate/HTTPS Errors

### Error: "NET::ERR_CERT_AUTHORITY_INVALID" or "SSL connection error"

**Symptom:** Browser warns about invalid certificate, or connection fails

**Root Cause:** Development HTTPS certificate not trusted

**Solution 1: Trust Development Certificate**
```bash
dotnet dev-certs https --trust
```

**If that doesn't work:**

**Solution 2: Clean and Reinstall Certificate**
```bash
# Remove existing certs
dotnet dev-certs https --clean

# Create new cert
dotnet dev-certs https --trust

# Restart all services
```

**Solution 3: Use HTTP Instead**

Edit [`launchSettings.json`](../src/Catalog.API/Properties/launchSettings.json) in each API:
```json
{
  "applicationUrl": "http://localhost:7001"  // Remove https://
}
```

⚠️ **Remember to update frontend URLs to use HTTP!**

---

### Error: "The SSL connection could not be established"

**Symptom:** Service-to-service calls fail with SSL errors

**Root Cause:** Server making the call doesn't trust the target's certificate

**Example:** Basket.API → Catalog.API fails because Basket doesn't trust Catalog's cert

**Solution: Disable Certificate Validation (DEV ONLY!)**

In [`Basket.API/Program.cs`](../src/Basket.API/Program.cs):
```csharp
builder.Services.AddHttpClient<CatalogClient>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });
```

⚠️ **NEVER use this in production!**

---

## Azure Resource Errors

### Error: "Could not initialize Azure Queue. Using in-memory storage only"

**Symptom:** Log warning when Basket.API or Ordering.API starts

**Root Cause:** Can't connect to Azure Storage (Azurite not running)

**Solution:**
```bash
# Install Azurite globally
npm install -g azurite

# Start Azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

**Or use Docker:**
```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

**Verify it's running:**
```bash
curl http://127.0.0.1:10000/devstoreaccount1?comp=list
# Should return XML
```

---

### Error: "Cannot connect to Cosmos DB"

**Symptom:** Catalog.API crashes on startup or can't store products

**Solutions:**

#### Solution 1: Start Cosmos DB Emulator

**Windows:**
- Install from: https://aka.ms/cosmosdb-emulator
- Start from Start Menu

**Docker (all platforms):**
```bash
docker run -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmosdb-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
```

Wait 2 minutes for emulator to fully start!

**Verify:**
```bash
curl https://localhost:8081/_explorer/index.html
# Should return HTML
```

#### Solution 2: Check Connection String

In [`Catalog.API/appsettings.json`](../src/Catalog.API/appsettings.json):
```json
{
  "CosmosDb": {
    "ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```

That's the default emulator key. It should work unless you changed it!

---

## Performance Issues

### Issue: "Services are slow to respond"

**Symptom:** API calls taking 5+ seconds

**Common Causes:**

#### Cause 1: Cold Start
**Solution:** First request is always slow. Try again.

#### Cause 2: Cosmos DB Emulator Slow
**Solution:** Use Azure Cosmos DB instead of emulator:
1. Create Cosmos DB in Azure Portal
2. Update connection string in appsettings.json
3. Emulator is resource-heavy!

#### Cause 3: Too Many Debug Breakpoints
**Solution:** Remove breakpoints, run without debugging (Ctrl+F5)

#### Cause 4: Service Chain Delays
**Symptom:** Frontend → Basket → Catalog is slow

**Explanation:**
```
Frontend request (200ms)
  → Basket.API (300ms)
    → Catalog.API (400ms)
      → Cosmos DB (500ms)
Total: 1.4 seconds!
```

**With Aspire:** Built-in performance tracing shows exactly where the delay is.

---

## The Nuclear Option

### When All Else Fails: Complete Reset

If you've tried everything and it's still broken:

```bash
# 1. Kill all processes
# Windows: Ctrl+C in all terminals
# Or: taskkill /F /IM dotnet.exe /T

# 2. Kill all Node processes
# Windows: taskkill /F /IM node.exe /T
# macOS/Linux: killall node

# 3. Stop all containers
docker stop $(docker ps -aq)
docker rm $(docker ps -aq)

# 4. Clean build artifacts
cd src
for /d %d in (*) do (cd %d && dotnet clean && cd ..)

# 5. Remove all node_modules
cd ecommerce-web
rmdir /s /q node_modules

# 6. Start fresh
# Install npm packages
cd src/ecommerce-web
npm install

# 7. Trust certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# 8. Start everything in order (see Proper Startup Order)
```

---

## Proper Startup Order

**IMPORTANT:** Services must start in this order due to dependencies!

```bash
# Step 1: Infrastructure (no dependencies)
# Terminal 1: Cosmos DB
docker run -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator

# Terminal 2: Azurite
azurite --silent

# Wait 30 seconds for infrastructure to be ready!

# Step 2: Catalog.API (depends on Cosmos DB)
# Terminal 3:
cd src/Catalog.API
dotnet run
# Wait for: "Now listening on: https://localhost:7001"

# Step 3: Basket.API (depends on Catalog + Azurite)
# Terminal 4:
cd src/Basket.API
dotnet run
# Wait for: "Now listening on: https://localhost:7002"

# Step 4: Ordering.API (depends on Basket + Azurite)
# Terminal 5:
cd src/Ordering.API
dotnet run
# Wait for: "Now listening on: https://localhost:7003"

# Step 5: AI Assistant.API (optional, depends on OpenAI config)
# Terminal 6:
cd src/AIAssistant.API
dotnet run
# Wait for: "Now listening on: https://localhost:7004"

# Step 6: Frontend (depends on all APIs)
# Terminal 7:
cd src/ecommerce-web
npm run dev
# Wait for: "Local: http://localhost:5173"
```

**Total Startup Time:** 5-10 minutes depending on your machine

**Total Terminals Open:** 7

**With Aspire:** F5 → Everything starts automatically in 30 seconds, one dashboard.

---

## Prevention: Use .NET Aspire!

Every single issue in this guide is automatically prevented when using .NET Aspire:

| Issue | Manual Solution | Aspire Solution |
|-------|----------------|-----------------|
| Service not found | Check ports, configs, etc. | Automatic service discovery |
| Wrong startup order | Manual coordination | Automatic orchestration |
| Port conflicts | Edit configs | Dynamic port allocation |
| CORS issues | Manual CORS setup | Pre-configured |
| Certificate errors | Trust certs manually | Handled automatically |
| Log correlation | Hunt across terminals | Unified dashboard |
| Resource connection | Manual config | Automatic connection strings |

**Ready to never see these errors again?**

→ [Exercise 1: Creating a System Topology with Aspire](../../exercises/01-system-topology/README.md)

Migrate this application to Aspire and watch all these problems disappear! 🎉
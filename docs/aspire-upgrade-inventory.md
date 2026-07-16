# Aspire Workshop — Upgrade Inventory

> **PR-1 / Read-only audit.** No source files were changed.  
> Generated: 2026-07-16  
> **Upgrade scope: 13.1.0 → 13.4.6** (baseline confirmed by audit; original task assumed 13.2 but repo is on 13.1.0)  
> Tracking issue: [tamirdresher_microsoft/tamresearch1#4774](https://github.com/tamirdresher_microsoft/tamresearch1/issues/4774)  
> Linked PR: [#4](https://github.com/tamirdresher/aspire-workshop/pull/4)

---

## 1. Aspire Package / SDK Version Pins

### 1a. AppHost SDK (Project `Sdk` attribute in `.csproj`)

| File | SDK | Current version |
|------|-----|-----------------|
| `Examples/AspireCustomResource/AspireCustomResource.AppHost/AspireCustomResource.AppHost.csproj` | `Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Integrations/Services/AspireCustomResource.AppHost/AspireCustomResource.AppHost.csproj` | `Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Services/AspireCustomResource.AppHost/AspireCustomResource.AppHost.csproj` | `Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Testing/src/NoteTaker.AppHost/NoteTaker.AppHost.csproj` | `Aspire.AppHost.Sdk` | **13.0.0** ⚠️ |
| `Exercise/workshop/Lesson-01/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.AppHost.Sdk` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.AppHost.Sdk` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.AppHost.Sdk` | **13.1.0** |

> ⚠️ `NoteTaker.AppHost.csproj` is on `13.0.0` — one minor version behind the rest.

---

### 1b. Single-file AppHost `#:sdk` directives

| File | Directive | Version |
|------|-----------|---------|
| `Examples/AspirePublish/AppHost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Customizations/AppHosts/Annotations/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Customizations/AppHosts/Commands/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Customizations/AppHosts/Eventing/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Customizations/AppHosts/Parameters/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Customizations/AppHosts/Pipelines/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Customizations/AppHosts/UrlCustomizations/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Integrations/AppHosts/AllEmulators/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Integrations/AppHosts/ConfigureInfrastructure/AppHost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Integrations/AppHosts/CustomBicep/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Integrations/AppHosts/CustomizeContainerResources/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |
| `Examples/Integrations/AppHosts/ExternalResources/apphost.cs` | `#:sdk Aspire.AppHost.Sdk` | **13.1.0** |

---

### 1c. Single-file AppHost `#:package` directives

| File | Package | Version |
|------|---------|---------|
| `Examples/AspirePublish/AppHost.cs` | `Aspire.Hosting.Docker` | **13.1.0-preview.1.25616.3** 🔶 |
| `Examples/AspirePublish/AppHost.cs` | `Aspire.Hosting.Python` | **13.1.0** |
| `Examples/Customizations/AppHosts/Commands/apphost.cs` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Examples/Customizations/AppHosts/Eventing/apphost.cs` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Examples/Customizations/AppHosts/Eventing/apphost.cs` | `Aspire.Hosting.PostgreSQL` | **13.1.0** |
| `Examples/Customizations/AppHosts/Pipelines/apphost.cs` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Examples/Integrations/AppHosts/AllEmulators/apphost.cs` | `Aspire.Hosting.Azure.Storage` | **13.1.0** |
| `Examples/Integrations/AppHosts/AllEmulators/apphost.cs` | `Aspire.Hosting.Azure.CosmosDB` | **13.1.0** |
| `Examples/Integrations/AppHosts/AllEmulators/apphost.cs` | `Aspire.Hosting.Azure.AIFoundry` | **13.0.2-preview.1.25603.5** ⚠️🔶 |
| `Examples/Integrations/AppHosts/ConfigureInfrastructure/AppHost.cs` | `Aspire.Hosting.Azure.Storage` | **13.1.0** |
| `Examples/Integrations/AppHosts/ConfigureInfrastructure/AppHost.cs` | `Azure.Provisioning.Storage` | **1.1.2** |
| `Examples/Integrations/AppHosts/CustomBicep/apphost.cs` | `Aspire.Hosting.Azure` | **13.1.0** |
| `Examples/Integrations/AppHosts/CustomizeContainerResources/apphost.cs` | `Aspire.Hosting.PostgreSQL` | **13.1.0** |
| `Examples/Integrations/AppHosts/CustomizeContainerResources/apphost.cs` | `Aspire.Hosting.Redis` | **13.1.0** |

> ⚠️🔶 `Aspire.Hosting.Azure.AIFoundry@13.0.2-preview` is both preview **and** an older minor version.  
> 🔶 `Aspire.Hosting.Docker@13.1.0-preview` is a preview package (no stable release yet for this version).

---

### 1d. `PackageReference` pins in `.csproj` files

| File | Package | Version |
|------|---------|---------|
| `Examples/AspireCustomResource/AspireCustomResource.AppHost/...csproj` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Examples/AspireCustomResource/AspireCustomResource.Web/...csproj` | `Aspire.StackExchange.Redis.OutputCaching` | **13.1.0** |
| `Examples/Integrations/Services/AspireCustomResource.AppHost/...csproj` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Examples/Integrations/Services/AspireCustomResource.Web/...csproj` | `Aspire.StackExchange.Redis.OutputCaching` | **13.1.0** |
| `Examples/Services/AspireCustomResource.AppHost/...csproj` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Examples/Services/AspireCustomResource.Web/...csproj` | `Aspire.StackExchange.Redis.OutputCaching` | **13.1.0** |
| `Examples/Testing/src/backend/Backend.csproj` | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | **13.1.0** |
| `Examples/Testing/src/backend/Backend.csproj` | `Aspire.StackExchange.Redis` | **13.1.0** |
| `Examples/Testing/src/backend/Backend.csproj` | `Aspire.RabbitMQ.Client` | **13.1.0** |
| `Examples/Testing/src/NoteTaker.AppHost/NoteTaker.AppHost.csproj` | `Aspire.Hosting.JavaScript` | **13.1.0** |
| `Examples/Testing/src/NoteTaker.AppHost/NoteTaker.AppHost.csproj` | `Aspire.Hosting.Python` | **13.1.0** |
| `Examples/Testing/src/NoteTaker.AppHost/NoteTaker.AppHost.csproj` | `Aspire.Hosting.PostgreSQL` | **13.1.0** |
| `Examples/Testing/src/NoteTaker.AppHost/NoteTaker.AppHost.csproj` | `Aspire.Hosting.RabbitMQ` | **13.1.0** |
| `Examples/Testing/src/NoteTaker.AppHost/NoteTaker.AppHost.csproj` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Examples/Testing/src/NoteTaker.Tests/NoteTaker.Tests.csproj` | `Aspire.Hosting.Testing` | **13.0.2** ⚠️ |
| `Exercise/workshop/Lesson-01/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Docker` | **13.1.0-preview.1.25616.3** 🔶 |
| `Exercise/workshop/Lesson-01/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.JavaScript` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.StackExchange.Redis.OutputCaching` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.Microsoft.EntityFrameworkCore.Cosmos` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.Azure.Storage.Queues` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.Microsoft.Azure.Cosmos` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Docker` | **13.1.0-preview.1.25616.3** 🔶 |
| `Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Azure.CosmosDB` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Azure.Storage` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.JavaScript` | **13.1.0** |
| `Exercise/workshop/Lesson-02/code/Bookstore.Worker/Bookstore.Worker.csproj` | `Aspire.Azure.Storage.Queues` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.StackExchange.Redis.OutputCaching` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.Microsoft.EntityFrameworkCore.Cosmos` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.Azure.Storage.Queues` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.API/Bookstore.API.csproj` | `Aspire.Microsoft.Azure.Cosmos` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Redis` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Docker` | **13.1.0-preview.1.25616.3** 🔶 |
| `Exercise/workshop/Lesson-03/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Azure.CosmosDB` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.Azure.Storage` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.AppHost/Bookstore.AppHost.csproj` | `Aspire.Hosting.JavaScript` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.AppHost.Tests/Bookstore.AppHost.Tests.csproj` | `Aspire.Hosting.Testing` | **13.1.0** |
| `Exercise/workshop/Lesson-03/code/Bookstore.Worker/Bookstore.Worker.csproj` | `Aspire.Azure.Storage.Queues` | **13.1.0** |

---

### Version Anomalies Summary

| Anomaly | Location | Notes |
|---------|----------|-------|
| `Aspire.AppHost.Sdk/13.0.0` (not 13.1.0) | `Examples/Testing/src/NoteTaker.AppHost` | Needs bump to match rest |
| `Aspire.Hosting.Testing@13.0.2` (not 13.1.0) | `Examples/Testing/src/NoteTaker.Tests` | Needs bump to 13.1.0 |
| `Aspire.Hosting.Docker@13.1.0-preview.1.25616.3` | Lesson-01, Lesson-02, Lesson-03 AppHosts + AspirePublish single-file | Preview package — watch for stable release |
| `Aspire.Hosting.Azure.AIFoundry@13.0.2-preview.1.25603.5` | `Examples/Integrations/AppHosts/AllEmulators` | Both preview AND older minor — needs upgrade |

---

## 2. Hardcoded Port Occurrences

| File | Line | Pattern |
|------|------|---------|
| `README.md` | 172 | `http://localhost:15888` (or similar) |
| `Exercise/workshop/Lesson-01/README.md` | 525 | `http://localhost:15888` (or similar) |

> Port `15888` is the Aspire Dashboard default — the docs correctly note "or similar" but could be replaced with a version-resilient note explaining that the port is printed at startup.

No occurrences of `18888`, `4317`, or `4318` were found in documentation or source files.

---

## 3. Stale CLI Patterns

### Hard stale: `dotnet new install Aspire.ProjectTemplates`

| File | Line | Pattern | Notes |
|------|------|---------|-------|
| `README.md` | 132 | `dotnet new install Aspire.ProjectTemplates` | Pre-8.0/old workload install method. Modern: `dotnet workload install aspire` (or rely on SDK auto-restore) |
| `Exercise/workshop/Lesson-01/README.md` | 430 | `dotnet add ... package Aspire.Hosting.JavaScript --version 13.1.0` | Hardcodes `13.1.0` — version should not be hardcoded in step instructions |

### Valid (not stale) CLI usage

The following `aspire run` / `aspire ps` occurrences are **valid** current Aspire CLI commands and require no changes:

| File | Line | Pattern |
|------|------|---------|
| `Exercise/workshop/Lesson-01/README.md` | 278 | `aspire run` |
| `Exercise/workshop/Lesson-02/README.md` | 267 | `aspire run` |
| `Exercise/workshop/Lesson-03/README.md` | 195 | `aspire run` |

### `settings.json` / config format

All `settings.json` matches in the search are `launchSettings.json` schema references — none are stale `settings.json` Aspire config format. No `aspire.config.json` references were found (this format may not yet have been adopted in the workshop).

---

## 4. `aspire-13.2-upgrade` Branch Delta

The branch `aspire-13.2-upgrade` **does not exist** in this repository.  
Only `main` is present on the remote.

```
$ git branch -a
* main
  remotes/origin/HEAD -> origin/main
  remotes/origin/main
```

No partial upgrade work exists on a feature branch. All changes will start fresh from `main`.

> **PR-3 (Picard's plan) is a no-op.** Picard's decomposition included a PR-3 to merge/cherry-pick the `aspire-13.2-upgrade` branch. Since that branch does not exist, PR-3 can be skipped entirely — there is nothing to merge. The upgrade proceeds directly from `main` in PR-2.

---

## 5. Recommendations for PR-2

### Target version
Bump everything to **13.4.6** (confirmed latest stable at time of this inventory). The original task assumed 13.2 as the baseline, but the audit found the repo is on 13.1.0 — the full upgrade span is therefore **13.1.0 → 13.4.6**, crossing two minor versions. See the Addendum (Section 6) for the 13.1 → 13.2 delta to watch for during the bump.

### Priority fixes for PR-2

| Priority | Action | Affected Files |
|----------|--------|----------------|
| P0 | Bump `Aspire.AppHost.Sdk` from `13.0.0` → target in `NoteTaker.AppHost.csproj` | 1 file |
| P0 | Bump `Aspire.Hosting.Testing` from `13.0.2` → target in `NoteTaker.Tests.csproj` | 1 file |
| P1 | Bump all `13.1.0` pins (SDK + packages) to target in all `.csproj` files | 7 AppHost .csproj + ~30 PackageReference entries |
| P1 | Bump all 12 single-file `#:sdk Aspire.AppHost.Sdk@13.1.0` directives | 12 `.cs` files |
| P1 | Bump all `#:package` directives in single-file hosts | ~14 entries across 8 `.cs` files |
| P2 | Resolve `Aspire.Hosting.Docker@13.1.0-preview` — replace with stable if available | 3 `.csproj` + 1 `.cs` |
| P2 | Resolve `Aspire.Hosting.Azure.AIFoundry@13.0.2-preview` — check for stable/newer | 1 `.cs` file |
| P3 | Replace `dotnet new install Aspire.ProjectTemplates` with `dotnet workload install aspire` in `README.md` | `README.md:132` |
| P3 | Remove hardcoded `--version 13.1.0` from `dotnet add package` example in Lesson-01 README | `Exercise/workshop/Lesson-01/README.md:430` |
| P3 | Replace hardcoded `localhost:15888` with resilient wording in 2 README files | `README.md:172`, `Lesson-01/README.md:525` |

### Centralization opportunity
There is **no `Directory.Packages.props`** (Central Package Management) in the repo. All 7 AppHost `.csproj` files and 12 single-file apps pin versions individually. PR-2 should evaluate introducing `Directory.Packages.props` for the `.csproj`-based projects to centralize version control — though note that single-file `#:sdk`/`#:package` directives cannot use CPM and must be updated individually.

### No existing `docs/` folder
This file (`docs/aspire-upgrade-inventory.md`) is the first file in a new `docs/` directory.

---

## 6. Addendum: 13.1.0 → 13.2.0 Delta

> Provided by Seven (Research & Docs). These are the changes that land between the workshop's current baseline (13.1.0) and the first intermediate target (13.2.0). PR-2 must account for all of them on the way to 13.4.6.

### Breaking Changes (13.1 → 13.2)

| Item | Change |
|------|--------|
| `AddAzureRedisEnterprise` | Renamed to `AddAzureManagedRedis` in 13.1 |
| `aspire mcp server` | Renamed to `aspire agent server` in 13.2 |
| `.aspire/settings.json` + `apphost.run.json` | Superseded by `aspire.config.json` in 13.2 (auto-migrated on first run) |
| ACR implicit provisioning | Removed in 13.1 — explicit `AddContainerRegistry` now required |

**Impact on this workshop:**
- No `AddAzureRedisEnterprise` usage found in audit → no rename needed.
- `apphost.run.json` files exist in Examples/Customizations and Examples/Integrations AppHosts (found during audit) → these will be auto-migrated to `aspire.config.json` when the CLI is upgraded; PR-2 should verify migration happens cleanly and update any README instructions that reference `apphost.run.json` by name.
- No ACR/`AddContainerRegistry` usage found in audit → no explicit provisioning fix needed.

### Key New in 13.1 (not in workshop baseline)

- `aspire agent init` — AI agent + MCP server setup
- Dashboard Parameters tab
- `ContainerRegistryResource` + `WithContainerRegistry`
- `Aspire.Hosting.DevTunnels` graduated stable

**Workshop coverage gap:** None of these features are demonstrated in the current lesson material. Consider adding a bonus exercise or callout note in Lesson-03 for `aspire agent init`.

### Key New in 13.2 (not in workshop baseline)

- **Detached mode**: `aspire start` / `aspire stop` / `aspire ps` / `aspire describe` / `aspire wait` / `aspire export`
- **New CLI commands**: `aspire doctor`, `aspire secret`, `aspire certs`, `aspire docs`, `aspire config`
- **Workflow commands**: `aspire restore`, `aspire run --isolated`
- **Unified config**: `aspire.config.json` replaces `.aspire/settings.json` + `apphost.run.json`
- **Language-aware scaffolding**: `aspire new` / `aspire init` supports C#, TypeScript, Python
- **VS Code Aspire panel**

**Workshop coverage gap:** README.md currently documents only `aspire run`. PR-3 (docs update PR, separate from the version bump) should add a "What's new in 13.2" callout and update the README quick-start to use `aspire start` (detached mode) alongside `aspire run`.

---

*End of inventory — generated by Data (Code Expert) as part of Squad task 4774 PR-1.*

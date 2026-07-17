# PR-10 current-state validation

Validated on 2026-07-17 against the current `main` baseline. This is an
actionable pre-merge validation pass, not the final sign-off: PR-3 through PR-9
are still unmerged, so the checks that depend on their changes are listed
explicitly below.

## Environment

| Tool | Version or status |
| --- | --- |
| .NET SDK | 10.0.301 |
| Aspire CLI | 13.4.6 |
| Node.js | 24.14.0 |
| npm | 11.9.0 |
| Python | 3.12.10 |
| Azure CLI | 2.84.0 |
| Bicep CLI | 0.44.1 |
| Container runtime | Not installed |

`aspire doctor` passed the CLI, SDK, and trusted development certificate
checks. Its only failure was the missing Docker-compatible container runtime.

## Coverage

The current tree contains:

- 6 .NET solutions, covering 35 projects.
- 2 standalone project-based AppHosts, covering the remaining 8 C# projects.
- 12 file-based C# AppHosts.
- 2 test projects containing 15 tests.
- 5 npm applications.
- 2 Python services.
- 5 Bicep templates.

All 43 C# projects are therefore included in the build coverage below.

## Results

| Surface | Result | Evidence and remaining gap |
| --- | --- | --- |
| .NET solutions and project AppHosts | Pass | All 6 solutions and both standalone AppHost projects build in Release. |
| File-based C# AppHosts | Blocked | 4 of 12 build normally. The other 8 fail restore with `NU1008` because their inline `#:package` versions conflict with repository-wide Central Package Management. All 8 compile when CPM is disabled diagnostically, confirming that the remaining normal-build failure is the version-management conflict. |
| .NET tests | Environment blocked | Both test projects compile. All 15 tests fail before reaching assertions because Aspire cannot find Docker; 12 failures are in NoteTaker and 3 are in the Lesson 3 AppHost tests. |
| npm applications | Pass | Clean installs and every available lint, build, dependency-tree, and package syntax check pass across all 5 applications. |
| Python services | Partial | Both source trees pass `compileall`. NoteTaker requirements resolve in a pip dry run. AspirePublish dependency resolution correctly rejects this host because its `pyproject.toml` requires Python 3.13 or newer. |
| Bicep | Pass | All 5 templates compile with Bicep CLI 0.44.1. |
| JSON | Pass | All 146 source JSON files parse with a BOM-aware standards parser. Generated outputs, `bin`, `obj`, and `node_modules` are excluded. |
| Markdown links | Pass | All repository-local Markdown links resolve. |
| Shell syntax | Pass | `Exercise/start/run.sh` passes `bash -n`. |
| Aspire runtime | Version blocked | The container-free Lesson 1 AppHost builds and reports startup, but Aspire CLI 13.4.6 loses its auxiliary backchannel against AppHost SDK 13.1.0 because `NotifyAppHostReadyAsync` is unavailable. The run was stopped cleanly. |

The validation used these command families:

- `dotnet build` in Release for every solution, project AppHost, and file AppHost.
- `dotnet test` in Release for both test projects.
- `npm ci`, `npm run lint`, `npm run build`, and `npm ls --all` where
  supported by each application.
- `python -m compileall` plus pip dependency-resolution dry runs.
- `az bicep build` for every Bicep entry point.
- `aspire doctor`, followed by `aspire start --isolated --non-interactive`
  and `aspire stop` for the container-free runtime probe.
- Repository-wide JSON parsing, Markdown local-link resolution, shell syntax,
  and `git diff --check`.

The build sweep also reports 16 distinct NuGet security advisories across these
centrally resolved packages:

| Package | Version | Advisories |
| --- | --- | --- |
| `MessagePack` | 2.5.192 | 11 (9 moderate, 2 high) |
| `Microsoft.OpenApi` | 2.0.0 | 1 high |
| `OpenTelemetry.Api` | 1.14.0 | 1 moderate |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.14.0 | 3 moderate |

Package and AppHost SDK upgrades remain in PR-3's scope. Two nullable
dereference warnings also remain in the duplicated Services URL customization
sample and should be re-evaluated with PR-5 rather than changed independently
here.

## Independent cleanup completed

- Removed an orphaned type-name fragment from the Custom Bicep file AppHost.
  The fragment caused `CS1001` and `CS1002` once the CPM restore blocker was
  bypassed. The AppHost now compiles in the diagnostic build.
- Removed unused `CosmosClient` declarations and imports from the Lesson 2 and
  Lesson 3 APIs. Both solutions still build successfully.
- Removed a duplicated 398-line Visual Studio template block from `.gitignore`.
  The retained block is a strict superset of the removed patterns.
- Replaced the PR scaffold with this validation baseline and rerun checklist.

## Required rerun after upstream PRs merge

- [ ] **PR-3 ([#8](https://github.com/tamirdresher/aspire-workshop/pull/8))**
      Rebase the package, AppHost SDK, and file-AppHost pins; rerun all 20 .NET
      build entry points without a CPM bypass; confirm `NU1008` is gone; repeat
      the NuGet advisory audit; and rerun the Lesson 1 Aspire lifecycle check
      with CLI and SDK versions aligned.
- [ ] **PR-4 ([#9](https://github.com/tamirdresher/aspire-workshop/pull/9))**
      Rerun all Markdown local-link checks plus scans for stale CLI commands,
      fixed dashboard ports, and obsolete `dotnet run` AppHost guidance.
- [ ] **PR-5 ([#10](https://github.com/tamirdresher/aspire-workshop/pull/10))**
      Rebuild every customization and service sample, resolve or account for
      the two URL-customization nullable warnings, and exercise each exposed
      resource command through Aspire.
- [ ] **PR-6 ([#11](https://github.com/tamirdresher/aspire-workshop/pull/11))**
      Perform a clean npm install for the TypeScript AppHost, run its lint and
      build scripts, start it with Aspire, wait for each resource, and verify
      its service endpoints.
- [ ] **PR-7 ([#12](https://github.com/tamirdresher/aspire-workshop/pull/12))**
      Rebuild the Go and Bun AppHosts, rerun `go test ./...` and Bun checks,
      then start both samples and verify their health and API endpoints.
- [ ] **PR-8 ([#13](https://github.com/tamirdresher/aspire-workshop/pull/13))**
      Validate publish manifests and generated Bicep, then exercise
      publish/deploy/destroy and Kubernetes guidance against an isolated
      disposable target.
- [ ] **PR-9 ([#14](https://github.com/tamirdresher/aspire-workshop/pull/14))**
      Rerun skill and agent documentation link checks, verify documented CLI
      commands against the installed Aspire version, and test the documented
      project-local installation flow.
- [ ] **Container-backed regression pass**
      Install or enable Docker/Podman and rerun all 15 .NET tests; any failure
      that reaches an assertion becomes a code or test follow-up.
- [ ] **Python 3.13 pass**
      Create a clean Python 3.13 environment, resolve the AspirePublish
      `pyproject.toml`, and run its available service checks.
- [ ] **Final integrated sweep**
      Re-run this entire matrix after PR-3 through PR-9 are merged, record the
      final commit SHA and tool versions, and open focused issues for any
      remaining reproducible failures.

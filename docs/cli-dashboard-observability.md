# Aspire CLI, Dashboard & Observability

> Current guidance for driving and observing the workshop AppHosts with the
> Aspire CLI and developer dashboard.

This guide was validated on **July 17, 2026** against
[Aspire 13.4.6](https://github.com/microsoft/aspire/releases/tag/v13.4.6), the
latest stable release at that time, and the local `aspire <command> --help`
output. Check [Aspire releases](https://github.com/microsoft/aspire/releases)
and run `aspire --version` before relying on version-specific behavior.

---

## 1. Install and maintain the Aspire CLI

Aspire uses the standalone **`aspire` CLI**, not the retired .NET workload.

### Prerequisites

- **.NET 10 SDK** or later — <https://dotnet.microsoft.com/download>
- **Docker Desktop** (or another OCI runtime) for container resources
- **Node.js 18+** for the JavaScript/TypeScript examples

### Install the CLI

**Windows (PowerShell):**

```powershell
irm https://aspire.dev/install.ps1 | iex
```

**Linux / macOS (bash):**

```bash
curl -sSL https://aspire.dev/install.sh | bash
```

Package-manager alternatives:

```bash
dotnet tool install -g Aspire.Cli
npm install -g @microsoft/aspire-cli
```

See the [official installation guide](https://aspire.dev/get-started/install-cli/)
for WinGet, Homebrew, mise, and platform details. Verify the active binary:

```bash
aspire --version
aspire doctor
```

> The `dotnet new install Aspire.ProjectTemplates` workflow from earlier previews
> is no longer required. The CLI provisions templates and SDK bits on demand
> (`aspire new`, `aspire init`, `aspire restore`).

### Update the CLI and AppHost packages

```bash
aspire update --self
aspire update --apphost <path-to-apphost>
aspire update --apphost <path-to-apphost> --channel stable
```

`aspire update --self` updates install-script binaries directly. Package-managed
installs print the corresponding npm or .NET tool update command instead of
overwriting package-manager-owned files.

Automation must explicitly approve project changes:

```bash
aspire update --apphost <path-to-apphost> --yes --non-interactive
```

In 13.4, `aspire doctor` reports the CLI version, update notices, the detected
AppHost SDK version, .NET SDKs, container runtime, certificate status, and
conflicting CLI installations. Use `--format Json` for machine-readable output.

Trust the local HTTPS development certificate once:

```bash
aspire certs trust
```

---

## 2. Discover and run AppHosts

The CLI separates **candidate AppHost files** on disk from **running AppHosts**.

| Task | Command | Notes |
|------|---------|-------|
| List AppHost project files in the workspace | `aspire ls` | Add `--format Json` for tooling; `--all` ignores discovery filters |
| List **running** AppHosts | `aspire ps` | `--follow` streams updates as processes start/stop |
| Run an AppHost interactively (dev loop) | `aspire run` | Starts the app and streams output; the dashboard URL is printed |
| Start an AppHost in the background | `aspire start` | Pair with `aspire stop` |
| Wait for a resource to reach a state | `aspire wait <resource>` | Useful in scripts/CI |

```bash
# From the repository root, find AppHosts and select one explicitly
aspire ls
aspire run --apphost Examples/Services/AspireCustomResource.AppHost/AspireCustomResource.AppHost.csproj
```

> **`aspire ls` vs `aspire ps`:** `ls` lists candidate AppHost *files*; `ps` lists
> *running* AppHost processes. Use `describe`, not `ps`, for resource state.

This repository contains many AppHosts. Pass `--apphost <path>` to lifecycle and
diagnostic commands so scripts do not pause for an interactive selection.

---

## 3. Discover and add integrations

Aspire hosting integrations (Redis, PostgreSQL, Azure services, etc.) are managed
through `aspire integration` and `aspire add`:

```bash
aspire integration list
aspire integration search javascript
aspire add javascript --apphost Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj
```

`integration list` and `integration search` are read-only. `aspire add` modifies
the selected AppHost and resolves the package version from its configured channel.
For automation, use `--format Json --non-interactive` with the discovery commands.

---

## 4. Inspecting running resources

**Prefer `aspire describe` over parsing `aspire ps`.** `aspire ps` answers which
AppHosts are running. `aspire describe` returns the resource graph, including
resource names, types, states, endpoints, and health.

```bash
aspire describe                 # all resources in the running AppHost
aspire describe api             # a single resource by name
aspire describe --format Json   # structured output for tooling
aspire describe --follow        # stream resource state changes live
aspire describe --include-hidden # include proxies and infra resources
```

Use `--include-hidden` when a resource seems missing - proxies and infrastructure
resources are hidden by default.

---

## 5. Search logs, traces, and spans

Aspire exposes two complementary log surfaces:

- `aspire logs` reads resource console output (stdout/stderr) through the local
  AppHost backchannel.
- `aspire otel ...` reads structured OpenTelemetry data from the dashboard
  telemetry API.

### Console output

```bash
aspire logs
aspire logs api --tail 100 --timestamps
aspire logs api --follow
aspire logs api --search "timeout"
```

Console-log search matches message text and resource names. Structured search
supports fields and OpenTelemetry attributes:

```bash
aspire otel logs api --search "severity:error \"connection failed\""
aspire otel logs --search "resource:api -severity:debug"
aspire otel traces --search "status:error duration:>500"
aspire otel traces --search "@http.status_code:500"
aspire otel spans --search "@http.method:GET duration:>100"
```

All terms are combined with `AND`. Use quoted phrases, `field:value`,
`@attribute:value`, negation (`-severity:debug`), and numeric comparisons.
Filtering happens server-side before output is returned. See the
[search syntax reference](https://aspire.dev/reference/cli/search-filter/).

```bash
aspire otel traces api --has-error
aspire otel spans --trace-id <trace-id>
aspire otel logs --trace-id <trace-id>
```

The last two commands correlate a selected trace with its spans and structured
logs. Export a portable telemetry and resource snapshot for offline diagnosis:

```bash
aspire export --output aspire-telemetry.zip
```

---

## 6. Use the Aspire Dashboard safely

When an AppHost starts through `aspire run`, `aspire start`, or an IDE, the CLI or
IDE prints an authenticated dashboard login URL.

### AppHost endpoints are dynamic

Do not hardcode dashboard, resource-service, or OTLP ports. Aspire 13.4 assigns
the dashboard's supporting ports dynamically. Treat these values as runtime data:

- Open the dashboard login URL printed at startup.
- Use `aspire describe --format Json` for application resource endpoints.
- Let Aspire inject `OTEL_EXPORTER_OTLP_ENDPOINT` into managed resources.
- Read runtime configuration in tests instead of copying local port numbers.

### Standalone dashboard

Run a dashboard without an AppHost when any OpenTelemetry-enabled app needs a
short-lived local telemetry viewer:

```bash
aspire dashboard run
```

`aspire dashboard run` is a preview, foreground command; stop it with Ctrl+C.
Standalone mode has separate documented defaults and explicit endpoint options
(`--frontend-url`, `--otlp-grpc-url`, and `--otlp-http-url`). Read the startup
output or set those options deliberately rather than assuming an AppHost's ports.

Paste the full login URL printed by the standalone command into `--dashboard-url`:

```bash
aspire otel logs --dashboard-url "<dashboard-login-url>" --search "severity:error"
aspire otel traces --dashboard-url "<dashboard-login-url>" --search "status:error"
```

The login URL contains a secret token; do not commit or share it. Standalone mode
shows telemetry but does not show Aspire resources unless a resource service is
configured. See the [standalone dashboard guide](https://aspire.dev/dashboard/standalone/).

### Dashboard investigation

- **Resources**: inspect runtime state, health, commands, and generated endpoints.
- **Console logs**: stream stdout/stderr for a selected resource.
- **Structured logs**: search by severity, message, resource, trace ID, or attributes.
- **Traces**: inspect service-to-service flow, errors, spans, and correlated logs.
- **Metrics**: inspect available instrument measurements for each resource.

---

## 7. Recommended investigation order

1. Run `aspire describe` to check state, health, and endpoints.
2. Run `aspire otel logs <resource> --search "<query>"` for structured errors.
3. Run `aspire logs <resource> --tail 100` for process console output.
4. Run `aspire otel traces <resource> --has-error` for cross-service failures.
5. Run `aspire export` when a portable diagnostic bundle is needed.

## 8. Quick reference

| Goal | Command |
|------|---------|
| Install / update the CLI | `irm https://aspire.dev/install.ps1 \| iex` · `aspire update --self` |
| Update an AppHost | `aspire update --apphost <path> --yes --non-interactive` |
| Check environment | `aspire doctor` |
| Find AppHost files | `aspire ls` |
| List running AppHosts | `aspire ps` |
| Run the app | `aspire run --apphost <path>` |
| Add an integration | `aspire integration search <q>` → `aspire add <name>` |
| Inspect resources | `aspire describe [--follow] [--include-hidden]` |
| Tail logs | `aspire logs <resource> --follow` |
| Search logs server-side | `aspire logs <resource> --search "<query>"` |
| Search structured logs | `aspire otel logs <resource> --search "<query>"` |
| Search traces | `aspire otel traces <resource> --search "<query>"` |
| Export telemetry | `aspire export` |
| Launch dashboard | `aspire dashboard run` |

---

*Command options were captured from Aspire CLI 13.4.6. Run
`aspire <command> --help` to confirm behavior for your installed version.*

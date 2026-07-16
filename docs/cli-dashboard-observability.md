# Aspire CLI, Dashboard & Observability

> Reference for the current Aspire CLI (13.4+), the developer dashboard, and the
> built-in observability workflow. Use this alongside the hands-on lessons — the
> lessons show *what* to build; this guide shows *how to drive and observe* it.

This guide targets **Aspire CLI 13.4** and later. Command output shown here comes
from `aspire <command> --help`; run those yourself to see the exact options for
your installed version.

---

## 1. Installing the Aspire CLI

Aspire is now driven by a standalone **`aspire` CLI** rather than a `dotnet`
workload. Install it once, globally, and keep it current with `aspire update --self`.

### Prerequisites

- **.NET 10 SDK** or later — <https://dotnet.microsoft.com/download>
- **Docker Desktop** (or another OCI runtime) for container resources
- **Node.js 18+** for the JavaScript/TypeScript examples

### Install the Aspire CLI

**Windows (PowerShell):**

```powershell
iex "& { $(irm https://aspire.dev/install.ps1) }"
```

**Linux / macOS (bash):**

```bash
curl -sSL https://aspire.dev/install.sh | bash
```

**Cross-platform (.NET global tool):**

```bash
dotnet tool install -g Aspire.Cli
```

The install script places the CLI under `~/.aspire/bin` (`%USERPROFILE%\.aspire\bin`
on Windows) and adds it to your `PATH`. Verify the install:

```bash
aspire --version
```

> The `dotnet new install Aspire.ProjectTemplates` workflow from earlier previews
> is no longer required — the Aspire CLI provisions templates and SDK bits on demand
> (`aspire new`, `aspire init`, `aspire restore`).

### Keep the CLI up to date

```bash
aspire update --self            # update the CLI itself
aspire update                   # update integrations pinned in the AppHost
aspire update --channel stable  # or: daily
```

### Verify your environment

Before you start a lesson, confirm your machine is set up correctly:

```bash
aspire doctor
```

`aspire doctor` diagnoses common environment problems (missing SDK, container
runtime, certificates, PATH issues) and reports pass/fail checks. Add
`--format json` for scriptable output.

Trust the local HTTPS development certificate once:

```bash
aspire certs
```

---

## 2. Discovering and running AppHosts

The CLI separates **candidate AppHost files** on disk from **running AppHosts**.

| Task | Command | Notes |
|------|---------|-------|
| List AppHost project files in the workspace | `aspire ls` | Add `--format json` for tooling; `--all` ignores `.gitignore`/built-in filters |
| List **running** AppHosts | `aspire ps` | `--follow` streams updates as processes start/stop |
| Run an AppHost interactively (dev loop) | `aspire run` | Starts the app and streams output; the dashboard URL is printed |
| Start an AppHost in the background | `aspire start` | Pair with `aspire stop` |
| Wait for a resource to reach a state | `aspire wait <resource>` | Useful in scripts/CI |

```bash
# From the repo root — find AppHosts, then run one
aspire ls
aspire run
```

> **`aspire ls` vs `aspire ps`:** `ls` lists candidate AppHost *files*; `ps` lists
> *running* AppHost processes. Don't use `ls` expecting live resource state.

---

## 3. Managing integrations

Aspire hosting integrations (Redis, PostgreSQL, Azure services, etc.) are managed
through `aspire integration` and `aspire add`:

```bash
aspire integration list             # browse available hosting integrations
aspire integration search postgres  # search by keyword
aspire add redis                    # add an integration to the AppHost
```

`aspire add` wires the package into your AppHost project and updates version pins.
Use `aspire integration search` to find the exact integration name before adding.

---

## 4. Inspecting running resources

**Prefer `aspire describe` over parsing raw `aspire ps` data.** `aspire ps` answers
"which AppHosts are running"; `aspire describe` gives you the structured resource
graph — resource names, types, states, endpoints, and health — for a running AppHost.

```bash
aspire describe                 # all resources in the running AppHost
aspire describe api             # a single resource by name
aspire describe --format json   # structured output for tooling
aspire describe --follow        # stream resource state changes live
aspire describe --include-hidden # include proxies and infra resources
```

Use `--include-hidden` when a resource seems "missing" — proxies and infrastructure
resources are hidden by default.

---

## 5. Observability from the CLI

The Aspire dashboard exposes a telemetry API that the CLI reads directly, so you can
inspect logs, traces, and spans without leaving the terminal.

### Logs

```bash
aspire logs                 # logs from all resources
aspire logs api             # logs for one resource
aspire logs api --follow    # stream in real time
aspire logs api --tail 100  # last 100 lines
aspire logs api --timestamps
```

### Server-side search

For large log volumes, filter **on the server** instead of piping through `grep`:

```bash
aspire logs api --search "timeout"
```

The `--search` flag runs a full-text query against the dashboard telemetry store, so
you only transfer matching lines. See <https://aka.ms/aspire/cli-search> for query
syntax.

### OpenTelemetry data (logs, spans, traces)

```bash
aspire otel logs api      # structured logs via the telemetry API
aspire otel spans api     # spans for a resource
aspire otel traces api    # traces for a resource
```

### Export telemetry for offline analysis or bug reports

```bash
aspire export             # export telemetry + resource data to a zip
```

---

## 6. The Aspire Dashboard

When you run an AppHost (`aspire run`, `aspire start`, or `F5`/`dotnet run` on the
AppHost), the developer dashboard launches automatically.

### Ports are dynamic — read the printed URL

**Do not assume a fixed dashboard port.** The classic `http://localhost:15888`
default from earlier previews is no longer guaranteed. The CLI assigns ports
dynamically (which avoids conflicts in containers and CI) and **prints the dashboard
URL to the console** when the app starts. Always open the URL from the CLI output.

You can also launch the standalone dashboard explicitly:

```bash
aspire dashboard run    # start the dashboard (Preview)
```

The same applies to the **OTLP ingestion endpoint** the dashboard listens on — it is
assigned at startup and surfaced through environment variables injected into your
resources (for example `OTEL_EXPORTER_OTLP_ENDPOINT`). Read the endpoint from
configuration rather than hardcoding a port.

### Dashboard highlights (Aspire 13.4)

- **Resource detail pane** — selecting a resource opens a consolidated pane with its
  endpoints, environment variables, console logs, and linked telemetry (structured
  logs, traces, metrics) in one place.
- **Resource types** — resources are grouped and filterable by type (projects,
  containers, executables, and cloud/integration resources), making large graphs
  easier to navigate.
- **Trace sampling** — the traces view supports sampling so high-volume apps stay
  responsive; you can drill from a trace into the spans and correlated logs.
- **Structured logs & traces search** — the Logs and Traces pages support server-side
  filtering that mirrors the `aspire logs --search` CLI experience.

For the authoritative, version-specific list of changes, see the official release
notes: <https://github.com/dotnet/aspire/releases> and the dashboard docs at
<https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview>.

---

## 7. Quick reference

| Goal | Command |
|------|---------|
| Install / update the CLI | `iex "& { $(irm https://aspire.dev/install.ps1) }"` · `aspire update --self` |
| Check environment | `aspire doctor` |
| Find AppHost files | `aspire ls` |
| List running AppHosts | `aspire ps` |
| Run the app | `aspire run` |
| Add an integration | `aspire integration search <q>` → `aspire add <name>` |
| Inspect resources | `aspire describe [--follow] [--include-hidden]` |
| Tail logs | `aspire logs <resource> --follow` |
| Search logs server-side | `aspire logs <resource> --search "<query>"` |
| View traces/spans | `aspire otel traces <resource>` |
| Export telemetry | `aspire export` |
| Launch dashboard | `aspire dashboard run` |

---

*Command options were captured from `aspire <command> --help` on Aspire CLI 13.4.
Run the same commands locally to confirm behavior for your installed version.*

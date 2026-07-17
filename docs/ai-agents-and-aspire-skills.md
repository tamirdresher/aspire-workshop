# AI coding agents and Aspire skills

Aspire gives coding agents the same application model and telemetry that developers use in
the dashboard. The official
[`microsoft/aspire-skills`](https://github.com/microsoft/aspire-skills) bundle adds workflow
instructions so an agent uses that context safely and consistently.

This guide was validated with the stable
[Aspire 13.4.6 release](https://github.com/microsoft/aspire/releases/tag/v13.4.6). Check
`aspire --version` and the [Aspire releases](https://github.com/microsoft/aspire/releases)
before a workshop because later versions can add skills or CLI options.

## Why skills instead of a large prompt

An Aspire skill is a Markdown instruction bundle with a `SKILL.md` entry point. Skills do not
run services or expose application data by themselves. They teach a compatible coding agent
when to use the Aspire CLI or MCP server and which lifecycle guardrails to follow.

Use the first-party bundle instead of copying an old `AGENTS.md` file or maintaining custom
commands in a system prompt. If a repository still has Aspire guidance in `AGENTS.md`, run
`aspire agent init`, review the generated skills, and remove the superseded guidance.

## Prerequisites

Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/) with the package manager
used for the workshop environment. For example:

```bash
# .NET global tool
dotnet tool install -g Aspire.Cli

# Or npm
npm install -g @microsoft/aspire-cli
```

Validate the installation before configuring an agent:

```bash
aspire --version
aspire doctor --non-interactive
```

The CLI should report Aspire 13.4 or later for the commands in this guide. Update an existing
CLI with its package manager, or use `aspire update --self --yes --non-interactive` when the
installation supports self-update. Do not install the retired Aspire workload.

## Install project-local Aspire guidance

From the workspace root, use the interactive setup:

```bash
aspire agent init
```

Select only the skill location used by the active agent host. The standard location,
`.agents/skills/`, is supported by VS Code, GitHub Copilot, and OpenCode. Other supported
locations include `.github/skills/`, `.claude/skills/`, and `.opencode/skill/`.

For deterministic automation, install the six workflow skills explicitly:

```bash
aspire agent init \
  --non-interactive \
  --skill-locations standard \
  --skills aspire,aspire-init,aspire-orchestration,aspire-monitoring,aspire-deployment,aspireify
```

The standalone interactive flow selects the five skills safe for an already-wired workspace
by default. `aspireify` is available but opt-in because it is a one-time wiring workflow.
When `aspire init` creates an AppHost skeleton in an existing codebase, `aspireify` is
pre-selected for the next step.

Run `aspire agent init` again whenever you need to refresh or reconfigure the installed
guidance. The CLI validates the skill bundle before writing files.

### GitHub Copilot CLI plugin

The Aspire CLI path is recommended for project-local setup. For a user-level Copilot CLI
plugin instead, install the official marketplace entry:

```bash
copilot plugin marketplace add microsoft/aspire-skills
copilot plugin install aspire@aspire-skills
```

Do not install both project-local and user-level copies unless the environment intentionally
needs both. Project-local skills take precedence because they can carry repository-specific
guidance.

## The six workflow skills

| Skill | Use it for |
| --- | --- |
| `aspire` | Detect the AppHost, apply safety guardrails, and route a request to the right workflow. |
| `aspire-init` | Choose `aspire new` for a new app or `aspire init` for an existing repo, then create the AppHost skeleton. |
| `aspireify` | Scan an existing codebase after `aspire init`, propose a resource graph, and wire the AppHost, references, and telemetry. |
| `aspire-orchestration` | Start, stop, wait for, inspect, and recover local AppHost resources. |
| `aspire-monitoring` | Inspect resource state, console logs, structured logs, traces, metrics, browser telemetry, and dashboard data. |
| `aspire-deployment` | Publish, deploy, and destroy AppHost-modeled applications across supported targets. |

The top-level `aspire` skill is a router, not a replacement for the other five. Install the
bundle together so it can hand work to the correct workflow.

The setup flow can also offer companion tools such as `playwright-cli` and `dotnet-inspect`.
They are not part of the six-skill `microsoft/aspire-skills` workflow bundle:

- Use `playwright-cli` only after Aspire has identified the correct frontend endpoint.
- Use `dotnet-inspect` for non-Aspire .NET API inspection. For Aspire APIs, prefer
  `aspire docs api search`.

## Agent-safe runtime workflow

Humans can use `aspire run` for an interactive foreground session. Coding agents should use
`aspire start`, which starts the AppHost in the background and returns control to the agent.

Use this sequence:

```bash
# 1. Start a specific AppHost. Isolated mode prevents worktree collisions.
aspire start \
  --apphost Exercise/workshop/Lesson-03/code/Bookstore.AppHost \
  --isolated \
  --non-interactive

# 2. Wait for the resource instead of polling an HTTP port.
aspire wait api \
  --apphost Exercise/workshop/Lesson-03/code/Bookstore.AppHost \
  --non-interactive

# 3. Read structured resource state.
aspire describe \
  --apphost Exercise/workshop/Lesson-03/code/Bookstore.AppHost \
  --format Json \
  --non-interactive
```

Always pass `--apphost` when a workspace contains multiple AppHosts, as this workshop does.
Use `--isolated` in worktrees or whenever another copy of the app might already be running.

Do not:

- start an AppHost with `dotnet run` from an agent;
- guess a dashboard, OTLP, or application port;
- poll a URL while a resource is still starting;
- use `aspire ps` when raw resource details are required; use `aspire describe`;
- restart the entire AppHost when a resource-scoped command is sufficient;
- kill processes by name to recover from a conflict.

The AppHost dashboard and telemetry ports are dynamic. Discover endpoints from
`aspire describe --format Json`, the Aspire MCP resource tools, or the URL printed by the
CLI.

## Inspect before changing code

The dashboard, CLI, and MCP server read from the same resource and OpenTelemetry data. Gather
evidence before editing:

```bash
# Console output
aspire logs api --tail 100 --format Json --non-interactive

# Full-text console-log search
aspire logs api --search "timeout" --format Json --non-interactive

# Structured logs and field filters
aspire otel logs api \
  --search "@http.status_code:500" \
  --format Json \
  --non-interactive

# Distributed traces that contain errors
aspire otel traces api --has-error --format Json --non-interactive
```

Use `aspire describe --include-hidden` or the corresponding MCP tools only when hidden
resources matter to the investigation. After a code change, restart only the affected
resource with an available resource command:

```bash
aspire resource api restart \
  --apphost Exercise/workshop/Lesson-03/code/Bookstore.AppHost \
  --non-interactive
```

Then call `aspire wait` again and re-check the relevant logs or traces. If AppHost code changed,
let the orchestration skill restart the AppHost so the application model is rebuilt.

Stop the background AppHost when the task is complete:

```bash
aspire stop \
  --apphost Exercise/workshop/Lesson-03/code/Bookstore.AppHost \
  --non-interactive
```

## Look up APIs and integrations

Avoid guessing package names or AppHost APIs:

```bash
# Search conceptual documentation.
aspire docs search "browser logs" --format Json --non-interactive

# Search the current C# API reference.
aspire docs api search AddRedis \
  --language csharp \
  --format Json \
  --non-interactive

# Discover a hosting integration before adding it.
aspire integration search redis \
  --apphost Exercise/workshop/Lesson-03/code/Bookstore.AppHost \
  --format Json \
  --non-interactive

# Add the integration selected from the search results.
aspire add redis \
  --apphost Exercise/workshop/Lesson-03/code/Bookstore.AppHost \
  --non-interactive
```

Do not edit generated TypeScript AppHost files under `.aspire/modules/`. Regenerate them
through the Aspire CLI and integration workflow.

## Create or Aspire-enable an application

Use the CLI entry point that matches the repository state:

```bash
# Greenfield application
aspire new

# Existing codebase without an AppHost
aspire init
```

`aspire init` creates a minimal AppHost and can configure the agent environment. The
`aspireify` skill then inventories the repo, proposes the resource graph, wires projects and
containers, and validates the result. In an already-wired Aspire workspace, do not run
`aspireify` again for routine AppHost edits.

## Prompt examples for the workshop

These requests give the installed router enough intent to select the correct workflow:

> Start the Lesson 3 Bookstore AppHost in isolated mode, wait for the API to become healthy,
> and summarize the resource state.

> Investigate failed API requests using structured logs and traces. Show the evidence before
> changing code, then verify the fix.

> Find the current Redis hosting integration, add it to the Bookstore AppHost, and wait for it
> to become healthy before testing the API.

> Publish the AppHost for Docker Compose, show the deployment plan first, and do not deploy
> until the generated artifacts are valid.

## Security and review

- Treat MCP access like local dashboard access. It can reveal logs, traces, endpoints, and
  command surfaces for the running application.
- Keep secrets in Aspire parameters or the configured secret store. Do not paste them into
  prompts, skill files, source code, or logs.
- Review generated AppHost changes and deployment plans before accepting them.
- Keep `--non-interactive` on agent-run commands so prompts cannot block automation.
- Use the dashboard's AI Agents setup experience or `aspire agent init` to regenerate
  configuration instead of hand-editing stale MCP settings.

## Authoritative references

- [Use AI coding agents](https://aspire.dev/get-started/ai-coding-agents/)
- [Aspire skills](https://aspire.dev/get-started/aspire-skills/)
- [`aspire agent init` reference](https://aspire.dev/reference/cli/commands/aspire-agent-init/)
- [AI coding agents and the Aspire Dashboard](https://aspire.dev/dashboard/ai-coding-agents/)
- [Aspire MCP server](https://aspire.dev/get-started/aspire-mcp-server/)
- [Aspire CLI command reference](https://aspire.dev/reference/cli/commands/aspire/)
- [`microsoft/aspire-skills`](https://github.com/microsoft/aspire-skills)

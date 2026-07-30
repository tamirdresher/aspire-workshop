# Aspire in 3 Hours

A beginner-friendly, hands-on-first workshop that turns the Bookstore sample into an observable distributed application, adds data integrations, gives an AI coding agent Aspire-aware workflows, and finishes by inspecting publish/deploy pipelines.

## Run the deck

The deck uses a project-local, pinned Reveal.js installation. From the repository root:

```bash
npm ci --prefix docs/workshop-3h-2026 --registry=https://packagefeedproxy.microsoft.io/npm/
npm start --prefix docs/workshop-3h-2026
```

Open <http://127.0.0.1:8080/docs/workshop-3h-2026/reveal/> if the browser does not open automatically. Press `S` for speaker view, `F` for full screen, and `Esc` for the slide overview.

The required npm proxy is `https://packagefeedproxy.microsoft.io/npm/`. Passing it with `--registry` keeps the configuration scoped to the command and does not store credentials or change the user's global npm settings. If your environment already injects `NPM_CONFIG_REGISTRY`, this explicit option is still safe.

## Exactly 180 minutes

| Time | Segment | Mode |
| ---: | --- | --- |
| 0–10 | Welcome, outcomes, preflight | Explain/check |
| 10–20 | Aspire mental model | Explain |
| 20–50 | Lab 1: Service Defaults + AppHost | **Hands-on (30)** |
| 50–55 | Service discovery bridge | Explain |
| 55–80 | Lab 2: discovery + dashboard | **Hands-on (25)** |
| 80–90 | Break | Break |
| 90–95 | Integrations primer | Explain |
| 95–130 | Lab 3: Redis, Cosmos, queue, seed | **Hands-on (35)** |
| 130–135 | Agent workflow primer | Explain |
| 135–165 | Lab 4: AI agents + Aspire skills | **Hands-on (30)** |
| 165–170 | Publish/deploy primer | Explain |
| 170–180 | Lab 5 walkthrough, troubleshooting, wrap | **Hands-on (10)** |

Labs total **130 of 180 minutes (72%)**. Keep explain blocks short and preserve the lab stop times.

## Workshop files

- [Student lab sheet](./student-lab-sheet.md) — commands, edits, checkpoints, and recovery paths.
- [Preflight checklist](./preflight-checklist.md) — prerequisites and environment checks.
- [Facilitator runbook](./facilitator-runbook.md) — minute-by-minute delivery guide.
- [Reveal.js deck](./reveal/index.html) — the presentation.

The compressed workshop deliberately uses checked-in Lesson 1 and Lesson 2 snapshots as recovery points. The canonical full instructions remain in [Lesson 1](../../Exercise/workshop/Lesson-01/README.md), [Lesson 2](../../Exercise/workshop/Lesson-02/README.md), [AI coding agents and Aspire skills](../ai-agents-and-aspire-skills.md), and [Publish, Deploy, and Destroy](../../Examples/AspirePublish/README.md).

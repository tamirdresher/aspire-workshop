# Facilitator runbook

This runbook is fixed at **180 minutes**. Labs consume **130 minutes (72%)**. The checked-in lesson snapshots are intentional recovery points; do not spend class time rescuing every local edit.

## Before students arrive

- Run the complete [preflight checklist](./preflight-checklist.md).
- Warm Lesson 1 and Lesson 2 restores and container images.
- Run the deck with `npm start --prefix docs/workshop-3h-2026`.
- Open the [student lab sheet](./student-lab-sheet.md) in a second window.
- Confirm no AppHosts are running with `aspire ps`.
- Keep a terminal at the repository root and another in `Exercise/workshop/Lesson-02/code`.
- Do not use an Azure subscription during the three-hour delivery.

## Minute-by-minute guide

| Clock | Duration | Instructor action | Student checkpoint |
| --- | ---: | --- | --- |
| 00:00–00:03 | 3 | Welcome; frame the outcome as “one model, one dashboard, deployable artifacts.” | Repo open. |
| 00:03–00:06 | 3 | Show the exact agenda and 72% hands-on ratio. | Understand stop times. |
| 00:06–00:10 | 4 | Run the short preflight commands. Pair students immediately when blocked. | Aspire doctor and Docker usable. |
| 00:10–00:15 | 5 | Introduce the application model: AppHost, resources, references, endpoints, lifecycle, and health. | Can distinguish lifecycle state from health. |
| 00:15–00:20 | 5 | Use the Lab 1 **Concepts you must understand first** slide and its 4-minute instructor callout: contrast AppHost orchestration with Service Defaults. | Ready to locate both responsibilities in code. |
| 00:20–00:22 | 2 | Read the Lab 1 launch slide: objective, commands, success criteria, link. | Commands copied. |
| 00:22–00:45 | 23 | Students restore/build, inspect Service Defaults/AppHost, and run. Circulate. | Dashboard has four app resources. |
| 00:45–00:50 | 5 | Debrief: ask one student to explain “Service Defaults vs AppHost.” Use the solution snapshot for anyone blocked. | Lab 1 success criteria met. |
| 00:50–00:55 | 5 | Use the Lab 2 concept slide and 4-minute callout: follow `WithReference` into injected config and teach graph/health → trace → logs debugging. | Ready to prove discovery and signals. |
| 00:55–00:57 | 2 | Read the Lab 2 launch slide and point to the exact Lesson 1 section. | Dashboard open. |
| 00:57–01:15 | 18 | Students inspect injected config, open Web, find a Web→API trace, and run CLI diagnostics. | Trace crosses Web and API. |
| 01:15–01:20 | 5 | Debrief with “show me where the actual API endpoint came from.” | Lab 2 success criteria met. |
| 01:20–01:30 | 10 | **Break.** Ask students to stop Lesson 1 and start Docker if needed. | Lesson 1 stopped. |
| 01:30–01:35 | 5 | Use the Lab 3 concept slide and 4-minute callout: compare Redis cache, Cosmos persistence, and queue decoupling; trace hosting→reference→client. | Ready to justify and inspect each integration. |
| 01:35–01:37 | 2 | Read Lab 3 launch slide; warn about Cosmos emulator cold start. | Commands copied. |
| 01:37–02:04 | 27 | Students run Lesson 2, inspect the model, seed data, and inspect a trace. Use warm-up time to review code. | Seeded books visible. |
| 02:04–02:10 | 6 | Debrief cache invalidation after seed and why health-aware `WaitFor` matters. | Lab 3 success criteria met. |
| 02:10–02:15 | 5 | Use the Lab 4 concept slide and 4-minute callout: teach router→subskill dispatch and safe `start`→`wait`→`describe`→`stop`. | Ready to audit the agent workflow. |
| 02:15–02:17 | 2 | Read Lab 4 launch slide; emphasize explicit `aspire agent init`. | Correct skill location selected. |
| 02:17–02:40 | 23 | Students initialize skills and run the prompt. Check that agents use `--apphost`, `--isolated`, `wait`, and structured output. | Agent reports resource state. |
| 02:40–02:45 | 5 | Debrief the six skills and evidence-first diagnostics. Stop the AppHost. | Lab 4 success criteria met. |
| 02:45–02:50 | 5 | Use the Lab 5 concept slide and 3-minute callout: separate artifact generation from target execution; preview steps and side effects. | Ready to publish without deploying. |
| 02:50–02:52 | 2 | Read Lab 5 launch slide; explicitly prohibit live deploy/destroy. | In sample directory. |
| 02:52–02:57 | 5 | Students list pipeline steps and publish Compose artifacts. | Compose YAML generated. |
| 02:57–02:59 | 2 | Rapid troubleshooting: dynamic ports, doctor, proxy, Cosmos warm-up, explicit AppHost. | Recovery map understood. |
| 02:59–03:00 | 1 | Wrap: one model, observable runtime, safe agent workflow, reviewed deployment artifacts. | Next link bookmarked. |

Total: **180 minutes**.

## Lab facilitation notes

### Lab 1

- Prefer inspection plus execution over typing every line; the full lesson is longer than this workshop.
- If restore consumes more than five minutes, pair the student or move them to the checked-in Lesson 1 snapshot.
- Ask: “What belongs in Service Defaults, and what belongs in AppHost?”

### Lab 2

- Never give students a port. Ask them to discover it from the dashboard or `aspire describe`.
- A valid trace should show at least Web and API spans.
- If telemetry is empty, generate another page request and refresh the trace list.

### Lab 3

- First Cosmos emulator start can take 1–3 minutes. This is expected, not a lecture.
- If the API fails while Cosmos is warming, stop and rerun after the container is healthy.
- Call out the seed flow's output-cache eviction, which reflects the merged workshop friction fix.

### Lab 4

- `aspire agent init` is required; do not substitute a copied prompt or stale `AGENTS.md`.
- The expected agent sequence is `start` → `wait` → `describe`/logs → `stop`.
- Project-local skills should be reviewed like code. Do not expose dashboard login tokens or secrets.

### Lab 5

- Publish only. The goal is to inspect a pipeline and generated artifacts.
- `deploy` and `destroy` are shown with `--list-steps` so students understand side effects without executing them.
- Mention that Kubernetes APIs in the sample are preview-pinned and require a registry, current cluster context, Helm, and routing/TLS decisions.

## Time recovery rules

1. Preserve every LAB launch slide and its success criteria.
2. Cut debrief discussion before cutting hands-on time.
3. At 10 minutes behind, skip optional trace details in Lab 3.
4. At 15 minutes behind, demonstrate Lab 5 publish while students follow the artifact.
5. Never omit `aspire agent init`, agent-safe lifecycle, or the publish/deploy/destroy distinction.

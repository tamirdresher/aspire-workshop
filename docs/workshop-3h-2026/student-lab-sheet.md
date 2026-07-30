# Student lab sheet

Work from the repository root unless a step changes directory. The workshop uses the C# AppHost track to fit three hours. Stop any foreground AppHost with `Ctrl+C` before changing lessons.

## Lab 1 — Service Defaults and AppHost (30 minutes)

**Objective:** recognize the cross-cutting defaults, start the completed Lesson 1 topology, and locate its AppHost model.

1. Prepare the Lesson 1 snapshot:

   ```bash
   cd Exercise/workshop/Lesson-01/code
   dotnet restore Bookstore.sln
   npm --prefix Bookstore.Admin ci --registry=https://packagefeedproxy.microsoft.io/npm/
   dotnet build Bookstore.sln
   ```

2. Open `Bookstore.ServiceDefaults/Extensions.cs`. Find:
   - `AddServiceDefaults()`
   - OpenTelemetry, health checks, service discovery, and resilient HTTP
   - `MapDefaultEndpoints()`

3. Open `Bookstore.AppHost/Program.cs`. Draw the resource graph from the `AddProject`/`AddViteApp` calls and `WithReference` edges.

4. Start the C# AppHost:

   ```bash
   aspire run --apphost Bookstore.AppHost/Bookstore.AppHost.csproj
   ```

5. Open the authenticated dashboard URL printed by the CLI.

**Success criteria:** the build passes; the dashboard lists `api`, `web`, `worker`, and `admin`; API and Web report healthy; and you can explain the difference between Service Defaults and AppHost.

**Follow here:** [Lesson 1 — Part A: Add Service Defaults Project](../../Exercise/workshop/Lesson-01/README.md#part-a-add-service-defaults-project) and [Part B: Add App Host Project](../../Exercise/workshop/Lesson-01/README.md#part-b-add-app-host-project).

## Lab 2 — Service discovery and dashboard validation (25 minutes)

**Objective:** prove that Web reaches API by resource name and trace one request.

1. Keep the Lesson 1 AppHost running. In `Bookstore.AppHost/Program.cs`, find:

   ```csharp
   .WithReference(api)
   .WaitFor(api)
   ```

2. In `Bookstore.Web/Bookstore.Web/Program.cs`, find the named endpoint:

   ```csharp
   client.BaseAddress = new("https+http://api");
   ```

3. Open the `web` endpoint from the dashboard and load the books page.

4. In **Resources**, inspect Web's environment/configuration and find a `services__api__...` value.

5. In **Traces**, open a request that crosses `web` to `api`. Then inspect structured logs for the same trace.

6. In a second terminal, query runtime state rather than guessing ports:

   ```bash
   aspire describe --apphost Exercise/workshop/Lesson-01/code/Bookstore.AppHost/Bookstore.AppHost.csproj
   aspire logs api --tail 20 --apphost Exercise/workshop/Lesson-01/code/Bookstore.AppHost/Bookstore.AppHost.csproj
   ```

**Success criteria:** the bookstore loads; configuration contains the injected API endpoint; a distributed trace includes Web and API; and `aspire describe` reports healthy resources.

**Follow here:** [Lesson 1 — Part C: Configure Service Discovery](../../Exercise/workshop/Lesson-01/README.md#part-c-configure-service-discovery) and [Explore the Dashboard](../../Exercise/workshop/Lesson-01/README.md#explore-the-dashboard).

## Lab 3 — Integrations and seed flow (35 minutes)

**Objective:** run Redis, Cosmos DB, Azurite, and the Bookstore services as one model, then seed and verify data.

1. Stop Lesson 1 with `Ctrl+C`, then start the completed Lesson 2 snapshot:

   ```bash
   cd Exercise/workshop/Lesson-02/code
   dotnet restore Bookstore.sln
   npm --prefix Bookstore.Admin ci --registry=https://packagefeedproxy.microsoft.io/npm/
   aspire run --apphost Bookstore.AppHost/Bookstore.AppHost.csproj
   ```

2. In `Bookstore.AppHost/Program.cs`, find `AddRedis`, `AddAzureCosmosDB`, `AddAzureStorage`, each `WithReference`, and each `WaitFor`.

3. Wait for all containers to become healthy. The Cosmos emulator may take 1–3 minutes on first start. If API becomes unhealthy during the cold start, stop, let the emulator finish warming, and run the same AppHost command again.

4. In the dashboard, run the highlighted **Seed data** command on `api`. Alternatively, from a second terminal at the repository root:

   ```bash
   aspire resource api seed-db --show-response --apphost Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj
   ```

5. Open the Web endpoint and verify books appear. Run seed again and verify the `/books` output reflects the seeded data rather than stale output-cache content.

6. Inspect a trace that includes a database or cache operation.

**Success criteria:** Redis, Cosmos, storage, API, Web, Worker, and Admin are running; the seed command succeeds; books appear; and a second seed is visible without waiting for cache expiration.

**Follow here:** [Lesson 2 — Step 1: Add Redis for Caching](../../Exercise/workshop/Lesson-02/README.md#step-1-add-redis-for-caching) through [Step 4: Add Commands](../../Exercise/workshop/Lesson-02/README.md#step-4-add-commands).

## Lab 4 — AI coding agents and Aspire skills (30 minutes)

**Objective:** install first-party project guidance and have an agent use Aspire lifecycle and telemetry commands safely.

1. Stop any foreground AppHost. From the repository root, initialize agent guidance interactively:

   ```bash
   aspire agent init
   ```

   Select the location used by your agent host. The standard `.agents/skills/` location works with VS Code, GitHub Copilot, and OpenCode.

2. For a deterministic setup, the equivalent explicit command is:

   ```bash
   aspire agent init --non-interactive --skill-locations standard --skills aspire,aspire-init,aspire-orchestration,aspire-monitoring,aspire-deployment,aspireify
   ```

3. Ask the agent:

   > Start the Lesson 1 Bookstore AppHost in isolated mode, wait for the API to become healthy, and summarize the resource state. Then show the last 20 API console log lines. Do not guess ports.

4. Confirm the agent uses the lifecycle pattern:

   ```bash
   aspire start --apphost Exercise/workshop/Lesson-01/code/Bookstore.AppHost --isolated --non-interactive
   aspire wait api --apphost Exercise/workshop/Lesson-01/code/Bookstore.AppHost --non-interactive
   aspire describe --apphost Exercise/workshop/Lesson-01/code/Bookstore.AppHost --format Json --non-interactive
   aspire logs api --tail 20 --format Json --non-interactive
   aspire stop --apphost Exercise/workshop/Lesson-01/code/Bookstore.AppHost --non-interactive
   ```

5. Review generated skill files before committing them. Do not install both project-local and user-level copies unless that is intentional.

**Success criteria:** `aspire agent init` completes; the agent starts in background, waits for `api`, reports structured resource state, reads logs, does not guess a port, and stops cleanly.

**Follow here:** [AI coding agents — Install project-local Aspire guidance](../ai-agents-and-aspire-skills.md#install-project-local-aspire-guidance) and [Agent-safe runtime workflow](../ai-agents-and-aspire-skills.md#agent-safe-runtime-workflow).

## Lab 5 — Publish/deploy walkthrough (10 minutes)

**Objective:** distinguish publish, deploy, and destroy without provisioning cloud resources.

1. Inspect the sample:

   ```bash
   cd Examples/AspirePublish
   aspire publish --list-steps -- --target compose
   aspire deploy --list-steps -- --target compose
   aspire destroy --list-steps --yes --non-interactive -- --target compose
   ```

2. Generate local Docker Compose artifacts only:

   ```bash
   aspire publish --output-path ./aspire-output/compose --environment Production --non-interactive -- --target compose
   ```

3. Open `aspire-output/compose/docker-compose.yaml`. Find the workloads, environment/configuration, and generated networking.

4. Do **not** run `aspire deploy` or `aspire destroy` in class. Those commands have side effects; deployment requires target credentials and destroy requires deliberate approval.

**Success criteria:** you can state that publish writes artifacts, deploy executes a target pipeline, and destroy removes tracked target resources; `docker-compose.yaml` exists.

**Follow here:** [Publish, Deploy, and Destroy — What the commands do](../../Examples/AspirePublish/README.md#what-the-commands-do) and [Docker Compose target](../../Examples/AspirePublish/README.md#docker-compose-target).

## Fast recovery

| Symptom | Recovery |
| --- | --- |
| `aspire` not found or diagnostics fail | Run `aspire doctor`; reinstall/update with the package manager from the preflight guide. |
| npm cannot restore | Add `--registry=https://packagefeedproxy.microsoft.io/npm/`; never paste registry credentials into files. |
| Dashboard URL unknown | Read the authenticated URL printed at startup; never assume a port. |
| Resource endpoint unknown | Run `aspire describe --format Json` with the explicit `--apphost`. |
| Cosmos/API unhealthy on first run | Allow 1–3 minutes for emulator warm-up, stop the AppHost, then start it again. |
| Port/file collision in a worktree | Use `aspire start --isolated`; stop by AppHost path, never kill processes by name. |
| Too far behind | Switch to the next checked-in lesson snapshot and continue from its `code` directory. |

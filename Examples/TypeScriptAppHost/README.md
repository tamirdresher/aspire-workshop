# TypeScript AppHost (GA)

This sample shows the **GA** TypeScript AppHost shape in Aspire 13.4:
an `apphost.mts` entry point, a generated SDK under `.aspire/modules/`, and the
`Aspire.Hosting.JavaScript` `addNodeApp` API used to orchestrate a plain
Node.js/Express API.

It intentionally does **not** cover Go, Bun, or deployment/publish scenarios —
see the other `Examples/` folders for those.

## What's here

```
TypeScriptAppHost/
├── apphost.mts            # AppHost entry point (orchestration code)
├── aspire.config.json      # AppHost config: SDK version + hosting packages
├── package.json            # AppHost-root package.json (npm scripts, toolchain)
├── tsconfig.apphost.json   # Type-checking config for apphost.mts
├── eslint.config.mjs       # Lint config for apphost.mts
├── .gitignore              # Ignores node_modules/, dist/, and .aspire/
└── express-api/            # Node.js/Express API orchestrated by the AppHost
    ├── package.json
    └── index.js
```

`.aspire/modules/` (the generated TypeScript SDK) is **not** checked into
source control — it's regenerated automatically by the Aspire CLI. Never edit
files inside it by hand.

## Prerequisites

- **.NET 10 SDK** — required by the Aspire CLI and hosting runtime.
- **Node.js 20.19+, 22.13+, or 24+** — required by the TypeScript AppHost GA
  engine range (older Node.js versions aren't supported).
- **npm 10+** (the default package manager; this sample doesn't require
  pnpm/yarn/bun).
- **Aspire CLI 13.4.6** — install or update the .NET global tool with:
  ```bash
  dotnet tool install -g Aspire.Cli
  # If it is already installed:
  dotnet tool update -g Aspire.Cli
  ```
  Verify with `aspire --version`. If the CLI and configured SDK versions differ,
  run `aspire update --yes` from this sample directory.

## Run it

From this directory (`Examples/TypeScriptAppHost`):

```bash
npm ci
npm --prefix express-api ci
aspire run
```

`aspire run` will:
1. Regenerate `.aspire/modules/` from the `Aspire.Hosting.JavaScript` package
   declared in `aspire.config.json` if it's missing or stale.
2. Start the Aspire dashboard and the `api` resource (the Express app), which
   Aspire runs directly with `node index.js` and assigns a port through the
   `PORT` environment variable.
3. Probe `/health` and mark the resource healthy before dependent resources
   would be allowed to start.

Open the dashboard URL printed in the terminal to see the `api` resource
running and healthy, then browse to the HTTP endpoint shown for it (normally
`http://localhost:3001/`) to see:

```json
{ "message": "Hello from the TypeScript AppHost sample API!", "resource": "api" }
```

Press <kbd>Ctrl+C</kbd> in the terminal to stop the AppHost.

## Validate

```bash
npm run build   # tsc -p tsconfig.apphost.json — type-checks apphost.mts
npm run lint    # eslint apphost.mts
```

For a detached, collision-safe smoke test:

```bash
aspire start --isolated --non-interactive
aspire wait api --status healthy --timeout 120 --non-interactive
aspire describe api --non-interactive
aspire stop --non-interactive
```

`aspire describe api` prints the external endpoint. In this sample, Aspire
proxies `http://localhost:3001` to the randomized `PORT` assigned to the Node.js
process. Request that endpoint and expect the JSON response shown above.

## How the pieces fit together

- **`aspire.config.json`** pins the Aspire SDK and
  `Aspire.Hosting.JavaScript` integration used to generate the typed SDK in
  `.aspire/modules/`. Add or update integrations with the Aspire CLI rather
  than editing generated modules.
- **`apphost.mts`** imports `createBuilder` from the generated SDK and calls
  `addNodeApp('api', './express-api', 'index.js')` followed by
  `withHttpEndpoint({ port: 3001, env: 'PORT' })` and
  `withHttpHealthCheck({ path: '/health' })` to wire up and monitor the API,
  matching the current `Aspire.Hosting.JavaScript` generated API
  (see [Set up JavaScript apps in the AppHost](https://aspire.dev/docs/apphost/javascript/)).
- No `ASPIREATS001`-style suppression is needed here — that guidance applied to
  earlier preview releases and doesn't apply to the GA `addNodeApp` /
  `addJavaScriptApp` APIs used in this sample.

## Adding more resources

To extend this sample (for example, with Redis or Postgres), run
`aspire add <integration>` from this directory — it updates
`aspire.config.json` and regenerates `.aspire/modules/` for you. See
[TypeScript AppHost project structure](https://aspire.dev/app-host/typescript-apphost/)
and [Set up JavaScript apps in the AppHost](https://aspire.dev/docs/apphost/javascript/)
for the full API surface.

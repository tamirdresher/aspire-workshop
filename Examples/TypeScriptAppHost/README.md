# TypeScript AppHost (GA)

This sample shows the **GA** TypeScript AppHost shape introduced in Aspire 13.4:
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

- **Node.js 20.19+, 22.13+, or 24+** — required by the TypeScript AppHost GA
  engine range (older Node.js versions aren't supported).
- **npm 10+** (the default package manager; this sample doesn't require
  pnpm/yarn/bun).
- **Aspire CLI 13.4+** — install with:
  ```bash
  npm install -g @microsoft/aspire-cli
  ```
  Verify with `aspire --version` and run `aspire update` if you're on an older
  13.x release.

## Run it

From this directory (`Examples/TypeScriptAppHost`):

```bash
npm install            # installs the AppHost-root toolchain (tsx, eslint, typescript, ...)
cd express-api && npm install && cd ..   # installs the Express API's own dependencies
aspire run
```

`aspire run` will:
1. Type-check `apphost.mts` (`tsc --noEmit`) and fail fast on compile errors.
2. Regenerate `.aspire/modules/` from the `Aspire.Hosting.JavaScript` package
   declared in `aspire.config.json` if it's missing or stale.
3. Start the Aspire dashboard and the `api` resource (the Express app), which
   Aspire runs directly with `node index.js` and assigns a port through the
   `PORT` environment variable.

Open the dashboard URL printed in the terminal to see the `api` resource
running and healthy, then browse to its HTTP endpoint (for example
`http://localhost:3001/`) to see:

```json
{ "message": "Hello from the TypeScript AppHost sample API!", "resource": "api" }
```

Press <kbd>Ctrl+C</kbd> in the terminal to stop the AppHost.

## Validate without running the dashboard

```bash
npm run build   # tsc -p tsconfig.apphost.json — type-checks apphost.mts
npm run lint    # eslint apphost.mts
```

## How the pieces fit together

- **`aspire.config.json`** replaces the older `.aspire/settings.json` /
  `apphost.run.json` files. Its `packages` section (`Aspire.Hosting.JavaScript`)
  is what the CLI uses to generate the typed SDK in `.aspire/modules/` — it was
  added with `aspire add javascript` from this directory.
- **`apphost.mts`** imports `createBuilder` from the generated SDK and calls
  `addNodeApp('api', './express-api', 'index.js')` followed by
  `withHttpEndpoint({ port: 3001, env: 'PORT' })` to wire up the API resource,
  matching the current `Aspire.Hosting.JavaScript` GA API
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

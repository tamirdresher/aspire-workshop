# Bun Polyglot Example

Demonstrates orchestrating a **Bun** HTTP service from a .NET Aspire AppHost using the
[`Aspire.Hosting.JavaScript`](https://www.nuget.org/packages/Aspire.Hosting.JavaScript)
integration and its `AddBunApp(...)` extension method.

## What's here

```
Bun/
├── Polyglot.Bun.AppHost/    # Aspire AppHost that orchestrates bun-api
│   ├── Polyglot.Bun.AppHost.csproj
│   └── AppHost.cs
└── bun-api/                 # The Bun service itself
    ├── package.json
    └── index.ts
```

`bun-api` is a minimal `Bun.serve` HTTP service (TypeScript, no build step) with two routes:

- `GET /health` — returns `200 OK`
- `GET /api/hello` — returns a small JSON greeting

## Prerequisites

- **.NET 10 SDK** or later
- **Bun runtime** available on `PATH` — `bun --version` should work in your terminal.
  Install: <https://bun.sh/docs/installation>

## How `AddBunApp` works

```csharp
var api = builder.AddBunApp("bun-api", "../bun-api", "index.ts")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();
```

- Aspire runs `bun index.ts` from the `bun-api` directory. Bun executes TypeScript directly
  — there's no separate transpile step, unlike `AddNodeApp`/`AddJavaScriptApp`.
- `WithHttpEndpoint(port: 3000, env: "PORT")` assigns the service a port and passes it to
  the process via the `PORT` environment variable, which `index.ts` reads via
  `process.env.PORT` at startup.
- Because `bun-api` has a `package.json`, Aspire automatically configures Bun as the
  resource's package manager (dependency install, if any, runs via `bun install`).
- When publishing, Aspire generates a Dockerfile based on the official `oven/bun` image
  (both build and runtime stages), since Bun ships its own runtime rather than relying on a
  separate Node.js base image.

## Run it

```powershell
cd Examples/Polyglot/Bun/Polyglot.Bun.AppHost
dotnet run
```

Open the Aspire dashboard URL printed in the console, find the `bun-api` resource, and use
its endpoint to browse to `/api/hello`.

## Validate without running the full app

```powershell
# Confirm the AppHost project builds
cd Examples/Polyglot/Bun/Polyglot.Bun.AppHost
dotnet build

# If you have Bun installed, confirm the service starts standalone
cd ../bun-api
bun run index.ts
```

## Package version note

`AddBunApp` was added to `Aspire.Hosting.JavaScript` in the `13.4` release. This repo's
central pin for `Aspire.Hosting.JavaScript` is `13.1.0` (used by the other JavaScript
examples in the workshop), so this AppHost uses a `VersionOverride="13.4.6"` on its own
`PackageReference` instead of bumping the shared pin — the rest of the workshop is
unaffected.

# Bun Polyglot Example

Demonstrates orchestrating a **Bun** HTTP service from a .NET Aspire AppHost using the
[`Aspire.Hosting.JavaScript`](https://www.nuget.org/packages/Aspire.Hosting.JavaScript)
integration and its `AddBunApp(...)` extension method.

## What's here

```
Bun/
├── Polyglot.Bun.AppHost/    # Aspire AppHost that orchestrates bun-api
│   ├── Polyglot.Bun.AppHost.csproj
│   ├── AppHost.cs
│   └── aspire.config.json
└── bun-api/                 # The Bun service itself
    ├── package.json
    └── index.ts
```

`bun-api` is a minimal `Bun.serve` HTTP service (TypeScript, no build step) with two routes:

- `GET /health` — returns `200 OK`
- `GET /api/hello` — returns a small JSON greeting

## Prerequisites

- **.NET 10 SDK** or later
- **Aspire CLI 13.4.6** or later — `aspire --version` should work in your terminal.
  Install: <https://aspire.dev/get-started/install-cli/>
- **Bun runtime** available on `PATH` — `bun --version` should work in your terminal.
  Install: <https://bun.sh/docs/installation>

## How `AddBunApp` works

```csharp
builder.AddBunApp("bun-api", "../bun-api", "index.ts")
    .WithHttpEndpoint(env: "PORT")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();
```

- Aspire runs `bun index.ts` from the `bun-api` directory. Bun executes TypeScript directly
  — there's no separate transpile step, unlike `AddNodeApp`/`AddJavaScriptApp`.
- `WithHttpEndpoint(env: "PORT")` dynamically allocates a port and passes it to the process
  through the `PORT` environment variable, which `index.ts` reads at startup.
- `WithHttpHealthCheck("/health")` lets Aspire wait for the service to become healthy.
- `WithExternalHttpEndpoints()` exposes the endpoint outside the Aspire application network.
- Because `bun-api` has a `package.json`, Aspire automatically configures Bun as the
  resource's package manager (dependency install, if any, runs via `bun install`).
- When publishing, Aspire generates a Dockerfile based on the official `oven/bun` image
  (both build and runtime stages), since Bun ships its own runtime rather than relying on a
  separate Node.js base image.

## Run it

```powershell
cd Examples/Polyglot/Bun/Polyglot.Bun.AppHost
aspire start
aspire wait bun-api
aspire describe bun-api
```

Open the Aspire dashboard URL printed by `aspire start`, or append `/api/hello` to the
external endpoint shown by `aspire describe`. Run `aspire stop` when finished.

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

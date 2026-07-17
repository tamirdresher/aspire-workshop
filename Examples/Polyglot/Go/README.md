# Go Polyglot Example

Demonstrates orchestrating a **Go** HTTP service from a .NET Aspire AppHost using the
[`Aspire.Hosting.Go`](https://www.nuget.org/packages/Aspire.Hosting.Go) integration and its
`AddGoApp(...)` extension method.

## What's here

```
Go/
├── Polyglot.Go.AppHost/     # Aspire AppHost that orchestrates go-api
│   ├── Polyglot.Go.AppHost.csproj
│   ├── AppHost.cs
│   └── aspire.config.json
└── go-api/                  # The Go service itself
    ├── go.mod
    └── main.go
```

`go-api` is a minimal `net/http` service with two routes:

- `GET /health` — returns `200 OK`
- `GET /api/hello` — returns a small JSON greeting

## Prerequisites

- **.NET 10 SDK** or later
- **Aspire CLI 13.4.6** or later — `aspire --version` should work in your terminal.
  Install: <https://aspire.dev/get-started/install-cli/>
- **Go SDK** (1.22+) available on `PATH` — `go version` should work in your terminal.
  Download: <https://go.dev/dl/>

## How `AddGoApp` works

```csharp
builder.AddGoApp("go-api", "../go-api")
    .WithHttpEndpoint(env: "PORT")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();
```

- Aspire runs `go run .` from the `go-api` directory (the one containing `go.mod`).
- `WithHttpEndpoint(env: "PORT")` dynamically allocates a port and passes it to the process
  through the `PORT` environment variable, which `main.go` reads at startup.
- `WithHttpHealthCheck("/health")` lets Aspire wait for the service to become healthy.
- `WithExternalHttpEndpoints()` exposes the endpoint outside the Aspire application network.
- Build-time flags (`buildTags`, `ldFlags`, `gcFlags`, `raceDetector`) and pre-start module
  commands (`WithModTidy()`, `WithModVendor()`, `WithModDownload()`) are also available as
  parameters/extensions on `AddGoApp`, but aren't needed for this small example.

## Run it

```powershell
cd Examples/Polyglot/Go/Polyglot.Go.AppHost
aspire start
aspire wait go-api
aspire describe go-api
```

Open the Aspire dashboard URL printed by `aspire start`, or append `/api/hello` to the
external endpoint shown by `aspire describe`. Run `aspire stop` when finished.

## Publish behavior

When a publisher emits container build artifacts, Aspire uses `go-api` as the build context
and generates a multi-stage Dockerfile if the folder doesn't already contain one. The build
stage compiles a static Linux binary, and the runtime stage runs it as a non-root user.

## Validate without running the full app

```powershell
# Confirm the AppHost project builds
cd Examples/Polyglot/Go/Polyglot.Go.AppHost
dotnet build

# Compile and test every package without leaving a binary in the source tree
cd ../go-api
go test ./...
```

## Package version note

`Aspire.Hosting.Go` does not yet have a stable release upstream — only `13.4.x` preview
builds exist on NuGet as of this writing. This example pins to the latest available preview
(`13.4.6-preview.1.26319.6`) via a dedicated entry in the repo's `Directory.Packages.props`,
separate from the rest of the workshop's `13.1.0` pins, so it doesn't affect other examples.
Revisit this pin once `Aspire.Hosting.Go` reaches a stable release.

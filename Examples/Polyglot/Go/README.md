# Go Polyglot Example

Demonstrates orchestrating a **Go** HTTP service from a .NET Aspire AppHost using the
[`Aspire.Hosting.Go`](https://www.nuget.org/packages/Aspire.Hosting.Go) integration and its
`AddGoApp(...)` extension method.

## What's here

```
Go/
├── Polyglot.Go.AppHost/     # Aspire AppHost that orchestrates go-api
│   ├── Polyglot.Go.AppHost.csproj
│   └── AppHost.cs
└── go-api/                  # The Go service itself
    ├── go.mod
    └── main.go
```

`go-api` is a minimal `net/http` service with two routes:

- `GET /health` — returns `200 OK`
- `GET /api/hello` — returns a small JSON greeting

## Prerequisites

- **.NET 10 SDK** or later
- **Go SDK** (1.22+) available on `PATH` — `go version` should work in your terminal.
  Download: <https://go.dev/dl/>
- Docker Desktop (or another OCI-compatible container runtime), used by Aspire for the
  dashboard's OpenTelemetry pipeline — not required to run `go-api` itself, since
  `AddGoApp` runs it as a local process (`go run .`), not a container, in local dev.

## How `AddGoApp` works

```csharp
var api = builder.AddGoApp("go-api", "../go-api")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter();
```

- Aspire runs `go run .` from the `go-api` directory (the one containing `go.mod`).
- `WithHttpEndpoint(port: 8080, env: "PORT")` assigns the service a port and passes it to
  the process via the `PORT` environment variable, which `main.go` reads at startup.
- `WithOtlpExporter()` wires up OpenTelemetry export to the Aspire dashboard.
- Build-time flags (`buildTags`, `ldFlags`, `gcFlags`, `raceDetector`) and pre-start module
  commands (`WithModTidy()`, `WithModVendor()`, `WithModDownload()`) are also available as
  parameters/extensions on `AddGoApp`, but aren't needed for this small example.

## Run it

```powershell
cd Examples/Polyglot/Go/Polyglot.Go.AppHost
dotnet run
```

Open the Aspire dashboard URL printed in the console, find the `go-api` resource, and use
its endpoint to browse to `/api/hello`.

## Publish behavior

Running `aspire publish` (or `dotnet run -- --publisher manifest`) against this AppHost
generates a container image for `go-api` using a Go base image, since Go apps have no
runtime interpreter to ship alongside source the way Python or JavaScript do — the compiled
Go binary itself becomes the container's entry point.

## Validate without running the full app

```powershell
# Confirm the AppHost project builds
cd Examples/Polyglot/Go/Polyglot.Go.AppHost
dotnet build

# Confirm the Go service compiles
# -buildvcs=false avoids a VCS-status error when building inside a larger git repo
cd ../go-api
go build -buildvcs=false ./...
```

## Package version note

`Aspire.Hosting.Go` does not yet have a stable release upstream — only `13.4.x` preview
builds exist on NuGet as of this writing. This example pins to the latest available preview
(`13.4.6-preview.1.26319.6`) via a dedicated entry in the repo's `Directory.Packages.props`,
separate from the rest of the workshop's `13.1.0` pins, so it doesn't affect other examples.
Revisit this pin once `Aspire.Hosting.Go` reaches a stable release.

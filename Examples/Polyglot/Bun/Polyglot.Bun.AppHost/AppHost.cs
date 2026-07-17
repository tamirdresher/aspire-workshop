using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// AddBunApp runs the given script directly with `bun <script>` — Bun executes TypeScript
// natively, so index.ts needs no separate build step. WithHttpEndpoint's `env` parameter
// passes the assigned port to the process via PORT, which bun-api/index.ts reads at startup.
var api = builder.AddBunApp("bun-api", "../bun-api", "index.ts")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();

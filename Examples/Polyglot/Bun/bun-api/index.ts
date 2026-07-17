// bun-api is a tiny HTTP service used to demonstrate hosting a Bun application
// from a .NET Aspire AppHost via the Aspire.Hosting.JavaScript integration's
// AddBunApp helper.
//
// Bun natively runs TypeScript, so no separate build/transpile step is required.

const port = Number(process.env.PORT ?? 3000);

const server = Bun.serve({
  port,
  fetch(request) {
    const url = new URL(request.url);

    if (url.pathname === "/health") {
      return new Response("OK", { status: 200 });
    }

    if (url.pathname === "/api/hello") {
      return Response.json({
        message: "Hello from the Bun service!",
        from: "bun-api",
        atUtc: new Date().toISOString(),
      });
    }

    return new Response("Not Found", { status: 404 });
  },
});

console.log(`bun-api listening on http://localhost:${server.port}`);

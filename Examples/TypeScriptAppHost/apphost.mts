// Aspire TypeScript AppHost (GA) — see https://aspire.dev/app-host/typescript-apphost/
//
// This sample orchestrates a plain Node.js/Express API using `addNodeApp`,
// the GA hosting API from `Aspire.Hosting.JavaScript`. Aspire runs the app
// directly with Node.js and injects the assigned port through the `PORT`
// environment variable. The HTTP health check controls the resource's
// readiness state in the dashboard and `aspire wait`.

import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

const api = await builder
  .addNodeApp('api', './express-api', 'index.js')
  .withHttpEndpoint({ port: 3001, env: 'PORT' })
  .withHttpHealthCheck({ path: '/health' })
  .withExternalHttpEndpoints();

await builder.build().run();
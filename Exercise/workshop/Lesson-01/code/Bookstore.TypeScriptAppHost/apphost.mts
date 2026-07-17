// Aspire TypeScript AppHost
// For more information, see: https://aspire.dev

import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

const api = builder.addProject('api', '../Bookstore.API/Bookstore.API.csproj');

await builder
  .addProject('web', '../Bookstore.Web/Bookstore.Web/Bookstore.Web.csproj')
  .withReference(api)
  .waitFor(api)
  .withExternalHttpEndpoints();

await builder
  .addProject('worker', '../Bookstore.Worker/Bookstore.Worker.csproj')
  .withReference(api)
  .waitFor(api);

await builder
  .addViteApp('admin', '../Bookstore.Admin')
  .withReference(api)
  .waitFor(api)
  .withExternalHttpEndpoints();

await builder.build().run();
// Aspire TypeScript AppHost
// For more information, see: https://aspire.dev

import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

const cache = builder.addRedis('cache');

const cosmos = builder
  .addAzureCosmosDB('cosmos-account')
  .runAsEmulator({
    configureContainer: async (emulator) => {
      await emulator.withGatewayPort({ port: 7777 });
    },
  })
  .addCosmosDatabase('cosmos');

await cosmos.addContainer('books', '/id');
await cosmos.addContainer('carts', '/id');
await cosmos.addContainer('orders', '/id');

const queue = builder
  .addAzureStorage('storage')
  .runAsEmulator()
  .addQueues('queue');

const api = builder
  .addProject('api', '../Bookstore.API/Bookstore.API.csproj')
  .withReference(cache)
  .withReference(cosmos)
  .withReference(queue)
  .waitFor(cache)
  .waitFor(cosmos)
  .waitFor(queue)
  .withHttpCommand('/seed', 'Seed data', {
    description: 'Add sample books to the catalog.',
    iconName: 'Database',
    isHighlighted: true,
    methodName: 'POST',
  });

await builder
  .addProject('web', '../Bookstore.Web/Bookstore.Web/Bookstore.Web.csproj')
  .withReference(api)
  .waitFor(api)
  .withReference(cache)
  .waitFor(cache)
  .withExternalHttpEndpoints();

await builder
  .addProject('worker', '../Bookstore.Worker/Bookstore.Worker.csproj')
  .withReference(api)
  .waitFor(api)
  .withReference(queue)
  .waitFor(queue);

await builder
  .addViteApp('admin', '../Bookstore.Admin')
  .withReference(api)
  .waitFor(api)
  .withExternalHttpEndpoints();

await builder.build().run();
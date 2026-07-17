# Lesson 2: Integrations and Data

In this lesson, we will enhance our Bookstore application by adding data persistence and caching using .NET Aspire integrations. We will use **Redis** for caching and **Azure Cosmos DB** (via the emulator) for storing book data, accessing it through **Entity Framework Core**.

## Goals

1.  Add **Redis** for output caching to improve performance.
2.  Add **Azure Cosmos DB** for persistent storage of books.
3.  Use **Entity Framework Core** to interact with Cosmos DB.
4.  Add **Health Checks** to monitor the status of our services.

## Choose Your AppHost Track

Continue with the same AppHost language you selected in Lesson 1. The application projects and integration client code are shared; only the orchestration model differs.

| Track | AppHost model |
| --- | --- |
| C# | [`code/Bookstore.AppHost/Program.cs`](./code/Bookstore.AppHost/Program.cs) |
| TypeScript | [`code/Bookstore.TypeScriptAppHost/apphost.mts`](./code/Bookstore.TypeScriptAppHost/apphost.mts) |

Docker Desktop must be running for Redis, the Cosmos DB emulator, and Azurite. From the lesson's `code` directory, prepare the shared projects:

```bash
cd Exercise/workshop/Lesson-02/code
dotnet restore Bookstore.sln
npm --prefix Bookstore.Admin ci
```

Start the **C# AppHost**:

```bash
aspire run --apphost Bookstore.AppHost/Bookstore.AppHost.csproj
```

Or prepare and start the **TypeScript AppHost**:

```bash
npm --prefix Bookstore.TypeScriptAppHost ci
aspire restore --apphost Bookstore.TypeScriptAppHost
npm --prefix Bookstore.TypeScriptAppHost run build
npm --prefix Bookstore.TypeScriptAppHost run dev
```

The `dev` script performs a Release solution build before the TypeScript AppHost starts the shared .NET projects together, preventing concurrent first-build output races on Windows. The TypeScript AppHost uses the GA `apphost.mts` shape and Aspire `13.4.6`. Its generated `.aspire/modules` API is recreated by `aspire restore` and must not be edited. Press `Ctrl+C` to stop the selected track.

The AppHost-specific snippets in the steps below use C#. The complete TypeScript equivalent is:

```typescript
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
```

## Step 1: Add Redis for Caching

We'll start by adding a Redis cache to our application to store the output of our API endpoints.

1.  **Add Redis to the AppHost**:
    Open `Bookstore.AppHost/Program.cs` and add the Redis resource:

    ```csharp
    var cache = builder.AddRedis("cache");
    ```

2.  **Pass Redis to the API**:
    Update the API project registration in `Bookstore.AppHost/Program.cs` to reference the cache:

    ```csharp
    var api = builder.AddProject<Projects.Bookstore_API>("api")
        .WithReference(cache)
        .WaitFor(cache);
    ```

3.  **Configure the API**:
    In `Bookstore.API/Program.cs`, add the Redis output cache service:

    ```csharp
    builder.AddRedisOutputCache("cache");
    ```

    And enable the middleware:

    ```csharp
    app.UseOutputCache();
    ```

    Finally, cache the `/books` endpoint:

    ```csharp
    app.MapGet("/books", ...)
       .CacheOutput();
    ```

## Step 2: Add Azure Cosmos DB with EF Core

Now we will replace the in-memory list of books with a persistent database using Azure Cosmos DB and Entity Framework Core.

1.  **Add Cosmos DB to the AppHost**:
    In `Bookstore.AppHost/Program.cs`, add the Cosmos DB resource and a database:

    ```csharp
    var cosmos = builder.AddAzureCosmosDB("cosmosdb")
        .RunAsEmulator(emulator =>
        {
            emulator.WithGatewayPort(7777);
        })
        .AddCosmosDatabase("cosmos");
    
    cosmos.AddContainer("books", "/id");
    cosmos.AddContainer("carts", "/id");
    cosmos.AddContainer("orders", "/id");
    ```

    *Note: We use the emulator for local development.*

2.  **Pass Cosmos DB to the API**:
    Update the API project registration in `Bookstore.AppHost/Program.cs`:

    ```csharp
    var api = builder.AddProject<Projects.Bookstore_API>("api")
        .WithReference(cache)
        .WaitFor(cache)
        .WithReference(cosmos)
        .WaitFor(cosmos);
    ```

3.  **Add EF Core and Repository Files**:
    Copy the following files into your `Bookstore.API` project under a new `Data` folder:
    *   `Data/BookstoreDbContext.cs`: Defines the EF Core context for Cosmos DB.
    *   `Data/BookstoreRepository.cs`: Encapsulates data access logic.

    *(These files are provided in the `code/Bookstore.API/Data` directory of this lesson)*

4.  **Configure the API**:
    In `Bookstore.API/Program.cs`, register the DbContext and Repository:

    ```csharp
    // Add EF Core Context
    builder.AddCosmosDbContext<BookstoreDbContext>("cosmos");

    // Add Repository
    builder.Services.AddScoped<BookstoreRepository>();
    ```

    Ensure the database is created at startup:

    ```csharp
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
    ```

5.  **Update Endpoints**:
    Refactor your API endpoints in `Bookstore.API/Program.cs` to use `BookstoreRepository` instead of the static list.

    Example:
    ```csharp
    app.MapGet("/books", async (BookstoreRepository repository) =>
    {
        var books = await repository.GetBooksAsync();
        return Results.Ok(books);
    })
    .CacheOutput();
    ```

## Step 3: Add Azure Storage Queue

We will add an Azure Storage Queue to handle background processing tasks.

1.  **Add Storage to the AppHost**:
    In `Bookstore.AppHost/Program.cs`, add the Azure Storage resource and a queue:

    ```csharp
    var storage = builder.AddAzureStorage("storage")
        .RunAsEmulator();

    var queue = storage.AddQueues("queue");
    ```

2.  **Pass Storage to the API**:
    Update the API project registration in `Bookstore.AppHost/Program.cs` to reference the queue:

    ```csharp
    var api = builder.AddProject<Projects.Bookstore_API>("api")
        // ... other references ...
        .WithReference(queue)
        .WaitFor(queue);
    ```

## Step 4: Add Commands

We can add custom commands to the Aspire Dashboard to perform actions on our resources.

1.  **Add Command Extensions**:
    Create a new file `Bookstore.AppHost/ApiCommandExtensions.cs` and add the extension methods for seeding the database.
    *(This file is provided in the `code/Bookstore.AppHost` directory of this lesson)*

    The callback command uses a typed Boolean argument, health-aware command state, a confirmation prompt, explicit dashboard/API visibility, and a text result that can open in the dashboard. The HTTP command sends the required `POST` request and displays its response body.

2.  **Register Commands**:
    In `Bookstore.AppHost/Program.cs`, use the extension methods to add commands to the API resource:

    ```csharp
    var api = builder.AddProject<Projects.Bookstore_API>("api")
        // ... other configuration ...
        .WithSeedCommand()
        .WithSeedHttpCommand();
    ```

    The TypeScript track does not need a C# extension file. Its API registration calls `withHttpCommand('/seed', 'Seed data', { methodName: 'POST', ... })`, which exposes the same HTTP seed action in the dashboard.

    After the C# AppHost starts, the callback command can also be invoked from a separate terminal:

    ```bash
    aspire resource api seed-db --show-response --apphost Exercise/workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj
    ```

    `InteractionInput.Name` becomes the CLI option name, so command arguments are passed as named options.

## Step 5: Configure Cloud Resources (C# Track)

Now we will add logic to support deploying to Azure Cloud resources or using the local emulator based on configuration.

The TypeScript AppHost's `runAsEmulator()` calls apply in run mode; publish mode retains the underlying Azure Cosmos DB and Storage resources. The explicit `UseCloudResources` switch in this step is the C# customization path.

1.  **Add NuGet Packages**:
    Add the necessary NuGet packages to the AppHost project:
    ```bash
    dotnet add workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj package Azure.Provisioning.Storage
    dotnet add workshop/Lesson-02/code/Bookstore.AppHost/Bookstore.AppHost.csproj package Azure.Provisioning.CosmosDB
    ```

2.  **Update AppHost Program**:
    Update `Bookstore.AppHost/Program.cs` to conditionally configure resources:

    ```csharp
    using Azure.Provisioning.Storage;
    using Azure.Provisioning.CosmosDB;

    var builder = DistributedApplication.CreateBuilder(args);

    var useCloudResources = builder.Configuration.GetValue<bool>("UseCloudResources");

    // ... Redis configuration ...

    // Add Cosmos DB
    var cosmos = builder.AddAzureCosmosDB("cosmosdb");

    if (useCloudResources)
    {
        // Cloud configuration will be added in the next step
    }
    else
    {
        cosmos.RunAsEmulator(emulator =>
        {
            emulator.WithGatewayPort(7777);
        });
    }
    
    // ... Cosmos DB containers ...

    // Add Azure Storage
    var storage = builder.AddAzureStorage("storage");

    if (useCloudResources)
    {
        // Cloud configuration will be added in the next step
    }
    else
    {
        storage.RunAsEmulator();
    }
    
    // ... Queue configuration ...
    ```

3.  **Add Configuration Setting**:
    Add the `UseCloudResources` setting to `Bookstore.AppHost/appsettings.json`:
    ```json
    {
      "UseCloudResources": false
    }
    ```

## Step 6: Add Customizations (C# Track)

We can customize the cloud resources, such as setting the location or SKU.

1.  **Customize Cosmos DB**:
    Update the `if (useCloudResources)` block for Cosmos DB in `Bookstore.AppHost/Program.cs`:

    ```csharp
    if (useCloudResources)
    {
        cosmos.ConfigureInfrastructure(infra =>
        {
            var account = infra.GetProvisionableResources().OfType<CosmosDBAccount>().Single();
            account.Location = "eastus";
        });
    }
    ```

2.  **Customize Storage**:
    Update the `if (useCloudResources)` block for Storage in `Bookstore.AppHost/Program.cs`:

    ```csharp
    if (useCloudResources)
    {
        storage.ConfigureInfrastructure(infra =>
        {
            var account = infra.GetProvisionableResources().OfType<StorageAccount>().Single();
            account.Sku = new StorageSku { Name = StorageSkuName.StandardLrs };
        });
    }
    ```

## Step 7: Run and Publish

1.  **Run Locally**:
    Start the selected AppHost using the command in [Choose Your AppHost Track](#choose-your-apphost-track). For the C# track, ensure `UseCloudResources` is `false` in `appsettings.json`. Verify that Redis, Cosmos DB, and Storage use their local containers.

2.  **Publish to Azure**:
    Use `aspire publish --apphost <AppHost-directory>` to produce deployment artifacts for the selected AppHost. For the C# track, set `UseCloudResources` to `true` when exercising its custom infrastructure branch.

## Summary

You have integrated Redis, Cosmos DB, and Azure Storage, added a dashboard seed command, and run the same Bookstore topology from either a C# or TypeScript AppHost. The C# track also demonstrates custom Azure infrastructure settings.

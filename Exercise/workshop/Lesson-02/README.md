# Lesson 2: Integrations and Data

In this lesson, we will enhance our Bookstore application by adding data persistence and caching using .NET Aspire integrations. We will use **Redis** for caching and **Azure Cosmos DB** (via the emulator) for storing book data, accessing it through **Entity Framework Core**.

## Goals

1.  Add **Redis** for output caching to improve performance.
2.  Add **Azure Cosmos DB** for persistent storage of books.
3.  Use **Entity Framework Core** to interact with Cosmos DB.
4.  Add **Health Checks** to monitor the status of our services.

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
    Create a new file `Bookstore.AppHost/ApiCommandExtensions.cs` and add the extension methods for adding commands to add seeding data to the databse.
    *(This file is provided in the `code/Bookstore.AppHost` directory of this lesson)*

2.  **Register Commands**:
    In `Bookstore.AppHost/Program.cs`, use the extension methods to add commands to the API resource:

    ```csharp
    var api = builder.AddProject<Projects.Bookstore_API>("api")
        // ... other configuration ...
        .WithSeedCommand()
        .WithSeedHttpCommand();
    ```

## Step 5: Configure Cloud Resources

Now we will add logic to support deploying to Azure Cloud resources or using the local emulator based on configuration.

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

## Step 6: Add Customizations

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
    Run the application with `aspire run`. Ensure `UseCloudResources` is `false` in `appsettings.json`. Verify that the emulators are used.

2.  **Publish to Azure**:
    To deploy to Azure, you would typically set `UseCloudResources` to `true` (or override it via environment variables) and use `azd up` or `dotnet publish`.

## Summary

You have successfully integrated Redis, Cosmos DB, and Azure Storage, added custom commands, and configured your application for both local development with emulators and cloud deployment with customizations!

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

## Step 7: Run, Publish, Deploy, and Destroy

`UseCloudResources` selects emulator or Azure-backed integrations inside the application
model. It does **not** select a compute deployment target. Azure resources can contribute
infrastructure steps without a compute target, but publishing or deploying the application
services as workloads requires a deployment environment such as:

* `AddDockerComposeEnvironment("env")` for Docker Compose.
* `AddKubernetesEnvironment("k8s")` for an existing Kubernetes cluster.
* `AddAzureKubernetesEnvironment("aks")` for Aspire-provisioned AKS.
* `AddAzureContainerAppEnvironment("aca")` for Azure Container Apps.

This lesson uses `AddDockerComposeEnvironment("env")` as its compute target. In a multi-target
AppHost, add only the environment selected by configuration. The runnable [AspirePublish
example](../../../Examples/AspirePublish/README.md) shows that pattern for Docker Compose and
Kubernetes.

The React admin is published with `PublishAsStaticWebsite("/api", api, ...)`: Aspire builds
the static files, serves them from a YARP container, and proxies `/api` to the API through
service discovery. The Vite development proxy mirrors that route locally. This publishing API
is experimental in 13.4, so the AppHost scopes its `ASPIREJAVASCRIPT001` suppression to that
call. Using `PublishAsDockerFile()` alone for a static JavaScript app creates a build-only
image and deployment validation rejects it unless another resource consumes the files.

The Bookstore still declares Azure Cosmos DB and Storage resources. Their emulator overrides
apply to local orchestration, while `publish` and `deploy` contribute Azure Bicep and
provisioning steps. Authenticate with `az login`, provide a subscription and location, and use
an isolated resource group before deploying this topology. Its destroy pipeline runs both
Compose teardown and Azure resource-group teardown.

1. **Run locally**:

   Ensure `UseCloudResources` is `false`, then start the AppHost:

   ```bash
   aspire start --apphost code/Bookstore.AppHost/Bookstore.AppHost.csproj
   ```

   Local orchestration is separate from deployment. The dashboard URL is allocated at
   runtime; use the URL printed by the CLI instead of assuming a fixed port.

2. **Inspect the deployment pipeline**:

   Lesson 2 pins its AppHost SDK and hosting integrations to Aspire 13.4.6 so the current CLI
   can expose pipeline steps. List the exact steps before changing infrastructure:

   ```bash
   aspire deploy \
     --apphost code/Bookstore.AppHost/Bookstore.AppHost.csproj \
     --list-steps
   ```

   Confirm that the graph contains Docker Compose build, publish, deploy, and destroy steps
   for the application. A non-empty graph does not by itself guarantee a complete deployment;
   check that the expected compute resources participate before deploying.

3. **Publish artifacts**:

   ```bash
   aspire publish \
     --apphost code/Bookstore.AppHost/Bookstore.AppHost.csproj \
     --output-path ./aspire-output \
     --environment Production \
     --non-interactive
   ```

   `publish` generates target artifacts for review or GitOps. It does not provision or start
   the target. Depending on the resources and selected environment, output can include Docker
   Compose files, Helm charts, Kubernetes manifests, or Azure infrastructure templates.

4. **Deploy**:

   ```bash
   aspire deploy \
     --apphost code/Bookstore.AppHost/Bookstore.AppHost.csproj \
     --output-path ./aspire-output \
     --environment Production
   ```

   `deploy` runs the target pipeline. Confirm that `--list-steps` contains the expected
   compute build and install steps first. Supply all credentials, external parameters,
   registry settings, and cloud settings explicitly when adding `--non-interactive` for CI.

5. **Destroy the same environment**:

   ```bash
   aspire destroy \
     --apphost code/Bookstore.AppHost/Bookstore.AppHost.csproj \
     --output-path ./aspire-output \
     --environment Production
   ```

   Review the plan before confirming: this lesson removes the Compose stack and its isolated
   Azure deployment resource group. In unattended automation, both `--yes` and
   `--non-interactive` are required. Reuse the same AppHost path, output path, environment,
   and target configuration that were used for deployment.

### Kubernetes prerequisites and boundaries

The Kubernetes and AKS hosting integrations that accompany stable Aspire 13.4.6 are preview
packages. Discover the current version before adding one:

```bash
aspire integration search kubernetes --format Json --non-interactive
```

An existing-cluster deployment requires `kubectl`, Helm 4.2 or later, the intended current
context, and a registry reachable from the workstation and cluster. AKS additionally requires
an Azure subscription, Azure CLI authentication, and an isolated resource group.

Generated Services do not by themselves provide a production ingress or TLS setup. Configure
an installed Ingress controller or Gateway API implementation. For new AKS deployments,
Aspire's current guidance uses Application Gateway for Containers, Gateway API, and
cert-manager. See the [complete deployment example](../../../Examples/AspirePublish/README.md)
before deploying this larger Bookstore topology.

## Summary

You have integrated Redis, Cosmos DB, and Azure Storage, added custom commands, configured
local and cloud-backed resources, and learned the current separation between local
orchestration, publishing artifacts, deploying a target, and destroying its resources.

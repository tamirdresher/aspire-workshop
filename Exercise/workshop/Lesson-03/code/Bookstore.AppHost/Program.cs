using Azure.Provisioning.Storage;
using Azure.Provisioning.CosmosDB;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Bookstore.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var useCloudResources = builder.Configuration.GetValue<bool>("UseCloudResources");

#pragma warning disable ASPIRECERTIFICATES001
// Add Redis for caching
var cache = builder.AddRedis("cache")
                   .WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

// Add Cosmos DB
var cosmos = builder.AddAzureCosmosDB("cosmosdb");

if (useCloudResources)
{
    cosmos.ConfigureInfrastructure(infra =>
    {
        var account = infra.GetProvisionableResources().OfType<CosmosDBAccount>().Single();
        account.Location = AzureLocation.EastUS2;
    });
}
else
{
    cosmos.RunAsEmulator(emulator =>
    {
        emulator.WithGatewayPort(7777);
    });
}

var cosmosDb = cosmos.AddCosmosDatabase("cosmos");
cosmosDb.AddContainer("books", "/id");
cosmosDb.AddContainer("carts", "/id");
cosmosDb.AddContainer("orders", "/id");

// Add Azure Storage Queue for background processing
var storage = builder.AddAzureStorage("storage");

if (useCloudResources)
{
    storage.ConfigureInfrastructure(infra =>
    {
        var account = infra.GetProvisionableResources().OfType<StorageAccount>().Single();
        account.Sku = new StorageSku { Name = StorageSkuName.StandardLrs };
    });
}
else
{
    storage.RunAsEmulator();
}

var queue = storage.AddQueues("queue");

// Add API with Redis caching, Cosmos DB, and Queue
var api = builder.AddProject<Projects.Bookstore_API>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(cosmosDb)
    .WaitFor(cosmosDb)
    .WithReference(queue)
    .WaitFor(queue)
    .WithSeedCommand()
    .WithSeedHttpCommand();

// Add Admin React app (Node.js)
var admin = builder.AddJavaScriptApp("admin", "../Bookstore.Admin")
    .WithReference(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// Add Customer Web app
var web = builder.AddProject<Projects.Bookstore_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithReference(cache)
    .WaitFor(cache)
    .WithExternalHttpEndpoints();

// Add Worker service for book descriptions
var worker = builder.AddProject<Projects.Bookstore_Worker>("worker")
    .WithReference(api)
    .WaitFor(api)
    .WithReference(queue)
    .WaitFor(queue);

builder.AddTalkingClock("talking-clock");

builder.Build().Run();

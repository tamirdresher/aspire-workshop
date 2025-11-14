var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB emulator
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator();

// Add Azure Storage emulator (Azurite)
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var queues = storage.AddQueues("queues");
var blobs = storage.AddBlobs("blobs");

// Add Azure OpenAI (requires configuration or will use placeholder)
var openai = builder.AddConnectionString("openai");

// Add Catalog API with Cosmos DB
var catalogApi = builder.AddProject("catalog-api", "../Catalog.API/Catalog.API.csproj")
    .WithReference(cosmos)
    .WithExternalHttpEndpoints();

// Add Basket API with Queue Storage
var basketApi = builder.AddProject("basket-api", "../Basket.API/Basket.API.csproj")
    .WithReference(queues)
    .WithReference(blobs)
    .WithExternalHttpEndpoints();

// Add Ordering API with Queue Storage
var orderingApi = builder.AddProject("ordering-api", "../Ordering.API/Ordering.API.csproj")
    .WithReference(queues)
    .WithExternalHttpEndpoints();

// Add AI Assistant API with OpenAI
var aiAssistantApi = builder.AddProject("ai-assistant-api", "../AIAssistant.API/AIAssistant.API.csproj")
    .WithReference(openai)
    .WithExternalHttpEndpoints();

builder.Build().Run();

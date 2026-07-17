#:sdk Aspire.AppHost.Sdk@13.4.6
#:property ManagePackageVersionsCentrally=false

#:package Aspire.Hosting.Azure.Storage@13.4.6
#:package Aspire.Hosting.Azure.CosmosDB@13.4.6
#:package Aspire.Hosting.Foundry@13.4.6-preview.1.26319.6

#pragma warning disable ASPIRECOSMOSDB001

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
                     .RunAsEmulator();
var queues = storage.AddQueues("queues");

var cosmos = builder.AddAzureCosmosDB("restaurant")
        .RunAsPreviewEmulator(emulator =>
        {
            emulator.WithGatewayPort(7777);
            emulator.WithDataExplorer();
        })
        .AddCosmosDatabase("cosmos");
    
var dishes = cosmos.AddContainer("dish", "/id");

var foundry = builder.AddFoundry("foundry")
        .RunAsFoundryLocal();

var chat = foundry.AddDeployment("chat", "phi-3.5-mini", "1", "Microsoft");

var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(queues)
    .WithReference(chat);

builder.Build().Run();
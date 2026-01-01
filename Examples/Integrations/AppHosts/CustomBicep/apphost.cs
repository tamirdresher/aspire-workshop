#:sdk Aspire.AppHost.Sdk@13.1.0

#:package Aspire.Hosting.Azure@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddBicepTemplate(
    name: "storage",
    bicepFile: "./custom-storage.bicep")
    .WithParameter("storageAccountName", "techtraincustomstrg");

builder.AddCSharpApp("api", "./../../Services/AspireCustomResource.ApiService/")
       .WithEnvironment("ConnectionStrings__storage", storage.GetOutput("connectionString"));



builder.Build().Run();
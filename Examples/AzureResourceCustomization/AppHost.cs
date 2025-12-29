#:package Aspire.Hosting.Azure.Storage@13.1.0
#:package Azure.Provisioning.Storage@1.1.2

#:sdk Aspire.AppHost.Sdk@13.1.0

using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
  
bool useCloudResources = false;
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

builder.Build().Run();

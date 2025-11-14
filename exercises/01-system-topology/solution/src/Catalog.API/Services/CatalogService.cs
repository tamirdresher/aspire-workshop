using Microsoft.Azure.Cosmos;
using ECommerce.Shared.Models;

namespace Catalog.API.Services;

public class CatalogService
{
    private readonly Container _container;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(CosmosClient cosmosClient, IConfiguration configuration, ILogger<CatalogService> logger)
    {
        _logger = logger;
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "ECommerceDB";
        var containerName = configuration["CosmosDb:CatalogContainerName"] ?? "Catalog";
        
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<List<CatalogItem>> GetItemsAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", (page - 1) * pageSize)
                .WithParameter("@limit", pageSize);

            var items = new List<CatalogItem>();
            using var iterator = _container.GetItemQueryIterator<CatalogItem>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog items");
            return new List<CatalogItem>();
        }
    }

    public async Task<CatalogItem?> GetItemByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<CatalogItem>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<CatalogItem>> GetItemsByCategoryAsync(string category)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Category = @category")
                .WithParameter("@category", category);

            var items = new List<CatalogItem>();
            using var iterator = _container.GetItemQueryIterator<CatalogItem>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalog items by category");
            return new List<CatalogItem>();
        }
    }

    public async Task<CatalogItem> CreateItemAsync(CatalogItem item)
    {
        var response = await _container.CreateItemAsync(item, new PartitionKey(item.Id));
        return response.Resource;
    }

    public async Task<CatalogItem> UpdateItemAsync(CatalogItem item)
    {
        var response = await _container.UpsertItemAsync(item, new PartitionKey(item.Id));
        return response.Resource;
    }

    public async Task DeleteItemAsync(string id)
    {
        await _container.DeleteItemAsync<CatalogItem>(id, new PartitionKey(id));
    }
}

using Azure.Storage.Queues;
using System.Text.Json;
using ECommerce.Shared.Models;

namespace Basket.API.Services;

public class BasketService
{
    private readonly Dictionary<string, CustomerBasket> _baskets = new();
    private readonly QueueClient? _queueClient;
    private readonly ILogger<BasketService> _logger;
    private readonly CatalogClient _catalogClient;

    public BasketService(IConfiguration configuration, ILogger<BasketService> logger, CatalogClient catalogClient)
    {
        _logger = logger;
        _catalogClient = catalogClient;
        
        try
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                _queueClient = new QueueClient(connectionString, "checkout-queue");
                _queueClient.CreateIfNotExists();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize Azure Queue. Using in-memory storage only.");
        }
    }

    public Task<CustomerBasket?> GetBasketAsync(string buyerId)
    {
        _baskets.TryGetValue(buyerId, out var basket);
        return Task.FromResult(basket);
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        // PROBLEM: Service-to-service call requires manual validation
        // We need to ensure Catalog.API is running and reachable
        // If it's down or misconfigured, basket updates fail
        foreach (var item in basket.Items)
        {
            try
            {
                var productExists = await _catalogClient.ValidateProductExistsAsync(item.ProductId);
                if (!productExists)
                {
                    _logger.LogWarning("Product {ProductId} not found in catalog, removing from basket", item.ProductId);
                    basket.Items.Remove(item);
                }
            }
            catch (InvalidOperationException ex)
            {
                // Catalog API is unavailable - log but don't fail the basket update
                // PROBLEM: Without service mesh or circuit breaker, we can't handle this gracefully
                _logger.LogError(ex, "Cannot validate product {ProductId} - Catalog API unavailable", item.ProductId);
                // In a real system, you'd want retry logic, circuit breaker, etc.
                // Aspire provides patterns for this through its integrations
            }
        }
        
        _baskets[basket.BuyerId] = basket;
        return basket;
    }

    public Task DeleteBasketAsync(string buyerId)
    {
        _baskets.Remove(buyerId);
        return Task.CompletedTask;
    }

    public async Task<bool> CheckoutBasketAsync(string buyerId, string shippingAddress)
    {
        if (!_baskets.TryGetValue(buyerId, out var basket))
        {
            return false;
        }

        try
        {
            // Send to queue for order processing
            if (_queueClient != null)
            {
                var checkoutMessage = new
                {
                    BuyerId = buyerId,
                    Items = basket.Items,
                    Total = basket.Total(),
                    ShippingAddress = shippingAddress,
                    CheckoutDate = DateTime.UtcNow
                };

                var messageJson = JsonSerializer.Serialize(checkoutMessage);
                await _queueClient.SendMessageAsync(messageJson);
            }

            // Clear basket after checkout
            _baskets.Remove(buyerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout for buyer {BuyerId}", buyerId);
            return false;
        }
    }
}

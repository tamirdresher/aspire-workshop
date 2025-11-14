using Azure.Storage.Queues;
using System.Text.Json;
using ECommerce.Shared.Models;

namespace Basket.API.Services;

public class BasketService
{
    private readonly Dictionary<string, CustomerBasket> _baskets = new();
    private readonly QueueClient? _queueClient;
    private readonly ILogger<BasketService> _logger;

    public BasketService(IConfiguration configuration, ILogger<BasketService> logger)
    {
        _logger = logger;
        
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

    public Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        _baskets[basket.BuyerId] = basket;
        return Task.FromResult(basket);
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

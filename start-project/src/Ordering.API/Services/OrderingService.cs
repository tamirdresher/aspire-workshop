using Azure.Storage.Queues;
using System.Text.Json;
using ECommerce.Shared.Models;
using ECommerce.Shared.Events;

namespace Ordering.API.Services;

public class OrderingService
{
    private readonly Dictionary<string, Order> _orders = new();
    private readonly QueueClient? _queueClient;
    private readonly ILogger<OrderingService> _logger;
    private readonly BasketClient _basketClient;

    public OrderingService(IConfiguration configuration, ILogger<OrderingService> logger, BasketClient basketClient)
    {
        _logger = logger;
        _basketClient = basketClient;
        
        try
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                _queueClient = new QueueClient(connectionString, "order-notifications");
                _queueClient.CreateIfNotExists();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize Azure Queue. Using in-memory storage only.");
        }
    }

    public Task<List<Order>> GetOrdersAsync(string? buyerId = null)
    {
        var orders = string.IsNullOrEmpty(buyerId) 
            ? _orders.Values.ToList() 
            : _orders.Values.Where(o => o.BuyerId == buyerId).ToList();
        
        return Task.FromResult(orders);
    }

    public Task<Order?> GetOrderByIdAsync(string id)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        // PROBLEM: Cross-service dependency creates fragility
        // We need to call Basket.API to get the basket data
        // If Basket.API is down, order creation fails
        // No automatic retry, no fallback, no service mesh
        try
        {
            var basket = await _basketClient.GetBasketAsync(order.BuyerId);
            if (basket != null)
            {
                _logger.LogInformation("Retrieved basket with {ItemCount} items for order creation",
                    basket.Items.Count);
                // In a real system, you'd populate order items from the basket
                // For this demo, we'll just validate the basket exists
            }
            else
            {
                _logger.LogWarning("No basket found for buyer {BuyerId}, creating order anyway", order.BuyerId);
            }
        }
        catch (InvalidOperationException ex)
        {
            // PROBLEM: Service unavailable - what do we do?
            // Without Aspire's resilience patterns, we have to handle this manually
            _logger.LogError(ex, "Cannot retrieve basket - Basket.API unavailable. Order creation may be incomplete.");
            // In production, you'd want: retry logic, circuit breaker, fallback strategy
            // Aspire integrations provide patterns for handling this
        }
        
        order.Id = Guid.NewGuid().ToString();
        order.OrderDate = DateTime.UtcNow;
        order.Status = "Pending";
        
        _orders[order.Id] = order;

        // Try to delete the basket after order creation
        try
        {
            await _basketClient.DeleteBasketAsync(order.BuyerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete basket after order creation");
            // Non-critical failure, don't block order creation
        }

        // Send notification
        if (_queueClient != null)
        {
            try
            {
                var orderEvent = new OrderPlacedEvent
                {
                    OrderId = order.Id,
                    BuyerId = order.BuyerId,
                    OrderDate = order.OrderDate,
                    Total = order.Total
                };

                var messageJson = JsonSerializer.Serialize(orderEvent);
                await _queueClient.SendMessageAsync(messageJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order notification");
            }
        }

        return order;
    }

    public async Task<Order?> UpdateOrderStatusAsync(string id, string status)
    {
        if (!_orders.TryGetValue(id, out var order))
        {
            return null;
        }

        var oldStatus = order.Status;
        order.Status = status;

        // Send notification
        if (_queueClient != null)
        {
            try
            {
                var statusEvent = new OrderStatusChangedEvent
                {
                    OrderId = id,
                    OldStatus = oldStatus,
                    NewStatus = status,
                    ChangedDate = DateTime.UtcNow
                };

                var messageJson = JsonSerializer.Serialize(statusEvent);
                await _queueClient.SendMessageAsync(messageJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending status change notification");
            }
        }

        return order;
    }
}

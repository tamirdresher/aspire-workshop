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

    public OrderingService(IConfiguration configuration, ILogger<OrderingService> logger)
    {
        _logger = logger;
        
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
        order.Id = Guid.NewGuid().ToString();
        order.OrderDate = DateTime.UtcNow;
        order.Status = "Pending";
        
        _orders[order.Id] = order;

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

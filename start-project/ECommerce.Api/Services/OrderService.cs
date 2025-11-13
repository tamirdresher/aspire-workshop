using ECommerce.Shared.Models;

namespace ECommerce.Api.Services;

public class OrderService
{
    private readonly List<Order> _orders = new();
    private int _nextOrderId = 1;

    public Task<List<Order>> GetAllOrdersAsync()
    {
        return Task.FromResult(_orders);
    }

    public Task<Order?> GetOrderByIdAsync(int id)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        return Task.FromResult(order);
    }

    public Task<Order> CreateOrderAsync(Order order)
    {
        order.Id = _nextOrderId++;
        order.OrderDate = DateTime.UtcNow;
        order.Status = "Pending";
        
        // Calculate total amount
        order.TotalAmount = order.Items.Sum(item => item.Quantity * item.UnitPrice);
        
        _orders.Add(order);
        return Task.FromResult(order);
    }

    public Task<Order?> UpdateOrderStatusAsync(int id, string status)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order != null)
        {
            order.Status = status;
        }
        return Task.FromResult(order);
    }
}

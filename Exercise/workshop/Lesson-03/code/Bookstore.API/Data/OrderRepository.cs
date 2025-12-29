using Microsoft.EntityFrameworkCore;
using Bookstore.Shared;

namespace Bookstore.API.Data;

public class OrderRepository
{
    private readonly BookstoreDbContext _context;

    public OrderRepository(BookstoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _context.Orders.ToListAsync();
    }

    public async Task<Order?> GetOrderAsync(string id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task CreateOrderAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }
}

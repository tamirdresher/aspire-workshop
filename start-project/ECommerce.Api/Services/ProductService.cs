using ECommerce.Shared.Models;

namespace ECommerce.Api.Services;

public class ProductService
{
    private readonly List<Product> _products = new()
    {
        new Product
        {
            Id = 1,
            Name = "Laptop",
            Description = "High-performance laptop for developers",
            Price = 1299.99m,
            ImageUrl = "/images/laptop.jpg",
            StockQuantity = 50,
            Category = "Electronics"
        },
        new Product
        {
            Id = 2,
            Name = "Mechanical Keyboard",
            Description = "RGB mechanical keyboard with blue switches",
            Price = 149.99m,
            ImageUrl = "/images/keyboard.jpg",
            StockQuantity = 100,
            Category = "Electronics"
        },
        new Product
        {
            Id = 3,
            Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse",
            Price = 49.99m,
            ImageUrl = "/images/mouse.jpg",
            StockQuantity = 150,
            Category = "Electronics"
        },
        new Product
        {
            Id = 4,
            Name = "USB-C Hub",
            Description = "7-in-1 USB-C hub with multiple ports",
            Price = 79.99m,
            ImageUrl = "/images/hub.jpg",
            StockQuantity = 75,
            Category = "Accessories"
        },
        new Product
        {
            Id = 5,
            Name = "Monitor",
            Description = "27-inch 4K monitor",
            Price = 399.99m,
            ImageUrl = "/images/monitor.jpg",
            StockQuantity = 30,
            Category = "Electronics"
        }
    };

    public Task<List<Product>> GetAllProductsAsync()
    {
        return Task.FromResult(_products);
    }

    public Task<Product?> GetProductByIdAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        var products = _products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(products);
    }
}

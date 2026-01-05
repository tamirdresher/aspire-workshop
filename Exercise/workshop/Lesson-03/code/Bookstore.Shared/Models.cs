using System.Text.Json.Serialization;

namespace Bookstore.Shared;

public class Book
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = "https://loremflickr.com/200/300/";
    public string Description { get; set; } = string.Empty;
    public int Stock { get; set; } = 10;
}

public class CartItem
{
    public string BookId { get; set; } = string.Empty;
    public Book? Book { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice => Book?.Price * Quantity ?? 0;
}

public class Cart
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<CartItem> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
    public int TotalItems => Items.Sum(i => i.Quantity);
}

public class Order
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<CartItem> Items { get; set; } = new();
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
}

public class BookCreatedMessage
{
    public string BookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}

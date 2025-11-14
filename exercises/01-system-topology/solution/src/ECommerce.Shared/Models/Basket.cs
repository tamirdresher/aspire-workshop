namespace ECommerce.Shared.Models;

public class BasketItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string PictureUrl { get; set; } = string.Empty;
}

public class CustomerBasket
{
    public string BuyerId { get; set; } = string.Empty;
    public List<BasketItem> Items { get; set; } = new();
    
    public decimal Total() => Items.Sum(i => i.UnitPrice * i.Quantity);
}

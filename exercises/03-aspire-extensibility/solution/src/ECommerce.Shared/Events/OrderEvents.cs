namespace ECommerce.Shared.Events;

public class OrderPlacedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}

public class OrderStatusChangedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; }
}

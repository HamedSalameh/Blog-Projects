namespace Shared
{
    public class OrderPlacedEvent
    {
        public string OrderId { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public decimal Total { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string PaymentId { get; set; } = default!;
    }

    public class OrderItem
    {
        public string ProductId { get; set; } = default!;
        public int Quantity { get; set; }
    }
}

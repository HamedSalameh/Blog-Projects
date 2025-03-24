namespace OrderProcessingDemo.DTO
{
    public class PlaceOrderRequest
    {
        public string UserId { get; set; } = default!;
        public decimal Total { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}

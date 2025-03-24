namespace InventoryServiceWorker
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = "order-placed";
        public string GroupId { get; set; } = "inventory-service";
    }
}

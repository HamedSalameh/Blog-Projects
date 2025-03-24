namespace OrderProcessingDemo;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string OrderPlacedTopic { get; set; } = "order-placed";
}
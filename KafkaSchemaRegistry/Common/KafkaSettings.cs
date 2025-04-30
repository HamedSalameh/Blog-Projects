public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "user-consumer-group";
    public string KafkaUrl { get; set; } = "localhost:9092";
    public string SchemaRegistryUrl { get; set; } = "localhost:8081";
    public string TopicName { get; set; } = "user-events";
    public string DlqTopicName { get; set; } = "user-events-dlq";
    public string ClientId { get; set; } = "user-producer";
}

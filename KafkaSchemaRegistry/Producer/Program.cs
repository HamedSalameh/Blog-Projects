using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaSchemas;
using Microsoft.Extensions.Configuration;

// Load configuration from appsettings.json
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

// Bind the KafkaSettings section to a strongly typed class
var kafkaSettings = config.GetSection("KafkaSettings").Get<KafkaSettings>();

// Configure Schema Registry
var schemaRegistryConfig = new SchemaRegistryConfig
{
    Url = kafkaSettings.SchemaRegistryUrl
};

// Configure Kafka producer settings
var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaSettings.BootstrapServers,
    ClientId = "user-producer", // You can use kafkaSettings.ClientId if exposed
    EnableIdempotence = true,   // Ensures no duplicate messages
    Acks = Acks.All,            // Wait for all replicas to acknowledge
    CompressionType = CompressionType.Gzip // Compress messages to reduce size
};

// Create a schema registry client
using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

// Build the Kafka producer and attach the Avro serializer for message values
using var producer = new ProducerBuilder<string, UserCreated>(producerConfig)
    .SetValueSerializer(new AvroSerializer<UserCreated>(schemaRegistry))
    .Build();

bool _running = true;

// Handle CTRL+C to allow graceful shutdown
Console.CancelKeyPress += (_, args) =>
{
    _running = false;
    args.Cancel = true;
    Console.WriteLine("\nCTRL+C detected. Exiting...");
};

// Continuously send a test user message every 2 seconds
while (_running)
{
    Console.WriteLine("Sending user...");

    var message = new UserCreated
    {
        UserId = Guid.NewGuid().ToString(),
        UserName = "John",
        Email = "JohnDoe@example.com"
    };

    // Produce the message to the configured Kafka topic
    await producer.ProduceAsync(kafkaSettings.TopicName, new Message<string, UserCreated>
    {
        Key = message.UserId,
        Value = message
    });

    await Task.Delay(2000); // Wait for 2 seconds before sending the next message
}

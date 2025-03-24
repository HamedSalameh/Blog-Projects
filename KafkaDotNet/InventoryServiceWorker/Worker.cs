using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Shared;
using System.Text.Json;

namespace InventoryServiceWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly KafkaSettings _kafkaSettings;
    private IConsumer<Ignore, string>? _consumer;

    public Worker(ILogger<Worker> logger, IOptions<KafkaSettings> kafkaOptions)
    {
        _logger = logger;
        _kafkaSettings = kafkaOptions.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe(_kafkaSettings.Topic);

        _logger.LogInformation("Kafka consumer started and subscribed to topic: {Topic}", _kafkaSettings.Topic);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensures method runs asynchronously

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer?.Consume(stoppingToken);
                if (result == null || string.IsNullOrWhiteSpace(result.Message?.Value))
                    continue;

                var order = JsonSerializer.Deserialize<OrderPlacedEvent>(result.Message.Value);

                if (order == null)
                {
                    _logger.LogWarning("Received null or malformed order event");
                    continue;
                }

                _logger.LogInformation("Order received: {OrderId} at {Timestamp}", order.OrderId, order.Timestamp);

                foreach (var item in order.Items)
                {
                    _logger.LogInformation(" - Product: {ProductId}, Quantity: {Quantity}", item.ProductId, item.Quantity);
                    await Task.Delay(125);
                    _logger.LogInformation("Order inventory updated: {OrderId}", order.OrderId);

                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Kafka message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing Kafka message");
            }
        }
    }

    public override void Dispose()
    {
        _consumer?.Close(); // Ensures graceful shutdown
        _consumer?.Dispose();
        base.Dispose();
    }
}

using Confluent.Kafka;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared;
using System.Text.Json;

namespace InventoryServiceWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly KafkaSettings _kafkaSettings;
    private IConsumer<Ignore, string>? _consumer;
    private IProducer<Null, string>? _producer;

    // Simulated store for idempotency check (use a persistent store in production)
    private readonly MemoryCache _processedOrderCache = new(new MemoryCacheOptions());

    public Worker(ILogger<Worker> logger, IOptions<KafkaSettings> kafkaOptions)
    {
        _logger = logger;
        _kafkaSettings = kafkaOptions.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        InitializeComsuner();
        InitializeProducer();

        _logger.LogInformation("Kafka consumer started and subscribed to topic: {Topic}", _kafkaSettings.Topic);

        return base.StartAsync(cancellationToken);
    }

    private void InitializeProducer()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers
        };

        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    private void InitializeComsuner()
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

                OrderPlacedEvent? order = await DeserializeOrSendToDLQ(result.Message.Value, result, stoppingToken);
                if (order == null)
                {
                    _logger.LogWarning("Invalid (empty) order message received. Skipping...");
                    _consumer?.Commit(result);
                    continue;
                }

                // Idempotency check
                bool isDuplicate = _processedOrderCache.TryGetValue(order.OrderId, out _);
                if (isDuplicate)
                {
                    _logger.LogInformation("Duplicate order skipped: {OrderId}", order.OrderId);
                    _consumer?.Commit(result);
                    continue;
                }

                _logger.LogInformation("Order received: {OrderId} at {Timestamp}", order.OrderId, order.Timestamp);
                await HandleOrder(order);

                // Mark as processed with expiration
                _processedOrderCache.Set(order.OrderId, true, TimeSpan.FromHours(24));

                _consumer?.Commit(result); // Commit offset after successful processing
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested. Exiting consume loop gracefully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing Kafka message");
            }
        }
    }

    private async Task<OrderPlacedEvent?> DeserializeOrSendToDLQ(string messageValue, ConsumeResult<Ignore, string> result, CancellationToken token)
    {
        try
        {
            return JsonSerializer.Deserialize<OrderPlacedEvent>(messageValue);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to deserialize message. Sending to DLQ.");
            _ = await _producer?.ProduceAsync("order-placed-dlq", new Message<Null, string>
            {
                Value = messageValue
            }, token);

            _consumer?.Commit(result);
            return null;
        }
    }

    private async Task HandleOrder(OrderPlacedEvent order)
    {
        _logger.LogInformation("Order received: {OrderId} at {Timestamp}", order.OrderId, order.Timestamp);

        foreach (var item in order.Items)
        {
            _logger.LogInformation(" - Product: {ProductId}, Quantity: {Quantity}", item.ProductId, item.Quantity);
            await Task.Delay(125);
        }

        _logger.LogInformation("Order inventory updated: {OrderId}", order.OrderId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_consumer != null)
        {
            _logger.LogInformation("Closing Kafka consumer...");
            _consumer.Close();
            _consumer.Dispose();
            _producer?.Dispose();
            _processedOrderCache.Dispose();

            _logger.LogInformation("Kafka consumer closed.");
        }

        await base.StopAsync(cancellationToken);
    }
}

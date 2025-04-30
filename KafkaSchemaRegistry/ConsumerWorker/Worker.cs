using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaSchemas;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace ConsumerWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly KafkaSettings _kafkaSettings;
        private readonly AsyncRetryPolicy _retryPolicy;

        private IConsumer<string, UserCreated>? _consumer;
        private ISchemaRegistryClient? _schemaRegistry;
        private IProducer<string, string>? _dlqProducer;

        private readonly string _topic;

        public Worker(ILogger<Worker> logger, IOptions<KafkaSettings> kafkaSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _kafkaSettings = kafkaSettings.Value ?? throw new ArgumentNullException(nameof(kafkaSettings));
            _topic = _kafkaSettings.TopicName ?? "user-events";

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) =>
                    {
                        _logger.LogWarning(exception,
                            "Processing failed. Retry {RetryCount} in {Delay}s.",
                            retryCount, timeSpan.TotalSeconds);
                    });
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = _kafkaSettings.SchemaRegistryUrl
            };

            _schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

            _consumer = new ConsumerBuilder<string, UserCreated>(consumerConfig)
                .SetValueDeserializer(new AvroDeserializer<UserCreated>(_schemaRegistry).AsSyncOverAsync())
                .Build();

            // Create a separate producer for sending failed messages to the DLQ
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers
            };

            _dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();

            _consumer.Subscribe(_topic);

            _logger.LogInformation("Kafka consumer started and subscribed to topic: {Topic}", _topic);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var result = _consumer?.Consume(stoppingToken);
                            if (result == null) continue;

                            var user = result.Message.Value;

                            try
                            {
                                await _retryPolicy.ExecuteAsync(async () =>
                                {
                                    await ProcessUserAsync(user);
                                });

                                _logger.LogInformation("Successfully processed user: {UserId} - {Email}", user.UserId, user.Email);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Message failed after retries. Sending to DLQ...");

                                var dlqPayload = JsonSerializer.Serialize(user);

                                await _dlqProducer.ProduceAsync(_kafkaSettings.DlqTopicName, new Message<string, string>
                                {
                                    Key = result.Message.Key,
                                    Value = dlqPayload
                                });

                                _logger.LogInformation("Message sent to DLQ: {DlqTopic}", _kafkaSettings.DlqTopicName);
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, "Kafka consume error.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumer stopping...");
                }
                finally
                {
                    _consumer?.Close();
                    _consumer?.Dispose();
                    _schemaRegistry?.Dispose();
                    _dlqProducer?.Flush();
                    _dlqProducer?.Dispose();
                }
            }, stoppingToken);
        }

        private async Task ProcessUserAsync(UserCreated user)
        {
            await Task.Delay(10); // Simulate some processing

            // Simulate a failure condition
            if (user.Email == "fail@example.com")
                throw new Exception("Simulated failure");
        }
    }
}

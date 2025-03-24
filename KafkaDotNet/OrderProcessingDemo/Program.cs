using Confluent.Kafka;
using Microsoft.Extensions.Options;
using OrderProcessingDemo.DTO;
using OrderProcessingDemo.Endpoints.PlaceOrder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind Kafka settings from config
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("Kafka"));

// setup Kafka producer
ConfigureKafka(builder);

var app = builder.Build();

app.MapPostEndpoint("/placeorder");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();

static void ConfigureKafka(WebApplicationBuilder builder)
{
    builder.Services.AddSingleton(sp =>
    {
        var kafkaSettings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            Acks = Acks.All,            // Wait for all replicas to acknowledge
            EnableIdempotence = true,   // Ensure exactly-once semantics
            MessageSendMaxRetries = 3,  // Retry 3 times
            RetryBackoffMs = 100        // Wait 100ms between retries
        };

        return new ProducerBuilder<Null, string>(config).Build();
    });
}
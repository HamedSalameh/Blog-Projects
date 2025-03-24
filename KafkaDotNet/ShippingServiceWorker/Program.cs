using ShippingServiceWorker;

var builder = Host.CreateApplicationBuilder(args);
// Bind Kafka settings from config
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("Kafka"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

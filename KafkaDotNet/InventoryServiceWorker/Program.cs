using InventoryServiceWorker;

var builder = Host.CreateApplicationBuilder(args);

// load kafka configuration
builder.Configuration.AddJsonFile("appsettings.json");
// Bind Kafka settings from config
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("Kafka"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

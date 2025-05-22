using RateLimitingWithRedis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddSingleton(provide =>
{
    var redis = provide.GetRequiredService<IConnectionMultiplexer>();
    var logger = provide.GetRequiredService<ILogger<TokenBucketRateLimiter>>();
    return new TokenBucketRateLimiter(redis, logger, 10, 1);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// configure rate limiting with Redis

#region Basic Rate Limiting with Redis
//app.Use(async (context, next) =>
//{
//    var redisRateLimiter = context.RequestServices.GetRequiredService<RedisRateLimiter>();
//    var clientIp = context.Connection.RemoteIpAddress?.ToString();
//    var isAllowed = await redisRateLimiter.IsAllowedAsync(clientIp);
//    if (!isAllowed)
//    {
//        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
//        await context.Response.WriteAsync("Rate limit exceeded.");
//        return;
//    }
//    await next();
//});

#endregion

#region Slide Window Rate Limiting

app.Use(async (context, next) =>
{
    var rateLimiter = context.RequestServices.GetRequiredService<TokenBucketRateLimiter>();

    // Use client IP or user ID
    var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    if (!await rateLimiter.IsAllowedAsync(clientKey))
    {
        context.Response.StatusCode = 429;
        context.Response.Headers["Retry-After"] = "60"; // Optional
        await context.Response.WriteAsync("Too many requests. Try again later.");
        return;
    }

    await next();
});

#endregion

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

using Amazon;
using Amazon.CognitoIdentityProvider;
using AWSCognitoUserMgmt.IdentityProvider;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind AWS Cognito configuration from appsettings.json
builder.Services.Configure<IdentityProviderConfiguration>(
    builder.Configuration.GetSection("AWS:Cognito"));

// Register AmazonCognitoIdentityProviderClient with DI
builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(sp =>
{
    var config = sp.GetRequiredService<IOptions<IdentityProviderConfiguration>>().Value;
    var awsRegion = RegionEndpoint.GetBySystemName(config.Region);
    return new AmazonCognitoIdentityProviderClient(awsRegion);
});

builder.Services.AddScoped<ICognitoService, CognitoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

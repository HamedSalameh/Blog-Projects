using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrderProcessingDemo.DTO;
using Shared;
using System.Text.Json;

namespace OrderProcessingDemo.Endpoints.PlaceOrder
{
    public static class PlaceOrderEndpoint
    {
        public static IEndpointConventionBuilder MapPostEndpoint(this IEndpointRouteBuilder builder, string pattern = "")
        {
            var endpoint = builder.MapPost(pattern, PlaceOrderAsync)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError);

            return endpoint;
        }

        public static async Task<IResult> PlaceOrderAsync([FromBody] PlaceOrderRequest placeOrderRequest, 
            CancellationToken cancellationToken,
            ILoggerFactory loggerFactory,
            IProducer<Null, string> producer,
            IOptions<KafkaSettings> kafkaOptions)
        {
            ILogger logger = loggerFactory.CreateLogger("PlaceOrderEndpoint");

            if (placeOrderRequest == null)
            {
                logger.LogError("Invalid request object");
                return Results.BadRequest("The submitted request is not valid or empty");
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);  // Fake some processing time, charge payment, etc.
            logger.LogInformation("Order processed successfully");

            // Submit the order to Kafka for further processing
            var orderPlaceEvent = new OrderPlacedEvent
            {
                OrderId = Guid.NewGuid().ToString(),
                UserId = placeOrderRequest.UserId,
                Total = placeOrderRequest.Total,
                Items = placeOrderRequest.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList(),
                Timestamp = DateTime.UtcNow,
                PaymentId = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(orderPlaceEvent);

            await producer.ProduceAsync(kafkaOptions.Value.OrderPlacedTopic, new Message<Null, string>
            {
                Value = json
            }).ConfigureAwait(false);

            logger.LogInformation("Order placed event sent to Kafka");

            return Results.Ok();
        }
    }
}

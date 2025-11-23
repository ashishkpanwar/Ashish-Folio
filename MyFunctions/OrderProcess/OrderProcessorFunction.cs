using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Ashish_Backend_Folio.Data;// IMessageProcessingTracker, OrderDto, IOrderService
using System.Threading.Tasks;
using Ashish_Backend_Folio.Data.Dtos;
using Ashish_Backend_Folio.Data.Services.Interfaces;


namespace MyFunctions.Functions
{
    public class OrderProcessorFunction
    {
        private readonly IMessageProcessingTracker _tracker;
        private readonly IOrderService _orderService;
        private readonly AppDbContext _db;
        private readonly ILogger<OrderProcessorFunction> _logger;

        // maxDeliveryCount is configured on the subscription; use it for logic if needed
        private const int MaxDeliveryCountHint = 10;

        public OrderProcessorFunction(IMessageProcessingTracker tracker,
                                      IOrderService orderService,
                                      AppDbContext db,
                                      ILoggerFactory loggerFactory)
        {
            _tracker = tracker;
            _orderService = orderService;
            _db = db;
            _logger = loggerFactory.CreateLogger<OrderProcessorFunction>();
        }

        [Function("ProcessOrder")]
        public async Task RunAsync(
        [ServiceBusTrigger("ashish-folio-sb-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
            FunctionContext context)
        {
            var logger = context.GetLogger<OrderProcessorFunction>();
            var messageId = message.MessageId ?? Guid.NewGuid().ToString();
            logger.LogInformation("Received message. MessageId={MessageId} DeliveryCount={DeliveryCount}", messageId, message.DeliveryCount);

            // 1) Idempotency check
            if (await _tracker.HasBeenProcessedAsync(messageId))
            {
                logger.LogInformation("Message {MessageId} already processed — skipping.", messageId);
                return; // complete
            }

            // 2) Deserialize payload (use Utf8 JSON API)
            OrderDto? order;
            try
            {
                var bytes = message.Body.ToArray();
                order = JsonSerializer.Deserialize<OrderDto>(bytes);
                if (order == null)
                {
                    logger.LogWarning("Empty order payload. MessageId={MessageId}", messageId);
                    throw new InvalidOperationException("Payload deserialized to null");
                }
            }
            catch (Exception ex)
            {
                // Deserialization failure — send to DLQ after retries
                logger.LogError(ex, "Deserialization failed for message {MessageId}", messageId);
                throw; // let Functions/Service Bus retry and eventually DLQ
            }

            // 3) Business processing inside DB transaction + mark processed (atomic)
            try
            {
                await using var trx = await _db.Database.BeginTransactionAsync();

                // Ensure HandleOrderAsync is idempotent (by OrderId). If not, check DB for existing order before insert.
                await _orderService.HandleOrderAsync(order);

                // Mark message as processed (so duplicates are ignored). Persist inside same transaction.
                await _tracker.MarkProcessedAsync(messageId);

                await trx.CommitAsync();

                logger.LogInformation("Processed message {MessageId} successfully.", messageId);
            }
            catch (Exception ex)
            {
                // If we want special handling before letting it retry:
                logger.LogError(ex, "Processing failed for message {MessageId} (DeliveryCount={DeliveryCount})", messageId, message.DeliveryCount);

                // Example: if message.DeliveryCount is high we could log with higher severity or create an alert
                if (message.DeliveryCount >= MaxDeliveryCountHint - 1)
                    logger.LogWarning("Message {MessageId} is close to max delivery count.", messageId);

                // Re-throw to allow Service Bus retry / eventual DLQ
                throw;
            }
        }
    }


}

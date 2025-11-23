using Ashish_Backend_Folio.Data;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace MyFunctions.Functions
{
    public class DeadLetterProcessorFunction
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DeadLetterProcessorFunction> _logger;

        public DeadLetterProcessorFunction(AppDbContext db, ILoggerFactory loggerFactory)
        {
            _db = db;
            _logger = loggerFactory.CreateLogger<DeadLetterProcessorFunction>();
        }

        // Note: subscriptionName + "/$DeadLetterQueue" is how you trigger DLQ for a topic subscription
        [Function("ProcessDeadLetter")]
        public async Task RunAsync(
        [ServiceBusTrigger("orders-topic", "orders-subscription/$DeadLetterQueue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage deadMessage,
            FunctionContext context)
        {
            var logger = context.GetLogger<DeadLetterProcessorFunction>();
            var messageId = deadMessage.MessageId ?? Guid.NewGuid().ToString();

            logger.LogWarning("DLQ message received. MessageId={MessageId} DeadLetterReason={Reason}", messageId,
                deadMessage.ApplicationProperties.ContainsKey("DeadLetterReason") ? deadMessage.ApplicationProperties["DeadLetterReason"] : "unknown");

            // Try to capture context to help remediation
            var reason = deadMessage.ApplicationProperties.TryGetValue("DeadLetterReason", out var r) ? r?.ToString() : null;
            var description = deadMessage.ApplicationProperties.TryGetValue("DeadLetterErrorDescription", out var d) ? d?.ToString() : null;

            try
            {
                var bytes = deadMessage.Body.ToArray();
                var bodyText = System.Text.Encoding.UTF8.GetString(bytes);

                // Persist DLQ item to a table for manual inspection or attempt best-effort auto-repair
                // Example: log to DB table DeadLetterMessages (not shown) or store to blob storage
                logger.LogInformation("DLQ payload: {Payload}", bodyText);

                // Optionally: attempt automatic repair or reprocessing if safe
                // e.g., check schema issues, fix small problems, republish to topic via IEventPublisher
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed reading DLQ message {MessageId}", messageId);
                // do not throw — we want DLQ function to handle without creating recursive DLQ events
            }
        }
    }
}

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace Ashish_Backend_Folio.Messaging
{
    public class ServiceBusRawPublisher : IEventPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusRawPublisher> _logger;

        public ServiceBusRawPublisher(ServiceBusClient client, ILogger<ServiceBusRawPublisher> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<bool> PublishAsync(EventMessage message, CancellationToken ct = default)
        {
            var sender = _client.CreateSender(message.Destination);
            var sbMsg = new ServiceBusMessage(message.Payload)
            {
                ContentType = message.ContentType,
                MessageId = message.MessageId
            };

            foreach (var kv in message.Properties)
                sbMsg.ApplicationProperties[kv.Key] = kv.Value;

            try
            {
                await sender.SendMessageAsync(sbMsg, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ServiceBus send failed for {Destination} MessageId={MessageId}", message.Destination, message.MessageId);
                return false;
            }
        }

        public ValueTask DisposeAsync() => _client.DisposeAsync();
    }

}

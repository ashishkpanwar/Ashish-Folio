using Ashish_Backend_Folio.Services.Interface;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace Ashish_Backend_Folio.Services.Implementation
{
    public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
    {

        private readonly ServiceBusClient _client;

        public ServiceBusPublisher(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task PublishAsync<T>(T payload)
        {
            var sender = _client.CreateSender("ashish-folio-sb-queue");
            var json = JsonSerializer.Serialize(payload);
            var message = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            await sender.SendMessageAsync(message);
        }

        public async ValueTask DisposeAsync() => await _client.DisposeAsync();
    }
}

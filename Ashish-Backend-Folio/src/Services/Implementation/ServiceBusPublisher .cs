using Ashish_Backend_Folio.Services.Interface;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace Ashish_Backend_Folio.Services.Implementation
{
    public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
    {

        private readonly ServiceBusClient _client;

        public ServiceBusPublisher(IConfiguration config)
        {
            var conn = config["ServiceBus:ConnectionString"];

            if (!string.IsNullOrEmpty(conn))
                _client = new ServiceBusClient(conn);
            else
            {
                _client = new ServiceBusClient(
                    config["ServiceBus:Namespace"],
                    new DefaultAzureCredential()
                );
            }
        }

        public async Task PublishAsync<T>(string entityName, T payload)
        {
            var sender = _client.CreateSender(entityName);
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

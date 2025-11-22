using System.Text.Json;
using Ashish_Backend_Folio.Dtos.Request;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Ashish_Backend_Folio.Azure_Functions
{
    public class OrderConsumer
    {
        [FunctionName("OrderConsumer")]
        public async Task Run(
       [ServiceBusTrigger("orders-topic", "orders-subscription", Connection = "ServiceBusConnection")]
        string data,
       ILogger log)
        {
            var order = JsonSerializer.Deserialize<OrderRequest>(data);

            log.LogInformation("Received order {OrderId}", order?.OrderId);

            // TODO: process order, call APIs, update DB, etc.

            await Task.CompletedTask;
        }
    }
}

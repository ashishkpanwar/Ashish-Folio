using Ashish_Backend_Folio.Dtos.Request;
using Ashish_Backend_Folio.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using System.Text.Json;

namespace Ashish_Backend_Folio.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IEventPublisher _publisher;

        public OrdersController(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        // in OrdersController
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest dto)
        {
            // persist order to DB first (recommended)
            // enque event for downstream processing
            var payload = JsonSerializer.SerializeToUtf8Bytes(dto);
            var message = new EventMessage
            {
                Destination = "ashish-folio-sb-queue",
                Payload = payload,
                MessageId = dto.orderId, // use business id for dedupe
                Properties = new Dictionary<string, object> { ["eventType"] = "OrderCreated" }
            };
            var ok = await _publisher.PublishAsync(message);
            if (!ok) return StatusCode(StatusCodes.Status500InternalServerError, "Failed to publish event");
            return Accepted();
        }

    }

}

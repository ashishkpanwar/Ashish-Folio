using Ashish_Backend_Folio.Dtos.Request;
using Ashish_Backend_Folio.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;

namespace Ashish_Backend_Folio.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ServiceBusRawPublisher _publisher;

        public OrdersController(ServiceBusRawPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder()
        {
            await _publisher.PublishAsync(new());
            return Accepted();
        }
    }

}

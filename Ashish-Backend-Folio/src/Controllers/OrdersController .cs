using Ashish_Backend_Folio.Dtos.Request;
using Ashish_Backend_Folio.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Ashish_Backend_Folio.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IServiceBusPublisher _publisher;

        public OrdersController(IServiceBusPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderRequest dto)
        {
            await _publisher.PublishAsync("orders-topic", dto);
            return Accepted();
        }
    }

}

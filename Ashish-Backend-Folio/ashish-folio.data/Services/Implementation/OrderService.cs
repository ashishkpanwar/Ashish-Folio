using Ashish_Backend_Folio.data.Models;
using Ashish_Backend_Folio.Data;
using Ashish_Backend_Folio.Data.Dtos;
using Ashish_Backend_Folio.Data.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ashish_folio.data.Services.Implementation
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OrderService> _logger;

        public OrderService(AppDbContext db, ILogger<OrderService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task HandleOrderAsync(OrderDto dto, CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            // Check if order already exists (idempotency)
            var existing = await _db.Orders.AsNoTracking()
                                .FirstOrDefaultAsync(o => o.OrderId == dto.orderId.ToString(), ct);
            if (existing != null)
            {
                _logger.LogInformation("Order {OrderId} already exists — skipping insert.", dto.orderId);
                return;
            }

            // Insert new order
            var order = new Order
            {
                OrderId = dto.orderId.ToString(),
                CustomerId = (new Guid()).ToString(),
                Total = 20,
                CreatedAt =  DateTime.UtcNow 
            };

            _db.Orders.Add(order);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Unique index may race — handle like processed tracker
                _logger.LogWarning(ex, "Order insert conflict for {OrderId}; assuming already processed", dto.orderId);
            }

            // Optionally: publish downstream events, update other aggregates, etc.
        }
    }

}

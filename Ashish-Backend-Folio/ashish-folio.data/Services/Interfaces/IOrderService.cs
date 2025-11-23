using Ashish_Backend_Folio.Data.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ashish_Backend_Folio.Data.Services.Interfaces
{
    public interface IOrderService
    {
        /// <summary>Idempotent processing of an order. Must be safe to call multiple times for same OrderId.</summary>
        Task HandleOrderAsync(OrderDto dto, CancellationToken ct = default);
    }

}

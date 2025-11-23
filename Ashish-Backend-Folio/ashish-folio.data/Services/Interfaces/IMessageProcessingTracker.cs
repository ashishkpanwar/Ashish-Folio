using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ashish_Backend_Folio.Data.Services.Interfaces
{
    public interface IMessageProcessingTracker
    {
        Task<bool> HasBeenProcessedAsync(string messageId, CancellationToken ct = default);
        Task MarkProcessedAsync(string messageId, CancellationToken ct = default);
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ashish_Backend_Folio.data.Models;
using ashish_folio.data.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ashish_Backend_Folio.Data.Services.Implementation
{
    public class EfMessageProcessingTracker : IMessageProcessingTracker
    {
        private readonly AppDbContext _db;
        public EfMessageProcessingTracker(AppDbContext db) => _db = db;

        public async Task<bool> HasBeenProcessedAsync(string messageId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return false;
            return await _db.ProcessedMessages.AsNoTracking().AnyAsync(pm => pm.MessageId == messageId, ct);
        }

        public async Task MarkProcessedAsync(string messageId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return;

            // Insert and let unique index protect against races
            var pm = new ProcessedMessage { MessageId = messageId, ProcessedAt = DateTime.UtcNow };
            _db.ProcessedMessages.Add(pm);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // If unique constraint violated, treat as "already processed" and swallow
                // (Optional: inspect inner exception to confirm it's unique constraint)
                // Avoid throwing to keep processing idempotent-safe.
                // You might log at Debug level.
            }
        }
    }

}

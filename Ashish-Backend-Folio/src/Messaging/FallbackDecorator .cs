using Ashish_Backend_Folio.Data;
using Ashish_Backend_Folio.Models;
using System.Text.Json;

namespace Ashish_Backend_Folio.Messaging
{
    // Messaging/FallbackDecorator.cs
    public interface IFailedMessageStore
    {
        Task StoreAsync(EventMessage message, string failureReason, CancellationToken ct = default);
    }

    public class EfFailedMessageStore : IFailedMessageStore
    {
        private readonly AppDbContext _db;
        public EfFailedMessageStore(AppDbContext db) => _db = db;

        public async Task StoreAsync(EventMessage message, string failureReason, CancellationToken ct = default)
        {
            var row = new FailedOutboxMessage
            {
                Id = Guid.NewGuid(),
                Destination = message.Destination,
                MessageId = message.MessageId,
                ContentType = message.ContentType,
                Payload = message.Payload,
                PropertiesJson = JsonSerializer.Serialize(message.Properties),
                FailureReason = failureReason,
                CreatedAt = DateTime.UtcNow,
                Retries = 0
            };
            _db.FailedOutboxMessages.Add(row);
            await _db.SaveChangesAsync(ct);
        }
    }

    // Decorator
    public class FallbackDecorator : IEventPublisher
    {
        private readonly IEventPublisher _inner;
        private readonly IFailedMessageStore _store;
        private readonly ILogger<FallbackDecorator> _log;

        public FallbackDecorator(IEventPublisher inner, IFailedMessageStore store, ILogger<FallbackDecorator> log)
        {
            _inner = inner;
            _store = store;
            _log = log;
        }

        public async Task<bool> PublishAsync(EventMessage message, CancellationToken ct = default)
        {
            var ok = await _inner.PublishAsync(message, ct);
            if (ok) return true;

            // store for later replay
            try
            {
                await _store.StoreAsync(message, "PublishFailed", ct);
                _log.LogWarning("Stored failed message {MessageId} to fallback store", message.MessageId);
                return true; // indicate handled (queued for replay)
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to store failed message; giving up");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_inner != null) await _inner.DisposeAsync();
        }
    }

}

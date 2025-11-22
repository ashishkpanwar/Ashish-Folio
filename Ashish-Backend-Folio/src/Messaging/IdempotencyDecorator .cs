namespace Ashish_Backend_Folio.Messaging
{
    public class IdempotencyDecorator : IEventPublisher
    {
        private readonly IEventPublisher _inner;
        public IdempotencyDecorator(IEventPublisher inner) => _inner = inner;

        public async Task<bool> PublishAsync(EventMessage message, CancellationToken ct = default)
        {
            // ensure message id present
            if (string.IsNullOrEmpty(message.MessageId))
                message = message with { MessageId = Guid.NewGuid().ToString() };

            // If you want additional dedupe for providers that don't support it, implement a Redis set with add-if-not-exists here.

            return await _inner.PublishAsync(message, ct);
        }

        public async ValueTask DisposeAsync()
        {
            if (_inner != null) await _inner.DisposeAsync();
        }
    }

}

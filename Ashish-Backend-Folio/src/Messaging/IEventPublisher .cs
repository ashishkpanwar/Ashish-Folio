namespace Ashish_Backend_Folio.Messaging
{
    // Messaging/IEventPublisher.cs
    public interface IEventPublisher : IAsyncDisposable
    {
        /// <summary>
        /// Publish a message to a topic/queue. MessageId is used for idempotency/dedup.
        /// Returns true if published successfully (or queued to fallback store), false otherwise.
        /// </summary>
        Task<bool> PublishAsync(EventMessage message, CancellationToken ct = default);
    }

    public record EventMessage
    {
        public string Destination { get; init; } = null!; // topic or queue name
        public string MessageId { get; init; } = Guid.NewGuid().ToString();
        public string ContentType { get; init; } = "application/json";
        public byte[] Payload { get; init; } = Array.Empty<byte>();
        public IDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }

}

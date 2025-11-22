using Ashish_Backend_Folio.Messaging;


// Messaging/Policies/RetryDecorator.cs
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Ashish_Backend_Folio.Messaging.Policies
{
    public class RetryPublisherDecorator : IEventPublisher
    {
        private readonly IEventPublisher _inner;
        private readonly AsyncRetryPolicy<bool> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
        private readonly ILogger<RetryPublisherDecorator> _log;

        public RetryPublisherDecorator(IEventPublisher inner, ILogger<RetryPublisherDecorator> log)
        {
            _inner = inner;
            _log = log;

            // Retry on failure up to 3 times with exponential backoff
            _retryPolicy = Policy<bool>
                .HandleResult(res => res == false)
                .Or<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, retryCount, ctx) =>
                    {
                        _log.LogWarning("Publish retry {RetryCount} after {Delay}. Error: {Error}", retryCount, timespan, outcome.Exception?.Message ?? "false-result");
                    });

            // Circuit breaker for repeated failures
            _circuitBreaker = Policy
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5,
                    samplingDuration: TimeSpan.FromSeconds(10), // over 10 seconds
                    minimumThroughput: 5, 
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (ex, ts) => _log.LogWarning("Circuit open for {Duration} due to {Exception}", ts, ex.Message),
                    onReset: () => _log.LogInformation("Circuit reset"),
                    onHalfOpen: () => _log.LogInformation("Circuit half-open"));
        }

        public async Task<bool> PublishAsync(EventMessage message, CancellationToken ct = default)
        {
            // Combine policies
            return await _circuitBreaker.ExecuteAsync(() =>
                   _retryPolicy.ExecuteAsync(async ct2 =>
                   {
                       try
                       {
                           return await _inner.PublishAsync(message, ct2);
                       }
                       catch (Exception ex)
                       {
                           _log.LogError(ex, "Inner publisher threw");
                           throw;
                       }
                   }, ct)
            );
        }

        public async ValueTask DisposeAsync()
        {
            if (_inner != null) await _inner.DisposeAsync();
        }
    }

}

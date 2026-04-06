namespace ArchiForge.Core.Resilience;

/// <summary>
/// Thrown when a call is rejected because the circuit is open or a recovery probe is already in flight.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    /// <summary>UTC time after which the client may retry; null when unknown (e.g. probe in flight).</summary>
    public DateTimeOffset? RetryAfterUtc { get; }

    public CircuitBreakerOpenException(DateTimeOffset retryAfterUtc)
        : base(
            $"The upstream AI service is temporarily unavailable due to repeated failures. Retry after {retryAfterUtc:o}.")
    {
        RetryAfterUtc = retryAfterUtc;
    }

    public CircuitBreakerOpenException(string message)
        : base(message)
    {
    }
}

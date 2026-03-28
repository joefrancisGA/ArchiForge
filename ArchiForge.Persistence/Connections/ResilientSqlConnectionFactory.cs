using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Connections;

/// <summary>
/// Decorator over <see cref="ISqlConnectionFactory"/> that retries transient failures
/// with exponential backoff before surfacing the exception.
/// </summary>
/// <remarks>
/// Default policy: 3 retries with base delay 200 ms, doubling each attempt (200 → 400 → 800 ms).
/// Jitter (±25 %) prevents thundering-herd effects when many requests retry simultaneously.
/// Only exceptions identified as transient by <see cref="SqlTransientDetector"/> trigger retries.
/// </remarks>
public sealed class ResilientSqlConnectionFactory : ISqlConnectionFactory
{
    private readonly ISqlConnectionFactory _inner;
    private readonly ILogger<ResilientSqlConnectionFactory> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    public ResilientSqlConnectionFactory(
        ISqlConnectionFactory inner,
        ILogger<ResilientSqlConnectionFactory> logger,
        int maxRetries = 3,
        TimeSpan? baseDelay = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(200);
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        int attempt = 0;

        while (true)
        {
            try
            {
                return await _inner.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < _maxRetries && SqlTransientDetector.IsTransient(ex))
            {
                attempt++;
                TimeSpan delay = ComputeDelay(attempt);

                _logger.LogWarning(
                    ex,
                    "Transient SQL error on connection attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs} ms.",
                    attempt,
                    _maxRetries,
                    (int)delay.TotalMilliseconds);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Exponential backoff with ±25 % jitter: <c>baseDelay * 2^(attempt-1) * (0.75..1.25)</c>.
    /// </summary>
    internal TimeSpan ComputeDelay(int attempt)
    {
        double exponentialMs = _baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);

        // ThreadStatic Random avoids lock contention.
        double jitterFactor = 0.75 + (Random.Shared.NextDouble() * 0.5);

        return TimeSpan.FromMilliseconds(exponentialMs * jitterFactor);
    }
}

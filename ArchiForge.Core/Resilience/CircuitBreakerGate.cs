namespace ArchiForge.Core.Resilience;

/// <summary>
/// Thread-safe three-state circuit breaker (closed → open → half-open probe → closed).
/// </summary>
/// <remarks>
/// One probe at a time in half-open; concurrent callers receive <see cref="CircuitBreakerOpenException"/>
/// while a probe is in flight. User-initiated cancellation during a probe clears the probe slot without
/// counting as a failure so the next request can retry immediately.
/// </remarks>
public sealed class CircuitBreakerGate
{
    private readonly CircuitBreakerOptions _options;

    private readonly Func<DateTimeOffset> _utcNow;

    private readonly object _sync = new();

    private State _state = State.Closed;

    private int _consecutiveFailures;

    private DateTimeOffset _openUntilUtc;

    private bool _probeInFlight;

    /// <param name="options">Threshold and open duration.</param>
    /// <param name="utcNow">Optional clock for tests; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    public CircuitBreakerGate(CircuitBreakerOptions options, Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ApplyDefaults();
        _options = options;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Throws <see cref="CircuitBreakerOpenException"/> if the circuit rejects the call; otherwise returns
    /// so the caller may invoke the downstream operation (and then call <see cref="RecordSuccess"/> or
    /// <see cref="RecordFailure"/> / <see cref="RecordCallCancelled"/>).
    /// </summary>
    public void ThrowIfBroken()
    {
        lock (_sync)
        {
            if (_state == State.Closed)
                return;

            if (_state == State.Open)
            {
                if (_utcNow() < _openUntilUtc)
                    throw new CircuitBreakerOpenException(_openUntilUtc);

                if (_probeInFlight)
                    throw new CircuitBreakerOpenException(
                        "Upstream AI recovery probe in progress; retry shortly.");

                _state = State.HalfOpen;
                _probeInFlight = true;
                return;
            }

            // HalfOpen: only the probing thread holds the slot; others wait out.
            if (_probeInFlight)
                throw new CircuitBreakerOpenException(
                    "Upstream AI recovery probe in progress; retry shortly.");
        }
    }

    /// <summary>Call after a successful downstream invocation.</summary>
    public void RecordSuccess()
    {
        lock (_sync)
        {
            _consecutiveFailures = 0;
            _state = State.Closed;
            _probeInFlight = false;
        }
    }

    /// <summary>Call after a failed downstream invocation (counts toward opening the circuit).</summary>
    public void RecordFailure()
    {
        lock (_sync)
        {
            if (_state == State.HalfOpen)
            {
                _state = State.Open;
                _openUntilUtc = _utcNow().AddSeconds(_options.DurationOfBreakSeconds);
                _probeInFlight = false;
                _consecutiveFailures = _options.FailureThreshold;
                return;
            }

            _consecutiveFailures++;

            if (_consecutiveFailures >= _options.FailureThreshold)
            {
                _state = State.Open;
                _openUntilUtc = _utcNow().AddSeconds(_options.DurationOfBreakSeconds);
            }
        }
    }

    /// <summary>
    /// Call when the caller cancelled the operation so the probe slot is released without a failure tick.
    /// </summary>
    public void RecordCallCancelled()
    {
        lock (_sync)
        {
            if (_state != State.HalfOpen || !_probeInFlight)
                return;

            _probeInFlight = false;
            _state = State.Open;
            _openUntilUtc = _utcNow();
        }
    }

    private enum State
    {
        Closed,
        Open,
        HalfOpen
    }
}

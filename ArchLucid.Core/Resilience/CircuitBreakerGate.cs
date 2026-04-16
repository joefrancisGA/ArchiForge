using System.Diagnostics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Time;

using Microsoft.Extensions.Options;

namespace ArchLucid.Core.Resilience;

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
    private readonly string _gateName;

    private readonly CircuitBreakerOptions? _options;

    private readonly IOptionsMonitor<CircuitBreakerOptions>? _optionsMonitor;

    private readonly TimeProvider _timeProvider;

    private readonly Lock _sync = new();

    private State _state = State.Closed;

    private int _consecutiveFailures;

    private DateTimeOffset _openUntilUtc;

    private bool _probeInFlight;

    private DateTimeOffset? _lastStateChangeUtc;

    private readonly Action<CircuitBreakerAuditEntry>? _onAuditEntry;

    /// <param name="gateName">Stable low-cardinality label for metrics (e.g. keyed DI name).</param>
    /// <param name="options">Threshold and open duration.</param>
    /// <param name="timeProvider">Wall clock; defaults to <see cref="TimeProvider.System"/>.</param>
    /// <param name="onAuditEntry">Optional durable-audit hook (must not throw); invoked after OTel counters.</param>
    public CircuitBreakerGate(
        string gateName,
        CircuitBreakerOptions options,
        TimeProvider? timeProvider = null,
        Action<CircuitBreakerAuditEntry>? onAuditEntry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gateName);
        ArgumentNullException.ThrowIfNull(options);
        options.ApplyDefaults();
        _gateName = gateName;
        _options = options;
        _optionsMonitor = null;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _onAuditEntry = onAuditEntry;
    }

    /// <param name="optionsMonitor">Named options monitor; <see cref="IOptionsMonitor{TOptions}.Get"/> uses <paramref name="gateName"/>.</param>
    public CircuitBreakerGate(
        string gateName,
        IOptionsMonitor<CircuitBreakerOptions> optionsMonitor,
        TimeProvider? timeProvider = null,
        Action<CircuitBreakerAuditEntry>? onAuditEntry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gateName);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _gateName = gateName;
        _options = null;
        _optionsMonitor = optionsMonitor;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _onAuditEntry = onAuditEntry;
    }

    /// <summary>Compatibility constructor: wraps <paramref name="utcNow"/> in a <see cref="TimeProvider"/>.</summary>
    public CircuitBreakerGate(
        string gateName,
        CircuitBreakerOptions options,
        Func<DateTimeOffset>? utcNow,
        Action<CircuitBreakerAuditEntry>? onAuditEntry)
        : this(
            gateName,
            options,
            utcNow is null ? null : new DelegateTimeProvider(utcNow),
            onAuditEntry)
    {
    }

    /// <summary>Compatibility constructor: wraps <paramref name="utcNow"/> in a <see cref="TimeProvider"/>.</summary>
    public CircuitBreakerGate(
        string gateName,
        IOptionsMonitor<CircuitBreakerOptions> optionsMonitor,
        Func<DateTimeOffset>? utcNow,
        Action<CircuitBreakerAuditEntry>? onAuditEntry)
        : this(
            gateName,
            optionsMonitor,
            utcNow is null ? null : new DelegateTimeProvider(utcNow),
            onAuditEntry)
    {
    }

    /// <summary>Compatibility constructor: wraps <paramref name="utcNow"/> in a <see cref="TimeProvider"/>.</summary>
    public CircuitBreakerGate(string gateName, CircuitBreakerOptions options, Func<DateTimeOffset>? utcNow)
        : this(gateName, options, utcNow is null ? null : new DelegateTimeProvider(utcNow), onAuditEntry: null)
    {
    }

    /// <summary>Compatibility constructor: wraps <paramref name="utcNow"/> in a <see cref="TimeProvider"/>.</summary>
    public CircuitBreakerGate(string gateName, IOptionsMonitor<CircuitBreakerOptions> optionsMonitor, Func<DateTimeOffset>? utcNow)
        : this(gateName, optionsMonitor, utcNow is null ? null : new DelegateTimeProvider(utcNow), onAuditEntry: null)
    {
    }

    /// <summary>Stable low-cardinality gate label (e.g. keyed DI name).</summary>
    public string GateName => _gateName;

    /// <summary>Thread-safe snapshot of the internal state (<c>Closed</c>, <c>Open</c>, or <c>HalfOpen</c>).</summary>
    public string CurrentState
    {
        get
        {
            lock (_sync)
            {
                return _state.ToString();
            }
        }
    }

    /// <summary>Consecutive failure ticks in the closed state (or threshold after half-open failure); thread-safe snapshot.</summary>
    public int ConsecutiveFailureCount
    {
        get
        {
            lock (_sync)
            {
                return _consecutiveFailures;
            }
        }
    }

    /// <summary>Effective failure threshold from bound or monitored options.</summary>
    public int CurrentFailureThreshold
    {
        get
        {
            lock (_sync)
            {
                return ResolveOptions().FailureThreshold;
            }
        }
    }

    /// <summary>Effective break duration from bound or monitored options.</summary>
    public int CurrentDurationOfBreakSeconds
    {
        get
        {
            lock (_sync)
            {
                return ResolveOptions().DurationOfBreakSeconds;
            }
        }
    }

    /// <summary>UTC time of the last <c>Closed</c>↔<c>Open</c>↔<c>HalfOpen</c> transition; <see langword="null"/> until the first transition.</summary>
    public DateTimeOffset? LastStateChangeUtc
    {
        get
        {
            lock (_sync)
            {
                return _lastStateChangeUtc;
            }
        }
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
            {
                return;
            }

            if (_state == State.Open)
            {
                if (_timeProvider.GetUtcNow() < _openUntilUtc)
                {
                    EmitRejection();

                    throw new CircuitBreakerOpenException(_openUntilUtc);
                }

                if (_probeInFlight)
                {
                    EmitRejection();

                    throw new CircuitBreakerOpenException(
                        "Upstream AI recovery probe in progress; retry shortly.");
                }

                _state = State.HalfOpen;
                _probeInFlight = true;
                EmitStateTransition("Open", "HalfOpen");

                return;
            }

            // HalfOpen: only the probing thread holds the slot; others wait out.
            if (_probeInFlight)
            {
                EmitRejection();

                throw new CircuitBreakerOpenException(
                    "Upstream AI recovery probe in progress; retry shortly.");
            }
        }
    }

    /// <summary>Call after a successful downstream invocation.</summary>
    public void RecordSuccess()
    {
        lock (_sync)
        {
            bool wasHalfOpenProbe = _state == State.HalfOpen && _probeInFlight;

            if (wasHalfOpenProbe)
            {
                EmitProbeOutcome("success");
                EmitStateTransition("HalfOpen", "Closed");
            }

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
            CircuitBreakerOptions opts = ResolveOptions();

            if (_state == State.HalfOpen)
            {
                EmitProbeOutcome("failure");
                EmitStateTransition("HalfOpen", "Open");
                _state = State.Open;
                _openUntilUtc = _timeProvider.GetUtcNow().AddSeconds(opts.DurationOfBreakSeconds);
                _probeInFlight = false;
                _consecutiveFailures = opts.FailureThreshold;

                return;
            }

            _consecutiveFailures++;

            if (_consecutiveFailures < opts.FailureThreshold)
            {
                return;
            }

            EmitStateTransition("Closed", "Open");
            _state = State.Open;
            _openUntilUtc = _timeProvider.GetUtcNow().AddSeconds(opts.DurationOfBreakSeconds);
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
            {
                return;
            }

            EmitProbeOutcome("cancelled");
            EmitStateTransition("HalfOpen", "Open");
            _probeInFlight = false;
            _state = State.Open;
            _openUntilUtc = _timeProvider.GetUtcNow();
        }
    }

    private void EmitRejection()
    {
        TagList tags = new TagList { { "gate", _gateName } };
        ArchLucidInstrumentation.CircuitBreakerRejections.Add(1, tags);
        string state = _state.ToString();

        InvokeAuditEntry("Rejection", state, state, null);
    }

    private void EmitStateTransition(string fromState, string toState)
    {
        _lastStateChangeUtc = _timeProvider.GetUtcNow();

        TagList tags = new TagList
        {
            { "gate", _gateName },
            { "from_state", fromState },
            { "to_state", toState },
        };

        ArchLucidInstrumentation.CircuitBreakerStateTransitions.Add(1, tags);
        InvokeAuditEntry("StateTransition", fromState, toState, null);
    }

    private void EmitProbeOutcome(string outcome)
    {
        TagList tags = new TagList { { "gate", _gateName }, { "outcome", outcome } };
        ArchLucidInstrumentation.CircuitBreakerProbeOutcomes.Add(1, tags);
        string state = _state.ToString();

        InvokeAuditEntry("ProbeOutcome", state, state, outcome);
    }

    private void InvokeAuditEntry(string transitionType, string fromState, string toState, string? probeOutcome)
    {
        if (_onAuditEntry is null)
        {
            return;
        }

        try
        {
            _onAuditEntry.Invoke(
                new CircuitBreakerAuditEntry(
                    _gateName,
                    transitionType,
                    fromState,
                    toState,
                    probeOutcome,
                    _timeProvider.GetUtcNow()));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // Audit callbacks must never break the circuit breaker (non-fatal exceptions only).
        }
    }

    private CircuitBreakerOptions ResolveOptions()
    {
        if (_optionsMonitor is not null)
        {
            CircuitBreakerOptions resolved = _optionsMonitor.Get(_gateName);
            resolved.ApplyDefaults();

            return resolved;
        }

        return _options!;
    }

    private enum State
    {
        Closed,
        Open,
        HalfOpen
    }
}

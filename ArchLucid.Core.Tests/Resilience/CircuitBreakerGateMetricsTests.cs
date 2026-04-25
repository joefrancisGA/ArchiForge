using System.Diagnostics.Metrics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Resilience;
using ArchLucid.Core.Time;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Resilience;

/// <summary>Validates OTel counter emissions from <see cref="CircuitBreakerGate" /> (MeterListener).</summary>
[Trait("Suite", "Core")]
public sealed class CircuitBreakerGateMetricsTests
{
    [Fact]
    public void RecordFailure_at_threshold_emits_closed_to_open_transition()
    {
        _ = ArchLucidInstrumentation.CircuitBreakerStateTransitions;

        using MeasurementCapture capture = MeasurementCapture.Start();

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 10 };
        CircuitBreakerGate gate = new("test-gate", options);
        gate.RecordFailure();

        capture.Measures.Should().Contain(m =>
            m.Name == "archlucid_circuit_breaker_state_transitions_total"
            && m.Value == 1
            && m.Tags.Any(t =>
                t.Key == "gate" && string.Equals(t.Value as string, "test-gate", StringComparison.Ordinal))
            && m.Tags.Any(t =>
                t.Key == "from_state" && string.Equals(t.Value as string, "Closed", StringComparison.Ordinal))
            && m.Tags.Any(t =>
                t.Key == "to_state" && string.Equals(t.Value as string, "Open", StringComparison.Ordinal)));
    }

    [Fact]
    public void ThrowIfBroken_when_open_emits_rejection_metric()
    {
        _ = ArchLucidInstrumentation.CircuitBreakerRejections;

        using MeasurementCapture capture = MeasurementCapture.Start();

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 10 };
        CircuitBreakerGate gate = new("reject-gate", options);
        gate.RecordFailure();

        Action act = () => gate.ThrowIfBroken();

        act.Should().Throw<CircuitBreakerOpenException>();

        capture.Measures.Should().Contain(m =>
            m.Name == "archlucid_circuit_breaker_rejections_total"
            && m.Value == 1
            && m.Tags.Any(t =>
                t.Key == "gate" && string.Equals(t.Value as string, "reject-gate", StringComparison.Ordinal)));
    }

    [Fact]
    public void Half_open_probe_success_emits_probe_outcome_and_transition_to_closed()
    {
        _ = ArchLucidInstrumentation.CircuitBreakerProbeOutcomes;

        using MeasurementCapture capture = MeasurementCapture.Start();

        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 30 };
        CircuitBreakerGate gate = new("probe-gate", options, new DelegateTimeProvider(clock.ToFunc()));

        gate.RecordFailure();
        clock.Advance(TimeSpan.FromSeconds(31));
        gate.ThrowIfBroken();
        gate.RecordSuccess();

        capture.Measures.Should().Contain(m =>
            m.Name == "archlucid_circuit_breaker_probe_outcomes_total"
            && m.Value == 1
            && m.Tags.Any(t =>
                t.Key == "gate" && string.Equals(t.Value as string, "probe-gate", StringComparison.Ordinal))
            && m.Tags.Any(t =>
                t.Key == "outcome" && string.Equals(t.Value as string, "success", StringComparison.Ordinal)));

        capture.Measures.Should().Contain(m =>
            m.Name == "archlucid_circuit_breaker_state_transitions_total"
            && m.Tags.Any(t =>
                t.Key == "gate" && string.Equals(t.Value as string, "probe-gate", StringComparison.Ordinal))
            && m.Tags.Any(t =>
                t.Key == "from_state" && string.Equals(t.Value as string, "HalfOpen", StringComparison.Ordinal))
            && m.Tags.Any(t =>
                t.Key == "to_state" && string.Equals(t.Value as string, "Closed", StringComparison.Ordinal)));
    }

    private sealed class MutableUtcClock(DateTimeOffset start)
    {
        private DateTimeOffset _now = start;

        public void Advance(TimeSpan delta)
        {
            _now = _now.Add(delta);
        }

        public Func<DateTimeOffset> ToFunc()
        {
            return () => _now;
        }
    }

    private sealed class MeasurementCapture : IDisposable
    {
        private readonly MeterListener _listener = new();

        private readonly List<MeasurementRecord> _measures = [];

        private MeasurementCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<long>(OnMeasurementLong);
            _listener.Start();
        }

        public IReadOnlyList<MeasurementRecord> Measures => _measures;

        public void Dispose()
        {
            _listener.Dispose();
        }

        public static MeasurementCapture Start()
        {
            return new MeasurementCapture();
        }

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name is not (
                "archlucid_circuit_breaker_state_transitions_total"
                or "archlucid_circuit_breaker_rejections_total"
                or "archlucid_circuit_breaker_probe_outcomes_total"))
            {
                return;
            }

            meterListener.EnableMeasurementEvents(instrument);
        }

        private void OnMeasurementLong(
            Instrument instrument,
            long measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;
            List<KeyValuePair<string, object?>> tagList = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagList.Add(tag);
            }

            _measures.Add(new MeasurementRecord(instrument.Name, measurement, tagList));
        }
    }

    private sealed record MeasurementRecord(string Name, long Value, List<KeyValuePair<string, object?>> Tags);
}

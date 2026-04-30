using System.Diagnostics.Metrics;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Core.Tests.Audit;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DurableAuditLogRetryTests
{
    [Fact]
    public async Task TryLogAsync_succeeds_on_first_attempt_without_delay()
    {
        int calls = 0;

        await DurableAuditLogRetry.TryLogAsync(
            _ =>
            {
                calls++;

                return Task.CompletedTask;
            },
            NullLogger.Instance,
            "test-op",
            CancellationToken.None);

        calls.Should().Be(1);
    }

    [Fact]
    public async Task TryLogAsync_retries_then_succeeds()
    {
        int calls = 0;

        await DurableAuditLogRetry.TryLogAsync(
            _ =>
            {
                calls++;

                return calls < 2 ? throw new InvalidOperationException("transient") : Task.CompletedTask;
            },
            NullLogger.Instance,
            "test-op",
            CancellationToken.None);

        calls.Should().Be(2);
    }

    [Fact]
    public async Task TryLogAsync_suppresses_after_max_attempts()
    {
        int calls = 0;

        await DurableAuditLogRetry.TryLogAsync(
            _ =>
            {
                calls++;

                throw new InvalidOperationException("fail");
            },
            NullLogger.Instance,
            "test-op",
            CancellationToken.None,
            2);

        calls.Should().Be(2);
    }

    [Fact]
    public async Task TryLogAsync_propagates_operation_canceled_from_write()
    {
        Func<Task> act = () => DurableAuditLogRetry.TryLogAsync(
            _ => throw new OperationCanceledException(),
            NullLogger.Instance,
            "test-op",
            CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TryLogAsync_increments_audit_write_failures_total_when_abandoned_with_metric_event_type()
    {
        _ = ArchLucidInstrumentation.AuditWriteFailuresTotal;

        using AuditWriteFailureCapture capture = AuditWriteFailureCapture.Start();

        await DurableAuditLogRetry.TryLogAsync(
            _ => throw new InvalidOperationException("fail"),
            NullLogger.Instance,
            "test-op",
            CancellationToken.None,
            2,
            "Run.RetryRequested");

        capture.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_audit_write_failures_total"
            && m.Value == 1
            && m.Tags.Any(t => t.Key == "event_type" && (string?)t.Value == "Run.RetryRequested"));
    }

    [Fact]
    public async Task TryLogAsync_does_not_increment_counter_on_success_when_metric_event_type_set()
    {
        _ = ArchLucidInstrumentation.AuditWriteFailuresTotal;

        using AuditWriteFailureCapture capture = AuditWriteFailureCapture.Start();

        await DurableAuditLogRetry.TryLogAsync(
            _ => Task.CompletedTask,
            NullLogger.Instance,
            "test-op",
            CancellationToken.None,
            auditEventTypeForMetrics: "Run.RetryRequested");

        capture.LongMeasures.Should().BeEmpty();
    }

    [Fact]
    public async Task TryLogAsync_does_not_increment_counter_when_abandoned_without_metric_event_type()
    {
        _ = ArchLucidInstrumentation.AuditWriteFailuresTotal;

        using AuditWriteFailureCapture capture = AuditWriteFailureCapture.Start();

        await DurableAuditLogRetry.TryLogAsync(
            _ => throw new InvalidOperationException("fail"),
            NullLogger.Instance,
            "test-op",
            CancellationToken.None,
            2);

        capture.LongMeasures.Should().BeEmpty();
    }

    private sealed class AuditWriteFailureCapture : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<LongMeasurementRecord> _longMeasures = [];

        private AuditWriteFailureCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<long>(OnLong);
            _listener.Start();
        }

        public IReadOnlyList<LongMeasurementRecord> LongMeasures => _longMeasures;

        public void Dispose() => _listener.Dispose();

        public static AuditWriteFailureCapture Start() => new();

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
                return;

            if (instrument.Name == "archlucid_audit_write_failures_total")
                meterListener.EnableMeasurementEvents(instrument);
        }

        private void OnLong(
            Instrument instrument,
            long measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;
            _longMeasures.Add(new LongMeasurementRecord(instrument.Name, measurement, ToList(tags)));
        }

        private static List<KeyValuePair<string, object?>> ToList(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            List<KeyValuePair<string, object?>> list = [];

            foreach (KeyValuePair<string, object?> t in tags)
                list.Add(t);

            return list;
        }
    }

    private readonly record struct LongMeasurementRecord(
        string Name,
        long Value,
        IReadOnlyList<KeyValuePair<string, object?>> Tags);
}

using System.Diagnostics.Metrics;

using ArchLucid.Application.Telemetry;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Telemetry;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Tests.Telemetry;

/// <summary>
///     Improvement 12 â€” verifies the privacy-sensitive flag toggles between aggregated emission
///     (no <c>tenant_id</c> tag, no SQL row) and per-tenant emission (tag + row). The default of
///     <c>FirstTenantFunnelOptions.PerTenantEmission</c> is <c>false</c> per pending question 40 and
///     <c>docs/security/PRIVACY_NOTE.md</c> Â§3.A.
/// </summary>
public sealed class FirstTenantFunnelEmitterTests
{
    private const string FunnelCounterName = "archlucid_first_tenant_funnel_events_total";

    [SkippableFact]
    public async Task Emit_DefaultFlag_RecordsAggregatedCounterWithoutTenantTagAndDoesNotPersistRow()
    {
        FirstTenantFunnelOptions options = new() { PerTenantEmission = false };
        RecordingFirstTenantFunnelEventStore store = new();
        FirstTenantFunnelEmitter emitter = CreateEmitter(options, store);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        const string distinctEvent = FirstTenantFunnelEventNames.Signup;

        using FunnelCounterRecorder recorder = new();
        await emitter.EmitAsync(distinctEvent, tenantId);

        IReadOnlyList<RecordedMeasurement> mine = recorder.MeasurementsForEvent(distinctEvent);
        mine.Should().NotBeEmpty();
        mine.Should().OnlyContain(m => !m.Tags.ContainsKey("tenant_id"),
            "default emission must not correlate to a tenant");

        store.Appended.Should().BeEmpty("no per-tenant rows are written when the flag is off");
    }

    [SkippableFact]
    public async Task Emit_FlagOn_RecordsTenantTagAndPersistsRow()
    {
        FirstTenantFunnelOptions options = new() { PerTenantEmission = true };
        RecordingFirstTenantFunnelEventStore store = new();
        DateTime now = new(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc);
        FakeTimeProvider clock = new(now);
        FirstTenantFunnelEmitter emitter = CreateEmitter(options, store, clock);

        Guid tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        const string distinctEvent = FirstTenantFunnelEventNames.FirstFindingViewed;

        using FunnelCounterRecorder recorder = new();
        await emitter.EmitAsync(distinctEvent, tenantId);

        IReadOnlyList<RecordedMeasurement> mine = recorder.MeasurementsForEvent(distinctEvent);
        mine.Should().Contain(m =>
            m.Tags.ContainsKey("tenant_id")
            && m.Tags["tenant_id"] == tenantId.ToString("D"));

        store.Appended.Should().ContainSingle();
        store.Appended[0].EventName.Should().Be(distinctEvent);
        store.Appended[0].TenantId.Should().Be(tenantId);
        store.Appended[0].OccurredUtc.Should().Be(now);
    }

    [SkippableFact]
    public async Task Emit_FlagOnButStoreFails_StillRecordsAggregatedCounterAndDoesNotThrow()
    {
        FirstTenantFunnelOptions options = new() { PerTenantEmission = true };
        ThrowingFirstTenantFunnelEventStore store = new();
        FirstTenantFunnelEmitter emitter = CreateEmitter(options, store);

        Guid tenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        const string distinctEvent = FirstTenantFunnelEventNames.FirstRunCommitted;

        using FunnelCounterRecorder recorder = new();
        Func<Task> act = () => emitter.EmitAsync(distinctEvent, tenantId);

        await act.Should().NotThrowAsync("the emitter is fire-and-forget so SQL faults must not break callers");
        recorder.MeasurementsForEvent(distinctEvent).Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task Emit_UnknownEventName_Throws()
    {
        FirstTenantFunnelEmitter emitter = CreateEmitter(new FirstTenantFunnelOptions { PerTenantEmission = false });

        Func<Task> act = () => emitter.EmitAsync("not-a-funnel-event", Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [SkippableFact]
    public async Task Emit_FlagOnAndCancelled_PropagatesCancellation()
    {
        FirstTenantFunnelOptions options = new() { PerTenantEmission = true };
        CancellingFirstTenantFunnelEventStore store = new();
        FirstTenantFunnelEmitter emitter = CreateEmitter(options, store);

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = () => emitter.EmitAsync(
            FirstTenantFunnelEventNames.ThirtyMinuteMilestone,
            Guid.NewGuid(),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static FirstTenantFunnelEmitter CreateEmitter(
        FirstTenantFunnelOptions options,
        IFirstTenantFunnelEventStore? store = null,
        TimeProvider? timeProvider = null) =>
        new(
            new StaticOptionsMonitor<FirstTenantFunnelOptions>(options),
            store ?? new RecordingFirstTenantFunnelEventStore(),
            timeProvider ?? TimeProvider.System,
            NullLogger<FirstTenantFunnelEmitter>.Instance);

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue
        {
            get;
        } = value;

        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed record AppendedEvent(string EventName, Guid TenantId, DateTime OccurredUtc);

    private sealed class RecordingFirstTenantFunnelEventStore : IFirstTenantFunnelEventStore
    {
        public List<AppendedEvent> Appended
        {
            get;
        } = [];

        public Task AppendAsync(string eventName, Guid tenantId, DateTime occurredUtc, CancellationToken ct)
        {
            Appended.Add(new AppendedEvent(eventName, tenantId, occurredUtc));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingFirstTenantFunnelEventStore : IFirstTenantFunnelEventStore
    {
        public Task AppendAsync(string eventName, Guid tenantId, DateTime occurredUtc, CancellationToken ct) =>
            throw new InvalidOperationException("simulated SQL failure");
    }

    private sealed class CancellingFirstTenantFunnelEventStore : IFirstTenantFunnelEventStore
    {
        public Task AppendAsync(string eventName, Guid tenantId, DateTime occurredUtc, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class FunnelCounterRecorder : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly List<RecordedMeasurement> _measurements = [];
        private readonly Lock _gate = new();

        public FunnelCounterRecorder()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == ArchLucidInstrumentation.MeterName
                        && instrument.Name == FunnelCounterName)
                        listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
            {
                Dictionary<string, string> snap = new(StringComparer.Ordinal);

                foreach (KeyValuePair<string, object?> tag in tags)

                    snap[tag.Key] = tag.Value?.ToString() ?? string.Empty;

                lock (_gate)

                    _measurements.Add(new RecordedMeasurement(measurement, snap));
            });

            _listener.Start();
        }

        public IReadOnlyList<RecordedMeasurement> MeasurementsForEvent(string eventName)
        {
            lock (_gate)

                return _measurements
                    .Where(m => m.Tags.TryGetValue("event", out string? v) && v == eventName)
                    .ToList();
        }

        public void Dispose() => _listener.Dispose();
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private sealed record RecordedMeasurement(long Value, IReadOnlyDictionary<string, string> Tags);
}

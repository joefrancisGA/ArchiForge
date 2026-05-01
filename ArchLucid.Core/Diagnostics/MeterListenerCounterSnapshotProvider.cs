using System.Diagnostics.Metrics;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Subscribes to the <c>ArchLucid</c> <see cref="Meter" /> at construction and accumulates measurements for the
///     counters used by the <c>/why-archlucid</c> proof page and operator diagnostics
///     (<c>archlucid_runs_created_total</c>, <c>archlucid_findings_produced_total</c> by <c>severity</c>,
///     <c>archlucid_operator_task_success_total</c> by <c>task</c>).
/// </summary>
/// <remarks>
///     Designed to be registered as a <c>Singleton</c> so the listener stays alive for the host's lifetime; counts are
///     process-life cumulative and reset when the API host restarts. Lightweight by design — the snapshot endpoint
///     must remain safe for unauthenticated load on a Core Pilot demo route.
/// </remarks>
public sealed class MeterListenerCounterSnapshotProvider : IInstrumentationCounterSnapshotProvider, IDisposable
{
    private const string RunsCreatedInstrumentName = "archlucid_runs_created_total";
    private const string FindingsProducedInstrumentName = "archlucid_findings_produced_total";
    private const string OperatorTaskSuccessInstrumentName = "archlucid_operator_task_success_total";
    private const string SeverityTag = "severity";
    private const string TaskTag = "task";
    private const string UnknownSeverity = "unknown";
    private const string UnknownTask = "unknown";
    private readonly Dictionary<string, long> _findingsBySeverity = new(StringComparer.Ordinal);

    private readonly Lock _gate = new();
    private readonly MeterListener _listener;
    private readonly Dictionary<string, long> _operatorTaskSuccess = new(StringComparer.Ordinal);
    private long _runsCreated;

    public MeterListenerCounterSnapshotProvider()
    {
        _listener = new MeterListener { InstrumentPublished = OnInstrumentPublished };
        _listener.SetMeasurementEventCallback<long>(OnLongMeasurement);
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    /// <inheritdoc />
    public InstrumentationCounterSnapshot GetSnapshot()
    {
        lock (_gate)

            return new InstrumentationCounterSnapshot
            {
                RunsCreatedTotal = _runsCreated,
                FindingsProducedBySeverity =
                    new Dictionary<string, long>(_findingsBySeverity, StringComparer.Ordinal),
                OperatorTaskSuccessByTask =
                    new Dictionary<string, long>(_operatorTaskSuccess, StringComparer.Ordinal)
            };
    }

    private static bool IsTrackedInstrument(Instrument instrument)
    {
        if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            return false;

        return instrument.Name is RunsCreatedInstrumentName or FindingsProducedInstrumentName
            or OperatorTaskSuccessInstrumentName;
    }

    private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
    {
        if (!IsTrackedInstrument(instrument))
            return;

        listener.EnableMeasurementEvents(instrument);
    }

    private void OnLongMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        if (instrument.Name == RunsCreatedInstrumentName)
        {
            lock (_gate)

                _runsCreated += measurement;

            return;
        }

        if (instrument.Name == FindingsProducedInstrumentName)
        {
            string severity = ResolveSeverityTag(tags);

            lock (_gate)
            {
                _findingsBySeverity.TryGetValue(severity, out long current);
                _findingsBySeverity[severity] = current + measurement;
            }

            return;
        }

        if (instrument.Name != OperatorTaskSuccessInstrumentName)
            return;

        {
            string task = ResolveTaskTag(tags);

            lock (_gate)
            {
                _operatorTaskSuccess.TryGetValue(task, out long current);
                _operatorTaskSuccess[task] = current + measurement;
            }
        }
    }

    private static string ResolveSeverityTag(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            if (!string.Equals(tag.Key, SeverityTag, StringComparison.Ordinal))
                continue;
            if (tag.Value is string s && !string.IsNullOrWhiteSpace(s))
                return s;
            if (tag.Value is { } o)
                return o.ToString() ?? UnknownSeverity;
        }

        return UnknownSeverity;
    }

    private static string ResolveTaskTag(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            if (!string.Equals(tag.Key, TaskTag, StringComparison.Ordinal))
                continue;
            if (tag.Value is string s && !string.IsNullOrWhiteSpace(s))
                return s;
            if (tag.Value is { } o)
                return o.ToString() ?? UnknownTask;
        }

        return UnknownTask;
    }
}

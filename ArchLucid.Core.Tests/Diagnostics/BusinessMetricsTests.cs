using System.Diagnostics;
using System.Diagnostics.Metrics;

using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Diagnostics;

/// <summary>MeterListener coverage for business KPI instruments on <see cref="ArchLucidInstrumentation" />.</summary>
[Trait("Suite", "Core")]
public sealed class BusinessMetricsTests
{
    [Fact]
    public void RunsCreatedTotal_increment_emits_measurement()
    {
        _ = ArchLucidInstrumentation.RunsCreatedTotal;

        using BusinessMeasurementCapture capture = BusinessMeasurementCapture.Start();

        ArchLucidInstrumentation.RunsCreatedTotal.Add(1);

        capture.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_runs_created_total" && m.Value == 1 && m.Tags.Count == 0);
    }

    [Fact]
    public void FindingsProducedTotal_increment_with_severity_tag_emits_measurement()
    {
        _ = ArchLucidInstrumentation.FindingsProducedTotal;

        using BusinessMeasurementCapture capture = BusinessMeasurementCapture.Start();

        TagList tags = new() { { "severity", "Critical" } };

        ArchLucidInstrumentation.FindingsProducedTotal.Add(2, tags);

        capture.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_findings_produced_total"
            && m.Value == 2
            && m.Tags.Any(t =>
                t.Key == "severity" && string.Equals(t.Value as string, "Critical", StringComparison.Ordinal)));
    }

    [Fact]
    public void LlmCallsPerRun_record_emits_histogram_measurement()
    {
        _ = ArchLucidInstrumentation.LlmCallsPerRun;

        using BusinessMeasurementCapture capture = BusinessMeasurementCapture.Start();

        ArchLucidInstrumentation.LlmCallsPerRun.Record(7);

        capture.IntMeasures.Should().Contain(m =>
            m.Name == "archlucid_llm_calls_per_run" && m.Value == 7 && m.Tags.Count == 0);
    }

    [Fact]
    public void ExplanationCacheHits_and_Misses_increment_emit_measurements()
    {
        _ = ArchLucidInstrumentation.ExplanationCacheHits;
        _ = ArchLucidInstrumentation.ExplanationCacheMisses;

        using BusinessMeasurementCapture capture = BusinessMeasurementCapture.Start();

        ArchLucidInstrumentation.ExplanationCacheHits.Add(1);
        ArchLucidInstrumentation.ExplanationCacheMisses.Add(1);

        capture.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_explanation_cache_hits_total" && m.Value == 1);

        capture.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_explanation_cache_misses_total" && m.Value == 1);
    }

    [Fact]
    public void PipelineTimeoutsTotal_increment_emits_measurement()
    {
        _ = ArchLucidInstrumentation.PipelineTimeoutsTotal;

        using BusinessMeasurementCapture capture = BusinessMeasurementCapture.Start();

        ArchLucidInstrumentation.PipelineTimeoutsTotal.Add(1);

        capture.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_authority_pipeline_timeouts_total" && m.Value == 1 && m.Tags.Count == 0);
    }

    private sealed class BusinessMeasurementCapture : IDisposable
    {
        private static readonly string[] LongInstrumentNames =
        [
            "archlucid_runs_created_total",
            "archlucid_findings_produced_total",
            "archlucid_explanation_cache_hits_total",
            "archlucid_explanation_cache_misses_total",
            "archlucid_authority_pipeline_timeouts_total"
        ];

        private readonly List<IntMeasurementRecord> _intMeasures = [];

        private readonly MeterListener _listener = new();

        private readonly List<LongMeasurementRecord> _longMeasures = [];

        private BusinessMeasurementCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<long>(OnMeasurementLong);
            _listener.SetMeasurementEventCallback<int>(OnMeasurementInt);
            _listener.Start();
        }

        public IReadOnlyList<LongMeasurementRecord> LongMeasures => _longMeasures;

        public IReadOnlyList<IntMeasurementRecord> IntMeasures => _intMeasures;

        public void Dispose()
        {
            _listener.Dispose();
        }

        public static BusinessMeasurementCapture Start()
        {
            return new BusinessMeasurementCapture();
        }

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name == "archlucid_llm_calls_per_run")
            {
                meterListener.EnableMeasurementEvents(instrument);

                return;
            }

            if (LongInstrumentNames.Contains(instrument.Name))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        }

        private void OnMeasurementLong(
            Instrument instrument,
            long measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;

            _longMeasures.Add(new LongMeasurementRecord(instrument.Name, measurement, ToTagList(tags)));
        }

        private void OnMeasurementInt(
            Instrument instrument,
            int measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;

            _intMeasures.Add(new IntMeasurementRecord(instrument.Name, measurement, ToTagList(tags)));
        }

        private static List<KeyValuePair<string, object?>> ToTagList(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            List<KeyValuePair<string, object?>> list = [];

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                list.Add(tag);
            }

            return list;
        }
    }

    private sealed record LongMeasurementRecord(string Name, long Value, List<KeyValuePair<string, object?>> Tags);

    private sealed record IntMeasurementRecord(string Name, int Value, List<KeyValuePair<string, object?>> Tags);
}

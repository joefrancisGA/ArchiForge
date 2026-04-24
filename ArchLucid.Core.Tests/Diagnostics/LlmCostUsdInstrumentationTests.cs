using System.Diagnostics.Metrics;

using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
public sealed class LlmCostUsdInstrumentationTests
{
    [Fact]
    public void RecordLlmCostUsd_adds_to_counter_with_tenant_tag()
    {
        _ = ArchLucidInstrumentation.LlmCostUsdTotal;

        using DoubleMeasurementCapture capture = DoubleMeasurementCapture.Start();

        ArchLucidInstrumentation.RecordLlmCostUsd(0.0125m, "tenant-a");

        capture.DoubleMeasures.Should().Contain(m =>
            m.Name == "archlucid_llm_cost_usd_total"
            && Math.Abs(m.Value - 0.0125) < 1e-9
            && m.Tags.Any(t => t.Key == "tenant" && (string?)t.Value == "tenant-a"));
    }

    [Fact]
    public void RecordLlmCostUsd_uses_unknown_when_label_blank()
    {
        _ = ArchLucidInstrumentation.LlmCostUsdTotal;

        using DoubleMeasurementCapture capture = DoubleMeasurementCapture.Start();

        ArchLucidInstrumentation.RecordLlmCostUsd(0.01m, "  ");

        capture.DoubleMeasures.Should().Contain(m =>
            m.Tags.Any(t => t.Key == "tenant" && (string?)t.Value == "unknown"));
    }

    [Fact]
    public void RecordLlmCostUsd_skips_non_positive()
    {
        _ = ArchLucidInstrumentation.LlmCostUsdTotal;

        using DoubleMeasurementCapture capture = DoubleMeasurementCapture.Start();

        ArchLucidInstrumentation.RecordLlmCostUsd(0m, "t1");
        ArchLucidInstrumentation.RecordLlmCostUsd(-1m, "t1");

        capture.DoubleMeasures.Should().BeEmpty();
    }

    private sealed class DoubleMeasurementCapture : IDisposable
    {
        private readonly List<DoubleMeasurementRecord> _doubleMeasures = [];
        private readonly MeterListener _listener = new();

        private DoubleMeasurementCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<double>(OnMeasurementDouble);
            _listener.Start();
        }

        public IReadOnlyList<DoubleMeasurementRecord> DoubleMeasures => _doubleMeasures;

        public void Dispose()
        {
            _listener.Dispose();
        }

        public static DoubleMeasurementCapture Start()
        {
            return new DoubleMeasurementCapture();
        }

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name == "archlucid_llm_cost_usd_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        }

        private void OnMeasurementDouble(
            Instrument instrument,
            double measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;

            _doubleMeasures.Add(new DoubleMeasurementRecord(instrument.Name, measurement, ToTagList(tags)));
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

    private sealed record DoubleMeasurementRecord(string Name, double Value, List<KeyValuePair<string, object?>> Tags);
}

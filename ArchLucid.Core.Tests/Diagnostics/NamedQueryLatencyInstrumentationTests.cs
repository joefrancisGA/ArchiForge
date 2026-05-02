using System.Diagnostics.Metrics;

using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
public sealed class NamedQueryLatencyInstrumentationTests
{
    [Fact]
    public void RecordNamedQueryLatencyMilliseconds_emits_histogram_with_query_name_tag()
    {
        _ = ArchLucidInstrumentation.QueryNamedLatencyMilliseconds;

        using NamedQueryCapture cap = NamedQueryCapture.Start();

        ArchLucidInstrumentation.RecordNamedQueryLatencyMilliseconds("GetGoldenManifestById", 12.5);

        DoubleMeasurementRecord? hit = cap.DoubleMeasures.FirstOrDefault(m => m.Name == "archlucid_query_p95_ms");
        hit.Should().NotBeNull();
        hit!.Value.Should().BeApproximately(12.5, 0.001);
        hit.Tags.Should().Contain(t => t.Key == "query_name" && (string?)t.Value == "GetGoldenManifestById");
    }

    private sealed class NamedQueryCapture : IDisposable
    {
        private readonly List<DoubleMeasurementRecord> _doubleMeasures = [];

        private readonly MeterListener _listener = new();

        private NamedQueryCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<double>(OnDouble);
            _listener.Start();
        }

        public IReadOnlyList<DoubleMeasurementRecord> DoubleMeasures => _doubleMeasures;

        public void Dispose()
        {
            _listener.Dispose();
        }

        public static NamedQueryCapture Start() => new();

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
                return;

            if (instrument.Name == "archlucid_query_p95_ms")
                meterListener.EnableMeasurementEvents(instrument);
        }

        private void OnDouble(
            Instrument instrument,
            double measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;
            _doubleMeasures.Add(new DoubleMeasurementRecord(instrument.Name, measurement, ToList(tags)));
        }

        private static List<KeyValuePair<string, object?>> ToList(ReadOnlySpan<KeyValuePair<string, object?>> tags)
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

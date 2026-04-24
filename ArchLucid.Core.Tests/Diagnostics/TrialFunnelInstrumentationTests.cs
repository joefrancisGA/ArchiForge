using System.Diagnostics.Metrics;

using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Diagnostics;

[Trait("Suite", "Core")]
public sealed class TrialFunnelInstrumentationTests
{
    [Fact]
    public void TrialSignupsTotal_add_with_tags_emits_measurement()
    {
        _ = ArchLucidInstrumentation.TrialSignupsTotal;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        ArchLucidInstrumentation.RecordTrialSignup("self_service", "trial_provisioned");

        c.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_trial_signups_total"
            && m.Value == 1
            && m.Tags.Any(t => t.Key == "source" && (string?)t.Value == "self_service"));
    }

    [Fact]
    public void TrialSignupFailuresTotal_add_emits_measurement()
    {
        _ = ArchLucidInstrumentation.TrialSignupFailuresTotal;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        ArchLucidInstrumentation.RecordTrialSignupFailure("validation", "ArgumentException");

        c.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid_trial_signup_failures_total" && m.Value == 1);
    }

    [Fact]
    public void TrialFirstRunSeconds_record_emits_histogram()
    {
        _ = ArchLucidInstrumentation.TrialFirstRunSeconds;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        ArchLucidInstrumentation.RecordTrialFirstRunLatencySeconds(42);

        c.DoubleMeasures.Should().Contain(m =>
            m.Name == "archlucid_trial_first_run_seconds" && Math.Abs(m.Value - 42) < 0.001);
    }

    [Fact]
    public void TrialRunsUsedRatio_record_emits_histogram()
    {
        _ = ArchLucidInstrumentation.TrialRunsUsedRatio;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        ArchLucidInstrumentation.RecordTrialRunsUsedRatio(0.4);

        c.DoubleMeasures.Should()
            .Contain(m => m.Name == "archlucid_trial_runs_used_ratio" && Math.Abs(m.Value - 0.4) < 0.001);
    }

    [Fact]
    public void TrialConversionTotal_add_emits_measurement()
    {
        _ = ArchLucidInstrumentation.TrialConversionTotal;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        ArchLucidInstrumentation.RecordTrialConversion("Active", "Standard");

        c.LongMeasures.Should().Contain(m => m.Name == "archlucid_trial_conversion_total" && m.Value == 1);
    }

    [Fact]
    public void TrialExpirationsTotal_add_emits_measurement()
    {
        _ = ArchLucidInstrumentation.TrialExpirationsTotal;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        ArchLucidInstrumentation.RecordTrialExpiration("Active->Expired");

        c.LongMeasures.Should().Contain(m => m.Name == "archlucid_trial_expirations_total" && m.Value == 1);
    }

    [Fact]
    public void SponsorBannerFirstCommitBadgeRenderedTotal_add_emits_measurement()
    {
        _ = ArchLucidInstrumentation.SponsorBannerFirstCommitBadgeRenderedTotal;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        Guid tenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        ArchLucidInstrumentation.RecordSponsorBannerFirstCommitBadgeRendered(tenantId, "1-3");

        c.LongMeasures.Should().Contain(m =>
            m.Name == "archlucid.ui.sponsor_banner.first_commit_badge_rendered"
            && m.Value == 1
            && m.Tags.Any(t => t.Key == "tenant_id" && (string?)t.Value == tenantId.ToString("D"))
            && m.Tags.Any(t => t.Key == "days_since_first_commit_bucket" && (string?)t.Value == "1-3"));
    }

    [Fact]
    public void BillingCheckoutsTotal_add_emits_measurement()
    {
        _ = ArchLucidInstrumentation.BillingCheckoutsTotal;

        using TrialFunnelCapture c = TrialFunnelCapture.Start();

        ArchLucidInstrumentation.RecordBillingCheckout("Noop", "Team", "session_created");

        c.LongMeasures.Should().Contain(m => m.Name == "archlucid_billing_checkouts_total" && m.Value == 1);
    }

    [Fact]
    public void TrialActiveTenants_observable_gauge_reads_published_count()
    {
        ArchLucidInstrumentation.EnsureTrialFunnelObservableGaugesRegistered();
        ArchLucidInstrumentation.PublishTrialActiveTenantCount(11);

        using MeterListener listener = new();
        List<long> observed = [];

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name == "archlucid_trial_active_tenants")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "archlucid_trial_active_tenants")
            {
                observed.Add(measurement);
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        observed.Should().Contain(11);
    }

    private sealed class TrialFunnelCapture : IDisposable
    {
        private static readonly string[] LongNames =
        [
            "archlucid_trial_signups_total",
            "archlucid_trial_signup_failures_total",
            "archlucid_trial_conversion_total",
            "archlucid_trial_expirations_total",
            "archlucid_billing_checkouts_total",
            "archlucid.ui.sponsor_banner.first_commit_badge_rendered"
        ];

        private static readonly string[] DoubleNames =
        [
            "archlucid_trial_first_run_seconds",
            "archlucid_trial_runs_used_ratio"
        ];

        private readonly List<DoubleMeasurementRecord> _doubleMeasures = [];

        private readonly MeterListener _listener = new();

        private readonly List<LongMeasurementRecord> _longMeasures = [];

        private TrialFunnelCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<long>(OnLong);
            _listener.SetMeasurementEventCallback<double>(OnDouble);
            _listener.Start();
        }

        public IReadOnlyList<LongMeasurementRecord> LongMeasures => _longMeasures;

        public IReadOnlyList<DoubleMeasurementRecord> DoubleMeasures => _doubleMeasures;

        public void Dispose()
        {
            _listener.Dispose();
        }

        public static TrialFunnelCapture Start()
        {
            return new TrialFunnelCapture();
        }

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (LongNames.Contains(instrument.Name))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }

            if (DoubleNames.Contains(instrument.Name))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
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

    private sealed record LongMeasurementRecord(string Name, long Value, List<KeyValuePair<string, object?>> Tags);

    private sealed record DoubleMeasurementRecord(string Name, double Value, List<KeyValuePair<string, object?>> Tags);
}

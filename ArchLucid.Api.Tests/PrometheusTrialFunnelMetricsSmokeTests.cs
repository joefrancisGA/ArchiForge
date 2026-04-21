using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Ensures trial funnel instruments appear on the Prometheus scrape endpoint after emissions (requires Prometheus exporter).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class PrometheusTrialFunnelMetricsSmokeTests
{
    [Fact]
    public async Task Metrics_endpoint_lists_trial_funnel_series_after_emissions()
    {
        await using PrometheusEnabledArchLucidApiFactory factory = new();

        HttpClient client = factory.CreateClient();
        _ = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        ArchLucidInstrumentation.RecordTrialSignup("smoke", "unit");
        ArchLucidInstrumentation.RecordTrialSignupFailure("smoke", "unit");
        ArchLucidInstrumentation.RecordTrialSignupBaselineSkipped();
        ArchLucidInstrumentation.RecordTrialFirstRunLatencySeconds(12);
        ArchLucidInstrumentation.RecordTrialRunsUsedRatio(0.25);
        ArchLucidInstrumentation.RecordTrialConversion("Active", "Standard");
        ArchLucidInstrumentation.RecordTrialExpiration("Active->Expired");
        ArchLucidInstrumentation.RecordBillingCheckout("Noop", "Team", "session_created");
        ArchLucidInstrumentation.PublishTrialActiveTenantCount(3);

        HttpResponseMessage response = await client.GetAsync(new Uri("/metrics", UriKind.Relative));

        response.IsSuccessStatusCode.Should().BeTrue();
        string body = await response.Content.ReadAsStringAsync();

        string[] needles =
        [
            "archlucid_trial_signups_total",
            "archlucid_trial_signup_failures_total",
            "archlucid_trial_signup_baseline_skipped_total",
            "archlucid_trial_first_run_seconds",
            "archlucid_trial_runs_used_ratio",
            "archlucid_trial_conversion_total",
            "archlucid_trial_expirations_total",
            "archlucid_billing_checkouts_total",
            "archlucid_trial_active_tenants",
        ];

        foreach (string needle in needles)
        {
            body.Should().Contain(needle, $"expected Prometheus exposition to mention {needle}");
        }
    }
}

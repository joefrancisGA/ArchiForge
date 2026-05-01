using ArchLucid.Contracts.ValueReports;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Value;

using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Value;

public sealed class ValueReportBuilder(
    IValueReportMetricsReader metricsReader,
    IOptionsMonitor<ValueReportComputationOptions> optionsMonitor)
{
    private readonly IValueReportMetricsReader _metricsReader =
        metricsReader ?? throw new ArgumentNullException(nameof(metricsReader));

    private readonly IOptionsMonitor<ValueReportComputationOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    public async Task<ValueReportSnapshot> BuildAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTimeOffset fromUtcInclusive,
        DateTimeOffset toUtcExclusive,
        CancellationToken cancellationToken)
    {
        ValueReportComputationOptions o = _optionsMonitor.CurrentValue;
        ValueReportRawMetrics raw = await _metricsReader.ReadAsync(
            tenantId,
            workspaceId,
            projectId,
            fromUtcInclusive,
            toUtcExclusive,
            cancellationToken);

        decimal perManifestHours = raw.TenantBaselineManualPrepHoursPerReview
                                   ?? o.BaselineArchitectHoursBeforeArchLucidPerCommittedManifest;
        decimal manifestHours =
            raw.ManifestsCommittedCount * perManifestHours * o.ArchitectHoursSavedFractionVsBaseline;

        decimal governanceHours = raw.GovernanceEventCount * o.GovernanceReviewHoursPerGovernanceEvent;

        decimal driftHours = raw.DriftAlertEventCount * o.DriftReviewHoursPerDriftAlertEvent;

        decimal totalHours = manifestHours + governanceHours + driftHours;

        decimal llmWindowUsd = raw.RunsCompletedCount * o.EstimatedLlmUsdPerCompletedRun;

        decimal periodDays = (decimal)Math.Max(1d, (toUtcExclusive - fromUtcInclusive).TotalDays);
        decimal annualize = 365m / periodDays;
        decimal teamCostScale = 1m;
        if (raw.TenantBaselinePeoplePerReview is { } ppl)
        {
            decimal denom = raw.TenantArchitectureTeamSize is { } ats and > 0
                ? ats
                : o.DefaultTeamSizeForHourlyCostScaling;
            if (denom > 0m)
                teamCostScale = ppl / denom;
        }

        decimal hourly = o.FullyLoadedArchitectHourlyUsd * teamCostScale;

        decimal hoursValuePeriodUsd = totalHours * hourly;
        decimal annualizedHoursValueUsd = hoursValuePeriodUsd * annualize;
        decimal annualizedLlmUsd = llmWindowUsd * annualize;
        decimal baseline = o.BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel;
        decimal net = annualizedHoursValueUsd - baseline - annualizedLlmUsd;
        decimal roiPercent = baseline <= 0m ? 0m : net / baseline * 100m;

        IReadOnlyList<ValueReportRunStatusRow> rows = raw.RunStatusCounts
            .Select(static c => new ValueReportRunStatusRow(c.LegacyRunStatusLabel, c.Count))
            .ToList();

        ReviewCycleBaselineProvenance reviewProvenance;
        decimal? reviewDeltaHours = null;
        decimal? reviewDeltaPercent = null;

        if (raw.MeasuredAverageReviewCycleHoursForWindow is null)
        {
            reviewProvenance = ReviewCycleBaselineProvenance.NoMeasurementYet;
        }
        else
        {
            decimal measuredHours = raw.MeasuredAverageReviewCycleHoursForWindow.Value;
            decimal baselineReviewHours = raw.TenantBaselineReviewCycleHours ??
                                          o.BaselineArchitectHoursBeforeArchLucidPerCommittedManifest;

            reviewProvenance = raw.TenantBaselineReviewCycleHours is not null
                ? string.Equals(
                    raw.TenantBaselineReviewCycleSource,
                    "baseline_settings",
                    StringComparison.OrdinalIgnoreCase)
                    ? ReviewCycleBaselineProvenance.TenantSuppliedViaSettings
                    : ReviewCycleBaselineProvenance.TenantSuppliedAtSignup
                : ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions;

            reviewDeltaHours = baselineReviewHours - measuredHours;
            reviewDeltaPercent = baselineReviewHours > 0m
                ? 100m * (baselineReviewHours - measuredHours) / baselineReviewHours
                : null;
        }

        return new ValueReportSnapshot(
            tenantId,
            workspaceId,
            projectId,
            fromUtcInclusive,
            toUtcExclusive,
            rows,
            raw.RunsCompletedCount,
            raw.ManifestsCommittedCount,
            raw.GovernanceEventCount,
            raw.DriftAlertEventCount,
            manifestHours,
            governanceHours,
            driftHours,
            totalHours,
            llmWindowUsd,
            o.EstimatedLlmCostMethodologyNote,
            annualizedHoursValueUsd,
            annualizedLlmUsd,
            baseline,
            net,
            roiPercent,
            raw.TenantBaselineReviewCycleHours,
            raw.TenantBaselineReviewCycleSource,
            raw.TenantBaselineReviewCycleCapturedUtc,
            raw.MeasuredAverageReviewCycleHoursForWindow,
            raw.MeasuredReviewCycleSampleSize,
            reviewProvenance,
            reviewDeltaHours,
            reviewDeltaPercent,
            raw.FindingFeedbackNetScore,
            raw.FindingFeedbackVoteCount,
            raw.TenantBaselineManualPrepHoursPerReview,
            raw.TenantBaselinePeoplePerReview);
    }
}

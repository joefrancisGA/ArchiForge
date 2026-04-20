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

        decimal manifestHours = raw.ManifestsCommittedCount * o.BaselineArchitectHoursBeforeArchLucidPerCommittedManifest
            * o.ArchitectHoursSavedFractionVsBaseline;

        decimal governanceHours = raw.GovernanceEventCount * o.GovernanceReviewHoursPerGovernanceEvent;

        decimal driftHours = raw.DriftAlertEventCount * o.DriftReviewHoursPerDriftAlertEvent;

        decimal totalHours = manifestHours + governanceHours + driftHours;

        decimal llmWindowUsd = raw.RunsCompletedCount * o.EstimatedLlmUsdPerCompletedRun;

        decimal periodDays = (decimal)Math.Max(1d, (toUtcExclusive - fromUtcInclusive).TotalDays);
        decimal annualize = 365m / periodDays;
        decimal hourly = o.FullyLoadedArchitectHourlyUsd;

        decimal hoursValuePeriodUsd = totalHours * hourly;
        decimal annualizedHoursValueUsd = hoursValuePeriodUsd * annualize;
        decimal annualizedLlmUsd = llmWindowUsd * annualize;
        decimal baseline = o.BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel;
        decimal net = annualizedHoursValueUsd - baseline - annualizedLlmUsd;
        decimal roiPercent = baseline <= 0m ? 0m : net / baseline * 100m;

        IReadOnlyList<ValueReportRunStatusRow> rows = raw.RunStatusCounts
            .Select(static c => new ValueReportRunStatusRow(c.LegacyRunStatusLabel, c.Count))
            .ToList();

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
            roiPercent);
    }
}

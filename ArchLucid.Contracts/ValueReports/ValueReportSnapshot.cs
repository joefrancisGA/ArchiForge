namespace ArchLucid.Contracts.ValueReports;

/// <summary>
/// Tenant-scoped value metrics and ROI projection for sponsor-facing DOCX (see <c>ValueReportBuilder</c>).
/// </summary>
public sealed record ValueReportSnapshot(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ProjectId,
    DateTimeOffset PeriodFromUtc,
    DateTimeOffset PeriodToUtc,
    IReadOnlyList<ValueReportRunStatusRow> RunStatusRows,
    int RunsCompletedCount,
    int ManifestsCommittedCount,
    int GovernanceEventsHandledCount,
    int DriftAlertEventsCaughtCount,
    decimal EstimatedArchitectHoursSavedFromManifests,
    decimal EstimatedArchitectHoursSavedFromGovernanceEvents,
    decimal EstimatedArchitectHoursSavedFromDriftEvents,
    decimal EstimatedTotalArchitectHoursSaved,
    decimal EstimatedLlmCostForWindowUsd,
    string EstimatedLlmCostMethodologyNote,
    decimal AnnualizedHoursValueUsd,
    decimal AnnualizedLlmCostUsd,
    decimal BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel,
    decimal NetAnnualizedValueVersusRoiBaselineUsd,
    decimal RoiAnnualizedPercentVersusRoiBaseline);

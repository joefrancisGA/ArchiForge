namespace ArchLucid.Persistence.Value;

public sealed record ValueReportRunStatusCount(string LegacyRunStatusLabel, int Count);

public sealed record ValueReportRawMetrics(
    IReadOnlyList<ValueReportRunStatusCount> RunStatusCounts,
    int RunsCompletedCount,
    int ManifestsCommittedCount,
    int GovernanceEventCount,
    int DriftAlertEventCount,
    int FindingFeedbackNetScore,
    int FindingFeedbackVoteCount,
    decimal? TenantBaselineReviewCycleHours,
    string? TenantBaselineReviewCycleSource,
    DateTimeOffset? TenantBaselineReviewCycleCapturedUtc,
    decimal? MeasuredAverageReviewCycleHoursForWindow,
    int MeasuredReviewCycleSampleSize,
    decimal? TenantBaselineManualPrepHoursPerReview,
    int? TenantBaselinePeoplePerReview,
    int? TenantArchitectureTeamSize);

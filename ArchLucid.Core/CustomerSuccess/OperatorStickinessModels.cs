namespace ArchLucid.Core.CustomerSuccess;

/// <summary>Scope-scoped signals for operator home guidance (next-best-action selection).</summary>
public sealed record OperatorStickinessSignals(
    int TotalRunsInScope,
    int CommittedRunsInScope,
    Guid? LatestRunId,
    int ComparisonAuditEvents30D,
    int PendingGovernanceApprovals);

/// <summary>Durable, queryable pilot funnel milestones derived from existing tables (no PII columns).</summary>
public sealed record PilotFunnelSnapshot(
    DateTime? FirstRunCreatedUtc,
    DateTime? FirstGoldenManifestUtc,
    DateTime? FirstComparisonUtc,
    DateTime? FirstArtifactOrBundleDownloadUtc,
    DateTime? FirstReplayUtc,
    int TotalRunsInScope,
    int CommittedRunsInScope,
    int ProductLearningSignalsLast90Days);

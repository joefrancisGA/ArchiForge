namespace ArchLucid.Application.ExecDigest;

/// <summary>Immutable weekly digest payload assembled from existing read services.</summary>
public sealed record ExecDigestComposition(
    string WeekLabel,
    string? ComplianceDriftMarkdown,
    int? CommittedManifestsInWeek,
    IReadOnlyList<ExecDigestHighlightedRun> TopManifestRuns,
    string? FindingsDeltaSummary,
    string DashboardUrl,
    string SponsorValueReportUrl,
    string? LatestCommittedRunIdHex);

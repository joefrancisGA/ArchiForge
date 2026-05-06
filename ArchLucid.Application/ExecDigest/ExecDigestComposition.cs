namespace ArchLucid.Application.ExecDigest;
/// <summary>Immutable weekly digest payload assembled from existing read services.</summary>
public sealed record ExecDigestComposition(string WeekLabel, string? ComplianceDriftMarkdown, int? CommittedManifestsInWeek, IReadOnlyList<ExecDigestHighlightedRun> TopManifestRuns, string? FindingsDeltaSummary, string DashboardUrl, string SponsorValueReportUrl, string? LatestCommittedRunIdHex)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(WeekLabel, ComplianceDriftMarkdown, TopManifestRuns, FindingsDeltaSummary, DashboardUrl, SponsorValueReportUrl, LatestCommittedRunIdHex);
    private static byte __ValidatePrimaryConstructorArguments(System.String WeekLabel, System.String? ComplianceDriftMarkdown, System.Collections.Generic.IReadOnlyList<ArchLucid.Application.ExecDigest.ExecDigestHighlightedRun> TopManifestRuns, System.String? FindingsDeltaSummary, System.String DashboardUrl, System.String SponsorValueReportUrl, System.String? LatestCommittedRunIdHex)
    {
        ArgumentNullException.ThrowIfNull(WeekLabel);
        ArgumentNullException.ThrowIfNull(TopManifestRuns);
        ArgumentNullException.ThrowIfNull(DashboardUrl);
        ArgumentNullException.ThrowIfNull(SponsorValueReportUrl);
        return (byte)0;
    }
}
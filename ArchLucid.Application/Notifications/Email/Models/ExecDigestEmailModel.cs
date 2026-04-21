using ArchLucid.Application.ExecDigest;

namespace ArchLucid.Application.Notifications.Email.Models;

/// <summary>Razor model for <c>Templates/ExecDigest.cshtml</c>.</summary>
public sealed class ExecDigestEmailModel
{
    public string ProductName { get; init; } = "ArchLucid";

    public string WeekLabel { get; init; } = string.Empty;

    public string? ComplianceDriftMarkdown { get; init; }

    public int? CommittedManifestsInWeek { get; init; }

    public IReadOnlyList<ExecDigestHighlightedRun> TopRuns { get; init; } = [];

    public string? FindingsDeltaSummary { get; init; }

    public string DashboardUrl { get; init; } = string.Empty;

    public string SponsorValueReportUrl { get; init; } = string.Empty;

    public string UnsubscribeUrl { get; init; } = string.Empty;
}

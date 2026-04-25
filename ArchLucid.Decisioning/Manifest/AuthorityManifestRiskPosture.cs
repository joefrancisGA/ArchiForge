using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Manifest;

/// <summary>
///     Derives a coarse risk label from authority <see cref="GoldenManifest" /> unresolved issues (deterministic; no LLM).
/// </summary>
public static class AuthorityManifestRiskPosture
{
    /// <summary>
    ///     Returns <c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c> from the worst unresolved issue severity.
    /// </summary>
    public static string Derive(GoldenManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (manifest.UnresolvedIssues.Items.Count == 0)
            return "Low";


        int worst = 0;

        foreach (ManifestIssue issue in manifest.UnresolvedIssues.Items)

            worst = Math.Max(worst, MapSeverityRank(issue.Severity));


        return worst switch
        {
            >= 4 => "Critical",
            3 => "High",
            2 => "Medium",
            _ => "Low"
        };
    }

    private static int MapSeverityRank(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return 2;


        string s = severity.Trim();

        if (string.Equals(s, "Critical", StringComparison.OrdinalIgnoreCase))
            return 4;


        if (string.Equals(s, "High", StringComparison.OrdinalIgnoreCase))
            return 3;


        if (string.Equals(s, "Medium", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "Warning", StringComparison.OrdinalIgnoreCase))
            return 2;


        if (string.Equals(s, "Low", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "Info", StringComparison.OrdinalIgnoreCase))
            return 1;


        return 2;
    }
}

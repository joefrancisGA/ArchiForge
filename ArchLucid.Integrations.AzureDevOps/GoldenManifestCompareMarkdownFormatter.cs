using System.Text;

using ArchLucid.Core.Comparison;

namespace ArchLucid.Integrations.AzureDevOps;

/// <summary>
///     Renders <see cref="ComparisonResult" /> as Markdown aligned with
///     <c>integrations/github-action-manifest-delta/fetch-manifest-delta.mjs</c>.
/// </summary>
public static class GoldenManifestCompareMarkdownFormatter
{
    /// <summary>Builds PR-ready Markdown from a structured compare response.</summary>
    /// <param name="result">Non-null compare payload from <c>GET /v1/compare</c>.</param>
    /// <param name="operatorRunDeepLink">Optional single-run operator URL (appended line when non-empty).</param>
    public static string Format(ComparisonResult result, string? operatorRunDeepLink)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        StringBuilder lines = new();
        lines.AppendLine("## ArchLucid manifest delta");
        lines.AppendLine();
        lines.AppendLine($"- **Base run:** `{result.BaseRunId:D}`");
        lines.AppendLine($"- **Target run:** `{result.TargetRunId:D}`");
        lines.AppendLine($"- **Total delta rows:** {result.TotalDeltaCount}");
        lines.AppendLine();

        if (result.SummaryHighlights.Count > 0)
        {
            lines.AppendLine("### Highlights");
            lines.AppendLine();

            foreach (string h in result.SummaryHighlights.Take(20))

                lines.AppendLine($"- {h}");

            lines.AppendLine();
        }

        lines.AppendLine("### Delta buckets (counts)");
        lines.AppendLine();
        lines.AppendLine("| Bucket | Count |");
        lines.AppendLine("| --- | ---: |");
        lines.AppendLine($"| Decision changes | {result.DecisionChanges.Count} |");
        lines.AppendLine($"| Requirement changes | {result.RequirementChanges.Count} |");
        lines.AppendLine($"| Security changes | {result.SecurityChanges.Count} |");
        lines.AppendLine($"| Topology changes | {result.TopologyChanges.Count} |");
        lines.AppendLine($"| Cost changes | {result.CostChanges.Count} |");
        lines.AppendLine();

        if (string.IsNullOrWhiteSpace(operatorRunDeepLink))
            return lines.ToString();

        lines.AppendLine($"[Open operator run]({operatorRunDeepLink.Trim()})");
        lines.AppendLine();

        return lines.ToString();
    }
}

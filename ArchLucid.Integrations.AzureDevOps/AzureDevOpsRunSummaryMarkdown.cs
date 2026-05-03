using System.Text;

using ArchLucid.Contracts.Abstractions.Integrations;

namespace ArchLucid.Integrations.AzureDevOps;

/// <summary>Fallback PR Markdown when compare is unavailable (404, missing config, or no prior run).</summary>
public static class AzureDevOpsRunSummaryMarkdown
{
    public static string Format(Guid runId, Guid manifestId, IReadOnlyList<AuthorityRunCompletedFindingLink> findings, string? operatorRunDeepLink)
    {
        StringBuilder sb = new();
        sb.AppendLine("## ArchLucid — run completed");
        sb.AppendLine();
        sb.AppendLine($"- **Run:** `{runId:D}`");
        sb.AppendLine($"- **Golden manifest id:** `{manifestId:D}`");
        sb.AppendLine();

        if (findings.Count > 0)
        {
            sb.AppendLine("### Findings by severity");
            sb.AppendLine();

            IOrderedEnumerable<IGrouping<string, AuthorityRunCompletedFindingLink>> grouped = findings
                .GroupBy(f => string.IsNullOrWhiteSpace(f.Severity) ? "Unknown" : f.Severity.Trim())
                .OrderByDescending(g => g.Count());

            foreach (IGrouping<string, AuthorityRunCompletedFindingLink> g in grouped)
                sb.AppendLine($"- **{g.Key}:** {g.Count()}");

            sb.AppendLine();
        }

        if (string.IsNullOrWhiteSpace(operatorRunDeepLink))
            return sb.ToString();
        sb.AppendLine($"[Open operator run]({operatorRunDeepLink.Trim()})");
        sb.AppendLine();

        return sb.ToString();
    }
}

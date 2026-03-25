using System.Text;

namespace ArchiForge.Application.Diffs;

public sealed class MarkdownAgentResultDiffSummaryFormatter : IAgentResultDiffSummaryFormatter
{
    public string FormatMarkdown(AgentResultDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"# Agent Result Comparison: {diff.LeftRunId} -> {diff.RightRunId}");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine($"Comparison between agent outputs for runs **{diff.LeftRunId}** and **{diff.RightRunId}**.");
        sb.AppendLine();

        foreach (AgentResultDelta delta in diff.AgentDeltas.OrderBy(d => d.AgentType))
        {
            sb.AppendLine($"## {delta.AgentType}");
            sb.AppendLine();
            sb.AppendLine($"- Left Exists: {(delta.LeftExists ? "Yes" : "No")}");
            sb.AppendLine($"- Right Exists: {(delta.RightExists ? "Yes" : "No")}");
            sb.AppendLine($"- Left Confidence: {(delta.LeftConfidence.HasValue ? delta.LeftConfidence.Value.ToString("0.00") : "n/a")}");
            sb.AppendLine($"- Right Confidence: {(delta.RightConfidence.HasValue ? delta.RightConfidence.Value.ToString("0.00") : "n/a")}");
            sb.AppendLine();

            AppendSection(sb, "Added Claims", delta.AddedClaims);
            AppendSection(sb, "Removed Claims", delta.RemovedClaims);
            AppendSection(sb, "Added Evidence References", delta.AddedEvidenceRefs);
            AppendSection(sb, "Removed Evidence References", delta.RemovedEvidenceRefs);
            AppendSection(sb, "Added Findings", delta.AddedFindings);
            AppendSection(sb, "Removed Findings", delta.RemovedFindings);
            AppendSection(sb, "Added Required Controls", delta.AddedRequiredControls);
            AppendSection(sb, "Removed Required Controls", delta.RemovedRequiredControls);
            AppendSection(sb, "Added Warnings", delta.AddedWarnings);
            AppendSection(sb, "Removed Warnings", delta.RemovedWarnings);
        }

        if (diff.Warnings.Count <= 0) return sb.ToString();
        
        sb.AppendLine("## Warnings");
        sb.AppendLine();

        foreach (string warning in diff.Warnings)
        {
            sb.AppendLine($"- {warning}");
        }

        sb.AppendLine();

        return sb.ToString();
    }

    private static void AppendSection(
        StringBuilder sb,
        string title,
        IReadOnlyCollection<string> items)
    {
        sb.AppendLine($"### {title}");
        sb.AppendLine();

        if (items.Count == 0)
        {
            sb.AppendLine("- None");
            sb.AppendLine();
            return;
        }

        foreach (string item in items.OrderBy(x => x))
        {
            sb.AppendLine($"- {item}");
        }

        sb.AppendLine();
    }
}

using System.Text;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Formats an <see cref="EndToEndReplayComparisonReport"/> as Markdown, listing run-metadata changes,
/// agents with material results changes, manifest structural changes, and export-record diffs.
/// </summary>
public sealed class MarkdownEndToEndReplayComparisonSummaryFormatter
    : IEndToEndReplayComparisonSummaryFormatter
{
    /// <inheritdoc />
    public string FormatMarkdown(EndToEndReplayComparisonReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder sb = new();

        sb.AppendLine($"# End-to-End Replay Comparison: {report.LeftRunId} -> {report.RightRunId}");
        sb.AppendLine();

        AppendSection(sb, "Run Metadata Changes", report.RunDiff.ChangedFields);

        if (report.AgentResultDiff is not null)
        {
            List<string> changedAgents = report.AgentResultDiff.AgentDeltas
                .Where(d =>
                    d.AddedClaims.Count > 0 ||
                    d.RemovedClaims.Count > 0 ||
                    d.AddedFindings.Count > 0 ||
                    d.RemovedFindings.Count > 0 ||
                    d.AddedRequiredControls.Count > 0 ||
                    d.RemovedRequiredControls.Count > 0 ||
                    d.AddedWarnings.Count > 0 ||
                    d.RemovedWarnings.Count > 0)
                .Select(d => d.AgentType.ToString())
                .ToList();

            AppendSection(sb, "Agents With Material Changes", changedAgents);
        }

        if (report.ManifestDiff is not null)
        {
            AppendSection(sb, "Manifest Added Services", report.ManifestDiff.AddedServices);
            AppendSection(sb, "Manifest Removed Services", report.ManifestDiff.RemovedServices);
            AppendSection(sb, "Manifest Added Required Controls", report.ManifestDiff.AddedRequiredControls);
            AppendSection(sb, "Manifest Removed Required Controls", report.ManifestDiff.RemovedRequiredControls);
        }

        if (report.ExportDiffs.Count > 0)
        {
            List<string> exportChangeSummaries = report.ExportDiffs
                .Select(d => $"{d.LeftExportRecordId} -> {d.RightExportRecordId}: " +
                             $"{d.ChangedTopLevelFields.Count} top-level change(s), " +
                             $"{d.RequestDiff.ChangedFlags.Count} flag change(s), " +
                             $"{d.RequestDiff.ChangedValues.Count} value change(s)")
                .ToList();

            AppendSection(sb, "Export Diff Summary", exportChangeSummaries);
        }

        AppendSection(sb, "Interpretation Notes", report.InterpretationNotes);
        AppendSection(sb, "Warnings", report.Warnings);

        return sb.ToString();
    }

    private static void AppendSection(
        StringBuilder sb,
        string title,
        IReadOnlyCollection<string> items)
    {
        sb.AppendLine($"## {title}");
        sb.AppendLine();

        if (items.Count == 0)
        {
            sb.AppendLine("- None");
            sb.AppendLine();
            return;
        }

        foreach (string item in items)
        {
            sb.AppendLine($"- {item}");
        }

        sb.AppendLine();
    }
}


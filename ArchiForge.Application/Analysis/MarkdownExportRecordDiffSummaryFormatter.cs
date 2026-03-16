using System.Text;

namespace ArchiForge.Application.Analysis;

public sealed class MarkdownExportRecordDiffSummaryFormatter : IExportRecordDiffSummaryFormatter
{
    public string FormatMarkdown(ExportRecordDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        var sb = new StringBuilder();

        sb.AppendLine($"# Export Record Comparison: {diff.LeftExportRecordId} -> {diff.RightExportRecordId}");
        sb.AppendLine();
        sb.AppendLine($"- Left Run ID: {diff.LeftRunId}");
        sb.AppendLine($"- Right Run ID: {diff.RightRunId}");
        sb.AppendLine();

        AppendSection(sb, "Changed Top-Level Fields", diff.ChangedTopLevelFields);
        AppendSection(sb, "Changed Request Flags", diff.RequestDiff.ChangedFlags);
        AppendSection(sb, "Changed Request Values", diff.RequestDiff.ChangedValues);

        if (diff.Warnings.Count > 0)
        {
            AppendSection(sb, "Warnings", diff.Warnings);
        }

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

        foreach (var item in items.OrderBy(x => x))
        {
            sb.AppendLine($"- {item}");
        }

        sb.AppendLine();
    }
}


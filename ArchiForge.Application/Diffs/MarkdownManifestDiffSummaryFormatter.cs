using System.Text;

namespace ArchiForge.Application.Diffs;

public sealed class MarkdownManifestDiffSummaryFormatter : IManifestDiffSummaryFormatter
{
    public string FormatMarkdown(ManifestDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"# Manifest Comparison: {diff.LeftManifestVersion} -> {diff.RightManifestVersion}");
        sb.AppendLine();

        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine(
            $"Comparison between manifest **{diff.LeftManifestVersion}** and " +
            $"**{diff.RightManifestVersion}**.");
        sb.AppendLine();

        AppendStringSection(sb, "Added Services", diff.AddedServices);
        AppendStringSection(sb, "Removed Services", diff.RemovedServices);
        AppendStringSection(sb, "Added Datastores", diff.AddedDatastores);
        AppendStringSection(sb, "Removed Datastores", diff.RemovedDatastores);
        AppendStringSection(sb, "Added Required Controls", diff.AddedRequiredControls);
        AppendStringSection(sb, "Removed Required Controls", diff.RemovedRequiredControls);

        AppendRelationshipSection(sb, "Added Relationships", diff.AddedRelationships);
        AppendRelationshipSection(sb, "Removed Relationships", diff.RemovedRelationships);

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

    private static void AppendStringSection(
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

        foreach (string item in items.OrderBy(x => x))
        {
            sb.AppendLine($"- {item}");
        }

        sb.AppendLine();
    }

    private static void AppendRelationshipSection(
        StringBuilder sb,
        string title,
        IReadOnlyCollection<RelationshipDiffItem> items)
    {
        sb.AppendLine($"## {title}");
        sb.AppendLine();

        if (items.Count == 0)
        {
            sb.AppendLine("- None");
            sb.AppendLine();
            return;
        }

        foreach (RelationshipDiffItem item in items
                     .OrderBy(x => x.SourceId)
                     .ThenBy(x => x.TargetId)
                     .ThenBy(x => x.RelationshipType))
        {
            sb.AppendLine($"- **{item.SourceId}** -> **{item.TargetId}** ({item.RelationshipType})");

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                sb.AppendLine($"  - {item.Description}");
            }
        }

        sb.AppendLine();
    }
}

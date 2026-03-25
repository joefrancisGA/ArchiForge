using System.Text;

using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diffs;

public sealed class MarkdownManifestDiffExportService : IManifestDiffExportService
{
    public string GenerateMarkdownExport(
        GoldenManifest left,
        GoldenManifest right,
        ManifestDiffResult diff,
        string markdownSummary)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(diff);
        ArgumentNullException.ThrowIfNull(markdownSummary);

        StringBuilder sb = new();

        sb.AppendLine("# ArchiForge Manifest Comparison Export");
        sb.AppendLine();
        sb.AppendLine($"- Left Manifest Version: {diff.LeftManifestVersion}");
        sb.AppendLine($"- Right Manifest Version: {diff.RightManifestVersion}");
        sb.AppendLine($"- Generated UTC: {DateTime.UtcNow:O}");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(markdownSummary.Trim());
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        sb.AppendLine("## Left Manifest Snapshot");
        sb.AppendLine();
        sb.AppendLine($"- System Name: {left.SystemName}");
        sb.AppendLine($"- Run ID: {left.RunId}");
        sb.AppendLine($"- Manifest Version: {left.Metadata.ManifestVersion}");
        sb.AppendLine($"- Service Count: {left.Services.Count}");
        sb.AppendLine($"- Datastore Count: {left.Datastores.Count}");
        sb.AppendLine($"- Relationship Count: {left.Relationships.Count}");
        sb.AppendLine();

        sb.AppendLine("## Right Manifest Snapshot");
        sb.AppendLine();
        sb.AppendLine($"- System Name: {right.SystemName}");
        sb.AppendLine($"- Run ID: {right.RunId}");
        sb.AppendLine($"- Manifest Version: {right.Metadata.ManifestVersion}");
        sb.AppendLine($"- Service Count: {right.Services.Count}");
        sb.AppendLine($"- Datastore Count: {right.Datastores.Count}");
        sb.AppendLine($"- Relationship Count: {right.Relationships.Count}");
        sb.AppendLine();

        return sb.ToString();
    }
}

using System.Text;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Exports;

public sealed class MarkdownArchitectureExportService : IArchitectureExportService
{
    public string GenerateMarkdownPackage(
        GoldenManifest manifest,
        string mermaidDiagram,
        string markdownSummary,
        AgentEvidencePackage? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(mermaidDiagram);
        ArgumentNullException.ThrowIfNull(markdownSummary);

        var sb = new StringBuilder();

        sb.AppendLine($"# Architecture Export: {manifest.SystemName}");
        sb.AppendLine();
        sb.AppendLine("## Diagram");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine(mermaidDiagram.TrimEnd());
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine(markdownSummary.TrimEnd());
        sb.AppendLine();

        if (evidence is not null)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Evidence Package Snapshot");
            sb.AppendLine();
            sb.AppendLine($"- Evidence Package ID: {evidence.EvidencePackageId}");
            sb.AppendLine($"- Run ID: {evidence.RunId}");
            sb.AppendLine($"- Request ID: {evidence.RequestId}");
            sb.AppendLine($"- Policy Count: {evidence.Policies.Count}");
            sb.AppendLine($"- Service Catalog Hint Count: {evidence.ServiceCatalog.Count}");
            sb.AppendLine($"- Pattern Hint Count: {evidence.Patterns.Count}");
            sb.AppendLine($"- Evidence Note Count: {evidence.Notes.Count}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

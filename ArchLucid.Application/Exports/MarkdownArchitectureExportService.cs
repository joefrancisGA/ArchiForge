using System.Text;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Exports;

/// <summary>
///     Produces a Markdown export document containing the architecture diagram (as a Mermaid code fence),
///     the Markdown summary, and an optional compact evidence snapshot.
/// </summary>
public sealed class MarkdownArchitectureExportService : IArchitectureExportService
{
    /// <inheritdoc />
    public string GenerateMarkdownPackage(
        GoldenManifest manifest,
        string mermaidDiagram,
        string markdownSummary,
        AgentEvidencePackage? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(mermaidDiagram);
        ArgumentNullException.ThrowIfNull(markdownSummary);

        StringBuilder sb = new();

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

        if (evidence is null)
            return sb.ToString();

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

        return sb.ToString();
    }
}

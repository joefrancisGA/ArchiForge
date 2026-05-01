using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Exports;

/// <summary>
///     Bundles a <see cref="GoldenManifest" />, its Mermaid diagram, and a Markdown summary into
///     a single exportable document, optionally including an evidence snapshot.
/// </summary>
public interface IArchitectureExportService
{
    /// <summary>
    ///     Assembles a Markdown export package from the provided components.
    ///     When <paramref name="evidence" /> is supplied, a compact evidence snapshot section is appended.
    /// </summary>
    string GenerateMarkdownPackage(
        GoldenManifest manifest,
        string mermaidDiagram,
        string markdownSummary,
        AgentEvidencePackage? evidence = null);
}

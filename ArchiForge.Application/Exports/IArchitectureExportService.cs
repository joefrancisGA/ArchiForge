using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Exports;

public interface IArchitectureExportService
{
    string GenerateMarkdownPackage(
        GoldenManifest manifest,
        string mermaidDiagram,
        string markdownSummary,
        AgentEvidencePackage? evidence = null);
}

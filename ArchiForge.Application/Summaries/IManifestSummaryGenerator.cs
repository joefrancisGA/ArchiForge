using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Summaries;

public interface IManifestSummaryGenerator
{
    string GenerateMarkdown(
        GoldenManifest manifest,
        AgentEvidencePackage? evidence = null);
}

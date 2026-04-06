using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diffs;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Analysis;

public sealed class ArchitectureAnalysisReport
{
    public ArchitectureRun Run { get; set; } = new();
    public AgentEvidencePackage? Evidence { get; set; }
    public List<AgentExecutionTrace> ExecutionTraces { get; set; } = [];
    public GoldenManifest? Manifest { get; set; }
    public string? Diagram { get; set; }
    public string? Summary { get; set; }
    public DeterminismCheckResult? Determinism { get; set; }
    public ManifestDiffResult? ManifestDiff { get; set; }
    public AgentResultDiffResult? AgentResultDiff { get; set; }
    public List<string> Warnings { get; set; } = [];
}

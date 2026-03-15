using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application;

public sealed class CreateRunResult
{
    public ArchitectureRun Run { get; set; } = new();
    public EvidenceBundle EvidenceBundle { get; set; } = new();
    public List<AgentTask> Tasks { get; set; } = [];
}

public sealed class ExecuteRunResult
{
    public string RunId { get; set; } = string.Empty;
    public List<AgentResult> Results { get; set; } = [];
}

public sealed class CommitRunResult
{
    public GoldenManifest Manifest { get; set; } = new();
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application;

public sealed class ReplayRunResult
{
    public string OriginalRunId { get; set; } = string.Empty;
    public string ReplayRunId { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = string.Empty;
    public List<AgentResult> Results { get; set; } = [];
    public GoldenManifest? Manifest { get; set; }
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

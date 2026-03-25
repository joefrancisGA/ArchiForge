using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

/// <summary>Full run detail payload returned by the run detail endpoint.</summary>
public sealed class RunDetailsResponse
{
    public ArchitectureRun Run { get; set; } = new();

    public List<AgentTask> Tasks { get; set; } = [];

    public List<AgentResult> Results { get; set; } = [];

    public GoldenManifest? Manifest { get; set; }

    public List<DecisionTrace> DecisionTraces { get; set; } = [];
}

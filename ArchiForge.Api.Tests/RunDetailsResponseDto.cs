using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Tests;

public sealed class RunDetailsResponseDto
{
    public ArchitectureRun Run { get; set; } = new();
    public List<AgentTask> Tasks { get; set; } = [];
    public List<AgentResult> Results { get; set; } = [];
    public GoldenManifest? Manifest { get; set; }
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
}


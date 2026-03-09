using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Coordinator.Services;

public sealed class CoordinationResult
{
    public ArchitectureRun Run { get; set; } = new();

    public EvidenceBundle EvidenceBundle { get; set; } = new();

    public List<AgentTask> Tasks { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public List<string> Errors { get; set; } = [];

    public bool Success => Errors.Count == 0;
}
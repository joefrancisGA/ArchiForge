using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

public sealed class CreateArchitectureRunResponse
{
    public ArchitectureRun Run { get; set; } = new();
    public EvidenceBundle EvidenceBundle { get; set; } = new();
    public List<AgentTask> Tasks { get; set; } = [];
}
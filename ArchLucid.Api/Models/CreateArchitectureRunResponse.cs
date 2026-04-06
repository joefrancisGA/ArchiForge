using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class CreateArchitectureRunResponse
{
    public ArchitectureRun Run { get; set; } = new();
    public EvidenceBundle EvidenceBundle { get; set; } = new();
    public List<AgentTask> Tasks { get; set; } = [];
}

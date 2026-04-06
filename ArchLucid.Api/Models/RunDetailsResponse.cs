using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Api.Models;

/// <summary>Full run detail payload returned by the run detail endpoint.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class RunDetailsResponse
{
    public ArchitectureRun Run { get; set; } = new();
    public List<AgentTask> Tasks { get; set; } = [];
    public List<AgentResult> Results { get; set; } = [];
    public GoldenManifest? Manifest { get; set; }
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
}

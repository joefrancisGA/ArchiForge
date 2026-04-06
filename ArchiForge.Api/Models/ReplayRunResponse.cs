using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ReplayRunResponse
{
    public string OriginalRunId { get; set; } = string.Empty;
    public string ReplayRunId { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = string.Empty;
    public List<AgentResult> Results { get; set; } = [];
    public GoldenManifest? Manifest { get; set; }
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

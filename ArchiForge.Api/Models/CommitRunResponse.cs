using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class CommitRunResponse
{
    public GoldenManifest Manifest { get; set; } = new();
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

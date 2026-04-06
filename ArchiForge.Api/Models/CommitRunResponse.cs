using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class CommitRunResponse
{
    public GoldenManifest Manifest { get; set; } = new();
    public List<RunEventTrace> DecisionTraces { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

public sealed class CommitRunResponse
{
    public GoldenManifest Manifest { get; set; } = new();
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

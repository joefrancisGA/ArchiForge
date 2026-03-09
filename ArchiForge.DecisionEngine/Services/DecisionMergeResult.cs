using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.DecisionEngine.Services;

public sealed class DecisionMergeResult
{
    public GoldenManifest Manifest { get; set; } = new();

    public List<string> Warnings { get; set; } = [];

    public List<string> Errors { get; set; } = [];

    public List<DecisionTrace> DecisionTraces { get; set; } = [];

    public bool Success => Errors.Count == 0;
}
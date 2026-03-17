using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

public sealed class RunDetailsResponse
{
    public ArchitectureRun Run { get; set; } = new();

    public List<AgentTask> Tasks { get; set; } = [];

    public List<AgentResult> Results { get; set; } = [];

    public GoldenManifest? Manifest { get; set; }

    public List<DecisionTrace> DecisionTraces { get; set; } = [];
}

public sealed class RunListItemResponse
{
    public string RunId { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public string? CurrentManifestVersion { get; set; }

    public string SystemName { get; set; } = string.Empty;
}


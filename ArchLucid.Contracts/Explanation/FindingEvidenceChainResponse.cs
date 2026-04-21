namespace ArchLucid.Contracts.Explanation;

/// <summary>
/// Read-only pointers linking one findings-snapshot finding to persisted run artifacts (ADR 0021 read-path explainability).
/// </summary>
public sealed class FindingEvidenceChainResponse
{
    public string RunId { get; set; } = string.Empty;

    public string FindingId { get; set; } = string.Empty;

    public string? ManifestVersion { get; set; }

    public Guid? FindingsSnapshotId { get; set; }

    public Guid? ContextSnapshotId { get; set; }

    public Guid? GraphSnapshotId { get; set; }

    public Guid? DecisionTraceId { get; set; }

    public Guid? GoldenManifestId { get; set; }

    public IReadOnlyList<string> RelatedGraphNodeIds { get; set; } = [];

    public IReadOnlyList<string> AgentExecutionTraceIds { get; set; } = [];
}

namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// One persisted pilot or product-team observation about output quality or repeat friction.
/// </summary>
public sealed record ProductLearningPilotSignalRecord
{
    public Guid SignalId { get; init; }
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid ProjectId { get; init; }
    public string? ArchitectureRunId { get; init; }
    public Guid? AuthorityRunId { get; init; }
    public string? ManifestVersion { get; init; }
    public string SubjectType { get; init; } = string.Empty;
    public string Disposition { get; init; } = string.Empty;
    public string? PatternKey { get; init; }
    public string? ArtifactHint { get; init; }
    public string? CommentShort { get; init; }
    public string? DetailJson { get; init; }
    public string? RecordedByUserId { get; init; }
    public string? RecordedByDisplayName { get; init; }
    public DateTime RecordedUtc { get; init; }
    public string TriageStatus { get; init; } = ProductLearningTriageStatusValues.Open;
}

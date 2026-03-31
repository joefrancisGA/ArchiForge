namespace ArchiForge.Persistence.Models;

public sealed class RunRecord
{
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    /// <summary>Scoped solution/project boundary (GUID). Distinct from <see cref="ProjectId"/> slug.</summary>
    public Guid ScopeProjectId { get; set; }
    public Guid RunId { get; set; }
    public string ProjectId { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; }
    public Guid? ContextSnapshotId { get; set; }
    public Guid? GraphSnapshotId { get; set; }
    public Guid? FindingsSnapshotId { get; set; }
    public Guid? GoldenManifestId { get; set; }
    public Guid? DecisionTraceId { get; set; }
    public Guid? ArtifactBundleId { get; set; }

    /// <summary>When set, the run is excluded from list/detail authority APIs (soft archival).</summary>
    public DateTime? ArchivedUtc { get; set; }
}

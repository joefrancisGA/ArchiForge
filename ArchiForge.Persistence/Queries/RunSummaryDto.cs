namespace ArchiForge.Persistence.Queries;

public class RunSummaryDto
{
    public Guid RunId { get; set; }
    public string ProjectId { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; }

    public bool HasContextSnapshot => ContextSnapshotId.HasValue;
    public bool HasGraphSnapshot => GraphSnapshotId.HasValue;
    public bool HasFindingsSnapshot => FindingsSnapshotId.HasValue;
    public bool HasGoldenManifest => GoldenManifestId.HasValue;
    public bool HasDecisionTrace => DecisionTraceId.HasValue;
    public bool HasArtifactBundle => ArtifactBundleId.HasValue;

    public Guid? ContextSnapshotId { get; set; }
    public Guid? GraphSnapshotId { get; set; }
    public Guid? FindingsSnapshotId { get; set; }
    public Guid? GoldenManifestId { get; set; }
    public Guid? DecisionTraceId { get; set; }
    public Guid? ArtifactBundleId { get; set; }
}

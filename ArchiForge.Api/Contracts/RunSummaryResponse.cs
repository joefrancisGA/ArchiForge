namespace ArchiForge.Api.HttpContracts;

public class RunSummaryResponse
{
    public Guid RunId { get; set; }
    public string ProjectId { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Guid? ContextSnapshotId { get; set; }
    public Guid? GraphSnapshotId { get; set; }
    public Guid? FindingsSnapshotId { get; set; }
    public Guid? GoldenManifestId { get; set; }
    public Guid? DecisionTraceId { get; set; }
    public Guid? ArtifactBundleId { get; set; }
}

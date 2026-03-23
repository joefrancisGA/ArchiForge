namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON contract for <see cref="ArchiForge.Persistence.Queries.RunSummaryDto"/> (authority run list and summary endpoints).
/// </summary>
public class RunSummaryResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.RunId"/>
    public Guid RunId
    {
        get; set;
    }
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.ProjectId"/>
    public string ProjectId { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.Description"/>
    public string? Description
    {
        get; set;
    }
    public DateTime CreatedUtc
    {
        get; set;
    }

    public Guid? ContextSnapshotId
    {
        get; set;
    }
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.GraphSnapshotId"/>
    public Guid? GraphSnapshotId
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.FindingsSnapshotId"/>
    public Guid? FindingsSnapshotId
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.GoldenManifestId"/>
    public Guid? GoldenManifestId
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.DecisionTraceId"/>
    public Guid? DecisionTraceId
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.ArtifactBundleId"/>
    public Guid? ArtifactBundleId
    {
        get; set;
    }
}

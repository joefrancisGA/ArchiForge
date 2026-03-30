namespace ArchiForge.Persistence.Queries;

/// <summary>
/// Lightweight projection of a <see cref="ArchiForge.Persistence.Models.RunRecord"/> for lists and cards (ids only for linked artifacts).
/// </summary>
/// <remarks>
/// Mapped by <see cref="IAuthorityQueryService"/> from runs; HTTP exposure as <see cref="ArchiForge.Api.Contracts.RunSummaryResponse"/> including derived <c>Has*</c> flags.
/// </remarks>
public class RunSummaryDto
{
    /// <summary>Run primary key.</summary>
    public Guid RunId { get; set; }

    /// <summary>Authority project slug this run belongs to.</summary>
    public string ProjectId { get; set; } = null!;

    /// <summary>Optional operator description.</summary>
    public string? Description { get; set; }

    /// <summary>When the run was created (UTC).</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary><see langword="true"/> when <see cref="ContextSnapshotId"/> is set.</summary>
    public bool HasContextSnapshot => ContextSnapshotId.HasValue;

    /// <summary><see langword="true"/> when <see cref="GraphSnapshotId"/> is set.</summary>
    public bool HasGraphSnapshot => GraphSnapshotId.HasValue;

    /// <summary><see langword="true"/> when <see cref="FindingsSnapshotId"/> is set.</summary>
    public bool HasFindingsSnapshot => FindingsSnapshotId.HasValue;

    /// <summary><see langword="true"/> when <see cref="GoldenManifestId"/> is set.</summary>
    public bool HasGoldenManifest => GoldenManifestId.HasValue;

    /// <summary><see langword="true"/> when <see cref="DecisionTraceId"/> is set.</summary>
    public bool HasDecisionTrace => DecisionTraceId.HasValue;

    /// <summary><see langword="true"/> when <see cref="ArtifactBundleId"/> is set.</summary>
    public bool HasArtifactBundle => ArtifactBundleId.HasValue;

    public Guid? ContextSnapshotId { get; set; }
    public Guid? GraphSnapshotId { get; set; }
    public Guid? FindingsSnapshotId { get; set; }
    public Guid? GoldenManifestId { get; set; }
    public Guid? DecisionTraceId { get; set; }
    public Guid? ArtifactBundleId { get; set; }
}

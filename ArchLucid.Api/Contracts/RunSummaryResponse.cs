using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON contract for <see cref="ArchiForge.Persistence.Queries.RunSummaryDto"/> (authority run list and summary endpoints).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API contract DTO; no business logic.")]
public class RunSummaryResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.RunId"/>
    public Guid RunId { get; set; }
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.ProjectId"/>
    public string ProjectId { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.Description"/>
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; }
    public Guid? ContextSnapshotId { get; set; }
    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.GraphSnapshotId"/>
    public Guid? GraphSnapshotId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.FindingsSnapshotId"/>
    public Guid? FindingsSnapshotId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.GoldenManifestId"/>
    public Guid? GoldenManifestId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.DecisionTraceId"/>
    public Guid? DecisionTraceId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.ArtifactBundleId"/>
    public Guid? ArtifactBundleId { get; set; }

    /// <summary>Operator-facing flags mirroring <see cref="ArchiForge.Persistence.Queries.RunSummaryDto"/> computed properties (JSON for UI without null inference).</summary>
    public bool HasContextSnapshot { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.HasGraphSnapshot"/>
    public bool HasGraphSnapshot { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.HasFindingsSnapshot"/>
    public bool HasFindingsSnapshot { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.HasGoldenManifest"/>
    public bool HasGoldenManifest { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.HasDecisionTrace"/>
    public bool HasDecisionTrace { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Queries.RunSummaryDto.HasArtifactBundle"/>
    public bool HasArtifactBundle { get; set; }
}

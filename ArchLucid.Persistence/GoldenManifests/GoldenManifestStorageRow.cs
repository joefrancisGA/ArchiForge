using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.GoldenManifests;

/// <summary>Dapper projection for <c>dbo.GoldenManifests</c> including legacy JSON columns.</summary>
[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class GoldenManifestStorageRow
{
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ManifestId { get; init; }
    public Guid RunId { get; init; }
    public Guid ContextSnapshotId { get; init; }
    public Guid GraphSnapshotId { get; init; }
    public Guid FindingsSnapshotId { get; init; }
    public Guid DecisionTraceId { get; init; }
    public DateTime CreatedUtc { get; init; }
    public string ManifestHash { get; init; } = null!;
    public string RuleSetId { get; init; } = null!;
    public string RuleSetVersion { get; init; } = null!;
    public string RuleSetHash { get; init; } = null!;
    public string MetadataJson { get; init; } = null!;
    public string RequirementsJson { get; init; } = null!;
    public string TopologyJson { get; init; } = null!;
    public string SecurityJson { get; init; } = null!;
    public string? ComplianceJson { get; init; }
    public string CostJson { get; init; } = null!;
    public string ConstraintsJson { get; init; } = null!;
    public string UnresolvedIssuesJson { get; init; } = null!;
    public string DecisionsJson { get; init; } = null!;
    public string AssumptionsJson { get; init; } = null!;
    public string WarningsJson { get; init; } = null!;
    public string ProvenanceJson { get; init; } = null!;

    /// <summary>Optional pointer to a JSON blob mirroring the manifest section columns (see <c>034_LargeArtifactBlobPointers</c>).</summary>
    public string? ManifestPayloadBlobUri { get; init; }
}

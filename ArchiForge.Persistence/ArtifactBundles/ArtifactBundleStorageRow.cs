using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.ArtifactBundles;

/// <summary>Dapper projection for <c>dbo.ArtifactBundles</c> including legacy JSON columns.</summary>
[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class ArtifactBundleStorageRow
{
    public Guid TenantId { get; init; }

    public Guid WorkspaceId { get; init; }

    public Guid ProjectId { get; init; }

    public Guid BundleId { get; init; }

    public Guid RunId { get; init; }

    public Guid ManifestId { get; init; }

    public DateTime CreatedUtc { get; init; }

    public string ArtifactsJson { get; init; } = null!;

    public string TraceJson { get; init; } = null!;
}

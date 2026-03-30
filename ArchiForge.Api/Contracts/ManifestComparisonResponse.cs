namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON contract for <see cref="ArchiForge.Persistence.Compare.ManifestComparisonResult"/>.
/// </summary>
public class ManifestComparisonResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.LeftManifestId"/>
    public Guid LeftManifestId { get; set; }
    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.RightManifestId"/>
    public Guid RightManifestId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.LeftManifestHash"/>
    public string LeftManifestHash { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.RightManifestHash"/>
    public string RightManifestHash { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.AddedCount"/>
    public int AddedCount { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.RemovedCount"/>
    public int RemovedCount { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.ChangedCount"/>
    public int ChangedCount { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.ManifestComparisonResult.Diffs"/>
    public List<DiffItemResponse> Diffs { get; set; } = [];

    /// <summary>Count of <see cref="Diffs"/>.</summary>
    public int DiffCount { get; set; }
}

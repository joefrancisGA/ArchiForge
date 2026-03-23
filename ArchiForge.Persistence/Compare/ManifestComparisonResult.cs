namespace ArchiForge.Persistence.Compare;

/// <summary>
/// Outcome of <see cref="IAuthorityCompareService.CompareManifestsAsync"/>: identity, hashes, and a unified diff list.
/// </summary>
/// <remarks>HTTP projection: <c>ArchiForge.Api.Contracts.ManifestComparisonResponse</c>.</remarks>
public class ManifestComparisonResult
{
    public Guid LeftManifestId
    {
        get; set;
    }

    public Guid RightManifestId
    {
        get; set;
    }

    public string LeftManifestHash { get; set; } = null!;
    public string RightManifestHash { get; set; } = null!;

    /// <summary>Flat diff items (<see cref="DiffKind"/> string values on each <see cref="DiffItem"/>).</summary>
    public List<DiffItem> Diffs { get; set; } = [];

    /// <summary>Count of items where <see cref="DiffItem.DiffKind"/> equals <see cref="DiffKind.Added"/>.</summary>
    public int AddedCount => Diffs.Count(x => x.DiffKind == DiffKind.Added);

    /// <summary>Count of items where <see cref="DiffItem.DiffKind"/> equals <see cref="DiffKind.Removed"/>.</summary>
    public int RemovedCount => Diffs.Count(x => x.DiffKind == DiffKind.Removed);

    /// <summary>Count of items where <see cref="DiffItem.DiffKind"/> equals <see cref="DiffKind.Changed"/>.</summary>
    public int ChangedCount => Diffs.Count(x => x.DiffKind == DiffKind.Changed);
}

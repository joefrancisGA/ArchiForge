namespace ArchLucid.Persistence.Coordination.Compare;

/// <summary>
///     Outcome of <see cref="IAuthorityCompareService.CompareManifestsAsync" />: identity, hashes, and a unified diff
///     list.
/// </summary>
/// <remarks>HTTP projection: <c>ArchLucid.Api.Contracts.ManifestComparisonResponse</c>.</remarks>
public class ManifestComparisonResult
{
    /// <summary>Id of the baseline (left) golden manifest.</summary>
    public Guid LeftManifestId
    {
        get;
        set;
    }

    /// <summary>Id of the target (right) golden manifest.</summary>
    public Guid RightManifestId
    {
        get;
        set;
    }

    /// <summary>Content hash of the baseline manifest (for quick equality check).</summary>
    public string LeftManifestHash
    {
        get;
        set;
    } = null!;

    /// <summary>Content hash of the target manifest.</summary>
    public string RightManifestHash
    {
        get;
        set;
    } = null!;

    /// <summary>Flat diff items (<see cref="DiffKind" /> string values on each <see cref="DiffItem" />).</summary>
    public List<DiffItem> Diffs
    {
        get;
        set;
    } = [];

    /// <summary>Count of items where <see cref="DiffItem.DiffKind" /> equals <see cref="DiffKind.Added" />.</summary>
    public int AddedCount => Diffs.Count(x => x.DiffKind == DiffKind.Added);

    /// <summary>Count of items where <see cref="DiffItem.DiffKind" /> equals <see cref="DiffKind.Removed" />.</summary>
    public int RemovedCount => Diffs.Count(x => x.DiffKind == DiffKind.Removed);

    /// <summary>Count of items where <see cref="DiffItem.DiffKind" /> equals <see cref="DiffKind.Changed" />.</summary>
    public int ChangedCount => Diffs.Count(x => x.DiffKind == DiffKind.Changed);
}

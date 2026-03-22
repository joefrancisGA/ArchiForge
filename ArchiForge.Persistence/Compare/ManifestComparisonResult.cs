namespace ArchiForge.Persistence.Compare;

public class ManifestComparisonResult
{
    public Guid LeftManifestId { get; set; }
    public Guid RightManifestId { get; set; }

    public string LeftManifestHash { get; set; } = null!;
    public string RightManifestHash { get; set; } = null!;

    public List<DiffItem> Diffs { get; set; } = new();

    public int AddedCount => Diffs.Count(x => x.DiffKind == DiffKind.Added);
    public int RemovedCount => Diffs.Count(x => x.DiffKind == DiffKind.Removed);
    public int ChangedCount => Diffs.Count(x => x.DiffKind == DiffKind.Changed);
}

namespace ArchiForge.Api.Contracts;

public class ManifestComparisonResponse
{
    public Guid LeftManifestId { get; set; }
    public Guid RightManifestId { get; set; }
    public string LeftManifestHash { get; set; } = null!;
    public string RightManifestHash { get; set; } = null!;
    public int AddedCount { get; set; }
    public int RemovedCount { get; set; }
    public int ChangedCount { get; set; }
    public List<DiffItemResponse> Diffs { get; set; } = [];
}

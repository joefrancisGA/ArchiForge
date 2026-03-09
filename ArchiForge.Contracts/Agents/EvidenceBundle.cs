namespace ArchiForge.Contracts.Agents;

public sealed class EvidenceBundle
{
    public string EvidenceBundleId { get; set; } = Guid.NewGuid().ToString("N");

    public string RequestDescription { get; set; } = string.Empty;

    public List<string> PolicyRefs { get; set; } = [];

    public List<string> ServiceCatalogRefs { get; set; } = [];

    public List<string> PriorManifestRefs { get; set; } = [];

    public Dictionary<string, string> Metadata { get; set; } = new();
}
namespace ArchiForge.ArtifactSynthesis.Models;

public class ArtifactBundle
{
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid BundleId { get; set; }
    public Guid RunId { get; set; }
    public Guid ManifestId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<SynthesizedArtifact> Artifacts { get; set; } = [];
    public SynthesisTrace Trace { get; set; } = new();
}

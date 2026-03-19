namespace ArchiForge.ArtifactSynthesis.Models;

public class ArtifactBundle
{
    public Guid BundleId { get; set; }
    public Guid RunId { get; set; }
    public Guid ManifestId { get; set; }
    public DateTime CreatedUtc { get; set; }

    public List<SynthesizedArtifact> Artifacts { get; set; } = new();
    public SynthesisTrace Trace { get; set; } = new();
}

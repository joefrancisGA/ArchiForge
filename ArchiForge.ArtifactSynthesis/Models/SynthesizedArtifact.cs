namespace ArchiForge.ArtifactSynthesis.Models;

public class SynthesizedArtifact
{
    public Guid ArtifactId { get; set; }
    public Guid RunId { get; set; }
    public Guid ManifestId { get; set; }
    public DateTime CreatedUtc { get; set; }

    public string ArtifactType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Format { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string ContentHash { get; set; } = null!;

    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>Architecture decisions that contributed to this artifact (provenance / explainability).</summary>
    public List<string> ContributingDecisionIds { get; set; } = [];
}

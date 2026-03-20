namespace ArchiForge.ArtifactSynthesis.Models;

public class SynthesizedArtifact
{
    public Guid ArtifactId { get; set; }
    public Guid RunId { get; set; }
    public Guid ManifestId { get; set; }
    public DateTime CreatedUtc { get; set; }

    public string ArtifactType { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Format { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string ContentHash { get; set; } = default!;

    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>Architecture decisions that contributed to this artifact (provenance / explainability).</summary>
    public List<string> ContributingDecisionIds { get; set; } = [];
}

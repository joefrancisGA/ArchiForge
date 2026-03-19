namespace ArchiForge.ArtifactSynthesis.Models;

public class SynthesisTrace
{
    public Guid TraceId { get; set; }
    public Guid RunId { get; set; }
    public Guid ManifestId { get; set; }
    public DateTime CreatedUtc { get; set; }

    public List<string> GeneratorsUsed { get; set; } = new();
    public List<string> SourceDecisionIds { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

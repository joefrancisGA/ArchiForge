namespace ArchiForge.ContextIngestion.Models;

public class NormalizedContextBatch
{
    public List<CanonicalObject> CanonicalObjects { get; set; } = [];

    /// <summary>Non-fatal issues during normalization (e.g. unsupported document content type).</summary>
    public List<string> Warnings { get; set; } = [];
}


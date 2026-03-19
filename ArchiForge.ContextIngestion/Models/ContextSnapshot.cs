namespace ArchiForge.ContextIngestion.Models;

public class ContextSnapshot
{
    public Guid SnapshotId { get; set; }

    public Guid RunId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public List<CanonicalObject> CanonicalObjects { get; set; } = [];

    public string? DeltaSummary { get; set; }

    public List<string> Warnings { get; set; } = [];

    public List<string> Errors { get; set; } = [];

    public Dictionary<string, string> SourceHashes { get; set; } = new();
}


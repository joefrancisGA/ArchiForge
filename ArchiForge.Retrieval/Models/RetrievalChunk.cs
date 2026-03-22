namespace ArchiForge.Retrieval.Models;

public class RetrievalChunk
{
    public string ChunkId { get; set; } = default!;
    public string DocumentId { get; set; } = default!;

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ManifestId { get; set; }

    public string SourceType { get; set; } = default!;
    public string SourceId { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public int ChunkOrdinal { get; set; }

    public float[] Embedding { get; set; } = Array.Empty<float>();

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

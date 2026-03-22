namespace ArchiForge.Retrieval.Models;

public class RetrievalChunk
{
    public string ChunkId { get; set; } = null!;
    public string DocumentId { get; set; } = null!;

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ManifestId { get; set; }

    public string SourceType { get; set; } = null!;
    public string SourceId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Text { get; set; } = null!;
    public int ChunkOrdinal { get; set; }

    public float[] Embedding { get; set; } = [];

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

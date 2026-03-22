namespace ArchiForge.Retrieval.Models;

public class RetrievalDocument
{
    public string DocumentId { get; set; } = null!;

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ManifestId { get; set; }

    public string SourceType { get; set; } = null!;
    public string SourceId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string ContentHash { get; set; } = null!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

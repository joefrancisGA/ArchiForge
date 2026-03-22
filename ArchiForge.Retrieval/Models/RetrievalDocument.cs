namespace ArchiForge.Retrieval.Models;

public class RetrievalDocument
{
    public string DocumentId { get; set; } = default!;

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ManifestId { get; set; }

    public string SourceType { get; set; } = default!;
    public string SourceId { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string ContentHash { get; set; } = default!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

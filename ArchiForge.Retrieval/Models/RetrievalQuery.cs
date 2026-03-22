namespace ArchiForge.Retrieval.Models;

public class RetrievalQuery
{
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ManifestId { get; set; }

    public string QueryText { get; set; } = null!;
    public int TopK { get; set; } = 8;
}

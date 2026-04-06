namespace ArchiForge.ContextIngestion.Models;

public class ContextDocumentReference
{
    public string DocumentId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = null!;
    public string ContentType { get; set; } = "text/plain";
    public string Content { get; set; } = null!;
}

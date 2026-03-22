namespace ArchiForge.Contracts.Requests;

public class ContextDocumentRequest
{
    public string Name { get; set; } = null!;
    public string ContentType { get; set; } = "text/plain";
    public string Content { get; set; } = null!;
}

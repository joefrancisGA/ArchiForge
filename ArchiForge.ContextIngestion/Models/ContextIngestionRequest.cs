namespace ArchiForge.ContextIngestion.Models;

public class ContextIngestionRequest
{
    public Guid RunId { get; set; }

    public string ProjectId { get; set; } = null!;

    public string? Description { get; set; }
}


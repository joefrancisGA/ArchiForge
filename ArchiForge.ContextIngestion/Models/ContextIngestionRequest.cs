namespace ArchiForge.ContextIngestion.Models;

public class ContextIngestionRequest
{
    public Guid RunId { get; set; }
    public string ProjectId { get; set; } = null!;
    public string? Description { get; set; }

    public List<string> InlineRequirements { get; set; } = new();
    public List<ContextDocumentReference> Documents { get; set; } = new();
    public List<string> PolicyReferences { get; set; } = new();
    public List<string> TopologyHints { get; set; } = new();
    public List<string> SecurityBaselineHints { get; set; } = new();

    public List<InfrastructureDeclarationReference> InfrastructureDeclarations { get; set; } = new();
}

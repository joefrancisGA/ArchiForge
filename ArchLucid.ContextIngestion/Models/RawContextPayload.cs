namespace ArchiForge.ContextIngestion.Models;

public class RawContextPayload
{
    public string? Description { get; set; }
    public List<string> InlineRequirements { get; set; } = [];
    public List<ContextDocumentReference> Documents { get; set; } = [];
    public List<string> PolicyReferences { get; set; } = [];
    public List<string> TopologyHints { get; set; } = [];
    public List<string> SecurityBaselineHints { get; set; } = [];
    public List<InfrastructureDeclarationReference> InfrastructureDeclarations { get; set; } = [];
}

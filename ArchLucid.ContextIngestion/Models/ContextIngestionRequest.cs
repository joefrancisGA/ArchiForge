namespace ArchLucid.ContextIngestion.Models;

public class ContextIngestionRequest
{
    public Guid RunId { get; set; }

    /// <summary>Optional correlation to <c>ArchitectureRequests.RequestId</c> when the run originated from an API request.</summary>
    public string? ArchitectureRequestId { get; set; }

    public string ProjectId { get; set; } = null!;
    public string? Description { get; set; }
    public List<string> InlineRequirements { get; set; } = [];
    public List<ContextDocumentReference> Documents { get; set; } = [];
    public List<string> PolicyReferences { get; set; } = [];
    public List<string> TopologyHints { get; set; } = [];
    public List<string> SecurityBaselineHints { get; set; } = [];
    public List<InfrastructureDeclarationReference> InfrastructureDeclarations { get; set; } = [];
}

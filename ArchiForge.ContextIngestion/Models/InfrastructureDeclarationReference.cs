namespace ArchiForge.ContextIngestion.Models;

public class InfrastructureDeclarationReference
{
    public string DeclarationId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = null!;
    /// <summary>Supported v1 values: <c>json</c>, <c>simple-terraform</c>.</summary>
    public string Format { get; set; } = "json";
    public string Content { get; set; } = null!;
}

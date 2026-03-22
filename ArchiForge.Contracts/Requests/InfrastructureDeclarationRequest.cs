namespace ArchiForge.Contracts.Requests;

public class InfrastructureDeclarationRequest
{
    public string Name { get; set; } = null!;
    /// <summary>Supported v1: <c>json</c>, <c>simple-terraform</c>.</summary>
    public string Format { get; set; } = "json";
    public string Content { get; set; } = null!;
}

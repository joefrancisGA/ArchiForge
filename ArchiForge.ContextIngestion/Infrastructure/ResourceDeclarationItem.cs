namespace ArchiForge.ContextIngestion.Infrastructure;

public class ResourceDeclarationItem
{
    public string Type { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Subtype { get; set; }
    public string? Region { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

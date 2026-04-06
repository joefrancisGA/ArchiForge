namespace ArchiForge.ContextIngestion.Models;

public class CanonicalObject
{
    public string ObjectId { get; set; } = Guid.NewGuid().ToString("N");
    public string ObjectType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string SourceType { get; set; } = null!;
    public string SourceId { get; set; } = null!;
    public Dictionary<string, string> Properties { get; set; } = [];
}

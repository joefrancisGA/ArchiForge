namespace ArchiForge.Api.Models;

public sealed class DiagramResponse
{
    public string ManifestVersion { get; set; } = string.Empty;

    public string Format { get; set; } = "mermaid";

    public string Diagram { get; set; } = string.Empty;
}

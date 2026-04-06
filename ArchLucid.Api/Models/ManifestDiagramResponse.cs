using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ManifestDiagramResponse
{
    public string ManifestVersion { get; set; } = string.Empty;
    public string DiagramType { get; set; } = "Mermaid";
    public string Content { get; set; } = string.Empty;
}


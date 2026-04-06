using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class DiagramResponse
{
    public string ManifestVersion { get; set; } = string.Empty;
    public string Format { get; set; } = "mermaid";
    public string Diagram { get; set; } = string.Empty;
}

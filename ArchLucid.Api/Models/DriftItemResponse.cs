using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class DriftItemResponse
{
    public string Category { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? StoredValue { get; set; }
    public string? RegeneratedValue { get; set; }
    public string Description { get; set; } = string.Empty;
}

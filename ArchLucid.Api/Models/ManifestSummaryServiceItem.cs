using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

/// <summary>Service entry within a <see cref="ManifestSummaryJsonResponse"/>.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ManifestSummaryServiceItem
{
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string RuntimePlatform { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public List<string> RequiredControls { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Api.Models;

/// <summary>
/// Response returned by the manifest bundle endpoint, containing the manifest, its Mermaid diagram, and a Markdown summary.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ManifestBundleResponse
{
    /// <summary>The manifest version identifier.</summary>
    public string ManifestVersion { get; set; } = string.Empty;

    /// <summary>The full golden manifest.</summary>
    public GoldenManifest Manifest { get; set; } = new();

    /// <summary>Mermaid diagram source generated from the manifest.</summary>
    public string Diagram { get; set; } = string.Empty;

    /// <summary>Markdown summary generated from the manifest.</summary>
    public string Summary { get; set; } = string.Empty;
}

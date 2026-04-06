using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

/// <summary>
/// Response returned by the manifest export endpoint, containing the rendered export content.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ManifestExportContentResponse
{
    /// <summary>The manifest version identifier.</summary>
    public string ManifestVersion { get; set; } = string.Empty;

    /// <summary>Format of the exported content (e.g. <c>markdown</c>).</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>The exported content.</summary>
    public string Content { get; set; } = string.Empty;
}

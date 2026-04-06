using System.Diagnostics.CodeAnalysis;

using ArchiForge.Application.Diffs;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ManifestCompareSummaryResponse
{
    public string LeftManifestVersion { get; set; } = string.Empty;
    public string RightManifestVersion { get; set; } = string.Empty;
    public string Format { get; set; } = "markdown";
    public string Summary { get; set; } = string.Empty;
    public ManifestDiffResult Diff { get; set; } = new();
}

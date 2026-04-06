using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Summaries;

/// <summary>
/// Generates a structured Markdown summary of a <see cref="GoldenManifest"/> using configurable
/// <see cref="ManifestSummaryOptions"/>. Intended for API responses and display surfaces.
/// </summary>
public interface IManifestSummaryService
{
    /// <summary>
    /// Generates a Markdown summary for <paramref name="manifest"/> according to
    /// <paramref name="options"/>. Uses <see cref="ManifestSummaryOptions.Default"/> when options are <c>null</c>.
    /// </summary>
    string GenerateMarkdown(GoldenManifest manifest, ManifestSummaryOptions? options = null);
}


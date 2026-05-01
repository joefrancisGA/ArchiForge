using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Diffs;

/// <summary>
///     Formats a pre-computed <see cref="ManifestDiffResult" /> together with the two source
///     manifests and a markdown summary into a shareable Markdown export document.
/// </summary>
public interface IManifestDiffExportService
{
    /// <summary>
    ///     Generates a complete Markdown export document for the manifest diff.
    /// </summary>
    /// <param name="left">Baseline manifest (used for contextual headers and metadata).</param>
    /// <param name="right">Comparison manifest.</param>
    /// <param name="diff">Pre-computed diff result from <see cref="IManifestDiffService" />.</param>
    /// <param name="markdownSummary">Human-readable summary paragraph to embed at the top of the document.</param>
    /// <returns>A Markdown-formatted string suitable for download or embedding.</returns>
    string GenerateMarkdownExport(
        GoldenManifest left,
        GoldenManifest right,
        ManifestDiffResult diff,
        string markdownSummary);
}

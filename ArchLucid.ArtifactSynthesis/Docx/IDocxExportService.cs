using ArchiForge.ArtifactSynthesis.Docx.Models;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Docx;

/// <summary>
/// Builds a Word OpenXML package from a golden manifest, synthesized artifacts, and optional comparison/explanation payloads.
/// </summary>
/// <remarks>Default implementation: <see cref="DocxExportService"/>. Used by <c>ArchiForge.Api.Controllers.DocxExportController</c>.</remarks>
public interface IDocxExportService
{
    /// <summary>
    /// Generates an improvement plan (with or without comparison), fills the DOCX template, and returns file bytes.
    /// </summary>
    /// <param name="request">Titles, flags, optional comparison/explanation/findings.</param>
    /// <param name="manifest">Primary manifest body.</param>
    /// <param name="artifacts">Appendix source list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File name, content type, and byte content.</returns>
    Task<DocxExportResult> ExportAsync(
        DocxExportRequest request,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        CancellationToken ct);
}

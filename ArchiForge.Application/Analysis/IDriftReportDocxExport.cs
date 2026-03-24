namespace ArchiForge.Application.Analysis;

/// <summary>
/// Generates a binary Docx representation of a <see cref="DriftAnalysisResult"/>.
/// </summary>
public interface IDriftReportDocxExport
{
    /// <summary>
    /// Produces a Docx document byte array for the supplied <paramref name="drift"/> result.
    /// </summary>
    /// <param name="drift">The drift analysis result to render.</param>
    /// <param name="comparisonRecordId">
    /// Optional comparison-record identifier included in the report header for traceability.
    /// </param>
    /// <returns>The raw Docx file bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="drift"/> is <see langword="null"/>.
    /// </exception>
    byte[] GenerateDocx(DriftAnalysisResult drift, string? comparisonRecordId = null);
}

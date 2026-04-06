namespace ArchiForge.Application.Analysis;

/// <summary>
/// Formats a <see cref="DriftAnalysisResult"/> (from a comparison drift analysis) into human-readable
/// output for display in API responses, reports, or emails.
/// </summary>
public interface IDriftReportFormatter
{
    /// <summary>
    /// Returns a Markdown report of the drift result.
    /// When <paramref name="comparisonRecordId"/> is provided it is included in the heading for traceability.
    /// </summary>
    string FormatMarkdown(DriftAnalysisResult drift, string? comparisonRecordId = null);

    /// <summary>
    /// Returns an HTML report of the drift result.
    /// When <paramref name="comparisonRecordId"/> is provided it is included in the page heading.
    /// </summary>
    string FormatHtml(DriftAnalysisResult drift, string? comparisonRecordId = null);
}

namespace ArchLucid.Application.Analysis;

/// <summary>
///     Formats an <see cref="ExportRecordDiffResult" /> as a human-readable Markdown summary.
/// </summary>
public interface IExportRecordDiffSummaryFormatter
{
    /// <summary>
    ///     Returns a Markdown summary of the top-level field changes and request-option diffs in <paramref name="diff" />
    ///     .
    /// </summary>
    string FormatMarkdown(ExportRecordDiffResult diff);
}

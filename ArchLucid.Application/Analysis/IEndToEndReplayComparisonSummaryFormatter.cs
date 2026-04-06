namespace ArchiForge.Application.Analysis;

/// <summary>
/// Formats an <see cref="EndToEndReplayComparisonReport"/> as a concise Markdown summary
/// for embedding in larger export documents or API responses.
/// </summary>
public interface IEndToEndReplayComparisonSummaryFormatter
{
    /// <summary>Returns a Markdown summary of <paramref name="report"/> covering run metadata, agent, manifest, and export diffs.</summary>
    string FormatMarkdown(EndToEndReplayComparisonReport report);
}


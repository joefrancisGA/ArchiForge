using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Summaries;

/// <summary>
/// Enriches per-connector delta text with normalized object counts and optional baseline vs prior snapshot hints.
/// </summary>
public interface IContextDeltaSummaryBuilder
{
    /// <param name="previous"></param>
    /// <param name="isFirstConnector">When true, prior-snapshot context is included once for the pipeline.</param>
    /// <param name="connectorType"></param>
    /// <param name="baseSummary"></param>
    /// <param name="batch"></param>
    string BuildSegment(
        string connectorType,
        string baseSummary,
        NormalizedContextBatch batch,
        ContextSnapshot? previous,
        bool isFirstConnector);
}

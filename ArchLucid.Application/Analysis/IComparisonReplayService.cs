namespace ArchLucid.Application.Analysis;

/// <summary>
///     Replays persisted comparison records, regenerating export artifacts or performing drift analysis
///     without requiring callers to rebuild the comparison from scratch.
/// </summary>
public interface IComparisonReplayService
{
    /// <summary>
    ///     Replays the comparison record identified by <see cref="ReplayComparisonRequest.ComparisonRecordId" />,
    ///     returning an exportable artifact in the requested format and replay mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the record does not exist, its payload cannot be rehydrated, or the format/mode is unsupported.
    /// </exception>
    Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the comparison record, rehydrates the stored payload, regenerates it, and returns drift analysis.</summary>
    Task<DriftAnalysisResult> AnalyzeDriftAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default);
}

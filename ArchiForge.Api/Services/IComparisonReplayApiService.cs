using ArchiForge.Application.Analysis;

namespace ArchiForge.Api.Services;

/// <summary>
/// API-layer contract for comparison replay and drift analysis, wrapping the core
/// <see cref="IComparisonReplayService"/> with diagnostics recording and structured logging.
/// </summary>
public interface IComparisonReplayApiService
{
    /// <summary>
    /// Replays a comparison record, recording timing diagnostics and logging the outcome.
    /// </summary>
    /// <param name="request">Replay configuration (comparison record id, format, mode, persist flag).</param>
    /// <param name="metadataOnly">
    /// When <see langword="true"/>, only metadata is returned and artifact re-generation is skipped.
    /// </param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The replay result from the underlying service.</returns>
    /// <exception cref="InvalidOperationException">Propagated when the comparison record is invalid.</exception>
    /// <exception cref="RunNotFoundException">Propagated when a referenced run cannot be found.</exception>
    Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        bool metadataOnly,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs drift analysis for an existing comparison record.
    /// </summary>
    /// <param name="comparisonRecordId">Identifier of the comparison record to analyse.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The drift analysis result.</returns>
    Task<DriftAnalysisResult> AnalyzeDriftAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default);
}


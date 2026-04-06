namespace ArchiForge.Application.Analysis;

/// <summary>
/// Builds a side-by-side end-to-end replay comparison report for two architecture runs.
/// Both runs are loaded through the canonical <c>IRunDetailQueryService</c> path so the
/// report reflects the same authoritative run state as all other consumers.
/// </summary>
public interface IEndToEndReplayComparisonService
{
    /// <summary>
    /// Builds an <see cref="EndToEndReplayComparisonReport"/> that compares the full
    /// replay output of <paramref name="leftRunId"/> against <paramref name="rightRunId"/>.
    /// </summary>
    /// <param name="leftRunId">The identifier of the baseline (left) run.</param>
    /// <param name="rightRunId">The identifier of the comparison (right) run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A report containing manifest diffs, agent result deltas, and any verification findings.</returns>
    /// <exception cref="RunNotFoundException">
    /// Thrown when either <paramref name="leftRunId"/> or <paramref name="rightRunId"/> does not exist.
    /// </exception>
    Task<EndToEndReplayComparisonReport> BuildAsync(
        string leftRunId,
        string rightRunId,
        CancellationToken cancellationToken = default);
}

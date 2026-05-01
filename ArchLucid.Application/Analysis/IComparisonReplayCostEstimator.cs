namespace ArchLucid.Application.Analysis;

/// <summary>
///     Heuristic replay cost / effort estimate for operators (does not execute a replay).
/// </summary>
public interface IComparisonReplayCostEstimator
{
    /// <summary>
    ///     Returns <see langword="null" /> when the comparison record does not exist.
    /// </summary>
    Task<ComparisonReplayCostEstimate?> TryEstimateAsync(
        string comparisonRecordId,
        string? format,
        string? replayMode,
        bool persistReplay,
        CancellationToken ct);
}

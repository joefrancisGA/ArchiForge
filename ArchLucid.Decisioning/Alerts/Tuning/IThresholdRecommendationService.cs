namespace ArchLucid.Decisioning.Alerts.Tuning;

/// <summary>
///     Suggests a threshold (simple or composite) by simulating each candidate and scoring outcomes with
///     <see cref="IAlertNoiseScorer" />.
/// </summary>
/// <remarks>
///     Implemented by <see cref="ThresholdRecommendationService" />. HTTP: <c>AlertTuningController</c>.
/// </remarks>
public interface IThresholdRecommendationService
{
    /// <summary>
    ///     Runs one simulation per distinct candidate threshold, scores with noise heuristics, and sets
    ///     <see cref="ThresholdRecommendationResult.RecommendedCandidate" /> to the best row.
    /// </summary>
    Task<ThresholdRecommendationResult> RecommendAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        ThresholdRecommendationRequest request,
        CancellationToken ct);
}

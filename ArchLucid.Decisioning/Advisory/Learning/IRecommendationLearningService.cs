namespace ArchLucid.Decisioning.Advisory.Learning;

/// <summary>
///     Builds and retrieves <see cref="RecommendationLearningProfile" /> snapshots from historical recommendation rows in
///     a scope.
/// </summary>
/// <remarks>
///     Implemented by <c>ArchLucid.Application.Advisory.RecommendationLearningService</c>. HTTP:
///     <c>RecommendationLearningController</c>.
/// </remarks>
public interface IRecommendationLearningService
{
    /// <summary>
    ///     Loads recent recommendations, runs <c>IRecommendationLearningAnalyzer</c>, persists the profile, and returns it.
    /// </summary>
    Task<RecommendationLearningProfile> RebuildProfileAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>Returns the latest stored profile, or <c>null</c> if none.</summary>
    Task<RecommendationLearningProfile?> GetLatestProfileAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}

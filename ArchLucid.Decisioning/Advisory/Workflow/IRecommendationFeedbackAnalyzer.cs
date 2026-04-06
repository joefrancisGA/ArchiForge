using JetBrains.Annotations;

namespace ArchiForge.Decisioning.Advisory.Workflow;

/// <summary>
/// Aggregates persisted recommendation rows into coarse counts for learning and dashboards.
/// </summary>
/// <remarks>
/// Implemented by <c>ArchiForge.Persistence.Advisory.RecommendationFeedbackAnalyzer</c>. Used by <see cref="ArchiForge.Decisioning.Advisory.Learning.IRecommendationLearningAnalyzer"/> when building profiles.
/// </remarks>
public interface IRecommendationFeedbackAnalyzer
{
    /// <summary>
    /// Returns counts keyed by <c>{Category}:{Status}</c> (e.g. <c>Security:Accepted</c>) for up to 1000 recent rows in the scope.
    /// </summary>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="workspaceId">Workspace scope.</param>
    /// <param name="projectId">Project scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary of composite key to count; empty when no rows.</returns>
    [UsedImplicitly]
    Task<IReadOnlyDictionary<string, int>> GetStatusCountsByCategoryAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}

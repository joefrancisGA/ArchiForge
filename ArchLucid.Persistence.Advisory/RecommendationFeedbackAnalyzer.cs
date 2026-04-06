using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IRecommendationFeedbackAnalyzer" />
public sealed class RecommendationFeedbackAnalyzer(IRecommendationRepository repository) : IRecommendationFeedbackAnalyzer
{
    /// <summary>
    /// Maximum number of recommendation rows loaded for feedback aggregation.
    /// Truncation keeps analytics queries fast; rows beyond this cap are excluded from counts.
    /// </summary>
    private const int AnalyticsBatchCap = 1000;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetStatusCountsByCategoryAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        IReadOnlyList<RecommendationRecord> items = await repository
            .ListByScopeAsync(tenantId, workspaceId, projectId, null, AnalyticsBatchCap, ct)
            ;

        return items
            .GroupBy(x => (x.Category, x.Status))
            .ToDictionary(
                g => $"{g.Key.Category}:{g.Key.Status}",
                g => g.Count());
    }
}

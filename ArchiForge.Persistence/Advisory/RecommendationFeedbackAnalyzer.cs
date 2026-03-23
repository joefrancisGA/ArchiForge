using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IRecommendationFeedbackAnalyzer" />
public sealed class RecommendationFeedbackAnalyzer(IRecommendationRepository repository) : IRecommendationFeedbackAnalyzer
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetStatusCountsByCategoryAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        var items = await repository
            .ListByScopeAsync(tenantId, workspaceId, projectId, null, 1000, ct)
            .ConfigureAwait(false);

        return items
            .GroupBy(x => $"{x.Category}:{x.Status}")
            .ToDictionary(g => g.Key, g => g.Count());
    }
}

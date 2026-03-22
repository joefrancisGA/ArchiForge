namespace ArchiForge.Decisioning.Advisory.Workflow;

public interface IRecommendationFeedbackAnalyzer
{
    Task<IReadOnlyDictionary<string, int>> GetStatusCountsByCategoryAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}

using ArchiForge.Decisioning.Advisory.Models;

namespace ArchiForge.Decisioning.Advisory.Workflow;

public interface IRecommendationWorkflowService
{
    Task PersistPlanAsync(
        ImprovementPlan plan,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    Task<RecommendationRecord?> ApplyActionAsync(
        Guid recommendationId,
        string userId,
        string userName,
        RecommendationActionRequest request,
        CancellationToken ct);
}

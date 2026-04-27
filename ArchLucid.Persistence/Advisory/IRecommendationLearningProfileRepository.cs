using ArchLucid.Decisioning.Advisory.Learning;

namespace ArchLucid.Persistence;

public interface IRecommendationLearningProfileRepository
{
    Task SaveAsync(RecommendationLearningProfile profile, CancellationToken ct);

    Task<RecommendationLearningProfile?> GetLatestAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}

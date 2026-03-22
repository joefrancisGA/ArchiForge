using ArchiForge.Decisioning.Advisory.Learning;

namespace ArchiForge.Persistence.Advisory;

public sealed class InMemoryRecommendationLearningProfileRepository : IRecommendationLearningProfileRepository
{
    private readonly List<RecommendationLearningProfile> _profiles = [];
    private readonly Lock _gate = new();

    public Task SaveAsync(RecommendationLearningProfile profile, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _profiles.Add(profile);

        return Task.CompletedTask;
    }

    public Task<RecommendationLearningProfile?> GetLatestAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _profiles
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId)
                .OrderByDescending(x => x.GeneratedUtc)
                .FirstOrDefault();

            return Task.FromResult(result);
        }
    }
}

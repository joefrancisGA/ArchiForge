using ArchiForge.Decisioning.Advisory.Learning;

namespace ArchiForge.Persistence.Advisory;

public sealed class InMemoryRecommendationLearningProfileRepository : IRecommendationLearningProfileRepository
{
    private const int MaxEntries = 500;
    private readonly List<RecommendationLearningProfile> _profiles = [];
    private readonly Lock _gate = new();

    public Task SaveAsync(RecommendationLearningProfile profile, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _profiles.Add(profile);
            if (_profiles.Count > MaxEntries)
                _profiles.RemoveRange(0, _profiles.Count - MaxEntries);
        }

        return Task.CompletedTask;
    }

    public Task<RecommendationLearningProfile?> GetLatestAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            RecommendationLearningProfile? result = _profiles
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

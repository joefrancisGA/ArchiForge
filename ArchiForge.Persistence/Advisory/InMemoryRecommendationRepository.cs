using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Persistence.Advisory;

public sealed class InMemoryRecommendationRepository : IRecommendationRepository
{
    private readonly List<RecommendationRecord> _items = [];
    private readonly object _gate = new();

    public Task UpsertAsync(RecommendationRecord recommendation, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            _items.RemoveAll(x => x.RecommendationId == recommendation.RecommendationId);
            _items.Add(recommendation);
        }

        return Task.CompletedTask;
    }

    public Task<RecommendationRecord?> GetByIdAsync(Guid recommendationId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            return Task.FromResult(_items.FirstOrDefault(x => x.RecommendationId == recommendationId));
        }
    }

    public Task<IReadOnlyList<RecommendationRecord>> ListByRunAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId &&
                    x.RunId == runId)
                .OrderByDescending(x => x.PriorityScore)
                .ThenByDescending(x => x.CreatedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<RecommendationRecord>>(result);
        }
    }

    public Task<IReadOnlyList<RecommendationRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId &&
                    (status == null || x.Status == status))
                .OrderByDescending(x => x.LastUpdatedUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<RecommendationRecord>>(result);
        }
    }
}

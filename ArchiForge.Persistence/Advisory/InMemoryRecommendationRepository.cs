using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IRecommendationRepository" />
public sealed class InMemoryRecommendationRepository : IRecommendationRepository
{
    private const int MaxEntries = 5_000;

    private readonly List<RecommendationRecord> _items = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task UpsertAsync(RecommendationRecord recommendation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _items.RemoveAll(x => x.RecommendationId == recommendation.RecommendationId);
            if (_items.Count >= MaxEntries)
                _items.RemoveAt(0);

            _items.Add(recommendation);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RecommendationRecord?> GetByIdAsync(Guid recommendationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_items.FirstOrDefault(x => x.RecommendationId == recommendationId));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RecommendationRecord>> ListByRunAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<RecommendationRecord> result = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId &&
                    x.RunId == runId)
                .OrderByDescending(x => x.PriorityScore)
                .ThenByDescending(x => x.CreatedUtc)
                .Take(500)
                .ToList();

            return Task.FromResult<IReadOnlyList<RecommendationRecord>>(result);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RecommendationRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int n = Math.Clamp(take <= 0 ? 50 : take, 1, 500);
        lock (_gate)
        {
            List<RecommendationRecord> result = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId &&
                    (status == null || x.Status == status))
                .OrderByDescending(x => x.LastUpdatedUtc)
                .Take(n)
                .ToList();

            return Task.FromResult<IReadOnlyList<RecommendationRecord>>(result);
        }
    }
}

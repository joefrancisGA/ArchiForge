using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Governance;

/// <summary>
/// In-memory implementation of <see cref="IPolicyPackRepository"/> for testing and storage-off mode.
/// Capped at <see cref="MaxEntries"/> packs; oldest entry is evicted on each insert when the cap is reached.
/// All operations are thread-safe via an exclusive lock.
/// </summary>
public sealed class InMemoryPolicyPackRepository : IPolicyPackRepository
{
    private const int MaxEntries = 2_000;
    private const int ListScopeCap = 500;

    private readonly List<PolicyPack> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(PolicyPack pack, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_items.Count >= MaxEntries)
                _items.RemoveAt(0);

            _items.Add(pack);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(PolicyPack pack, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            int i = _items.FindIndex(x => x.PolicyPackId == pack.PolicyPackId);
            if (i >= 0)
                _items[i] = pack;
        }

        return Task.CompletedTask;
    }

    public Task<PolicyPack?> GetByIdAsync(Guid policyPackId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.PolicyPackId == policyPackId));
    }

    public Task<IReadOnlyList<PolicyPack>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<PolicyPack> result = _items
                .Where(x => x.TenantId == tenantId && x.WorkspaceId == workspaceId && x.ProjectId == projectId)
                .OrderByDescending(x => x.CreatedUtc)
                .Take(ListScopeCap)
                .ToList();
            return Task.FromResult<IReadOnlyList<PolicyPack>>(result);
        }
    }
}

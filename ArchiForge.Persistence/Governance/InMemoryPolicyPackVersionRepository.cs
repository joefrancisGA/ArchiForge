using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Governance;

public sealed class InMemoryPolicyPackVersionRepository : IPolicyPackVersionRepository
{
    private readonly List<PolicyPackVersion> _items = [];
    private readonly object _gate = new();

    public Task CreateAsync(PolicyPackVersion version, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(version);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PolicyPackVersion version, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var idx = _items.FindIndex(x => x.PolicyPackVersionId == version.PolicyPackVersionId);
            if (idx >= 0)
                _items[idx] = version;
        }

        return Task.CompletedTask;
    }

    public Task<PolicyPackVersion?> GetByPackAndVersionAsync(
        Guid policyPackId,
        string version,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var row = _items.FirstOrDefault(
                x => x.PolicyPackId == policyPackId &&
                     string.Equals(x.Version, version, StringComparison.Ordinal));
            return Task.FromResult(row);
        }
    }

    public Task<IReadOnlyList<PolicyPackVersion>> ListByPackAsync(Guid policyPackId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x => x.PolicyPackId == policyPackId)
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<PolicyPackVersion>>(result);
        }
    }
}

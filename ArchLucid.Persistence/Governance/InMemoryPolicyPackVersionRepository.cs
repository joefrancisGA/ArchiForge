using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Governance;

/// <summary>
/// In-memory implementation of <see cref="IPolicyPackVersionRepository"/> for testing and storage-off mode.
/// Size-bounded at <see cref="MaxEntries"/>; oldest entries are trimmed from the front when the cap is exceeded.
/// All operations are thread-safe via an exclusive lock.
/// </summary>
public sealed class InMemoryPolicyPackVersionRepository : IPolicyPackVersionRepository
{
    private const int MaxEntries = 500;
    private readonly List<PolicyPackVersion> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(PolicyPackVersion version, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(version);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _items.Add(version);
            if (_items.Count > MaxEntries)
                _items.RemoveRange(0, _items.Count - MaxEntries);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PolicyPackVersion version, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(version);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            int idx = _items.FindIndex(x => x.PolicyPackVersionId == version.PolicyPackVersionId);
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
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            PolicyPackVersion? row = _items.FirstOrDefault(
                x => x.PolicyPackId == policyPackId &&
                     string.Equals(x.Version, version, StringComparison.Ordinal));
            return Task.FromResult(row);
        }
    }

    public Task<IReadOnlyList<PolicyPackVersion>> ListByPackAsync(Guid policyPackId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<PolicyPackVersion> result = _items
                .Where(x => x.PolicyPackId == policyPackId)
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<PolicyPackVersion>>(result);
        }
    }
}

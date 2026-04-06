using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IArchitectureDigestRepository" />
public sealed class InMemoryArchitectureDigestRepository : IArchitectureDigestRepository
{
    private const int MaxEntries = 2_000;

    private readonly List<ArchitectureDigest> _items = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(ArchitectureDigest digest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(digest);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_items.Count >= MaxEntries)
                _items.RemoveAt(0);

            _items.Add(digest);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ArchitectureDigest>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int n = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        lock (_gate)
        {
            List<ArchitectureDigest> result = _items
                .Where(d =>
                    d.TenantId == tenantId &&
                    d.WorkspaceId == workspaceId &&
                    d.ProjectId == projectId &&
                    !d.ArchivedUtc.HasValue)
                .OrderByDescending(d => d.GeneratedUtc)
                .Take(n)
                .ToList();

            return Task.FromResult<IReadOnlyList<ArchitectureDigest>>(result);
        }
    }

    /// <inheritdoc />
    public Task<ArchitectureDigest?> GetByIdAsync(Guid digestId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ArchitectureDigest? d = _items.FirstOrDefault(x => x.DigestId == digestId);
            return Task.FromResult(d is { ArchivedUtc: not null } ? null : d);
        }
    }

    /// <inheritdoc />
    public Task<int> ArchiveDigestsGeneratedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        DateTime cutoff = cutoffUtc.UtcDateTime;
        DateTime stamp = DateTime.UtcNow;
        int count = 0;
        lock (_gate)
            foreach (ArchitectureDigest d in _items.Where(d => !d.ArchivedUtc.HasValue && d.GeneratedUtc < cutoff))
            {
                d.ArchivedUtc = stamp;
                count++;
            }
        
        return Task.FromResult(count);
    }
}

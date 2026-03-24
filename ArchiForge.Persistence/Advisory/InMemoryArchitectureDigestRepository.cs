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
        var n = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        lock (_gate)
        {
            var result = _items
                .Where(d =>
                    d.TenantId == tenantId &&
                    d.WorkspaceId == workspaceId &&
                    d.ProjectId == projectId)
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
            return Task.FromResult(_items.FirstOrDefault(x => x.DigestId == digestId));
    }
}

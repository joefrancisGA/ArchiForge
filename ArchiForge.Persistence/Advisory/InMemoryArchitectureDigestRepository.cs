using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

public sealed class InMemoryArchitectureDigestRepository : IArchitectureDigestRepository
{
    private readonly List<ArchitectureDigest> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(ArchitectureDigest digest, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(digest);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ArchitectureDigest>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(d =>
                    d.TenantId == tenantId &&
                    d.WorkspaceId == workspaceId &&
                    d.ProjectId == projectId)
                .OrderByDescending(d => d.GeneratedUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<ArchitectureDigest>>(result);
        }
    }

    public Task<ArchitectureDigest?> GetByIdAsync(Guid digestId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.DigestId == digestId));
    }
}

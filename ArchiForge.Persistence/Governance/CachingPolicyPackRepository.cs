using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Persistence.Caching;

namespace ArchiForge.Persistence.Governance;

/// <summary>Decorates <see cref="IPolicyPackRepository"/> with hot-path reads for <see cref="IPolicyPackRepository.GetByIdAsync"/>.</summary>
public sealed class CachingPolicyPackRepository(IPolicyPackRepository inner, IHotPathReadCache hotPathReadCache)
    : IPolicyPackRepository
{
    private readonly IPolicyPackRepository _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly IHotPathReadCache _hotPathReadCache =
        hotPathReadCache ?? throw new ArgumentNullException(nameof(hotPathReadCache));

    /// <inheritdoc />
    public async Task CreateAsync(PolicyPack pack, CancellationToken ct)
    {
        await _inner.CreateAsync(pack, ct);

        await _hotPathReadCache.RemoveAsync(HotPathCacheKeys.PolicyPack(pack.PolicyPackId), ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PolicyPack pack, CancellationToken ct)
    {
        await _inner.UpdateAsync(pack, ct);

        await _hotPathReadCache.RemoveAsync(HotPathCacheKeys.PolicyPack(pack.PolicyPackId), ct);
    }

    /// <inheritdoc />
    public Task<PolicyPack?> GetByIdAsync(Guid policyPackId, CancellationToken ct) =>
        _hotPathReadCache.GetOrCreateAsync(
            HotPathCacheKeys.PolicyPack(policyPackId),
            innerCt => _inner.GetByIdAsync(policyPackId, innerCt),
            ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<PolicyPack>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct) => _inner.ListByScopeAsync(tenantId, workspaceId, projectId, ct);
}

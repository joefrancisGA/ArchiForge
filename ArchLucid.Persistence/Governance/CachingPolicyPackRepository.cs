using System.Data;

using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Caching;

namespace ArchLucid.Persistence.Governance;

/// <summary>
///     Decorates <see cref="IPolicyPackRepository" /> with hot-path reads for
///     <see cref="IPolicyPackRepository.GetByIdAsync" />.
/// </summary>
public sealed class CachingPolicyPackRepository(IPolicyPackRepository inner, IHotPathReadCache hotPathReadCache)
    : IPolicyPackRepository
{
    private readonly IHotPathReadCache _hotPathReadCache =
        hotPathReadCache ?? throw new ArgumentNullException(nameof(hotPathReadCache));

    private readonly IPolicyPackRepository _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public async Task CreateAsync(
        PolicyPack pack,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        await _inner.CreateAsync(pack, ct, connection, transaction);

        await HotPathCacheEviction.RemovePolicyPackAsync(_hotPathReadCache, pack.PolicyPackId, ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PolicyPack pack, CancellationToken ct)
    {
        await _inner.UpdateAsync(pack, ct);

        await HotPathCacheEviction.RemovePolicyPackAsync(_hotPathReadCache, pack.PolicyPackId, ct);
    }

    /// <inheritdoc />
    public Task<PolicyPack?> GetByIdAsync(Guid policyPackId, CancellationToken ct)
    {
        return _hotPathReadCache.GetOrCreateAsync(
            HotPathCacheKeys.PolicyPack(policyPackId),
            innerCt => _inner.GetByIdAsync(policyPackId, innerCt),
            ct,
            HotPathCacheKeys.LegacyPolicyPack(policyPackId));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PolicyPack>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        return _inner.ListByScopeAsync(tenantId, workspaceId, projectId, ct);
    }
}

using System.Data;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>Persistence port for <see cref="PolicyPack" /> aggregate metadata (not version rows).</summary>
/// <remarks>
///     SQL implementation: <c>DapperPolicyPackRepository</c>; in-memory: <c>InMemoryPolicyPackRepository</c>.
///     Used when publishing versions and when resolving assignments to pack names/types.
/// </remarks>
public interface IPolicyPackRepository
{
    /// <summary>Inserts a new pack row.</summary>
    Task CreateAsync(
        PolicyPack pack,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>Updates pack fields such as <see cref="PolicyPack.Status" /> and <see cref="PolicyPack.CurrentVersion" />.</summary>
    Task UpdateAsync(PolicyPack pack, CancellationToken ct);

    /// <summary>Loads a pack by primary key, or <c>null</c> if missing.</summary>
    Task<PolicyPack?> GetByIdAsync(Guid policyPackId, CancellationToken ct);

    /// <summary>
    ///     Lists packs created under the given tenant/workspace/project scope (pack authoring scope, not assignment scope).
    /// </summary>
    Task<IReadOnlyList<PolicyPack>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}

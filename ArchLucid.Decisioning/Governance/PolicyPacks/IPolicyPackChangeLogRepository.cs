using System.Data;

using ArchLucid.Contracts.Governance;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
///     Append-only persistence for <see cref="PolicyPackChangeLogEntry" /> (<c>dbo.PolicyPackChangeLog</c>).
/// </summary>
/// <remarks>
///     Implemented by <c>ArchLucid.Persistence.Governance.DapperPolicyPackChangeLogRepository</c> and
///     <c>InMemoryPolicyPackChangeLogRepository</c>.
/// </remarks>
public interface IPolicyPackChangeLogRepository
{
    /// <summary>Inserts one row. Implementations must not issue UPDATE or DELETE.</summary>
    Task AppendAsync(
        PolicyPackChangeLogEntry entry,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByPolicyPackIdAsync(
        Guid policyPackId,
        int maxRows = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByTenantAsync(
        Guid tenantId,
        int maxRows = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns change log rows for a tenant with <c>ChangedUtc</c> in <c>[fromUtc, toUtc)</c>, ascending by time.
    /// </summary>
    Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByTenantInRangeAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}

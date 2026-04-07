using System.Data;

using ArchLucid.Contracts.Governance;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="GovernanceEnvironmentActivation"/> records that track
/// which run/manifest version is active for a given deployment environment.
/// </summary>
public interface IGovernanceEnvironmentActivationRepository
{
    /// <summary>Persists a new environment activation record.</summary>
    /// <param name="item">The activation to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task CreateAsync(
        GovernanceEnvironmentActivation item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>Updates an existing environment activation record (e.g., to mark it inactive).</summary>
    /// <param name="item">The activation with updated values.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task UpdateAsync(
        GovernanceEnvironmentActivation item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns all activation records for the specified <paramref name="environment"/>,
    /// ordered by <c>ActivatedUtc</c> descending (most recent first).
    /// </summary>
    /// <param name="environment">Target deployment environment (e.g., <c>dev</c>, <c>test</c>, <c>prod</c>).</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default);

    /// <summary>Returns all activation records linked to <paramref name="runId"/>.</summary>
    /// <param name="runId">The run whose activation history is requested.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

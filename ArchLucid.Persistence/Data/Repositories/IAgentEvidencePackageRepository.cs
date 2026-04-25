using System.Data;

using ArchLucid.Contracts.Agents;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Persistence contract for <see cref="AgentEvidencePackage" /> records that hold the
///     assembled evidence fed to agents at task execution time.
/// </summary>
public interface IAgentEvidencePackageRepository
{
    /// <summary>Persists a new evidence package.</summary>
    /// <param name="evidencePackage">The package to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task CreateAsync(
        AgentEvidencePackage evidencePackage,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    ///     Returns the evidence package for the specified run, or <see langword="null" /> when none exists.
    /// </summary>
    /// <param name="runId">The run whose evidence package is requested.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<AgentEvidencePackage?> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the evidence package with the given primary key, or <see langword="null" /> when not found.
    /// </summary>
    /// <param name="evidencePackageId">The unique identifier of the evidence package.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<AgentEvidencePackage?> GetByIdAsync(
        string evidencePackageId,
        CancellationToken cancellationToken = default);
}

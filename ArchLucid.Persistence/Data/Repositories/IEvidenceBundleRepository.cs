using System.Data;

using ArchLucid.Contracts.Agents;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="EvidenceBundle"/> records that capture the
/// policy, service-catalog, and prior-manifest references assembled for a run.
/// </summary>
public interface IEvidenceBundleRepository
{
    /// <summary>Persists a new evidence bundle.</summary>
    /// <param name="evidenceBundle">The bundle to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task CreateAsync(
        EvidenceBundle evidenceBundle,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns the evidence bundle with the given primary key, or <see langword="null"/> when not found.
    /// </summary>
    /// <param name="evidenceBundleId">The unique identifier of the bundle.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<EvidenceBundle?> GetByIdAsync(string evidenceBundleId, CancellationToken cancellationToken = default);
}

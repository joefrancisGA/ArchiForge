using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Interfaces;

/// <summary>
///     Lists recent committed runs for a tenant so an admin can pick the latest non-demo (or demo when allowed)
///     reference-evidence anchor run.
/// </summary>
public interface IReferenceEvidenceRunLookup
{
    /// <summary>
    ///     Returns up to <paramref name="take" /> committed runs for <paramref name="tenantId" />, newest first.
    /// </summary>
    Task<IReadOnlyList<ReferenceEvidenceRunCandidate>> ListRecentCommittedRunsAsync(
        Guid tenantId,
        int take,
        CancellationToken cancellationToken = default);
}

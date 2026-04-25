using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Repositories;

/// <summary>In-memory storage has no cross-tenant SQL surface; reference-evidence admin export returns no candidates.</summary>
public sealed class InMemoryReferenceEvidenceRunLookup : IReferenceEvidenceRunLookup
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ReferenceEvidenceRunCandidate>> ListRecentCommittedRunsAsync(
        Guid tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ReferenceEvidenceRunCandidate>>([]);
    }
}

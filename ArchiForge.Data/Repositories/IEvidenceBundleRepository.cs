using ArchiForge.Contracts.Agents;

namespace ArchiForge.Data.Repositories;

public interface IEvidenceBundleRepository
{
    Task CreateAsync(EvidenceBundle evidenceBundle, CancellationToken cancellationToken = default);
    Task<EvidenceBundle?> GetByIdAsync(string evidenceBundleId, CancellationToken cancellationToken = default);
}

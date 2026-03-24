using ArchiForge.Contracts.Governance;

namespace ArchiForge.Data.Repositories;

public interface IGovernanceEnvironmentActivationRepository
{
    Task CreateAsync(GovernanceEnvironmentActivation item, CancellationToken cancellationToken = default);
    Task UpdateAsync(GovernanceEnvironmentActivation item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

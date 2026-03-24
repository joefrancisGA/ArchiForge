using ArchiForge.Contracts.Governance;

namespace ArchiForge.Data.Repositories;

public interface IGovernanceApprovalRequestRepository
{
    Task CreateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default);
    Task UpdateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default);
    Task<GovernanceApprovalRequest?> GetByIdAsync(string approvalRequestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

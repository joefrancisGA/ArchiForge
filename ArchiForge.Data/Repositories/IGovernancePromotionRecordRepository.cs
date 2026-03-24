using ArchiForge.Contracts.Governance;

namespace ArchiForge.Data.Repositories;

public interface IGovernancePromotionRecordRepository
{
    Task CreateAsync(GovernancePromotionRecord item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GovernancePromotionRecord>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

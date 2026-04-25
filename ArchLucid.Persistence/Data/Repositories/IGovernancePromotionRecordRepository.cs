using ArchLucid.Contracts.Governance;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Persistence contract for <see cref="GovernancePromotionRecord" /> entries that track
///     successful environment promotions triggered by approved governance requests.
/// </summary>
public interface IGovernancePromotionRecordRepository
{
    /// <summary>Persists a new promotion record.</summary>
    /// <param name="item">The record to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task CreateAsync(GovernancePromotionRecord item, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns all promotion records associated with <paramref name="runId" />,
    ///     ordered by <c>PromotedUtc</c> descending (newest first), capped at 200 rows (Dapper implementation).
    /// </summary>
    /// <param name="runId">The run whose promotion history is requested.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<GovernancePromotionRecord>> GetByRunIdAsync(string runId,
        CancellationToken cancellationToken = default);
}

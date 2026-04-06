using ArchiForge.Contracts.Governance;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="GovernanceApprovalRequest"/> records that track
/// manifest promotion approvals through the governance workflow.
/// </summary>
public interface IGovernanceApprovalRequestRepository
{
    /// <summary>
    /// Persists a new approval request.
    /// <paramref name="item"/> must have a non-empty <c>ApprovalRequestId</c>.
    /// </summary>
    Task CreateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the stored approval request with <paramref name="item"/>.
    /// Used to persist status transitions (e.g. Pending → Approved, Pending → Rejected).
    /// </summary>
    Task UpdateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the approval request with the specified <paramref name="approvalRequestId"/>,
    /// or <see langword="null"/> when not found.
    /// </summary>
    Task<GovernanceApprovalRequest?> GetByIdAsync(string approvalRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all approval requests for the given <paramref name="runId"/>, ordered by creation time descending.
    /// Returns an empty list (never <see langword="null"/>) when none are found.
    /// </summary>
    Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

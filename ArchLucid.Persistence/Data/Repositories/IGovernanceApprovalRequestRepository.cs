using ArchLucid.Contracts.Governance;

namespace ArchLucid.Persistence.Data.Repositories;

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

    /// <summary>
    /// Returns all approval requests in <see cref="GovernanceApprovalStatus.Submitted"/> or <see cref="GovernanceApprovalStatus.Draft"/> status,
    /// ordered by <see cref="GovernanceApprovalRequest.RequestedUtc"/> descending, limited to <paramref name="maxRows"/>.
    /// </summary>
    /// <remarks>
    /// <c>GovernanceApprovalRequests</c> is keyed by run, not tenant; this query is not tenant-scoped. Use run-level APIs or a future join when tenant isolation is required.
    /// </remarks>
    Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent decisions (<see cref="GovernanceApprovalStatus.Approved"/>, <see cref="GovernanceApprovalStatus.Rejected"/>, <see cref="GovernanceApprovalStatus.Promoted"/>)
    /// ordered by <see cref="GovernanceApprovalRequest.ReviewedUtc"/> descending, limited to <paramref name="maxRows"/>.
    /// Rows without <see cref="GovernanceApprovalRequest.ReviewedUtc"/> are excluded.
    /// </summary>
    Task<IReadOnlyList<GovernanceApprovalRequest>> GetRecentDecisionsAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns pending approval requests whose <see cref="GovernanceApprovalRequest.SlaDeadlineUtc"/> is at or before
    /// <paramref name="utcNow"/> and <see cref="GovernanceApprovalRequest.SlaBreachNotifiedUtc"/> is null.
    /// </summary>
    Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingSlaBreachedAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Patches <see cref="GovernanceApprovalRequest.SlaBreachNotifiedUtc"/> on the specified request.
    /// </summary>
    Task PatchSlaBreachNotifiedAsync(
        string approvalRequestId,
        DateTime slaBreachNotifiedUtc,
        CancellationToken cancellationToken = default);
}

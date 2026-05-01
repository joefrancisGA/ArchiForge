using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Builds a deterministic rationale payload for operators reviewing an approval request.
/// </summary>
public interface IGovernanceRationaleService
{
    /// <summary>Returns <see langword="null" /> when the approval request does not exist.</summary>
    Task<GovernanceRationaleResult?> GetApprovalRequestRationaleAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default);
}

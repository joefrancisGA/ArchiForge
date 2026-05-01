using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Assembles governance lineage for an approval request (run, promotions, optional authority findings/manifest).
/// </summary>
public interface IGovernanceLineageService
{
    /// <summary>
    ///     Returns <see langword="null" /> when the approval request does not exist.
    /// </summary>
    Task<GovernanceLineageResult?> GetApprovalRequestLineageAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default);
}

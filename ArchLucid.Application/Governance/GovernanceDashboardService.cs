using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Application.Governance;
/// <summary>
///     Default <see cref = "IGovernanceDashboardService"/> combining cross-run approval views and tenant-scoped policy
///     change log rows.
/// </summary>
public sealed class GovernanceDashboardService(IGovernanceApprovalRequestRepository approvalRequestRepository, IPolicyPackChangeLogRepository policyPackChangeLogRepository) : IGovernanceDashboardService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(approvalRequestRepository, policyPackChangeLogRepository);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Data.Repositories.IGovernanceApprovalRequestRepository approvalRequestRepository, ArchLucid.Decisioning.Governance.PolicyPacks.IPolicyPackChangeLogRepository policyPackChangeLogRepository)
    {
        ArgumentNullException.ThrowIfNull(approvalRequestRepository);
        ArgumentNullException.ThrowIfNull(policyPackChangeLogRepository);
        return (byte)0;
    }

    private readonly IGovernanceApprovalRequestRepository _approvalRequestRepository = approvalRequestRepository ?? throw new ArgumentNullException(nameof(approvalRequestRepository));
    private readonly IPolicyPackChangeLogRepository _policyPackChangeLogRepository = policyPackChangeLogRepository ?? throw new ArgumentNullException(nameof(policyPackChangeLogRepository));
    /// <inheritdoc/>
    public async Task<GovernanceDashboardSummary> GetDashboardAsync(Guid tenantId, int maxPending = 20, int maxDecisions = 20, int maxChanges = 20, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (maxPending <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPending));
        if (maxDecisions <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDecisions));
        if (maxChanges <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxChanges));
        Task<IReadOnlyList<GovernanceApprovalRequest>> pendingTask = _approvalRequestRepository.GetPendingAsync(maxPending, cancellationToken);
        Task<IReadOnlyList<GovernanceApprovalRequest>> decisionsTask = _approvalRequestRepository.GetRecentDecisionsAsync(maxDecisions, cancellationToken);
        Task<IReadOnlyList<PolicyPackChangeLogEntry>> changesTask = _policyPackChangeLogRepository.GetByTenantAsync(tenantId, maxChanges, cancellationToken);
        await Task.WhenAll(pendingTask, decisionsTask, changesTask);
        IReadOnlyList<GovernanceApprovalRequest> pending = await pendingTask;
        IReadOnlyList<GovernanceApprovalRequest> decisions = await decisionsTask;
        IReadOnlyList<PolicyPackChangeLogEntry> changes = await changesTask;
        return new GovernanceDashboardSummary
        {
            PendingApprovals = pending,
            RecentDecisions = decisions,
            RecentChanges = changes,
            PendingCount = pending.Count
        };
    }
}
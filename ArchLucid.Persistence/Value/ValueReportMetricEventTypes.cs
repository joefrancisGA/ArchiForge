using ArchLucid.Core.Audit;

namespace ArchLucid.Persistence.Value;

/// <summary>
///     Stable <c>dbo.AuditEvents.EventType</c> sets used for governance vs drift tallies in
///     <see cref="DapperValueReportMetricsReader" />.
/// </summary>
internal static class ValueReportMetricEventTypes
{
    internal static readonly IReadOnlyList<string> GovernanceEventTypes =
    [
        AuditEventTypes.GovernanceResolutionExecuted,
        AuditEventTypes.GovernanceConflictDetected,
        AuditEventTypes.GovernanceApprovalSubmitted,
        AuditEventTypes.GovernanceApprovalApproved,
        AuditEventTypes.GovernanceApprovalRejected,
        AuditEventTypes.GovernanceSelfApprovalBlocked,
        AuditEventTypes.GovernancePreCommitBlocked,
        AuditEventTypes.GovernancePreCommitWarned,
        AuditEventTypes.GovernanceApprovalSlaBreached,
        AuditEventTypes.GovernanceManifestPromoted,
        AuditEventTypes.GovernanceEnvironmentActivated,
        AuditEventTypes.Baseline.Governance.ApprovalRequestSubmitted,
        AuditEventTypes.Baseline.Governance.ApprovalRequestApproved,
        AuditEventTypes.Baseline.Governance.ApprovalRequestRejected,
        AuditEventTypes.Baseline.Governance.ManifestPromoted,
        AuditEventTypes.Baseline.Governance.EnvironmentActivated
    ];

    internal static readonly IReadOnlyList<string> DriftAlertEventTypes =
    [
        AuditEventTypes.AlertTriggered,
        AuditEventTypes.AlertResolved,
        AuditEventTypes.CompositeAlertTriggered
    ];
}

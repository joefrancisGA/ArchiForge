namespace ArchiForge.Application.Governance;

/// <summary>
/// Well-known event type constants used when writing audit log entries for governance
/// workflow transitions. These values are stored as the <c>EventType</c> field in
/// governance audit records so that event streams can be filtered and correlated.
/// </summary>
public static class GovernanceAuditEventTypes
{
    /// <summary>Raised when a new governance approval request is submitted.</summary>
    public const string ApprovalRequestSubmitted = "Governance.ApprovalRequestSubmitted";

    /// <summary>Raised when an approval request transitions to the <c>Approved</c> state.</summary>
    public const string ApprovalRequestApproved = "Governance.ApprovalRequestApproved";

    /// <summary>Raised when an approval request transitions to the <c>Rejected</c> state.</summary>
    public const string ApprovalRequestRejected = "Governance.ApprovalRequestRejected";

    /// <summary>Raised when a manifest is successfully promoted between environments.</summary>
    public const string ManifestPromoted = "Governance.ManifestPromoted";

    /// <summary>Raised when a manifest version is activated in a target environment.</summary>
    public const string EnvironmentActivated = "Governance.EnvironmentActivated";
}

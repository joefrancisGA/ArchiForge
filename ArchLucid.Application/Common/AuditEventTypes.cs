namespace ArchiForge.Application.Common;

/// <summary>
/// Stable event type strings for <see cref="IBaselineMutationAuditService"/> (trusted baseline only).
/// </summary>
public static class AuditEventTypes
{
    /// <summary>Architecture run / string-run workflow (legacy <c>ArchitectureRuns</c> pipeline).</summary>
    public static class Architecture
    {
        public const string RunCreated = "Architecture.RunCreated";

        public const string RunStarted = "Architecture.RunStarted";

        public const string RunExecuteSucceeded = "Architecture.RunExecuteSucceeded";

        public const string RunCompleted = "Architecture.RunCompleted";

        public const string RunFailed = "Architecture.RunFailed";
    }

    /// <summary>Governance workflow mutations when integrated with the trusted baseline.</summary>
    public static class Governance
    {
        public const string ApprovalRequestSubmitted = "Governance.ApprovalRequestSubmitted";

        public const string ApprovalRequestApproved = "Governance.ApprovalRequestApproved";

        public const string ApprovalRequestRejected = "Governance.ApprovalRequestRejected";

        public const string ManifestPromoted = "Governance.ManifestPromoted";

        public const string EnvironmentActivated = "Governance.EnvironmentActivated";
    }
}

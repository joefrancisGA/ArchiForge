namespace ArchLucid.Core.Audit;

public static class AuditEventTypes
{
    public const string RunStarted = "RunStarted";
    public const string RunCompleted = "RunCompleted";
    public const string ManifestGenerated = "ManifestGenerated";
    public const string ArtifactsGenerated = "ArtifactsGenerated";
    public const string ReplayExecuted = "ReplayExecuted";
    public const string ArtifactDownloaded = "ArtifactDownloaded";
    public const string BundleDownloaded = "BundleDownloaded";
    public const string RunExported = "RunExported";

    /// <summary>Emitted when a structured architecture analysis report is built via the primary analysis-report API (<c>POST .../analysis-report</c>).</summary>
    public const string ArchitectureAnalysisReportGenerated = "ArchitectureAnalysisReportGenerated";

    /// <summary>Emitted when the architecture-package DOCX export completes successfully (<c>GET .../docx/runs/{{runId}}/architecture-package</c>).</summary>
    public const string ArchitectureDocxExportGenerated = "ArchitectureDocxExportGenerated";

    /// <summary>Emitted when a replay export persists a new run export row (<c>RecordReplayExport</c> on replay POST).</summary>
    public const string ReplayExportRecorded = "ReplayExportRecorded";

    /// <summary>Emitted when <c>POST .../run/exports/compare/summary</c> persists an export-record diff comparison row.</summary>
    public const string ComparisonSummaryPersisted = "ComparisonSummaryPersisted";

    public const string RecommendationGenerated = "RecommendationGenerated";
    public const string RecommendationAccepted = "RecommendationAccepted";
    public const string RecommendationRejected = "RecommendationRejected";
    public const string RecommendationDeferred = "RecommendationDeferred";
    public const string RecommendationImplemented = "RecommendationImplemented";

    public const string RecommendationLearningProfileRebuilt = "RecommendationLearningProfileRebuilt";

    public const string AdvisoryScanScheduled = "AdvisoryScanScheduled";
    public const string AdvisoryScanExecuted = "AdvisoryScanExecuted";
    public const string ArchitectureDigestGenerated = "ArchitectureDigestGenerated";

    public const string DigestSubscriptionCreated = "DigestSubscriptionCreated";
    public const string DigestSubscriptionToggled = "DigestSubscriptionToggled";
    public const string DigestDeliverySucceeded = "DigestDeliverySucceeded";
    public const string DigestDeliveryFailed = "DigestDeliveryFailed";

    public const string AlertRuleCreated = "AlertRuleCreated";
    public const string AlertTriggered = "AlertTriggered";
    public const string AlertAcknowledged = "AlertAcknowledged";
    public const string AlertResolved = "AlertResolved";
    public const string AlertSuppressed = "AlertSuppressed";

    public const string AlertRoutingSubscriptionCreated = "AlertRoutingSubscriptionCreated";
    public const string AlertRoutingSubscriptionToggled = "AlertRoutingSubscriptionToggled";
    public const string AlertDeliverySucceeded = "AlertDeliverySucceeded";
    public const string AlertDeliveryFailed = "AlertDeliveryFailed";

    public const string CompositeAlertRuleCreated = "CompositeAlertRuleCreated";
    public const string CompositeAlertTriggered = "CompositeAlertTriggered";
    public const string AlertSuppressedByPolicy = "AlertSuppressedByPolicy";

    public const string AlertRuleSimulationExecuted = "AlertRuleSimulationExecuted";
    public const string AlertRuleCandidateComparisonExecuted = "AlertRuleCandidateComparisonExecuted";

    public const string AlertThresholdRecommendationExecuted = "AlertThresholdRecommendationExecuted";

    public const string PolicyPackCreated = "PolicyPackCreated";
    public const string PolicyPackVersionPublished = "PolicyPackVersionPublished";
    public const string PolicyPackAssigned = "PolicyPackAssigned";
    public const string PolicyPackAssignmentCreated = "PolicyPackAssignmentCreated";
    public const string PolicyPackAssignmentArchived = "PolicyPackAssignmentArchived";

    public const string GovernanceResolutionExecuted = "GovernanceResolutionExecuted";
    public const string GovernanceConflictDetected = "GovernanceConflictDetected";

    public const string GovernanceApprovalSubmitted = "GovernanceApprovalSubmitted";
    public const string GovernanceApprovalApproved = "GovernanceApprovalApproved";
    public const string GovernanceApprovalRejected = "GovernanceApprovalRejected";

    /// <summary>Durable audit when a reviewer is blocked from approving or rejecting their own governance request (segregation of duties).</summary>
    public const string GovernanceSelfApprovalBlocked = "GovernanceSelfApprovalBlocked";

    /// <summary>Emitted when optional pre-commit governance blocks manifest commit due to critical findings.</summary>
    public const string GovernancePreCommitBlocked = "GovernancePreCommitBlocked";

    /// <summary>Agent LLM output failed <c>AgentResult</c> JSON schema validation at parse time (payload lists errors and model metadata when known).</summary>
    public const string AgentResultSchemaViolation = "AgentResultSchemaViolation";

    public const string GovernanceManifestPromoted = "GovernanceManifestPromoted";
    public const string GovernanceEnvironmentActivated = "GovernanceEnvironmentActivated";

    /// <summary>Background <c>DataArchivalHostedService</c> iteration failed after logging (see payload for exception details).</summary>
    public const string DataArchivalHostLoopFailed = "DataArchivalHostLoopFailed";

    public const string CircuitBreakerStateTransition = "CircuitBreakerStateTransition";

    public const string CircuitBreakerRejection = "CircuitBreakerRejection";

    public const string CircuitBreakerProbeOutcome = "CircuitBreakerProbeOutcome";

    /// <summary>
    /// Stable namespaced strings for trusted-baseline mutation audit (<c>IBaselineMutationAuditService</c> → structured <c>ILogger</c> only).
    /// They are <b>not</b> written to <c>dbo.AuditEvents</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dual-written governance flows also call <c>IAuditService</c> with the top-level <c>GovernanceApproval*</c> / <c>GovernanceManifestPromoted</c> / <c>GovernanceEnvironmentActivated</c> constants above.
    /// Those durable <c>EventType</c> values (e.g. <c>GovernanceApprovalSubmitted</c>) differ from nested <c>Governance.*</c> string values (e.g. <c>Governance.ApprovalRequestSubmitted</c>) by design — do not unify without a migration plan for existing rows and log parsers.
    /// </para>
    /// </remarks>
    public static class Baseline
    {
        /// <summary>Architecture run / string <c>RunId</c> workflow (authority <c>dbo.Runs</c>).</summary>
        public static class Architecture
        {
            public const string RunCreated = "Architecture.RunCreated";

            public const string RunStarted = "Architecture.RunStarted";

            public const string RunExecuteSucceeded = "Architecture.RunExecuteSucceeded";

            public const string RunCompleted = "Architecture.RunCompleted";

            public const string RunFailed = "Architecture.RunFailed";
        }

        /// <summary>Governance workflow mutations when integrated with the trusted baseline (baseline log channel).</summary>
        public static class Governance
        {
            public const string ApprovalRequestSubmitted = "Governance.ApprovalRequestSubmitted";

            public const string ApprovalRequestApproved = "Governance.ApprovalRequestApproved";

            public const string ApprovalRequestRejected = "Governance.ApprovalRequestRejected";

            public const string ManifestPromoted = "Governance.ManifestPromoted";

            public const string EnvironmentActivated = "Governance.EnvironmentActivated";
        }
    }
}

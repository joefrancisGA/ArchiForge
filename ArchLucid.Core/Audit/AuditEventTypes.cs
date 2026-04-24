namespace ArchLucid.Core.Audit;

public static class AuditEventTypes
{
    public const string RunStarted = "RunStarted";
    public const string RunCompleted = "RunCompleted";

    /// <summary>Coordinator-level: architecture run created and persisted (coordinator create orchestrator).</summary>
    public const string CoordinatorRunCreated = "CoordinatorRunCreated";

    /// <summary>Coordinator-level: architecture run execution started (coordinator execute orchestrator).</summary>
    public const string CoordinatorRunExecuteStarted = "CoordinatorRunExecuteStarted";

    /// <summary>Coordinator-level: architecture run execution succeeded (coordinator execute orchestrator).</summary>
    public const string CoordinatorRunExecuteSucceeded = "CoordinatorRunExecuteSucceeded";

    /// <summary>Coordinator-level: architecture run commit completed (coordinator commit orchestrator).</summary>
    public const string CoordinatorRunCommitCompleted = "CoordinatorRunCommitCompleted";

    /// <summary>
    ///     Coordinator-level: architecture run failed after baseline <c>Architecture.RunFailed</c> (create, execute, or
    ///     commit path).
    /// </summary>
    public const string CoordinatorRunFailed = "CoordinatorRunFailed";

    public const string ManifestGenerated = "ManifestGenerated";
    public const string ArtifactsGenerated = "ArtifactsGenerated";
    public const string ReplayExecuted = "ReplayExecuted";

    /// <summary>
    ///     Demo seed or replay commit persisted the authority SQL FK chain (context / graph / findings / decision trace +
    ///     golden manifest) outside the main pipeline executor.
    /// </summary>
    public const string AuthorityCommittedChainPersisted = "AuthorityCommittedChainPersisted";

    public const string ArtifactDownloaded = "ArtifactDownloaded";
    public const string BundleDownloaded = "BundleDownloaded";

    /// <summary>
    ///     In-product support bundle ZIP from <c>POST …/admin/support-bundle</c>. Payload is JSON with file name and
    ///     size bytes only (no raw bundle contents).
    /// </summary>
    public const string SupportBundleDownloaded = "SupportBundleDownloaded";

    public const string RunExported = "RunExported";

    /// <summary>
    ///     Emitted when a structured architecture analysis report is built via the primary analysis-report API (
    ///     <c>POST .../analysis-report</c>).
    /// </summary>
    public const string ArchitectureAnalysisReportGenerated = "ArchitectureAnalysisReportGenerated";

    /// <summary>
    ///     Emitted when the architecture-package DOCX export completes successfully (
    ///     <c>GET .../docx/runs/{{runId}}/architecture-package</c>).
    /// </summary>
    public const string ArchitectureDocxExportGenerated = "ArchitectureDocxExportGenerated";

    /// <summary>
    ///     Stakeholder DOCX value report generated for the current scope (
    ///     <c>POST /v1/value-report/{{tenantId}}/generate</c>).
    /// </summary>
    public const string ValueReportGenerated = "ValueReportGenerated";

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

    /// <summary>
    ///     Durable audit when a reviewer is blocked from approving or rejecting their own governance request (segregation
    ///     of duties).
    /// </summary>
    public const string GovernanceSelfApprovalBlocked = "GovernanceSelfApprovalBlocked";

    /// <summary>Emitted when optional pre-commit governance blocks manifest commit due to critical findings.</summary>
    public const string GovernancePreCommitBlocked = "GovernancePreCommitBlocked";

    /// <summary>Emitted when pre-commit governance warns but allows commit due to WarnOnly severity configuration.</summary>
    public const string GovernancePreCommitWarned = "GovernancePreCommitWarned";

    /// <summary>Emitted when a governance approval request breaches its SLA deadline.</summary>
    public const string GovernanceApprovalSlaBreached = "GovernanceApprovalSlaBreached";

    /// <summary>
    ///     Agent LLM output failed <c>AgentResult</c> JSON schema validation at parse time (payload lists errors and
    ///     model metadata when known).
    /// </summary>
    public const string AgentResultSchemaViolation = "AgentResultSchemaViolation";

    /// <summary>Full agent trace prompt/response blob persistence failed or timed out after agent trace row insert.</summary>
    public const string AgentTraceBlobPersistenceFailed = "AgentTraceBlobPersistenceFailed";

    /// <summary>
    ///     Mandatory SQL inline fallback for full agent trace text failed or forensic coverage verification failed after
    ///     blob issues.
    /// </summary>
    public const string AgentTraceInlineFallbackFailed = "AgentTraceInlineFallbackFailed";

    public const string GovernanceManifestPromoted = "GovernanceManifestPromoted";
    public const string GovernanceEnvironmentActivated = "GovernanceEnvironmentActivated";

    /// <summary>
    ///     Emitted when an operator runs a governance policy-pack dry-run / what-if evaluation
    ///     (<c>POST /v1/governance/policy-packs/{id}/dry-run</c>). No real commit happens — the
    ///     payload captures the proposed thresholds (always passed through the LLM-prompt redaction
    ///     pipeline before serialisation, per PENDING_QUESTIONS Q37), the evaluated run ids, and
    ///     would-be delta counts so reviewers can audit what was simulated and by whom.
    /// </summary>
    public const string GovernanceDryRunRequested = "GovernanceDryRunRequested";

    /// <summary>
    ///     Background <c>DataArchivalHostedService</c> iteration failed after logging (see payload for exception
    ///     details).
    /// </summary>
    public const string DataArchivalHostLoopFailed = "DataArchivalHostLoopFailed";

    /// <summary>
    ///     Admin remediation removed orphan <c>dbo.ComparisonRecords</c> rows whose run ids do not exist on <c>dbo.Runs</c>
    ///     (see <c>DataConsistencyOrphanRemediationSql</c>). Payload includes dry-run flag, count, and ids.
    /// </summary>
    public const string ComparisonRecordOrphansRemediated = "ComparisonRecordOrphansRemediated";

    /// <summary>
    ///     Admin remediation removed orphan <c>dbo.GoldenManifests</c> rows (no matching <c>dbo.Runs.RunId</c>), after
    ///     deleting dependent <c>dbo.ArtifactBundles</c>.
    /// </summary>
    public const string GoldenManifestOrphansRemediated = "GoldenManifestOrphansRemediated";

    /// <summary>
    ///     Admin remediation removed orphan <c>dbo.FindingsSnapshots</c> rows (no matching run, not referenced by any golden
    ///     manifest).
    /// </summary>
    public const string FindingsSnapshotOrphansRemediated = "FindingsSnapshotOrphansRemediated";

    public const string CircuitBreakerStateTransition = "CircuitBreakerStateTransition";

    public const string CircuitBreakerRejection = "CircuitBreakerRejection";

    public const string CircuitBreakerProbeOutcome = "CircuitBreakerProbeOutcome";

    /// <summary>
    ///     Trust center: a third-party or owner-approved security assessment summary was published for procurement / customer
    ///     review
    ///     (payload: assessment code, summary reference, optional assessor display name).
    /// </summary>
    public const string SecurityAssessmentPublished = "SecurityAssessmentPublished";

    /// <summary>SaaS tenant registry: new tenant + default workspace identifiers created (or idempotent replay).</summary>
    public const string TenantProvisioned = "TenantProvisioned";

    /// <summary>
    ///     Public self-service registration completed (audit complements <see cref="TenantProvisioned" /> on the same
    ///     flow).
    /// </summary>
    public const string TenantSelfRegistered = "TenantSelfRegistered";

    /// <summary>Self-service trial activated with sample data (demo seed + trial window metadata).</summary>
    public const string TrialProvisioned = "TrialProvisioned";

    /// <summary>Trial marked converted (billing integration stub).</summary>
    public const string TenantTrialConverted = "TenantTrialConverted";

    /// <summary>
    ///     Automated trial lifecycle state transition (Worker scheduler; SQL row in <c>dbo.TenantLifecycleTransitions</c>
    ///     ).
    /// </summary>
    public const string TrialLifecycleTransition = "TrialLifecycleTransition";

    /// <summary>Emitted when a mutating request is blocked because the tenant trial expired or exceeded runs/seats (HTTP 402).</summary>
    public const string TrialLimitExceeded = "TrialLimitExceeded";

    /// <summary>Self-service signup or local trial identity registration attempt observed at HTTP entry (funnel top).</summary>
    public const string TrialSignupAttempted = "TrialSignupAttempted";

    /// <summary>Signup or trial bootstrap failed after <see cref="TrialSignupAttempted" /> (payload includes stage/reason).</summary>
    public const string TrialSignupFailed = "TrialSignupFailed";

    /// <summary>Prospect supplied optional review-cycle baseline hours at trial signup (persisted on <c>dbo.Tenants</c>).</summary>
    public const string TrialBaselineReviewCycleCaptured = "TrialBaselineReviewCycleCaptured";

    /// <summary>First golden manifest commit recorded for a self-service trial tenant (funnel depth).</summary>
    public const string TrialFirstRunCompleted = "TrialFirstRunCompleted";

    /// <summary>Admin initiated hosted billing checkout for trial conversion.</summary>
    public const string BillingCheckoutInitiated = "BillingCheckoutInitiated";

    /// <summary>Hosted billing checkout session created successfully (payload may include provider session id).</summary>
    public const string BillingCheckoutCompleted = "BillingCheckoutCompleted";

    /// <summary>
    ///     Tenant-level customer notification channel toggles updated (
    ///     <c>PUT /v1/notifications/customer-channel-preferences</c>).
    /// </summary>
    public const string TenantNotificationChannelPreferencesUpdated = "TenantNotificationChannelPreferencesUpdated";

    /// <summary>
    ///     Tenant Microsoft Teams incoming-webhook Key Vault reference upserted (
    ///     <c>POST /v1/integrations/teams/connections</c>).
    /// </summary>
    public const string TenantTeamsIncomingWebhookConnectionUpserted = "TenantTeamsIncomingWebhookConnectionUpserted";

    /// <summary>
    ///     Tenant Microsoft Teams incoming-webhook Key Vault reference removed (
    ///     <c>DELETE /v1/integrations/teams/connections</c>).
    /// </summary>
    public const string TenantTeamsIncomingWebhookConnectionRemoved = "TenantTeamsIncomingWebhookConnectionRemoved";

    /// <summary>Tenant weekly executive digest preferences updated (<c>POST /v1/tenant/exec-digest-preferences</c>).</summary>
    public const string ExecDigestPreferencesUpdated = "ExecDigestPreferencesUpdated";

    /// <summary>
    ///     Tenant crossed the configured warn threshold for the UTC-day combined LLM token budget (emitted at most once
    ///     per tenant per UTC day).
    /// </summary>
    public const string LlmTenantDailyBudgetApproaching = "LlmTenantDailyBudgetApproaching";

    /// <summary>
    ///     Phase 2 canonical durable catalog for coordinator run-stage semantics (ADR 0021 § Phase 2).
    ///     Dual-written with legacy <c>CoordinatorRun*</c> wire values until the Sunset window closes.
    /// </summary>
    public static class Run
    {
        /// <summary>Canonical twin of <see cref="CoordinatorRunCreated" />.</summary>
        public const string Created = "Run.Created";

        /// <summary>Canonical twin of <see cref="CoordinatorRunExecuteStarted" />.</summary>
        public const string ExecuteStarted = "Run.ExecuteStarted";

        /// <summary>Canonical twin of <see cref="CoordinatorRunExecuteSucceeded" />.</summary>
        public const string ExecuteSucceeded = "Run.ExecuteSucceeded";

        /// <summary>Canonical twin of <see cref="CoordinatorRunCommitCompleted" />.</summary>
        public const string CommitCompleted = "Run.CommitCompleted";

        /// <summary>Canonical twin of <see cref="CoordinatorRunFailed" />.</summary>
        public const string Failed = "Run.Failed";
    }

    /// <summary>
    ///     Stable namespaced strings for trusted-baseline mutation audit (<c>IBaselineMutationAuditService</c> → structured
    ///     <c>ILogger</c> only).
    ///     They are <b>not</b> written to <c>dbo.AuditEvents</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Dual-written governance flows also call <c>IAuditService</c> with the top-level <c>GovernanceApproval*</c> /
    ///         <c>GovernanceManifestPromoted</c> / <c>GovernanceEnvironmentActivated</c> constants above.
    ///         Those durable <c>EventType</c> values (e.g. <c>GovernanceApprovalSubmitted</c>) differ from nested
    ///         <c>Governance.*</c> string values (e.g. <c>Governance.ApprovalRequestSubmitted</c>) by design — do not unify
    ///         without a migration plan for existing rows and log parsers.
    ///     </para>
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

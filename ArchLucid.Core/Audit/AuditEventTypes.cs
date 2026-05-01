namespace ArchLucid.Core.Audit;

public static class AuditEventTypes
{
    public const string RunStarted = "RunStarted";
    public const string RunCompleted = "RunCompleted";

    public const string ManifestGenerated = "ManifestGenerated";

    /// <summary>Durable audit when a run's golden manifest is finalized (committed) in one atomic transaction with outbox.</summary>
    public const string ManifestFinalized = "ManifestFinalized";

    /// <summary>Product-facing run submission (<c>POST /v1/runs/{runId}/submit</c>, formerly execute).</summary>
    public const string RunSubmitted = "RunSubmitted";

    /// <summary>Operator viewed committed manifest JSON (<c>GET /v1/runs/{runId}/manifest</c>).</summary>
    public const string ManifestViewed = "ManifestViewed";

    /// <summary>Operator retrieved review trail / pipeline timeline (<c>GET /v1/runs/{runId}/review-trail</c>).</summary>
    public const string ReviewTrailAccessed = "ReviewTrailAccessed";

    /// <summary>Operator retrieved decision provenance graph (<c>GET …/review-trail/provenance</c>).</summary>
    public const string ProvenanceAccessed = "ProvenanceAccessed";

    /// <summary>Bulk findings list read (<c>GET /v1/runs/{runId}/findings</c>).</summary>
    public const string FindingsListAccessed = "FindingsListAccessed";

    /// <summary>Governance approval request created (<c>POST /v1/governance/approval-requests</c>).</summary>
    public const string GovernanceApprovalRequested = "GovernanceApprovalRequested";
    public const string ArtifactsGenerated = "ArtifactsGenerated";

    /// <summary>Artifact synthesis ended in hard failure (no usable bundle).</summary>
    public const string ArtifactSynthesisFailed = "ArtifactSynthesisFailed";

    /// <summary>Artifact synthesis produced a degraded bundle (see payload for missing artifact kinds).</summary>
    public const string ArtifactSynthesisPartial = "ArtifactSynthesisPartial";

    /// <summary>Architecture request draft or import persisted (namespaced <c>Request.*</c> durable type).</summary>
    public const string RequestCreated = "Request.Created";

    /// <summary>Request locked because a non-terminal run references it.</summary>
    public const string RequestLocked = "Request.Locked";

    /// <summary>Request released after all referencing runs reached a terminal state.</summary>
    public const string RequestReleased = "Request.Released";

    /// <summary>Golden manifest superseded by a newer authority row in the same scope (policy- or admin-driven).</summary>
    public const string ManifestSuperseded = "ManifestSuperseded";

    /// <summary>Golden manifest soft-archived (<c>ArchivedUtc</c> set).</summary>
    public const string ManifestArchived = "ManifestArchived";

    /// <summary>Findings snapshot generation reached a sealed terminal generation status.</summary>
    public const string FindingsSnapshotSealed = "FindingsSnapshotSealed";

    /// <summary>Human reviewer approved a finding.</summary>
    public const string FindingReviewApproved = "FindingReviewApproved";

    /// <summary>Human reviewer rejected a finding.</summary>
    public const string FindingReviewRejected = "FindingReviewRejected";

    /// <summary>Privileged override applied after rejection.</summary>
    public const string FindingReviewOverridden = "FindingReviewOverridden";

    public const string ReplayExecuted = "ReplayExecuted";

    /// <summary>Internal QA: POST <c>…/internal/architecture/runs/{{runId}}/determinism-check</c> completed.</summary>
    public const string InternalArchitectureDeterminismCheckExecuted = "InternalArchitectureDeterminismCheckExecuted";

    /// <summary>Internal dev: POST <c>…/internal/architecture/runs/{{runId}}/seed-fake-results</c> succeeded.</summary>
    public const string InternalArchitectureFakeResultsSeeded = "InternalArchitectureFakeResultsSeeded";

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
    ///     Architecture request draft imported from an uploaded TOML/JSON file (
    ///     <c>POST .../architecture/request/import</c>).
    /// </summary>
    public const string RequestFileImported = "RequestFileImported";

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

    /// <summary>Operator set pilot scorecard ROI baselines (<c>PUT /v1/pilots/scorecard/baselines</c>).</summary>
    public const string PilotScorecardBaselinesUpdated = "PilotScorecardBaselinesUpdated";
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

    /// <summary>
    ///     Operator ran pre-commit gate what-if with synthetic findings (
    ///     <c>POST /v1/governance/pre-commit/simulate</c>). Payload summarizes request parameters and gate outcome; no
    ///     manifest commit.
    /// </summary>
    public const string GovernancePreCommitSimulationEvaluated = "GovernancePreCommitSimulationEvaluated";

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
    ///     Durable audit when an operator validates a governance write path with <c>dryRun=true</c> (
    ///     approval request or promotion): same validation as a real commit runs, but no row/outbox/ integration
    ///     publish. Payload names the workflow (approval vs promotion) and the non-sensitive request fields so SIEM
    ///     can detect probing without relying on skipped <see cref="GovernanceApprovalSubmitted" /> rows.
    /// </summary>
    public const string GovernanceDryRunValidationAttempted = "GovernanceDryRunValidationAttempted";

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
    ///     Commercial Entra directory (<c>tid</c>) bound to an ArchLucid tenant after paid conversion
    ///     (<c>POST /v1/tenant/link-entra</c>).
    /// </summary>
    public const string TenantEntraDirectoryBound = "TenantEntraDirectoryBound";

    /// <summary>Optional: trial local <c>dbo.IdentityUsers</c> row linked to an Entra <c>oid</c> during handoff.</summary>
    public const string TrialLocalIdentityLinkedToEntra = "TrialLocalIdentityLinkedToEntra";

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

    /// <summary>
    ///     Durable failure on <c>POST /v1/register</c> (validation, duplicate org, or unexpected server error). Payload
    ///     includes <c>reason</c> and optional <c>message</c>.
    /// </summary>
    public const string TrialRegistrationFailed = "TrialRegistrationFailed";

    /// <summary>Prospect supplied optional review-cycle baseline hours at trial signup (persisted on <c>dbo.Tenants</c>).</summary>
    public const string TrialBaselineReviewCycleCaptured = "TrialBaselineReviewCycleCaptured";

    /// <summary>First save of <c>BaselineManualPrep*</c> on <c>dbo.Tenants</c> (settings or migration from prior null).</summary>
    public const string TrialBaselineManualPrepCaptured = "TrialBaselineManualPrepCaptured";

    /// <summary>Subsequent edits to <c>BaselineManualPrep*</c> after the first capture.</summary>
    public const string TrialBaselineManualPrepUpdated = "TrialBaselineManualPrepUpdated";

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
    ///     Outbound subscriber URL probe without persistence (<c>POST /v1/webhooks/dry-run</c>). Payload excludes shared
    ///     secrets and response bodies.
    /// </summary>
    public const string OutboundWebhookDryRunProbeExecuted = "OutboundWebhookDryRunProbeExecuted";

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

    public const string ScimTokenIssued = "ScimTokenIssued";

    public const string ScimTokenRevoked = "ScimTokenRevoked";

    public const string ScimUserProvisioned = "ScimUserProvisioned";

    public const string ScimUserUpdated = "ScimUserUpdated";

    /// <summary>SCIM IdP group-derived role superseded an operator-managed SCIM <c>manualResolvedRole</c> assignment.</summary>
    public const string RoleOverriddenByScim = "RoleOverriddenByScim";

    public const string ScimUserDeactivated = "ScimUserDeactivated";

    public const string ScimGroupProvisioned = "ScimGroupProvisioned";

    public const string ScimGroupMembershipChanged = "ScimGroupMembershipChanged";

    /// <summary>Pilot <c>archlucid try --real</c>: POST execute received with pilot try header (real AOAI attempt).</summary>
    public const string FirstRealValueRunStarted = "FirstRealValueRunStarted";

    /// <summary>Pilot <c>archlucid try --real</c>: pilot-marked execute completed without throwing.</summary>
    public const string FirstRealValueRunCompleted = "FirstRealValueRunCompleted";

    /// <summary>Pilot <c>archlucid try --real</c>: development seed path recorded simulator substitution after AOAI failure.</summary>
    public const string FirstRealValueRunFellBackToSimulator = "FirstRealValueRunFellBackToSimulator";

    /// <summary>
    ///     After execute, coordinator promoted <c>dbo.Runs.LegacyRunStatus</c> to <c>ReadyForCommit</c> when Topology,
    ///     Cost, Compliance, and Critic each contributed exactly one persisted agent result (ADR-0012; distinct from golden
    ///     manifest finalize at commit).
    /// </summary>
    public const string RunLegacyReadyForCommitPromoted = "RunLegacyReadyForCommitPromoted";

    /// <summary>
    ///     Canonical durable <c>dbo.AuditEvents</c> event types for architecture run-stage semantics (create, execute,
    ///     commit, failure).
    /// </summary>
    public static class Run
    {
        public const string Created = "Run.Created";

        public const string ExecuteStarted = "Run.ExecuteStarted";

        public const string ExecuteSucceeded = "Run.ExecuteSucceeded";

        public const string CommitCompleted = "Run.CommitCompleted";

        public const string Failed = "Run.Failed";

        /// <summary>Operator or API requested retry of a failed run (same <c>RunId</c>).</summary>
        public const string RetryRequested = "Run.RetryRequested";
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

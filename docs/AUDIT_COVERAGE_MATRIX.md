# Audit coverage matrix

This document maps **state-changing** workflows to the audit signals they emit. ArchLucid has two parallel **channels** that share one **string catalog** in `ArchLucid.Core.Audit.AuditEventTypes`:

1. **Durable SQL audit** — `IAuditService.LogAsync` → `IAuditRepository.AppendAsync` → `dbo.AuditEvents` (`ArchLucid.Core.Audit.AuditEvent`). Event types use **top-level** `AuditEventTypes.*` constants (e.g. `RunStarted`, `GovernanceApprovalSubmitted`).
2. **Baseline mutation log** — `IBaselineMutationAuditService.RecordAsync` → structured **ILogger** lines only (`ArchLucid.Application.Common.BaselineMutationAuditService`). Event types use **`AuditEventTypes.Baseline.Architecture.*`** and **`AuditEventTypes.Baseline.Governance.*`** (namespaced string values). These **do not** populate `dbo.AuditEvents`.

`ArchLucid.Application.Governance.GovernanceAuditEventTypes` mirrors **`AuditEventTypes.Baseline.Governance`** values for documentation and some workflow code paths. **`GovernanceWorkflowService`** dual-writes: baseline channel with **`Baseline.Governance.*`** **and** `IAuditService` with top-level `GovernanceApprovalSubmitted` / `GovernanceApprovalApproved` / `GovernanceApprovalRejected` / `GovernanceManifestPromoted` / `GovernanceEnvironmentActivated` (durable `EventType` strings differ from baseline — see XML remarks on `AuditEventTypes.Baseline`).

<!-- audit-core-const-count:92 -->

The HTML comment above is a **CI anchor**: `.github/workflows/ci.yml` compares `grep -c 'public const string' ArchLucid.Core/Audit/AuditEventTypes.cs` to the number in this comment. Update the comment whenever Core constants change, and extend the appendix table below.

---

## Design notes (ADR-style)

| Decision | Rationale |
|----------|-----------|
| **Circuit breaker → audit is fire-and-forget** | `CircuitBreakerGate` sits on the hot path; awaiting SQL or disk I/O would add tail latency and failure modes. The bridge schedules `LogAsync` on the thread pool and swallows exceptions so telemetry and breaker semantics stay reliable. |
| **Callback on `CircuitBreakerGate` instead of `IAuditService` in Core** | `ArchLucid.Core` must not reference persistence or host services. An optional `Action<CircuitBreakerAuditEntry>?` keeps tier boundaries; composition roots wire `CircuitBreakerAuditBridge.CreateCallback()`. |
| **`GetFilteredAsync` instead of overloading `GetByScopeAsync`** | Eight or more call sites and test doubles already depend on the original signature. A new method adds filtering without breaking consumers. |
| **Static matrix + CI count guard** | Cheaper than runtime introspection of all call sites. The matrix can drift; the CI guard at least forces developers to revisit this file when `AuditEventTypes` grows. |
| **Single Core catalog for baseline + durable** | Application references `ArchLucid.Core.Audit.AuditEventTypes.Baseline` so operators and developers have one file for all event-type strings; nested `Baseline` preserves namespaced baseline values without colliding with authority `RunStarted` / `RunCompleted`. |
| **Coordinator orchestration dual-write** | The three coordinator orchestrators (`Create`, `Execute`, `Commit`) now dual-write: `IBaselineMutationAuditService` (structured log, existing) **and** `IAuditService` (durable SQL). Durable calls use distinct `CoordinatorRun*` event types so they do not collide with authority-pipeline `RunStarted` / `RunCompleted`. Each durable call is wrapped in `try/catch` — audit failure must never break the main orchestration flow. |
| **Critical-path durable audit retry** | `CoordinatorRunCreated` on run create uses `ArchLucid.Core.Audit.DurableAuditLogRetry` (short exponential backoff, default 3 attempts) so a single transient SQL failure is less likely to drop the row. After exhaustion, failures are logged only — orchestration still completes. |
| **Database-level append-only on `dbo.AuditEvents`** | Migration **`051_AuditEvents_DenyUpdateDelete.sql`** (and the same idempotent **`DENY`** block in **`ArchLucid.Persistence/Scripts/ArchLucid.sql`** after the table DDL) issues **`DENY UPDATE`** and **`DENY DELETE`** on **`dbo.AuditEvents`** to the database role **`ArchLucidApp`** when that role exists. This closes the gap where code only `INSERT`s but ad-hoc SQL or bugs could mutate rows. **`dbo` / `db_owner`** are unaffected for break-glass. Local dev often has no **`ArchLucidApp`** role (app runs as **`dbo`** / SQL auth admin) — the migration **skips** until operators create the role and add the managed identity or SQL user (see **`docs/security/MANAGED_IDENTITY_SQL_BLOB.md`**). Deployments that only use **`db_datawriter`** without **`ArchLucidApp`** should create the role and move the app principal into it, or apply an environment-specific **`DENY`** to **`[db_datawriter]`** for this table. |

### Indexes on `dbo.AuditEvents`

| Index | Columns (notes) | Purpose |
|-------|-----------------|---------|
| **`IX_AuditEvents_Scope_OccurredUtc`** | `(TenantId, WorkspaceId, ProjectId, OccurredUtc DESC)` | Default newest-first listing within scope. |
| **`IX_AuditEvents_CorrelationId`** | `(CorrelationId)` **filtered** `WHERE CorrelationId IS NOT NULL` | Fast `GET /v1/audit/search?correlationId=…` and cross-request forensics. Added in migration **`055_AuditEvents_CorrelationId_RunId_Indexes.sql`**. |
| **`IX_AuditEvents_RunId_OccurredUtc`** | `(RunId, OccurredUtc DESC)` **filtered** `WHERE RunId IS NOT NULL` | Per-run audit timeline by `RunId`. Same migration. |

---

## Audit retrieval and export (read paths; no new `IAuditService` row)

Retention tiering (hot / warm / cold) and operational guidance: **`docs/AUDIT_RETENTION_POLICY.md`**.

| Capability | HTTP | Notes |
|------------|------|--------|
| Paginated audit (UI / API, newest first) | `GET /v1/audit` | Cap **500** rows per request. **Hot** tier (see retention doc). |
| Filtered audit search | `GET /v1/audit/search` | Cap **500**; keyset and filters. **Hot** tier. |
| Bulk export (compliance / archival) | `GET /v1/audit/export` | **`Accept: application/json`** or **`Accept: text/csv`**; UTC range **`fromUtc` / `toUtc`** (half-open); max **90 days** per request; **`maxRows`** clamped **1–10 000**; CSV sets **`Content-Disposition: attachment`**. **Warm** tier extraction to blob is **operator-scheduled** (see **`docs/AUDIT_RETENTION_POLICY.md`**). |

---

## Operations → durable audit (`IAuditService` → `dbo.AuditEvents`)

| Operation | Controller / service | Event type constant | Scope fields (RunId / ManifestId / ArtifactId) | DataJson (representative) |
|-----------|----------------------|---------------------|--------------------------------------------------|---------------------------|
| Authority pipeline manifest persisted | `AuthorityPipelineStagesExecutor` | `AuditEventTypes.ManifestGenerated` | RunId, ManifestId | `{ manifestHash, ruleSetId }` |
| Authority pipeline artifacts synthesized | `AuthorityPipelineStagesExecutor` | `AuditEventTypes.ArtifactsGenerated` | RunId, ManifestId | `{ bundleId, artifactCount }` |
| Authority run started (sync path, queued deferral, or queue resume) | `AuthorityRunOrchestrator` | `AuditEventTypes.RunStarted` | RunId | `{ projectId, queued, resumedFromQueue? }` |
| Authority run completed | `AuthorityRunOrchestrator` | `AuditEventTypes.RunCompleted` | RunId, ManifestId | `{ goldenManifestId, artifactBundleId, decisionTraceId }` |
| Authority replay executed | `AuthorityReplayController` | `AuditEventTypes.ReplayExecuted` | RunId | `{ mode, rebuilt manifest id? }` |
| Advisory scan lifecycle | `AdvisoryScanRunner` | `AdvisoryScanScheduled`, `AdvisoryScanExecuted`, `ArchitectureDigestGenerated`, … | varies by path | scan / digest payloads (JSON) |
| Advisory scheduling API | `AdvisorySchedulingController` | `AdvisoryScanScheduled` (and related) | per request | schedule metadata |
| Advisory API mutations | `AdvisoryController` | digest / scan event types; `RecommendationGenerated` + accept/reject/defer/implement | per action | per action |
| Digest delivery | `DigestDeliveryDispatcher` | `DigestDeliverySucceeded`, `DigestDeliveryFailed` | — | delivery metadata |
| Digest subscriptions API | `DigestSubscriptionsController` | `DigestSubscriptionCreated`, `DigestSubscriptionToggled` | — | subscription fields |
| Alert lifecycle | `AlertService` | `AlertTriggered`, `AlertAcknowledged`, `AlertResolved`, `AlertSuppressed` | — | alert ids / comments |
| Alert delivery | `AlertDeliveryDispatcher` | `AlertDeliverySucceeded`, `AlertDeliveryFailed` | — | routing metadata |
| Composite alerts | `CompositeAlertService` | `CompositeAlertTriggered`, `AlertSuppressedByPolicy` | — | rule / policy metadata |
| Alert rules API | `AlertRulesController` | `AlertRuleCreated` | — | rule summary |
| Composite rules API | `CompositeAlertRulesController` | `CompositeAlertRuleCreated` | — | rule summary |
| Alert routing API | `AlertRoutingSubscriptionsController` | `AlertRoutingSubscriptionCreated`, `AlertRoutingSubscriptionToggled` | — | channel metadata |
| Alert simulation API | `AlertSimulationController` | `AlertRuleSimulationExecuted`, `AlertRuleCandidateComparisonExecuted` | — | simulation parameters |
| Alert tuning API | `AlertTuningController` | `AlertThresholdRecommendationExecuted` | — | tuning context |
| Policy packs (host) | `PolicyPacksAppService` | `PolicyPackCreated`, `PolicyPackVersionPublished`, `PolicyPackAssigned`, `PolicyPackAssignmentCreated`, `PolicyPackAssignmentArchived` | — | pack / version ids |
| Governance resolution API | `GovernanceResolutionController` | `GovernanceResolutionExecuted`, `GovernanceConflictDetected` | — | resolution payload summary |
| Governance workflow (approval / promote / activate) | `GovernanceWorkflowService` | `GovernanceApprovalSubmitted`, `GovernanceApprovalApproved`, `GovernanceApprovalRejected`, `GovernanceSelfApprovalBlocked` (segregation-of-duties block), `GovernanceManifestPromoted`, `GovernanceEnvironmentActivated` | RunId when parseable | ids, environments, manifest version (JSON); self-approval block includes `approvalRequestId`, `requestedBy`, `attemptedReviewerBy` |
| Governance approval SLA breach | `ApprovalSlaMonitor` | `GovernanceApprovalSlaBreached` | — | `approvalRequestId`, `runId`, `requestedBy`, `slaDeadlineUtc`, `breachedByMinutes` |
| Pre-commit governance warn | `ArchitectureRunCommitOrchestrator` | `GovernancePreCommitWarned` | RunId when parseable | `reason`, `warnings`, `blockingFindingIds`, `policyPackId`, `minimumBlockingSeverity` |
| Recommendation learning rebuild | `RecommendationLearningController` | `RecommendationLearningProfileRebuilt` | — | profile id |
| Artifact / bundle / run export download | `ArtifactExportController` | `ArtifactDownloaded`, `BundleDownloaded`, `RunExported` | RunId (+ artifact when applicable) | format, byte counts, etc. |
| Architecture analysis report (primary JSON build) | `AnalysisReportsController` | `ArchitectureAnalysisReportGenerated` | RunId when parseable | section flags, `manifestVersion`, `warningCount` |
| Architecture package DOCX download | `DocxExportController` | `ArchitectureDocxExportGenerated` | RunId, ManifestId | `runId`, `compareWithRunId`, `byteCount` |
| Replay export persisted as new row | `ExportsController` (replay POST + metadata POST when `RecordReplayExport`) | `ReplayExportRecorded` | RunId when parseable | `sourceExportRecordId`, `recordedReplayExportRecordId`, `runId` |
| Comparison summary persisted (export diff) | `ExportsController` (`POST .../run/exports/compare/summary`, `persist: true`) | `ComparisonSummaryPersisted` | RunId when parseable | `comparisonId`, `sourceExportRecordId`, `leftExportRecordId`, `rightExportRecordId` |
| Data archival host failure | `DataArchivalHostIteration` | `DataArchivalHostLoopFailed` | — | exception summary |
| OpenAI circuit breaker | `CircuitBreakerAuditBridge` (wired from `CircuitBreakerGate`) | `CircuitBreakerStateTransition`, `CircuitBreakerRejection`, `CircuitBreakerProbeOutcome` | Tenant/Workspace/Project from ambient scope | `{ gate, fromState, toState, probeOutcome? }` |
| Agent result JSON failed schema validation (enforced parse) | `TopologyAgentHandler`, `ComplianceAgentHandler`, `CriticAgentHandler` → `AgentResultSchemaViolationAudit` | `AuditEventTypes.AgentResultSchemaViolation` | RunId / task context when parseable | schema errors, truncated JSON, agent type |
| Coordinator run created (dual-write) | `ArchitectureRunCreateOrchestrator` | `AuditEventTypes.CoordinatorRunCreated` | RunId | `{ requestId, systemName }` |
| Coordinator run execution started (dual-write) | `ArchitectureRunExecuteOrchestrator` | `AuditEventTypes.CoordinatorRunExecuteStarted` | RunId | `{ runId }` |
| Coordinator run execution succeeded (dual-write) | `ArchitectureRunExecuteOrchestrator` | `AuditEventTypes.CoordinatorRunExecuteSucceeded` | RunId | `{ runId, resultCount }` |
| Coordinator run commit completed (dual-write) | `ArchitectureRunCommitOrchestrator` | `AuditEventTypes.CoordinatorRunCommitCompleted` | RunId | `{ runId, manifestVersion, systemName }` |
| Coordinator run failed (dual-write) | `ArchitectureRunCreateOrchestrator`, `ArchitectureRunExecuteOrchestrator`, `ArchitectureRunCommitOrchestrator` | `AuditEventTypes.CoordinatorRunFailed` | RunId when parseable | `{ runId, reason }` (after baseline `Architecture.RunFailed`) |
| Agent trace blob persistence failed or timed out | `AgentExecutionTraceRecorder` | `AuditEventTypes.AgentTraceBlobPersistenceFailed` | RunId / task context when parseable | `{ traceId, runId, agentType, reason, failedBlobTypes? }` — emitted when inline blob writes after trace insert exhaust retries, time out, or throw unexpectedly; execute outcome elsewhere is unchanged. |
| Agent trace mandatory inline fallback failed or forensic verification failed | `AgentExecutionTraceRecorder` | `AuditEventTypes.AgentTraceInlineFallbackFailed` | RunId / task context when parseable | `{ traceId, runId, agentType, reason, exceptionDetail? }` — SQL inline patch threw, trace row missing on read, or blob+inline still missing non-empty prompt/response after patch; **`dbo.AgentExecutionTraces.InlineFallbackFailed`** set; execute outcome elsewhere is unchanged. |
| Orphan comparison-record remediation (execute) | `AdminDiagnosticsService` | `ComparisonRecordOrphansRemediated` | — | `{ dryRun: false, deletedCount, comparisonRecordIds[] }` — `POST .../admin/diagnostics/data-consistency/orphan-comparison-records?dryRun=false`; dry-run calls emit no audit row. |
| Orphan golden-manifest remediation (execute) | `AdminDiagnosticsService` | `GoldenManifestOrphansRemediated` | — | `{ dryRun: false, deletedCount, manifestIds[] }` — `POST .../orphan-golden-manifests?dryRun=false`; deletes `ArtifactBundles` first. |
| Orphan findings-snapshot remediation (execute) | `AdminDiagnosticsService` | `FindingsSnapshotOrphansRemediated` | — | `{ dryRun: false, deletedCount, findingsSnapshotIds[] }` — `POST .../orphan-findings-snapshots?dryRun=false`. |
| Self-service trial bootstrap (demo seed path) | `TrialTenantBootstrapService` | `TrialProvisioned` | Tenant when parseable | trial window / demo metadata (after tenant + workspace provisioning) |
| Trial signup channel opened (`POST /v1/register`, trial local register) | `RegistrationController`, `TrialLocalIdentityAuthController` | `TrialSignupAttempted` | Empty GUID scope before tenant exists | `{ channel }` / local identity context |
| Trial signup rejected (validation, duplicate slug, bootstrap) | `RegistrationController`, `TrialLocalIdentityAuthController`, `TrialTenantBootstrapService` | `TrialSignupFailed` | Tenant scope when known | `{ stage, reason, message? }` |
| Trial first golden manifest committed (signup → first-run funnel) | `SqlTrialFunnelCommitHook` | `TrialFirstRunCompleted` | Tenant + default workspace/project | `{ signupToCommitSeconds, trialRunUsageRatio }` |
| Billing checkout session (Noop / Stripe / Marketplace) | `BillingCheckoutController` | `BillingCheckoutInitiated`, `BillingCheckoutCompleted` | Tenant from ambient scope | `{ provider, tier, providerSessionId? }` |
| Trial converted (billing integration stub) | `TenantTrialController` (`POST …/convert`) | `TenantTrialConverted` | Tenant from ambient scope | `{ targetTier }` from request body when present |
| Trial lifecycle automation (expiry → read-only → export-only → purge) | `TrialLifecycleTransitionEngine` (Worker) | `TrialLifecycleTransition` | Tenant + default workspace when known | `{ fromStatus, toStatus, reason }` JSON |

---

## Baseline mutation logging only (`IBaselineMutationAuditService` — not `dbo.AuditEvents`)

| Operation | Orchestrator / service | Event type constant | Notes |
|-----------|------------------------|---------------------|-------|
| Architecture run create / fail | `ArchitectureRunCreateOrchestrator` | `AuditEventTypes.Baseline.Architecture.*` | Entity id in `RecordAsync` is run id or request id; details string only. **Dual-write:** also emits durable `CoordinatorRunCreated` via `IAuditService`. |
| Architecture run execute / commit | `ArchitectureRunExecuteOrchestrator`, `ArchitectureRunCommitOrchestrator` | `AuditEventTypes.Baseline.Architecture.*` (`Architecture.RunStarted`, `Architecture.RunCompleted`, `Architecture.RunFailed`, …) | Same logging channel. **Dual-write:** also emits durable `CoordinatorRunExecuteStarted`, `CoordinatorRunExecuteSucceeded`, `CoordinatorRunCommitCompleted` via `IAuditService`. |
| Governance workflow | `GovernanceWorkflowService` | `AuditEventTypes.Baseline.Governance.*` (mirrors `GovernanceAuditEventTypes`) | **Dual-write:** same service also calls `IAuditService` with top-level Core governance event types (see durable table above). |

**Implication:** operators searching **Audit log** in the UI see `IAuditService` rows, including governance transitions from `GovernanceWorkflowService`. Baseline mutation logs remain for grep-friendly structured logging.

---

## Known gaps (mutating behavior without durable `IAuditService` event)

No open gaps are tracked here for the areas previously listed. Notes:

- **ConversationController** — Removed from the gap list: the controller is read-only (GET endpoints only); there are no state mutations to audit.
- **GovernanceController** — Removed from the gap list: all POST actions delegate to `GovernanceWorkflowService`, which already dual-writes `IAuditService` (Core governance event types) and `IBaselineMutationAuditService`.

---

## Coverage statistics (manual; refresh when adding call sites)

| Metric | Approximate value |
|--------|-------------------|
| **Core `AuditEventTypes` `public const string` rows** | 85 (see CI marker above; includes nested `Baseline`) |
| **`await *auditService.LogAsync` production call sites** | ~43 (excluding tests; includes bridge) |
| **`IBaselineMutationAuditService.RecordAsync` call sites** | Orchestrators + `GovernanceWorkflowService` (log-only) |
| **Gaps listed** | 0 (resolved / out-of-scope notes in section above) |

---

## Appendix — Core `AuditEventTypes` registry (one row per constant)

| Constant | Value | Durable audit producer(s) |
|----------|-------|---------------------------|
| `RunStarted` | `RunStarted` | `AuthorityRunOrchestrator` |
| `RunCompleted` | `RunCompleted` | `AuthorityRunOrchestrator` |
| `ManifestGenerated` | `ManifestGenerated` | `AuthorityPipelineStagesExecutor` |
| `ArtifactsGenerated` | `ArtifactsGenerated` | `AuthorityPipelineStagesExecutor` |
| `ReplayExecuted` | `ReplayExecuted` | `AuthorityReplayController` |
| `ArtifactDownloaded` | `ArtifactDownloaded` | `ArtifactExportController` |
| `BundleDownloaded` | `BundleDownloaded` | `ArtifactExportController` |
| `RunExported` | `RunExported` | `ArtifactExportController` |
| `ArchitectureAnalysisReportGenerated` | `ArchitectureAnalysisReportGenerated` | `AnalysisReportsController` |
| `ArchitectureDocxExportGenerated` | `ArchitectureDocxExportGenerated` | `DocxExportController` |
| `ReplayExportRecorded` | `ReplayExportRecorded` | `ExportsController` |
| `ComparisonSummaryPersisted` | `ComparisonSummaryPersisted` | `ExportsController` |
| `GovernancePreCommitBlocked` | `GovernancePreCommitBlocked` | `ArchitectureRunCommitOrchestrator` (optional pre-commit gate) |
| `GovernancePreCommitWarned` | `GovernancePreCommitWarned` | `ArchitectureRunCommitOrchestrator` (warn-only severity in pre-commit gate) |
| `GovernanceApprovalSlaBreached` | `GovernanceApprovalSlaBreached` | `ApprovalSlaMonitor` (pending approval request past SLA deadline) |
| `CoordinatorRunCreated` | `CoordinatorRunCreated` | `ArchitectureRunCreateOrchestrator` (dual-write with baseline) |
| `CoordinatorRunExecuteStarted` | `CoordinatorRunExecuteStarted` | `ArchitectureRunExecuteOrchestrator` (dual-write with baseline) |
| `CoordinatorRunExecuteSucceeded` | `CoordinatorRunExecuteSucceeded` | `ArchitectureRunExecuteOrchestrator` (dual-write with baseline) |
| `CoordinatorRunCommitCompleted` | `CoordinatorRunCommitCompleted` | `ArchitectureRunCommitOrchestrator` (dual-write with baseline) |
| `CoordinatorRunFailed` | `CoordinatorRunFailed` | `ArchitectureRunCreateOrchestrator`, `ArchitectureRunExecuteOrchestrator`, `ArchitectureRunCommitOrchestrator` (dual-write with baseline `RunFailed`) |
| `RecommendationGenerated` | `RecommendationGenerated` | `AdvisoryController` |
| `RecommendationAccepted` | `RecommendationAccepted` | `AdvisoryController` |
| `RecommendationRejected` | `RecommendationRejected` | `AdvisoryController` |
| `RecommendationDeferred` | `RecommendationDeferred` | `AdvisoryController` |
| `RecommendationImplemented` | `RecommendationImplemented` | `AdvisoryController` |
| `RecommendationLearningProfileRebuilt` | `RecommendationLearningProfileRebuilt` | `RecommendationLearningController` |
| `AdvisoryScanScheduled` | `AdvisoryScanScheduled` | `AdvisoryScanRunner`, `AdvisorySchedulingController`, `AdvisoryController` |
| `AdvisoryScanExecuted` | `AdvisoryScanExecuted` | `AdvisoryScanRunner`, `AdvisoryController` |
| `ArchitectureDigestGenerated` | `ArchitectureDigestGenerated` | `AdvisoryScanRunner`, `AdvisoryController` |
| `DigestSubscriptionCreated` | `DigestSubscriptionCreated` | `DigestSubscriptionsController` |
| `DigestSubscriptionToggled` | `DigestSubscriptionToggled` | `DigestSubscriptionsController` |
| `DigestDeliverySucceeded` | `DigestDeliverySucceeded` | `DigestDeliveryDispatcher` |
| `DigestDeliveryFailed` | `DigestDeliveryFailed` | `DigestDeliveryDispatcher` |
| `AlertRuleCreated` | `AlertRuleCreated` | `AlertRulesController` |
| `AlertTriggered` | `AlertTriggered` | `AlertService` |
| `AlertAcknowledged` | `AlertAcknowledged` | `AlertService` |
| `AlertResolved` | `AlertResolved` | `AlertService` |
| `AlertSuppressed` | `AlertSuppressed` | `AlertService` |
| `AlertRoutingSubscriptionCreated` | `AlertRoutingSubscriptionCreated` | `AlertRoutingSubscriptionsController` |
| `AlertRoutingSubscriptionToggled` | `AlertRoutingSubscriptionToggled` | `AlertRoutingSubscriptionsController` |
| `AlertDeliverySucceeded` | `AlertDeliverySucceeded` | `AlertDeliveryDispatcher` |
| `AlertDeliveryFailed` | `AlertDeliveryFailed` | `AlertDeliveryDispatcher` |
| `CompositeAlertRuleCreated` | `CompositeAlertRuleCreated` | `CompositeAlertRulesController` |
| `CompositeAlertTriggered` | `CompositeAlertTriggered` | `CompositeAlertService` |
| `AlertSuppressedByPolicy` | `AlertSuppressedByPolicy` | `CompositeAlertService` |
| `AlertRuleSimulationExecuted` | `AlertRuleSimulationExecuted` | `AlertSimulationController` |
| `AlertRuleCandidateComparisonExecuted` | `AlertRuleCandidateComparisonExecuted` | `AlertSimulationController` |
| `AlertThresholdRecommendationExecuted` | `AlertThresholdRecommendationExecuted` | `AlertTuningController` |
| `PolicyPackCreated` | `PolicyPackCreated` | `PolicyPacksAppService` |
| `PolicyPackVersionPublished` | `PolicyPackVersionPublished` | `PolicyPacksAppService` |
| `PolicyPackAssigned` | `PolicyPackAssigned` | `PolicyPacksAppService` |
| `PolicyPackAssignmentCreated` | `PolicyPackAssignmentCreated` | `PolicyPacksAppService` |
| `PolicyPackAssignmentArchived` | `PolicyPackAssignmentArchived` | `PolicyPacksAppService` |
| `GovernanceResolutionExecuted` | `GovernanceResolutionExecuted` | `GovernanceResolutionController` |
| `GovernanceConflictDetected` | `GovernanceConflictDetected` | `GovernanceResolutionController` |
| `GovernanceApprovalSubmitted` | `GovernanceApprovalSubmitted` | `GovernanceWorkflowService` |
| `GovernanceApprovalApproved` | `GovernanceApprovalApproved` | `GovernanceWorkflowService` |
| `GovernanceApprovalRejected` | `GovernanceApprovalRejected` | `GovernanceWorkflowService` |
| `GovernanceSelfApprovalBlocked` | `GovernanceSelfApprovalBlocked` | `GovernanceWorkflowService` |
| `GovernanceManifestPromoted` | `GovernanceManifestPromoted` | `GovernanceWorkflowService` |
| `GovernanceEnvironmentActivated` | `GovernanceEnvironmentActivated` | `GovernanceWorkflowService` |
| `DataArchivalHostLoopFailed` | `DataArchivalHostLoopFailed` | `DataArchivalHostIteration` |
| `CircuitBreakerStateTransition` | `CircuitBreakerStateTransition` | `CircuitBreakerAuditBridge` |
| `CircuitBreakerRejection` | `CircuitBreakerRejection` | `CircuitBreakerAuditBridge` |
| `CircuitBreakerProbeOutcome` | `CircuitBreakerProbeOutcome` | `CircuitBreakerAuditBridge` |
| `TenantProvisioned` | `TenantProvisioned` | `TenantProvisioningService` |
| `TenantSelfRegistered` | `TenantSelfRegistered` | `RegistrationController` |
| `TrialProvisioned` | `TrialProvisioned` | `TrialTenantBootstrapService` |
| `TrialSignupAttempted` | `TrialSignupAttempted` | `RegistrationController`, `TrialLocalIdentityAuthController` |
| `TrialSignupFailed` | `TrialSignupFailed` | `RegistrationController`, `TrialLocalIdentityAuthController`, `TrialTenantBootstrapService` |
| `TrialFirstRunCompleted` | `TrialFirstRunCompleted` | `SqlTrialFunnelCommitHook` |
| `BillingCheckoutInitiated` | `BillingCheckoutInitiated` | `BillingCheckoutController` |
| `BillingCheckoutCompleted` | `BillingCheckoutCompleted` | `BillingCheckoutController` |
| `TenantTrialConverted` | `TenantTrialConverted` | `TenantTrialController` |
| `TrialLifecycleTransition` | `TrialLifecycleTransition` | `TrialLifecycleTransitionEngine` |
| `ComparisonRecordOrphansRemediated` | `ComparisonRecordOrphansRemediated` | `AdminDiagnosticsService` (orphan comparison-record remediation execute) |
| `GoldenManifestOrphansRemediated` | `GoldenManifestOrphansRemediated` | `AdminDiagnosticsService` (orphan golden-manifest remediation execute) |
| `FindingsSnapshotOrphansRemediated` | `FindingsSnapshotOrphansRemediated` | `AdminDiagnosticsService` (orphan findings-snapshot remediation execute) |
| `AgentResultSchemaViolation` | `AgentResultSchemaViolation` | `AgentResultSchemaViolationAudit` (topology / compliance / critic handlers on `AgentResultSchemaViolationException`) |
| `AgentTraceBlobPersistenceFailed` | `AgentTraceBlobPersistenceFailed` | `AgentExecutionTraceRecorder` |
| `AgentTraceInlineFallbackFailed` | `AgentTraceInlineFallbackFailed` | `AgentExecutionTraceRecorder` |

When adding a Core constant, add a row here and bump `audit-core-const-count`.

---

## Appendix — `AuditEventTypes.Baseline` registry (structured baseline log only)

| Constant path | Value | Baseline producer(s) |
|---------------|-------|----------------------|
| `Baseline.Architecture.RunCreated` | `Architecture.RunCreated` | `ArchitectureRunCreateOrchestrator` |
| `Baseline.Architecture.RunStarted` | `Architecture.RunStarted` | `ArchitectureRunExecuteOrchestrator` |
| `Baseline.Architecture.RunExecuteSucceeded` | `Architecture.RunExecuteSucceeded` | `ArchitectureRunExecuteOrchestrator` |
| `Baseline.Architecture.RunCompleted` | `Architecture.RunCompleted` | `ArchitectureRunCommitOrchestrator` |
| `Baseline.Architecture.RunFailed` | `Architecture.RunFailed` | Architecture run orchestrators, `ArchitectureRunService` |
| `Baseline.Governance.ApprovalRequestSubmitted` | `Governance.ApprovalRequestSubmitted` | `GovernanceWorkflowService` |
| `Baseline.Governance.ApprovalRequestApproved` | `Governance.ApprovalRequestApproved` | `GovernanceWorkflowService` |
| `Baseline.Governance.ApprovalRequestRejected` | `Governance.ApprovalRequestRejected` | `GovernanceWorkflowService` |
| `Baseline.Governance.ManifestPromoted` | `Governance.ManifestPromoted` | `GovernanceWorkflowService` |
| `Baseline.Governance.EnvironmentActivated` | `Governance.EnvironmentActivated` | `GovernanceWorkflowService` |

When adding a `Baseline` constant, add a row here and bump `audit-core-const-count`.

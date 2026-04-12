# Audit coverage matrix

This document maps **state-changing** workflows to the audit signals they emit. ArchLucid has two parallel **channels** that share one **string catalog** in `ArchLucid.Core.Audit.AuditEventTypes`:

1. **Durable SQL audit** — `IAuditService.LogAsync` → `IAuditRepository.AppendAsync` → `dbo.AuditEvents` (`ArchLucid.Core.Audit.AuditEvent`). Event types use **top-level** `AuditEventTypes.*` constants (e.g. `RunStarted`, `GovernanceApprovalSubmitted`).
2. **Baseline mutation log** — `IBaselineMutationAuditService.RecordAsync` → structured **ILogger** lines only (`ArchLucid.Application.Common.BaselineMutationAuditService`). Event types use **`AuditEventTypes.Baseline.Architecture.*`** and **`AuditEventTypes.Baseline.Governance.*`** (namespaced string values). These **do not** populate `dbo.AuditEvents`.

`ArchLucid.Application.Governance.GovernanceAuditEventTypes` mirrors **`AuditEventTypes.Baseline.Governance`** values for documentation and some workflow code paths. **`GovernanceWorkflowService`** dual-writes: baseline channel with **`Baseline.Governance.*`** **and** `IAuditService` with top-level `GovernanceApprovalSubmitted` / `GovernanceApprovalApproved` / `GovernanceApprovalRejected` / `GovernanceManifestPromoted` / `GovernanceEnvironmentActivated` (durable `EventType` strings differ from baseline — see XML remarks on `AuditEventTypes.Baseline`).

<!-- audit-core-const-count:66 -->

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
| **Database-level append-only on `dbo.AuditEvents`** | Migration **`051_AuditEvents_DenyUpdateDelete.sql`** (and the same idempotent **`DENY`** block in **`ArchLucid.Persistence/Scripts/ArchLucid.sql`** after the table DDL) issues **`DENY UPDATE`** and **`DENY DELETE`** on **`dbo.AuditEvents`** to the database role **`ArchLucidApp`** when that role exists. This closes the gap where code only `INSERT`s but ad-hoc SQL or bugs could mutate rows. **`dbo` / `db_owner`** are unaffected for break-glass. Local dev often has no **`ArchLucidApp`** role (app runs as **`dbo`** / SQL auth admin) — the migration **skips** until operators create the role and add the managed identity or SQL user (see **`docs/security/MANAGED_IDENTITY_SQL_BLOB.md`**). Deployments that only use **`db_datawriter`** without **`ArchLucidApp`** should create the role and move the app principal into it, or apply an environment-specific **`DENY`** to **`[db_datawriter]`** for this table. |

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
| Recommendation learning rebuild | `RecommendationLearningController` | `RecommendationLearningProfileRebuilt` | — | profile id |
| Artifact / bundle / run export download | `ArtifactExportController` | `ArtifactDownloaded`, `BundleDownloaded`, `RunExported` | RunId (+ artifact when applicable) | format, byte counts, etc. |
| Architecture analysis report (primary JSON build) | `AnalysisReportsController` | `ArchitectureAnalysisReportGenerated` | RunId when parseable | section flags, `manifestVersion`, `warningCount` |
| Architecture package DOCX download | `DocxExportController` | `ArchitectureDocxExportGenerated` | RunId, ManifestId | `runId`, `compareWithRunId`, `byteCount` |
| Replay export persisted as new row | `ExportsController` (replay POST + metadata POST when `RecordReplayExport`) | `ReplayExportRecorded` | RunId when parseable | `sourceExportRecordId`, `recordedReplayExportRecordId`, `runId` |
| Data archival host failure | `DataArchivalHostIteration` | `DataArchivalHostLoopFailed` | — | exception summary |
| OpenAI circuit breaker | `CircuitBreakerAuditBridge` (wired from `CircuitBreakerGate`) | `CircuitBreakerStateTransition`, `CircuitBreakerRejection`, `CircuitBreakerProbeOutcome` | Tenant/Workspace/Project from ambient scope | `{ gate, fromState, toState, probeOutcome? }` |

---

## Baseline mutation logging only (`IBaselineMutationAuditService` — not `dbo.AuditEvents`)

| Operation | Orchestrator / service | Event type constant | Notes |
|-----------|------------------------|---------------------|-------|
| Architecture run create / fail | `ArchitectureRunCreateOrchestrator` | `AuditEventTypes.Baseline.Architecture.*` | Entity id in `RecordAsync` is run id or request id; details string only. |
| Architecture run execute / commit | `ArchitectureRunExecuteOrchestrator`, `ArchitectureRunCommitOrchestrator` | `AuditEventTypes.Baseline.Architecture.*` (`Architecture.RunStarted`, `Architecture.RunCompleted`, `Architecture.RunFailed`, …) | Same logging channel. |
| Governance workflow | `GovernanceWorkflowService` | `AuditEventTypes.Baseline.Governance.*` (mirrors `GovernanceAuditEventTypes`) | **Dual-write:** same service also calls `IAuditService` with top-level Core governance event types (see durable table above). |

**Implication:** operators searching **Audit log** in the UI see `IAuditService` rows, including governance transitions from `GovernanceWorkflowService`. Baseline mutation logs remain for grep-friendly structured logging.

---

## Known gaps (mutating behavior without durable `IAuditService` event)

No open gaps are tracked here for the areas previously listed. Notes:

- **ConversationController** — Removed from the gap list: the controller is read-only (GET endpoints only); there are no state mutations to audit.
- **GovernanceController** — Removed from the gap list: all POST actions delegate to `GovernanceWorkflowService`, which already dual-writes `IAuditService` (Core governance event types) and `IBaselineMutationAuditService`.

**Note:** `ExportsController` `POST .../exports/compare/summary` with `persist: true` still records via `IComparisonAuditService` only (comparison audit tables). That path is separate from replay-export persistence and does not emit a Core `ReplayExportRecorded` row.

---

## Coverage statistics (manual; refresh when adding call sites)

| Metric | Approximate value |
|--------|-------------------|
| **Core `AuditEventTypes` `public const string` rows** | 66 (see CI marker above; includes nested `Baseline`) |
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

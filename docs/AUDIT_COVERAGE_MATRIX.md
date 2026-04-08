# Audit coverage matrix

This document maps **state-changing** workflows to the audit signals they emit. ArchLucid has two parallel tracks:

1. **Durable SQL audit** — `IAuditService.LogAsync` → `IAuditRepository.AppendAsync` → `dbo.AuditEvents` (`ArchLucid.Core.Audit.AuditEvent`, `ArchLucid.Core.Audit.AuditEventTypes`).
2. **Baseline mutation log** — `IBaselineMutationAuditService.RecordAsync` → structured **ILogger** lines only (`ArchLucid.Application.Common.BaselineMutationAuditService`). These use `ArchLucid.Application.Common.AuditEventTypes` names but **do not** populate `dbo.AuditEvents`.

A third string registry, `ArchLucid.Application.Governance.GovernanceAuditEventTypes`, mirrors governance event names for documentation and workflow code paths. **`GovernanceWorkflowService`** dual-writes: `IBaselineMutationAuditService` (structured logs) **and** `IAuditService` with Core `GovernanceApprovalSubmitted` / `GovernanceApprovalApproved` / `GovernanceApprovalRejected` / `GovernanceManifestPromoted` / `GovernanceEnvironmentActivated`.

<!-- audit-core-const-count:52 -->

The HTML comment above is a **CI anchor**: `.github/workflows/ci.yml` compares `grep -c 'public const string' ArchLucid.Core/Audit/AuditEventTypes.cs` to the number in this comment. Update the comment whenever Core constants change, and extend the appendix table below.

---

## Design notes (ADR-style)

| Decision | Rationale |
|----------|-----------|
| **Circuit breaker → audit is fire-and-forget** | `CircuitBreakerGate` sits on the hot path; awaiting SQL or disk I/O would add tail latency and failure modes. The bridge schedules `LogAsync` on the thread pool and swallows exceptions so telemetry and breaker semantics stay reliable. |
| **Callback on `CircuitBreakerGate` instead of `IAuditService` in Core** | `ArchLucid.Core` must not reference persistence or host services. An optional `Action<CircuitBreakerAuditEntry>?` keeps tier boundaries; composition roots wire `CircuitBreakerAuditBridge.CreateCallback()`. |
| **`GetFilteredAsync` instead of overloading `GetByScopeAsync`** | Eight or more call sites and test doubles already depend on the original signature. A new method adds filtering without breaking consumers. |
| **Static matrix + CI count guard** | Cheaper than runtime introspection of all call sites. The matrix can drift; the CI guard at least forces developers to revisit this file when `AuditEventTypes` grows. |

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
| Governance workflow (approval / promote / activate) | `GovernanceWorkflowService` | `GovernanceApprovalSubmitted`, `GovernanceApprovalApproved`, `GovernanceApprovalRejected`, `GovernanceManifestPromoted`, `GovernanceEnvironmentActivated` | RunId when parseable | ids, environments, manifest version (JSON) |
| Recommendation learning rebuild | `RecommendationLearningController` | `RecommendationLearningProfileRebuilt` | — | profile id |
| Artifact / bundle / run export download | `ArtifactExportController` | `ArtifactDownloaded`, `BundleDownloaded`, `RunExported` | RunId (+ artifact when applicable) | format, byte counts, etc. |
| Data archival host failure | `DataArchivalHostIteration` | `DataArchivalHostLoopFailed` | — | exception summary |
| OpenAI circuit breaker | `CircuitBreakerAuditBridge` (wired from `CircuitBreakerGate`) | `CircuitBreakerStateTransition`, `CircuitBreakerRejection`, `CircuitBreakerProbeOutcome` | Tenant/Workspace/Project from ambient scope | `{ gate, fromState, toState, probeOutcome? }` |

---

## Baseline mutation logging only (`IBaselineMutationAuditService` — not `dbo.AuditEvents`)

| Operation | Orchestrator / service | Event type constant | Notes |
|-----------|------------------------|---------------------|-------|
| Architecture run create / fail | `ArchitectureRunCreateOrchestrator` | `Application.Common.AuditEventTypes.Architecture.*` | Entity id in `RecordAsync` is run id or request id; details string only. |
| Architecture run execute / commit | `ArchitectureRunExecuteOrchestrator`, `ArchitectureRunCommitOrchestrator` | `Architecture.RunStarted`, `Architecture.RunCompleted`, `Architecture.RunFailed`, … | Same logging channel. |
| Governance workflow | `GovernanceWorkflowService` | `Application.Common.AuditEventTypes.Governance.*` (mirrors `GovernanceAuditEventTypes`) | **Dual-write:** same service also calls `IAuditService` with Core governance event types (see durable table above). |

**Implication:** operators searching **Audit log** in the UI see `IAuditService` rows, including governance transitions from `GovernanceWorkflowService`. Baseline mutation logs remain for grep-friendly structured logging.

---

## Known gaps (mutating behavior without durable `IAuditService` event)

| Area | Suggested Core `AuditEventTypes` name (new) | Notes |
|------|-----------------------------------------------|-------|
| `AnalysisReportsController` (report generation persistence) | `ArchitectureAnalysisReportGenerated` | Creates stored report content; today may use other persistence without audit row. |
| `ExportsController` / comparison replay flows | `RunExportRecorded` or reuse `RunExported` with clear payload | Uses `IComparisonAuditService` / export tables, not necessarily `AuditEvents`. |
| `ConversationController` (threads / messages) | `ConversationThreadCreated`, `ConversationMessageAppended` | No `IAuditService` usage found. |
| `DocxExportController` | `ArchitectureDocxExportGenerated` | Parallel to analysis artifacts. |
| `GovernanceController` (HTTP surface beyond workflow service) | Align with `Governance.*` or Core governance types | Verify each POST/PATCH; add `IAuditService` where missing. |

---

## Coverage statistics (manual; refresh when adding call sites)

| Metric | Approximate value |
|--------|-------------------|
| **Core `AuditEventTypes` constants** | 52 (see CI marker above) |
| **`await *auditService.LogAsync` production call sites** | ~40 (excluding tests; includes bridge) |
| **`IBaselineMutationAuditService.RecordAsync` call sites** | Orchestrators + `GovernanceWorkflowService` (log-only) |
| **Gaps listed** | 5 rows in table above (plus “dual registry” governance note) |

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
| `GovernanceManifestPromoted` | `GovernanceManifestPromoted` | `GovernanceWorkflowService` |
| `GovernanceEnvironmentActivated` | `GovernanceEnvironmentActivated` | `GovernanceWorkflowService` |
| `DataArchivalHostLoopFailed` | `DataArchivalHostLoopFailed` | `DataArchivalHostIteration` |
| `CircuitBreakerStateTransition` | `CircuitBreakerStateTransition` | `CircuitBreakerAuditBridge` |
| `CircuitBreakerRejection` | `CircuitBreakerRejection` | `CircuitBreakerAuditBridge` |
| `CircuitBreakerProbeOutcome` | `CircuitBreakerProbeOutcome` | `CircuitBreakerAuditBridge` |

When adding a Core constant, add a row here and bump `audit-core-const-count`.

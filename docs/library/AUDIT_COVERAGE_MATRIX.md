> **Scope:** Audit coverage matrix - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Audit coverage matrix

This document maps **state-changing** workflows to the audit signals they emit. ArchLucid has two parallel **channels** that share one **string catalog** in `ArchLucid.Core.Audit.AuditEventTypes`:

1. **Durable SQL audit** — `IAuditService.LogAsync` → `IAuditRepository.AppendAsync` → `dbo.AuditEvents` (`ArchLucid.Core.Audit.AuditEvent`). Event types use **top-level** `AuditEventTypes.*` constants (e.g. `RunStarted`, `GovernanceApprovalSubmitted`).
2. **Baseline mutation log** — `IBaselineMutationAuditService.RecordAsync` → structured **ILogger** lines only (`ArchLucid.Application.Common.BaselineMutationAuditService`). Event types use **`AuditEventTypes.Baseline.Architecture.*`** and **`AuditEventTypes.Baseline.Governance.*`** (namespaced string values). These **do not** populate `dbo.AuditEvents`.

`ArchLucid.Application.Governance.GovernanceAuditEventTypes` mirrors **`AuditEventTypes.Baseline.Governance`** values for documentation and some workflow code paths. **`GovernanceWorkflowService`** dual-writes: baseline channel with **`Baseline.Governance.*`** **and** `IAuditService` with top-level `GovernanceApprovalSubmitted` / `GovernanceApprovalApproved` / `GovernanceApprovalRejected` / `GovernanceManifestPromoted` / `GovernanceEnvironmentActivated` (durable `EventType` strings differ from baseline — see XML remarks on `AuditEventTypes.Baseline`).

<!-- audit-core-const-count:126 -->

The HTML comment above is a **CI anchor**: `.github/workflows/ci.yml` runs `scripts/ci/assert_audit_const_count.py`, which parses every `public const string` in `ArchLucid.Core/Audit/AuditEventTypes.cs` (top-level, `Run`, and `Baseline.*`), cross-checks names against the three appendix tables in this file, and compares the count to this comment. Update the comment whenever constants change, and extend the appendix rows below.

---

## Design notes (ADR-style)

| Decision | Rationale |
|----------|-----------|
| **Circuit breaker → audit is fire-and-forget** | `CircuitBreakerGate` sits on the hot path; awaiting SQL or disk I/O would add tail latency and failure modes. The bridge schedules `LogAsync` on the thread pool and swallows exceptions so telemetry and breaker semantics stay reliable. |
| **Callback on `CircuitBreakerGate` instead of `IAuditService` in Core** | `ArchLucid.Core` must not reference persistence or host services. An optional `Action<CircuitBreakerAuditEntry>?` keeps tier boundaries; composition roots wire `CircuitBreakerAuditBridge.CreateCallback()`. |
| **`GetFilteredAsync` instead of overloading `GetByScopeAsync`** | Eight or more call sites and test doubles already depend on the original signature. A new method adds filtering without breaking consumers. |
| **Static matrix + CI guard** | Cheaper than runtime introspection of all call sites. The matrix can drift; `assert_audit_const_count.py` fails merge with a per-name diff when rows or the count marker disagree with `AuditEventTypes.cs`. |
| **Single Core catalog for baseline + durable** | Application references `ArchLucid.Core.Audit.AuditEventTypes.Baseline` so operators and developers have one file for all event-type strings; nested `Baseline` preserves namespaced baseline values without colliding with authority `RunStarted` / `RunCompleted`. |
| **Coordinator orchestration durable echo** | The coordinator orchestrators (`Create`, `Execute`, `Commit`) call `IBaselineMutationAuditService.RecordAsync` for baseline `Architecture.*` events; `BaselineMutationAuditService` appends one durable `dbo.AuditEvents` row per signal using **`AuditEventTypes.Run.*`** via `BaselineMutationAuditArchitectureDurableWriter` (legacy `CoordinatorRun*` constants were removed). Pre-commit governance warnings/blocks on commit still call `IAuditService.LogAsync` directly from `ArchitectureRunCommitOrchestrator`. Failures on the durable echo path are swallowed — audit must not break orchestration. |
| **Critical-path durable audit retry** | `Run.Created`, `Run.ExecuteStarted`, `Run.ExecuteSucceeded`, and `Run.CommitCompleted` echoes use `ArchLucid.Core.Audit.DurableAuditLogRetry` (short exponential backoff, default 3 attempts). `Run.Failed` uses a single attempt with inner `try/catch` in the writer. After exhaustion, failures are logged only — orchestration still completes. |
| **Database-level append-only on `dbo.AuditEvents`** | Migration **`051_AuditEvents_DenyUpdateDelete.sql`** (and the same idempotent **`DENY`** block in **`ArchLucid.Persistence/Scripts/ArchLucid.sql`** after the table DDL) issues **`DENY UPDATE`** and **`DENY DELETE`** on **`dbo.AuditEvents`** to the database role **`ArchLucidApp`** when that role exists. This closes the gap where code only `INSERT`s but ad-hoc SQL or bugs could mutate rows. **`dbo` / `db_owner`** are unaffected for break-glass. Local dev often has no **`ArchLucidApp`** role (app runs as **`dbo`** / SQL auth admin) — the migration **skips** until operators create the role and add the managed identity or SQL user (see **`docs/security/MANAGED_IDENTITY_SQL_BLOB.md`**). Deployments that only use **`db_datawriter`** without **`ArchLucidApp`** should create the role and move the app principal into it, or apply an environment-specific **`DENY`** to **`[db_datawriter]`** for this table. |

### Indexes on `dbo.AuditEvents`

| Index | Columns (notes) | Purpose |
|-------|-----------------|---------|
| **`IX_AuditEvents_Scope_OccurredUtc`** | `(TenantId, WorkspaceId, ProjectId, OccurredUtc DESC)` | Default newest-first listing within scope. |
| **`IX_AuditEvents_CorrelationId`** | `(CorrelationId)` **filtered** `WHERE CorrelationId IS NOT NULL` | Fast `GET /v1/audit/search?correlationId=…` and cross-request forensics. Added in migration **`055_AuditEvents_CorrelationId_RunId_Indexes.sql`**. |
| **`IX_AuditEvents_RunId_OccurredUtc`** | `(RunId, OccurredUtc DESC)` **filtered** `WHERE RunId IS NOT NULL` | Per-run audit timeline by `RunId`. Same migration. |
| **`IX_AuditEvents_OccurredUtc_EventId`** | `(OccurredUtc DESC, EventId DESC)` **INCLUDE** tenant/workspace/project + `EventType` + `ActorUserId` + `RunId` | Stable keyset pagination for `GET /v1/audit/search` when many events share the same `OccurredUtc`. Migration **`109_AuditEvents_OccurredUtc_EventId_KeysetIndex.sql`**. |

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
| Governance policy-pack dry-run (what-if) | `PolicyPackDryRunService` (`POST /v1/governance/policy-packs/{id}/dry-run`) | `GovernanceDryRunRequested` | Tenant/Workspace/Project from ambient scope | `{ policyPackId, proposedThresholdsRedacted (string — proposedThresholds JSON after `LlmPromptRedaction`), evaluatedRunIds[], deltaCounts: { evaluated, wouldBlock, wouldAllow, runMissing } }` — payload **must** flow through the redaction pipeline (PENDING_QUESTIONS Q37); read-auth gated, no real commit. |
| Pre-commit governance warn | `ArchitectureRunCommitOrchestrator` | `GovernancePreCommitWarned` | RunId when parseable | `reason`, `warnings`, `blockingFindingIds`, `policyPackId`, `minimumBlockingSeverity` |
| Recommendation learning rebuild | `RecommendationLearningController` | `RecommendationLearningProfileRebuilt` | — | profile id |
| Artifact / bundle / run export download | `ArtifactExportController` | `ArtifactDownloaded`, `BundleDownloaded`, `RunExported` | RunId (+ artifact when applicable) | format, byte counts, etc. |
| Architecture analysis report (primary JSON build) | `AnalysisReportsController` | `ArchitectureAnalysisReportGenerated` | RunId when parseable | section flags, `manifestVersion`, `warningCount` |
| Architecture package DOCX download | `DocxExportController` | `ArchitectureDocxExportGenerated` | RunId, ManifestId | `runId`, `compareWithRunId`, `byteCount` |
| Architecture request file import (TOML/JSON draft) | `ImportRequestFileService` (`POST …/architecture/request/import`, `ImportRequestFileController`) | `RequestFileImported` | Tenant/Workspace/Project from ambient scope | `importId`, `requestId`, `format`, `sourceFileName` (JSON payload); correlation id when HTTP trace present |
| Tenant value report DOCX (sync or async completion) | `ValueReportController` | `ValueReportGenerated` | Tenant/Workspace/Project from ambient scope | `tenantId`, `from`, `to`, `byteCount`, `asyncJob` (JSON); async jobs also include `jobId` |
| Replay export persisted as new row | `ExportsController` (replay POST + metadata POST when `RecordReplayExport`) | `ReplayExportRecorded` | RunId when parseable | `sourceExportRecordId`, `recordedReplayExportRecordId`, `runId` |
| Comparison summary persisted (export diff) | `ExportsController` (`POST .../run/exports/compare/summary`, `persist: true`) | `ComparisonSummaryPersisted` | RunId when parseable | `comparisonId`, `sourceExportRecordId`, `leftExportRecordId`, `rightExportRecordId` |
| Data archival host failure | `DataArchivalHostIteration` | `DataArchivalHostLoopFailed` | — | exception summary |
| OpenAI circuit breaker | `CircuitBreakerAuditBridge` (wired from `CircuitBreakerGate`) | `CircuitBreakerStateTransition`, `CircuitBreakerRejection`, `CircuitBreakerProbeOutcome` | Tenant/Workspace/Project from ambient scope | `{ gate, fromState, toState, probeOutcome? }` |
| Security assessment published (trust center / procurement) | `SecurityTrustPublicationController` | `SecurityAssessmentPublished` | Tenant/Workspace/Project from ambient scope | `{ assessmentCode, summaryReference, assessorDisplayName? }` |
| Agent result JSON failed schema validation (enforced parse) | `TopologyAgentHandler`, `ComplianceAgentHandler`, `CriticAgentHandler` → `AgentResultSchemaViolationAudit` | `AuditEventTypes.AgentResultSchemaViolation` | RunId / task context when parseable | schema errors, truncated JSON, agent type |
| Coordinator run created (baseline → durable) | `BaselineMutationAuditService` (triggered by `ArchitectureRunCreateOrchestrator` baseline `Architecture.RunCreated`) | `AuditEventTypes.Run.Created` | RunId | `{ requestId, systemName }` |
| Coordinator run execution started (baseline → durable) | `BaselineMutationAuditService` (`ArchitectureRunExecuteOrchestrator` → `Architecture.RunStarted`) | `AuditEventTypes.Run.ExecuteStarted` | RunId | `{ runId }` |
| Coordinator run execution succeeded (baseline → durable) | `BaselineMutationAuditService` (`ArchitectureRunExecuteOrchestrator` → `Architecture.RunExecuteSucceeded`) | `AuditEventTypes.Run.ExecuteSucceeded` | RunId | `{ runId, resultCount }` |
| Coordinator run commit completed (baseline → durable) | `BaselineMutationAuditService` (`ArchitectureRunCommitOrchestrator` / `AuthorityDrivenArchitectureRunCommitOrchestrator` → `Architecture.RunCompleted`) | `AuditEventTypes.Run.CommitCompleted` | RunId | Coordinator path: `{ runId, manifestVersion, systemName }`; authority path adds `warningCount`, `commitPath` |
| Coordinator run failed (baseline → durable) | `BaselineMutationAuditService` (orchestrators → `Architecture.RunFailed`) via `BaselineMutationAuditArchitectureDurableWriter` | `AuditEventTypes.Run.Failed` | RunId when parseable | `{ runId, reason }` (after baseline `Architecture.RunFailed`) |
| Agent trace blob persistence failed or timed out | `AgentExecutionTraceRecorder` | `AuditEventTypes.AgentTraceBlobPersistenceFailed` | RunId / task context when parseable | `{ traceId, runId, agentType, reason, failedBlobTypes? }` — emitted when inline blob writes after trace insert exhaust retries, time out, or throw unexpectedly; execute outcome elsewhere is unchanged. |
| Agent trace mandatory inline fallback failed or forensic verification failed | `AgentExecutionTraceRecorder` | `AuditEventTypes.AgentTraceInlineFallbackFailed` | RunId / task context when parseable | `{ traceId, runId, agentType, reason, exceptionDetail? }` — SQL inline patch threw, trace row missing on read, or blob+inline still missing non-empty prompt/response after patch; **`dbo.AgentExecutionTraces.InlineFallbackFailed`** set; execute outcome elsewhere is unchanged. |
| Orphan comparison-record remediation (execute) | `AdminDiagnosticsService` | `ComparisonRecordOrphansRemediated` | — | `{ dryRun: false, deletedCount, comparisonRecordIds[] }` — `POST .../admin/diagnostics/data-consistency/orphan-comparison-records?dryRun=false`; dry-run calls emit no audit row. |
| Orphan golden-manifest remediation (execute) | `AdminDiagnosticsService` | `GoldenManifestOrphansRemediated` | — | `{ dryRun: false, deletedCount, manifestIds[] }` — `POST .../orphan-golden-manifests?dryRun=false`; deletes `ArtifactBundles` first. |
| Orphan findings-snapshot remediation (execute) | `AdminDiagnosticsService` | `FindingsSnapshotOrphansRemediated` | — | `{ dryRun: false, deletedCount, findingsSnapshotIds[] }` — `POST .../orphan-findings-snapshots?dryRun=false`. |
| Self-service trial bootstrap (demo seed path) | `TrialTenantBootstrapService` | `TrialProvisioned` | Tenant when parseable | trial window / demo metadata (after tenant + workspace provisioning) |
| Trial signup channel opened (`POST /v1/register`, trial local register) | `RegistrationController`, `TrialLocalIdentityAuthController` | `TrialSignupAttempted` | Empty GUID scope before tenant exists | `{ channel }` / local identity context |
| Public registration API failed (`POST /v1/register` — validation, duplicate org, or internal) | `RegistrationController` | `TrialRegistrationFailed` | Empty tenant scope (or after attempt) | `{ reason, code, message? }` — `reason` is `validation` / `conflict` / `internal` |
| Trial signup rejected (local identity, email policy, bootstrap; not `POST /v1/register` body path) | `TrialLocalIdentityAuthController`, `TrialTenantBootstrapService` | `TrialSignupFailed` | Tenant scope when known | `{ stage, reason, message? }` |
| Trial first golden manifest committed (signup → first-run funnel) | `SqlTrialFunnelCommitHook` | `TrialFirstRunCompleted` | Tenant + default workspace/project | `{ signupToCommitSeconds, trialRunUsageRatio }` |
| Authority committed manifest FK chain (demo trusted-baseline seed) | `DemoSeedService` | `AuthorityCommittedChainPersisted` | RunId, ManifestId | `{ source: "demo-seed", projectSlug, richFindingsAndGraph, contextSnapshotId, graphSnapshotId, findingsSnapshotId, decisionTraceId, manifestId }` |
| Authority committed manifest FK chain (replay commit) | `ReplayRunService` | `AuthorityCommittedChainPersisted` | RunId, ManifestId | `{ source: "replay-commit", projectSlug, richFindingsAndGraph: true, … }` — emitted only after `CommitAsync` succeeds. |
| Billing checkout session (Noop / Stripe / Marketplace) | `BillingCheckoutController` | `BillingCheckoutInitiated`, `BillingCheckoutCompleted` | Tenant from ambient scope | `{ provider, tier, providerSessionId? }` |
| Customer notification channel preferences upsert | `CustomerNotificationChannelPreferencesController` (`PUT …/customer-channel-preferences`) | `TenantNotificationChannelPreferencesUpdated` | Tenant + default workspace/project from scope | `{ email, teams, outboundWebhook }` booleans |
| Microsoft Teams incoming-webhook connection upsert | `TeamsIncomingWebhookConnectionsController` (`POST /v1/integrations/teams/connections`) | `TenantTeamsIncomingWebhookConnectionUpserted` | Tenant + default workspace/project from scope | Key Vault reference metadata (no secret material) |
| Microsoft Teams incoming-webhook connection remove | `TeamsIncomingWebhookConnectionsController` (`DELETE /v1/integrations/teams/connections`) | `TenantTeamsIncomingWebhookConnectionRemoved` | Tenant + default workspace/project from scope | connection id / scope fields |
| Weekly executive digest preferences upsert | `TenantExecDigestPreferencesController` (`POST …/tenant/exec-digest-preferences`) | `ExecDigestPreferencesUpdated` | Tenant + default workspace/project from scope | digest cadence / channel booleans (JSON) |
| Trial converted (billing integration stub) | `TenantTrialController` (`POST …/convert`) | `TenantTrialConverted` | Tenant from ambient scope | `{ targetTier }` from request body when present |
| Trial lifecycle automation (expiry → read-only → export-only → purge) | `TrialLifecycleTransitionEngine` (Worker) | `TrialLifecycleTransition` | Tenant + default workspace when known | `{ fromStatus, toStatus, reason }` JSON |
| LLM tenant daily budget warn (fire-and-forget) | `LlmDailyTenantBudgetTracker` | `AuditEventTypes.LlmTenantDailyBudgetApproaching` | Tenant/Workspace/Project from ambient scope | `{ utcDay, usedTotal, warnAt, maxTotal }` — emitted at most **once per tenant per UTC day**; scheduled on the thread pool with exception swallowing so the LLM completion path is never blocked. |
| SCIM bearer token minted (Enterprise) | `ScimTokensAdminController` (`POST /v1/admin/scim/tokens`) | `ScimTokenIssued` | Tenant from ambient scope | `{ tokenId, publicLookupKey }` — plaintext token returned once in response body only. |
| SCIM bearer token revoked | `ScimTokensAdminController` (`DELETE /v1/admin/scim/tokens/{id}`) | `ScimTokenRevoked` | Tenant from ambient scope | `{ tokenId }` |
| SCIM user provisioned | `ScimUserService` (`POST /scim/v2/Users`) | `ScimUserProvisioned` | Tenant from `IScopeContextProvider` | SCIM user id / externalId summary (JSON) |
| SCIM user updated (replace / patch) | `ScimUserService` | `ScimUserUpdated` | Tenant from scope | user id + changed fields summary |
| SCIM user deactivated | `ScimUserService` (deprovision / `Active=false`) | `ScimUserDeactivated` | Tenant from scope | user id |
| SCIM group provisioned | `ScimGroupService` | `ScimGroupProvisioned` | Tenant from scope | group id / displayName |
| SCIM group membership changed | `ScimGroupService` (`members` replace / patch) | `ScimGroupMembershipChanged` | Tenant from scope | `{ groupId }` and membership delta summary |
| Pilot `try --real` execute started (Development; real AOAI path) | `RunsController` (`POST .../execute`) when pilot real headers present | `FirstRealValueRunStarted` | RunId | pilot / real-mode context (JSON) |
| Pilot `try --real` execute completed without fallback | `RunsController` | `FirstRealValueRunCompleted` | RunId | completion summary (JSON) |
| Pilot `try --real` seed after AOAI fallback | `ArchitectureApplicationService` (`SeedFakeResultsAsync` with `PilotSeedFakeResultsOptions.MarkRealModeFellBackToSimulator`) | `FirstRealValueRunFellBackToSimulator` | RunId | marks run row + deployment snapshot; see [`docs/library/FIRST_REAL_VALUE.md`](FIRST_REAL_VALUE.md) |
| Legacy run header promoted post-execute (`dbo.Runs.LegacyRunStatus` → `ReadyForCommit` when Topology/Cost/Compliance/Critic each yielded one result — ADR-0012) | `ArchitectureRunExecuteOrchestrator.TryPromoteRunLegacyStatusIfAllResultsPresentAsync` | `RunLegacyReadyForCommitPromoted` | RunId | `{ runId, previousLegacyRunStatus, newLegacyRunStatus }` — direct `IAuditService` (distinct from coordinator `Run.*` durable echo baseline path; applies when promotion mutates SQL) |

---

## Baseline mutation logging only (`IBaselineMutationAuditService` — not `dbo.AuditEvents`)

| Operation | Orchestrator / service | Event type constant | Notes |
|-----------|------------------------|---------------------|-------|
| Architecture run create / fail | `ArchitectureRunCreateOrchestrator` | `AuditEventTypes.Baseline.Architecture.*` | Entity id in `RecordAsync` is run id or request id; details string only. **Durable echo:** `BaselineMutationAuditService` appends `AuditEventTypes.Run.*` rows (see durable table). |
| Architecture run execute / commit | `ArchitectureRunExecuteOrchestrator`, `ArchitectureRunCommitOrchestrator`, `AuthorityDrivenArchitectureRunCommitOrchestrator` | `AuditEventTypes.Baseline.Architecture.*` (`Architecture.RunStarted`, `Architecture.RunCompleted`, `Architecture.RunFailed`, …) | Same baseline channel. **Durable echo:** `Run.*` rows from `BaselineMutationAuditService` (see durable table). |
| Governance workflow | `GovernanceWorkflowService` | `AuditEventTypes.Baseline.Governance.*` (mirrors `GovernanceAuditEventTypes`) | **Dual-write:** same service also calls `IAuditService` with top-level Core governance event types via `DurableAuditLogRetry` (see durable table above). |

**Implication:** operators searching **Audit log** in the UI see `IAuditService` rows, including governance transitions from `GovernanceWorkflowService`. Baseline mutation logs remain for grep-friendly structured logging.

---

## Known gaps (mutating behavior without durable `IAuditService` event)

**Last reviewed:** 2026-04-23.

**Open gaps: 0** as of 2026-04-23. Architecture coordinator durable rows are emitted from `BaselineMutationAuditService` (not necessarily in the same file as each `RecordAsync`). Other `RecordAsync` call sites remain paired with a sibling durable call in-file **or** are explicitly allowed in `BaselineMutationAuditDualWritePairingTests`. The pairing rule is asserted by the test below.

**2026-04-23 addendum (implicit gap closed).** `IAuthorityCommittedManifestChainWriter.PersistCommittedChainAsync` (demo trusted-baseline seed + replay commit) previously wrote authority SQL rows without a durable audit row; it now emits **`AuthorityCommittedChainPersisted`** from `DemoSeedService` / `ReplayRunService` after successful persistence (replay: after `IArchLucidUnitOfWork.CommitAsync`). See `docs/CHANGELOG.md` § 2026-04-23 — durable audit for authority committed manifest chain.

| Surface previously flagged | Resolution | Verification |
|---------------------------|-----------|--------------|
| `ConversationController` | Read-only (GET endpoints only); no state to audit | Controller surface review |
| `GovernanceController` | All POST actions delegate to `GovernanceWorkflowService`, which already dual-writes | Five `RecordAsync` ↔ `LogAsync` pairs in `GovernanceWorkflowService.cs` |
| Coordinator orchestrators (`Create`, `Execute`, `Commit`) | Architecture durable `Run.*` echo centralized in `BaselineMutationAuditService`; commit orchestrators still emit pre-commit governance rows directly | `BaselineMutationAuditService.cs`, `BaselineMutationAuditArchitectureDurableWriter.cs`, orchestrator `RecordAsync` call sites |

**Future-drift signal.** Most `RecordAsync` call sites must still show an obvious durable sibling **or** be listed in `BaselineMutationAuditDualWritePairingTests.AllowedBaselineOnlyFiles`. Architecture coordinator create/execute orchestrators are exempt: durable rows are centralized in `BaselineMutationAuditService` + `BaselineMutationAuditArchitectureDurableWriter`. Governance and commit orchestrators retain in-file `LogAsync` where applicable. The pairing test is a static assertion against `ArchLucid.Application` source.

---

## Coverage statistics (manual; refresh when adding call sites)

| Metric | Approximate value |
|--------|-------------------|
| **Core `AuditEventTypes` `public const string` rows** | 125 (see CI marker above; includes nested `Baseline` and nested `Run`) |
| **`await *auditService.LogAsync` production call sites** | ~44 (excluding tests; includes bridge) |
| **`IBaselineMutationAuditService.RecordAsync` call sites** | Orchestrators + `GovernanceWorkflowService` (log-only) |
| **Gaps listed** | 0 (resolved / out-of-scope notes in section above) |

---

## Appendix — Core `AuditEventTypes` registry (one row per constant)

| Constant | Value | Durable audit producer(s) |
|----------|-------|---------------------------|
| `RunStarted` | `RunStarted` | `AuthorityRunOrchestrator` |
| `RunCompleted` | `RunCompleted` | `AuthorityRunOrchestrator` |
| `ManifestGenerated` | `ManifestGenerated` | `AuthorityPipelineStagesExecutor` |
| `ManifestFinalized` | `ManifestFinalized` | `ManifestFinalizationService` (`sp_FinalizeManifest` transactional path — see `MANIFEST_FINALIZATION_TRANSACTION.md`) |
| `RunSubmitted` | `RunSubmitted` | `RunsController` (`POST /v1/architecture/run/{runId}/execute`, `POST /v1/runs/{runId}/submit`) |
| `ManifestViewed` | `ManifestViewed` | `AuthorityQueryController` (`GET …/manifest` / `GET /v1/runs/{runId}/manifest`) |
| `ReviewTrailAccessed` | `ReviewTrailAccessed` | `AuthorityQueryController` (`GET …/pipeline-timeline`, `GET /v1/runs/{runId}/review-trail`) |
| `ProvenanceAccessed` | `ProvenanceAccessed` | `AuthorityQueryController` (`GET …/provenance`, `GET /v1/runs/{runId}/review-trail/provenance`) |
| `FindingsListAccessed` | `FindingsListAccessed` | — (constant catalogued for `GET /v1/runs/{runId}/findings`; durable `LogAsync` not wired yet) |
| `GovernanceApprovalRequested` | `GovernanceApprovalRequested` | `GovernanceController` (`POST /v1/governance/approval-requests`) |
| `ArtifactsGenerated` | `ArtifactsGenerated` | `AuthorityPipelineStagesExecutor` |
| `ReplayExecuted` | `ReplayExecuted` | `AuthorityReplayController` |
| `AuthorityCommittedChainPersisted` | `AuthorityCommittedChainPersisted` | `DemoSeedService`, `ReplayRunService` |
| `ArtifactDownloaded` | `ArtifactDownloaded` | `ArtifactExportController` |
| `BundleDownloaded` | `BundleDownloaded` | `ArtifactExportController` |
| `SupportBundleDownloaded` | `SupportBundleDownloaded` | `SupportBundleController` (`POST /v1/admin/support-bundle`) |
| `RunExported` | `RunExported` | `ArtifactExportController` |
| `ArchitectureAnalysisReportGenerated` | `ArchitectureAnalysisReportGenerated` | `AnalysisReportsController` |
| `ArchitectureDocxExportGenerated` | `ArchitectureDocxExportGenerated` | `DocxExportController` |
| `RequestFileImported` | `RequestFileImported` | `ImportRequestFileService` (`ImportRequestFileController`) |
| `ValueReportGenerated` | `ValueReportGenerated` | `ValueReportController`, `InMemoryValueReportJobQueue` |
| `ReplayExportRecorded` | `ReplayExportRecorded` | `ExportsController` |
| `ComparisonSummaryPersisted` | `ComparisonSummaryPersisted` | `ExportsController` |
| `GovernancePreCommitBlocked` | `GovernancePreCommitBlocked` | `ArchitectureRunCommitOrchestrator` (optional pre-commit gate) |
| `GovernancePreCommitWarned` | `GovernancePreCommitWarned` | `ArchitectureRunCommitOrchestrator` (warn-only severity in pre-commit gate) |
| `GovernanceApprovalSlaBreached` | `GovernanceApprovalSlaBreached` | `ApprovalSlaMonitor` (pending approval request past SLA deadline) |
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
| `PilotScorecardBaselinesUpdated` | `PilotScorecardBaselinesUpdated` | `PilotInProductScorecardService` (`PUT /v1/pilots/scorecard/baselines`) |
| `GovernanceApprovalRejected` | `GovernanceApprovalRejected` | `GovernanceWorkflowService` |
| `GovernanceSelfApprovalBlocked` | `GovernanceSelfApprovalBlocked` | `GovernanceWorkflowService` |
| `GovernanceManifestPromoted` | `GovernanceManifestPromoted` | `GovernanceWorkflowService` |
| `GovernanceEnvironmentActivated` | `GovernanceEnvironmentActivated` | `GovernanceWorkflowService` |
| `GovernanceDryRunRequested` | `GovernanceDryRunRequested` | `PolicyPackDryRunService` (POST `/v1/governance/policy-packs/{id}/dry-run`; redaction-pipeline mandatory per Q37) |
| `DataArchivalHostLoopFailed` | `DataArchivalHostLoopFailed` | `DataArchivalHostIteration` |
| `CircuitBreakerStateTransition` | `CircuitBreakerStateTransition` | `CircuitBreakerAuditBridge` |
| `CircuitBreakerRejection` | `CircuitBreakerRejection` | `CircuitBreakerAuditBridge` |
| `CircuitBreakerProbeOutcome` | `CircuitBreakerProbeOutcome` | `CircuitBreakerAuditBridge` |
| `SecurityAssessmentPublished` | `SecurityAssessmentPublished` | `SecurityTrustPublicationController` |
| `TenantProvisioned` | `TenantProvisioned` | `TenantProvisioningService` |
| `TenantSelfRegistered` | `TenantSelfRegistered` | `RegistrationController` |
| `TrialProvisioned` | `TrialProvisioned` | `TrialTenantBootstrapService` |
| `TrialSignupAttempted` | `TrialSignupAttempted` | `RegistrationController`, `TrialLocalIdentityAuthController` |
| `TrialRegistrationFailed` | `TrialRegistrationFailed` | `RegistrationController` (failed `POST /v1/register` responses) |
| `TrialBaselineReviewCycleCaptured` | `TrialBaselineReviewCycleCaptured` | `RegistrationController` (only when prospect supplied a baseline) |
| `TrialBaselineManualPrepCaptured` | `TrialBaselineManualPrepCaptured` | `TenantBaselineController` (first save of `BaselineManualPrep*` on `dbo.Tenants`) |
| `TrialBaselineManualPrepUpdated` | `TrialBaselineManualPrepUpdated` | `TenantBaselineController` (subsequent edits after first capture) |
| `TrialSignupFailed` | `TrialSignupFailed` | `TrialLocalIdentityAuthController`, `TrialTenantBootstrapService` |
| `TrialFirstRunCompleted` | `TrialFirstRunCompleted` | `SqlTrialFunnelCommitHook` |
| `BillingCheckoutInitiated` | `BillingCheckoutInitiated` | `BillingCheckoutController` |
| `BillingCheckoutCompleted` | `BillingCheckoutCompleted` | `BillingCheckoutController` |
| `TenantNotificationChannelPreferencesUpdated` | `TenantNotificationChannelPreferencesUpdated` | `CustomerNotificationChannelPreferencesController` |
| `TenantTeamsIncomingWebhookConnectionUpserted` | `TenantTeamsIncomingWebhookConnectionUpserted` | `TeamsIncomingWebhookConnectionsController` |
| `TenantTeamsIncomingWebhookConnectionRemoved` | `TenantTeamsIncomingWebhookConnectionRemoved` | `TeamsIncomingWebhookConnectionsController` |
| `ExecDigestPreferencesUpdated` | `ExecDigestPreferencesUpdated` | `TenantExecDigestPreferencesController` |
| `TenantTrialConverted` | `TenantTrialConverted` | `TenantTrialController` |
| `TrialLifecycleTransition` | `TrialLifecycleTransition` | `TrialLifecycleTransitionEngine` |
| `TrialLimitExceeded` | `TrialLimitExceeded` | `TrialLimitExceededAuditFilter`, `TrialLimitProblemResponse.TryLogAuditAsync` (on `TrialLimitExceededException`) |
| `ComparisonRecordOrphansRemediated` | `ComparisonRecordOrphansRemediated` | `AdminDiagnosticsService` (orphan comparison-record remediation execute) |
| `GoldenManifestOrphansRemediated` | `GoldenManifestOrphansRemediated` | `AdminDiagnosticsService` (orphan golden-manifest remediation execute) |
| `FindingsSnapshotOrphansRemediated` | `FindingsSnapshotOrphansRemediated` | `AdminDiagnosticsService` (orphan findings-snapshot remediation execute) |
| `AgentResultSchemaViolation` | `AgentResultSchemaViolation` | `AgentResultSchemaViolationAudit` (topology / compliance / critic handlers on `AgentResultSchemaViolationException`) |
| `AgentTraceBlobPersistenceFailed` | `AgentTraceBlobPersistenceFailed` | `AgentExecutionTraceRecorder` |
| `AgentTraceInlineFallbackFailed` | `AgentTraceInlineFallbackFailed` | `AgentExecutionTraceRecorder` |
| `LlmTenantDailyBudgetApproaching` | `LlmTenantDailyBudgetApproaching` | `LlmDailyTenantBudgetTracker` (fire-and-forget; one row per tenant per UTC day) |
| `ScimTokenIssued` | `ScimTokenIssued` | `ScimTokensAdminController` |
| `ScimTokenRevoked` | `ScimTokenRevoked` | `ScimTokensAdminController` |
| `ScimUserProvisioned` | `ScimUserProvisioned` | `ScimUserService` |
| `ScimUserUpdated` | `ScimUserUpdated` | `ScimUserService` |
| `ScimUserDeactivated` | `ScimUserDeactivated` | `ScimUserService` |
| `ScimGroupProvisioned` | `ScimGroupProvisioned` | `ScimGroupService` |
| `ScimGroupMembershipChanged` | `ScimGroupMembershipChanged` | `ScimGroupService` |
| `FirstRealValueRunStarted` | `FirstRealValueRunStarted` | `RunsController` (pilot real execute) |
| `FirstRealValueRunCompleted` | `FirstRealValueRunCompleted` | `RunsController` (pilot real execute success) |
| `FirstRealValueRunFellBackToSimulator` | `FirstRealValueRunFellBackToSimulator` | `ArchitectureApplicationService` (pilot seed after real-mode fallback) |
| `RunLegacyReadyForCommitPromoted` | `RunLegacyReadyForCommitPromoted` | `ArchitectureRunExecuteOrchestrator` (post-execute LegacyRunStatus promotion — ADR-0012) |

When adding a Core constant, add a row here and bump `audit-core-const-count`.

---

## Appendix — `AuditEventTypes.Run` registry (canonical coordinator durable rows)

| Constant | Value | Durable audit producer(s) |
|----------|-------|---------------------------|
| `Run.Created` | `Run.Created` | `BaselineMutationAuditService` / `BaselineMutationAuditArchitectureDurableWriter` (baseline `Architecture.RunCreated`) |
| `Run.ExecuteStarted` | `Run.ExecuteStarted` | `BaselineMutationAuditService` / `BaselineMutationAuditArchitectureDurableWriter` (baseline `Architecture.RunStarted`) |
| `Run.ExecuteSucceeded` | `Run.ExecuteSucceeded` | `BaselineMutationAuditService` / `BaselineMutationAuditArchitectureDurableWriter` (baseline `Architecture.RunExecuteSucceeded`) |
| `Run.CommitCompleted` | `Run.CommitCompleted` | `BaselineMutationAuditService` / `BaselineMutationAuditArchitectureDurableWriter` (baseline `Architecture.RunCompleted`) |
| `Run.Failed` | `Run.Failed` | `BaselineMutationAuditService` / `BaselineMutationAuditArchitectureDurableWriter` (baseline `Architecture.RunFailed`) |

When adding a `Run` constant, add a row here and bump `audit-core-const-count`.

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

---

## Quality assessment verification (2026-04-28)

Independent quality readiness review (weighted score **66.25%**) re-traced this matrix against orchestrator call sites. **No net-new durable audit gaps** were opened beyond the intentional baseline-vs-durable dual-channel split documented above — coordinator durable echoes remain on the critical path (`BaselineMutationAuditArchitectureDurableWriter`), and **explicit** `dbo.AuditEvents` rows for **silent** coordinator SQL mutations (`RunLegacyReadyForCommitPromoted` on `dbo.Runs.LegacyRunStatus` promotion) are layered per ADR-0012 traceability.

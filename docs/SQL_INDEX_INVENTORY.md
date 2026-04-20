> **Scope:** SQL Index Inventory - full detail, tables, and links in the sections below.

# SQL Index Inventory

This document lists every nonclustered index added by migrations 059–060 and the query pattern each one covers. The master DDL at `ArchLucid.Persistence/Scripts/ArchLucid.sql` contains the canonical `IF NOT EXISTS` versions; the individual migration files are incremental and applied by DbUp.

---

## Migration 059 — SLA Breach Monitoring + Blob Upload Diagnostics

| Index | Table | Key Columns | Include / Filter | Query Pattern |
|-------|-------|-------------|-----------------|---------------|
| `IX_GovernanceApprovalRequests_PendingSlaBreached` | `GovernanceApprovalRequests` | `SlaDeadlineUtc ASC` | **Include:** `ApprovalRequestId, RunId, RequestedBy, Status` **Filter:** `SlaDeadlineUtc IS NOT NULL AND SlaBreachNotifiedUtc IS NULL` | `ApprovalSlaMonitor.CheckAndEscalateAsync` — periodic background sweep for pending requests past their SLA deadline. Without this index the monitor table-scans every row. |
| `IX_GovernanceApprovalRequests_Status_RequestedUtc` | `GovernanceApprovalRequests` | `Status, RequestedUtc DESC` | **Include:** `RunId, ManifestVersion, SourceEnvironment, TargetEnvironment` | `GetPendingAsync` and `GetReviewedAsync` both filter by status and sort by time. The only prior index was `IX_..._RunId`. |
| `IX_AgentExecutionTraces_BlobUploadFailed` | `AgentExecutionTraces` | `RunId, CreatedUtc DESC` | **Filter:** `BlobUploadFailed = 1` | Operator diagnostic query to find traces where blob upload failed. Extremely sparse (failures are rare) so the filtered index is almost free to maintain. |
| `IX_AgentExecutionTraces_InlineFallbackFailed` | `AgentExecutionTraces` | `RunId, CreatedUtc DESC` | **Filter:** `InlineFallbackFailed = 1` | Operator diagnostic query for traces where mandatory SQL inline fallback or forensic verification failed (migration **065**). Sparse like blob-failure index. |

## Migration 060 — Broader Query Coverage

| Index | Table | Key Columns | Include / Filter | Query Pattern |
|-------|-------|-------------|-----------------|---------------|
| `IX_AuditEvents_EventType_OccurredUtc` | `AuditEvents` | `TenantId, WorkspaceId, ProjectId, EventType, OccurredUtc DESC` | — | `/v1/audit/search?eventType=...` filter. The existing scope index lacks `EventType` so the filter is residual. This index turns it into a seek. |
| `IX_ConversationThreads_Scope_Active` | `ConversationThreads` | `TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC` | **Filter:** `ArchivedUtc IS NULL` | Every list/count query filters out archived threads. The unfiltered scope index scans archived rows unnecessarily. |
| `IX_GovernanceEnvironmentActivations_RunId_ActivatedUtc` | `GovernanceEnvironmentActivations` | `RunId, ActivatedUtc DESC` | — | `GetByRunIdAsync` had no `RunId` index at all (migration 017 only created `Environment, IsActive`). |
| `IX_GovernanceEnvironmentActivations_Environment_ActivatedUtc` | `GovernanceEnvironmentActivations` | `Environment, ActivatedUtc DESC` | **Include:** `RunId, ManifestVersion, IsActive` | `GetByEnvironmentAsync` sorts by `ActivatedUtc` but the existing `(Environment, IsActive)` index does not cover the sort. |
| `IX_GovernancePromotionRecords_RunId_PromotedUtc` | `GovernancePromotionRecords` | `RunId, PromotedUtc DESC` | — | `GetByRunIdAsync` sorts by `PromotedUtc DESC` but the existing `IX_..._RunId` index has no sort column, forcing a runtime sort. |
| `IX_RecommendationRecords_Scope_Run_Priority` | `RecommendationRecords` | `TenantId, WorkspaceId, ProjectId, RunId, PriorityScore DESC, CreatedUtc DESC` | — | `ListByRunAsync` orders by `PriorityScore DESC, CreatedUtc DESC`. The existing scope-run index orders by `CreatedUtc` only, requiring a separate sort on priority. |
| `IX_RecommendationRecords_Scope_LastUpdatedUtc` | `RecommendationRecords` | `TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC` | — | `ListByScopeAsync` with no status filter orders by `LastUpdatedUtc`. The existing `IX_..._Scope_Status` leads with `Status`, making it useless when status is NULL (unfiltered). |
| `IX_Runs_ArchiveRetention` | `Runs` | `CreatedUtc ASC` | **Include:** `TenantId, WorkspaceId, ScopeProjectId` **Filter:** `ArchivedUtc IS NULL` | Global archive retention job (`WHERE ArchivedUtc IS NULL AND CreatedUtc < @Cutoff`) runs without scope columns. Existing filtered index `IX_Runs_Scope_CreatedUtc` leads with tenant, making it unusable for a cross-tenant sweep. |
| `IX_PolicyPackAssignments_Scope_Active` | `PolicyPackAssignments` | `TenantId, ScopeLevel, AssignedUtc DESC` | **Include:** `WorkspaceId, ProjectId, PolicyPackId, IsEnabled, BlockCommitOnCritical, BlockCommitMinimumSeverity` **Filter:** `ArchivedUtc IS NULL` | `ListByScopeAsync` filters `ArchivedUtc IS NULL` and OR-branches on `ScopeLevel`. Existing indexes include no archival filter. The `INCLUDE` list covers the SELECT columns for a covering scan. |

---

## Design Decisions

### Filtered indexes for sparse predicates

`BlobUploadFailed = 1`, `InlineFallbackFailed = 1`, `ArchivedUtc IS NULL`, `SlaBreachNotifiedUtc IS NULL` — these predicates match a small fraction of rows. Filtered indexes keep the B-tree tiny, costing almost nothing on writes while giving sub-millisecond seeks on reads.

### INCLUDE columns for covering queries

Where the query `SELECT` list is stable and small, `INCLUDE` columns are added to avoid key lookups back to the clustered index. This trades a bit of index size for significantly lower I/O on hot queries.

### Avoiding index duplication

The new `IX_GovernanceEnvironmentActivations_Environment_ActivatedUtc` does not replace the original `IX_..._Environment_IsActive` from migration 017 — queries that filter on `IsActive` still benefit from the original. If workload analysis shows the original is no longer used, it can be dropped in a future migration.

### Impact on write throughput

Each index adds a small overhead on INSERT/UPDATE. For high-write tables (`AuditEvents`, `Runs`), the indexes are either filtered (small) or composite with columns already in the clustered key (minimal extra leaf pages). Monitor `sys.dm_db_index_operational_stats` after deployment to verify write overhead is acceptable.

## Migration 084 — PAGE rowstore compression (storage / read amplification)

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.AuditEvents` | `ALTER INDEX ALL … REBUILD WITH (DATA_COMPRESSION = PAGE)` when any rowstore partition is not already PAGE | Cuts page count for `DataJson` + scope indexes; small extra CPU on inserts vs fewer logical reads on scans. |
| `dbo.AgentExecutionTraces` | Same | Large `TraceJson` / inline prompt columns benefit most from denser pages on Azure SQL. |

**Operational:** Prefer estimating with `sp_estimate_data_compression_savings` on a restored copy; confirm SKU supports compression (not Basic DTU). Rollback script restores **NONE** if any partition was **PAGE**.

## Migration 085 — PAGE compression on `dbo.Runs`

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.Runs` | `ALTER INDEX ALL … REBUILD WITH (DATA_COMPRESSION = PAGE)` when any rowstore partition is not already PAGE | Targets authority run header + scope/list indexes (e.g. **061** covering index); expect **longer rebuild** and higher log I/O than **084** — schedule off-peak. |

Rollback: **`Rollback/R085_PageCompression_Runs.sql`** restores **NONE** where partitions were **PAGE**.

## Migration 087 — PAGE compression on `dbo.DecisionTraces`

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.DecisionTraces` | `ALTER INDEX ALL … REBUILD WITH (DATA_COMPRESSION = PAGE)` when any rowstore partition is not already PAGE | Large trace payloads and `RunId` / `CreatedUtc` indexes benefit from denser pages; schedule off-peak like **084**/**085**. |

Rollback: **`Rollback/R087_PageCompression_DecisionTraces.sql`** restores **NONE** where partitions were **PAGE**.

## Migration 088 — PAGE compression on `dbo.DecisioningTraces`

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.DecisioningTraces` | `ALTER INDEX ALL … REBUILD WITH (DATA_COMPRESSION = PAGE)` when any rowstore partition is not already PAGE | Authority-side trace with multiple `NVARCHAR(MAX)` JSON columns; pairs with **087** on **`DecisionTraces`**. |

Rollback: **`Rollback/R088_PageCompression_DecisioningTraces.sql`**.

## Migration 089 — PAGE compression on `dbo.UsageEvents`

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.UsageEvents` | Same **PAGE** pattern when any rowstore partition is not already PAGE | Metering append stream; **`IX_UsageEvents_TenantRecorded2`** / **`IX_UsageEvents_KindRecorded2`** included in **`ALTER INDEX ALL`**. |

Rollback: **`Rollback/R089_PageCompression_UsageEvents.sql`**.

## Migration 090 — PAGE compression on `dbo.AlertRecords` + `dbo.AlertDeliveryAttempts`

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.AlertRecords` | Same **PAGE** pattern when any rowstore partition is not already PAGE | Operational alert history; schedule with other compression windows. |
| `dbo.AlertDeliveryAttempts` | Same | Delivery attempt ledger; paired in one migration like **084**. |

Rollback: **`Rollback/R090_PageCompression_AlertRecords_AlertDeliveryAttempts.sql`**.

**Outbox tables:** `IntegrationEventOutbox`, `RetrievalIndexingOutbox`, and `AuthorityPipelineWorkOutbox` are excluded from this PAGE series until workload-specific analysis; see **`docs/SQL_OUTBOX_TABLES_COMPRESSION.md`**.

## Migration 092 — Foreign keys (outbox + alerts, batch 1)

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.IntegrationEventOutbox` | `FK_IntegrationEventOutbox_Runs_RunId` on `RunId` → `dbo.Runs` | Invalid **`RunId`** nulled before add. |
| `dbo.RetrievalIndexingOutbox` | `FK_RetrievalIndexingOutbox_Runs_RunId` | Added only when every row has a matching **`Runs`** row (otherwise skipped). |
| `dbo.AuthorityPipelineWorkOutbox` | `FK_AuthorityPipelineWorkOutbox_Runs_RunId` | Same conditional add. |
| `dbo.AlertRecords` | `FK_AlertRecords_AlertRules_RuleId`, `FK_AlertRecords_Runs_RunId`, `FK_AlertRecords_Runs_ComparedToRunId`, `FK_AlertRecords_RecommendationRecords_RecommendationId` | Optional refs nulled when invalid; **`RuleId`** FK skipped if orphan **`AlertRules`** rows exist. |
| `dbo.AlertDeliveryAttempts` | `FK_…_AlertRecords_AlertId`, `FK_…_AlertRoutingSubscriptions_RoutingSubscriptionId` | Orphan attempts **deleted** before add. |

Rollback: **`Rollback/R092_FK_Outbox_Alerts_Batch1.sql`** drops the constraints (does not restore deleted rows or nulled columns).

## Migration 093 — Foreign keys (audit + recommendations + conversation messages, batch 2)

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.AuditEvents` | `FK_AuditEvents_Runs_RunId`, `FK_AuditEvents_GoldenManifests_ManifestId` | Invalid **`RunId`** / **`ManifestId`** nulled before add. |
| `dbo.RecommendationRecords` | `FK_RecommendationRecords_Runs_RunId`, `FK_RecommendationRecords_Runs_ComparedToRunId` | **`RunId`** FK added only when every row references **`Runs`**; invalid **`ComparedToRunId`** nulled first. |
| `dbo.ConversationMessages` | `FK_ConversationMessages_ConversationThreads_ThreadId` | Orphan messages (missing thread) **deleted** before add. |
| `dbo.AuditEvents.ArtifactId` | *(none)* | No single-column parent key for line-level artifact IDs (**`ArtifactBundleArtifacts`** uniqueness is composite). |

Rollback: **`Rollback/R093_FK_Audit_Recommendations_ConversationMessages_Batch2.sql`** drops the constraints (does not restore deleted messages or nulled audit columns).

## Migration 094 — `RowVersionStamp` (ROWVERSION) on alerts, recommendations, background jobs

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.AlertRecords` | `ALTER TABLE … ADD RowVersionStamp ROWVERSION` when absent | Same optimistic-concurrency pattern as **`dbo.Runs`**; repositories may adopt **`WHERE RowVersionStamp = @expected`** later. |
| `dbo.RecommendationRecords` | Same | |
| `dbo.BackgroundJobs` | Same | Worker state transitions benefit from rowversion tokens. |

Rollback: **`Rollback/R094_RowVersion_AlertRecords_RecommendationRecords_BackgroundJobs.sql`** drops the column (coordinate with app if concurrency checks were enabled).

## Migration 095 — CHECK constraints (status / severity / urgency batch)

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.PolicyPacks` | `CK_PolicyPacks_Status` | **`Draft`**, **`Active`**, **`Retired`** — matches **`PolicyPackStatus`**. |
| `dbo.AlertDeliveryAttempts` | `CK_AlertDeliveryAttempts_Status` | **`Started`**, **`Succeeded`**, **`Failed`** — matches **`AlertDeliveryAttemptStatus`**. |
| `dbo.AlertRecords` | `CK_AlertRecords_Severity` | **`Info`**, **`Warning`**, **`High`**, **`Critical`** — matches **`AlertSeverity`**. |
| `dbo.AlertRules` | `CK_AlertRules_Severity` | Same |
| `dbo.AlertRoutingSubscriptions` | `CK_AlertRoutingSubscriptions_MinimumSeverity` | Same ( **`MinimumSeverity`** column). |
| `dbo.CompositeAlertRules` | `CK_CompositeAlertRules_Severity` | Same |
| `dbo.RecommendationRecords` | `CK_RecommendationRecords_Urgency` | **`Critical`**, **`High`**, **`Medium`**, **`Low`** — matches **`RecommendationGenerator.MapUrgency`**. |

Each constraint is skipped when any row would violate the domain (remediate data, then re-run DbUp or ship a follow-up). Rollback: **`Rollback/R095_CheckConstraints_StatusDomains_Batch.sql`**.

## Migration 096 — `ISJSON` CHECK constraints (core JSON payloads)

| Object | Change | Notes |
|--------|--------|-------|
| `dbo.AuditEvents.DataJson` | `CK_AuditEvents_DataJson_IsJson` | **`CHECK (ISJSON(DataJson)=1)`** |
| `dbo.AgentExecutionTraces.TraceJson` | `CK_AgentExecutionTraces_TraceJson_IsJson` | |
| `dbo.AgentResults.ResultJson` | `CK_AgentResults_ResultJson_IsJson` | |
| `dbo.ComparisonRecords.PayloadJson` | `CK_ComparisonRecords_PayloadJson_IsJson` | |
| `dbo.DecisionTraces.EventJson` | `CK_DecisionTraces_EventJson_IsJson` | |
| `dbo.DecisioningTraces` | `CK_DecisioningTraces_*Json_IsJson` (four columns) | Applied / accepted / rejected / notes JSON |
| `dbo.AuthorityPipelineWorkOutbox.PayloadJson` | `CK_AuthorityPipelineWorkOutbox_PayloadJson_IsJson` | |
| `dbo.RecommendationRecords` | Three **`Supporting*IdsJson`** checks | |
| `dbo.BackgroundJobs.WorkUnitJson` | `CK_BackgroundJobs_WorkUnitJson_IsJson` | |

Wide manifest / snapshot JSON (**`GoldenManifests`**, **`ContextSnapshots`**, …) are **not** in this slice — add in a follow-up after catalog validation. Rollback: **`Rollback/R096_CheckJson_CorePayloadColumns.sql`**.

## Migration 098 — `IntegrationEventOutbox` dead-letter and pending-with-retry indexes

**Note:** Migration **`097`** is **`TenantOnboardingState`**; outbox operator indexes ship as **098**.

| Index | Table | Key columns | Filter / INCLUDE | Query pattern |
|-------|-------|-------------|------------------|---------------|
| `IX_IntegrationEventOutbox_DeadLetteredUtc` | `IntegrationEventOutbox` | `DeadLetteredUtc DESC`, `EventType` | **Filter:** `DeadLetteredUtc IS NOT NULL` — **INCLUDE:** scope + `RetryCount`, `LastErrorMessage` | Dead-letter dashboards and audits (sparse). |
| `IX_IntegrationEventOutbox_PendingWithRetries` | `IntegrationEventOutbox` | `NextRetryUtc ASC`, `CreatedUtc ASC` | **Filter:** pending, not dead-letter, **`RetryCount > 0`** — **INCLUDE:** `EventType`, scope, `RetryCount`, `LastErrorMessage` | Stuck / backoff sweeps without scanning the full pending queue. |

`RetrievalIndexingOutbox` / `AuthorityPipelineWorkOutbox` already have **`IX_*_Pending`** filtered on `ProcessedUtc IS NULL`; additional filtered indexes can follow if telemetry shows residual table scans.

Rollback: **`Rollback/R098_OutboxDeadLetterStuckRowIndexes.sql`**.

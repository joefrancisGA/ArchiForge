# SQL Index Inventory

This document lists every nonclustered index added by migrations 059–060 and the query pattern each one covers. The master DDL at `ArchLucid.Persistence/Scripts/ArchLucid.sql` contains the canonical `IF NOT EXISTS` versions; the individual migration files are incremental and applied by DbUp.

---

## Migration 059 — SLA Breach Monitoring + Blob Upload Diagnostics

| Index | Table | Key Columns | Include / Filter | Query Pattern |
|-------|-------|-------------|-----------------|---------------|
| `IX_GovernanceApprovalRequests_PendingSlaBreached` | `GovernanceApprovalRequests` | `SlaDeadlineUtc ASC` | **Include:** `ApprovalRequestId, RunId, RequestedBy, Status` **Filter:** `SlaDeadlineUtc IS NOT NULL AND SlaBreachNotifiedUtc IS NULL` | `ApprovalSlaMonitor.CheckAndEscalateAsync` — periodic background sweep for pending requests past their SLA deadline. Without this index the monitor table-scans every row. |
| `IX_GovernanceApprovalRequests_Status_RequestedUtc` | `GovernanceApprovalRequests` | `Status, RequestedUtc DESC` | **Include:** `RunId, ManifestVersion, SourceEnvironment, TargetEnvironment` | `GetPendingAsync` and `GetReviewedAsync` both filter by status and sort by time. The only prior index was `IX_..._RunId`. |
| `IX_AgentExecutionTraces_BlobUploadFailed` | `AgentExecutionTraces` | `RunId, CreatedUtc DESC` | **Filter:** `BlobUploadFailed = 1` | Operator diagnostic query to find traces where blob upload failed. Extremely sparse (failures are rare) so the filtered index is almost free to maintain. |

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

`BlobUploadFailed = 1`, `ArchivedUtc IS NULL`, `SlaBreachNotifiedUtc IS NULL` — these predicates match a small fraction of rows. Filtered indexes keep the B-tree tiny, costing almost nothing on writes while giving sub-millisecond seeks on reads.

### INCLUDE columns for covering queries

Where the query `SELECT` list is stable and small, `INCLUDE` columns are added to avoid key lookups back to the clustered index. This trades a bit of index size for significantly lower I/O on hot queries.

### Avoiding index duplication

The new `IX_GovernanceEnvironmentActivations_Environment_ActivatedUtc` does not replace the original `IX_..._Environment_IsActive` from migration 017 — queries that filter on `IsActive` still benefit from the original. If workload analysis shows the original is no longer used, it can be dropped in a future migration.

### Impact on write throughput

Each index adds a small overhead on INSERT/UPDATE. For high-write tables (`AuditEvents`, `Runs`), the indexes are either filtered (small) or composite with columns already in the clustered key (minimal extra leaf pages). Monitor `sys.dm_db_index_operational_stats` after deployment to verify write overhead is acceptable.

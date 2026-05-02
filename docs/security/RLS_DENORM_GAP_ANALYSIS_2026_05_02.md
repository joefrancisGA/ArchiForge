# RLS Denormalization Gap Analysis

**Date:** 2026-05-02  
**Migrations analyzed:** 036, 046, 070, 129 (plus spot-checks of 104, 118, 121, 122, 133)  
**Policy name progression:** `rls.ArchiforgeTenantScope` → renamed `rls.ArchLucidTenantScope` in 108  
**Security policy predicate:** `rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId)`

---

## 1. What the denormalization campaign achieved

The system ran two explicit denormalization waves and several targeted per-feature additions.

| Migration | Wave | Tables reached |
|-----------|------|----------------|
| 036 | Policy creation | 26 primary/parent tables with FILTER-only predicates |
| 046 | Wave 1 child | `ContextSnapshots`, `FindingsSnapshots`, `GoldenManifestAssumptions` |
| 070 | New table | `UsageEvents` — born with `TenantId NOT NULL`; FILTER + BLOCK predicates |
| 104 | FindingFeedback | `FindingFeedback` — born with `TenantId NOT NULL`; FILTER + BLOCK predicates |
| 118 | GovernanceTables | `GovernanceApprovalRequests`, `GovernancePromotionRecords`, `GovernanceEnvironmentActivations` — backfill + NOT NULL upgrade + FILTER + BLOCK predicates |
| 129 | Wave 2 child (massive) | ~35 tables across 8 families (details below) |

### Wave 2 (129) table families

| Family | Tables added to policy |
|--------|----------------------|
| FindingRecords tree | `FindingRecords`, `FindingRelatedNodes`, `FindingRecommendedActions`, `FindingProperties`, `FindingTraceGraphNodesExamined`, `FindingTraceRulesApplied`, `FindingTraceDecisionsTaken`, `FindingTraceAlternativePaths`, `FindingTraceNotes` |
| GraphSnapshot tree | `GraphSnapshots`, `GraphSnapshotEdges`, `GraphSnapshotNodes`, `GraphSnapshotNodeProperties`, `GraphSnapshotEdgeProperties`, `GraphSnapshotWarnings` |
| ContextSnapshot children | `ContextSnapshotCanonicalObjects`, `ContextSnapshotCanonicalObjectProperties`, `ContextSnapshotWarnings`, `ContextSnapshotErrors`, `ContextSnapshotSourceHashes` |
| ArtifactBundle children | `ArtifactBundleArtifacts`, `ArtifactBundleArtifactMetadata`, `ArtifactBundleArtifactDecisionLinks`, `ArtifactBundleTraceGenerators`, `ArtifactBundleTraceDecisionLinks`, `ArtifactBundleTraceNotes` |
| 036 admitted gaps | `ConversationMessages`, `PolicyPackVersions` |
| Composite alerts | `CompositeAlertRuleConditions` |
| Evolution | `EvolutionSimulationRuns` |
| GoldenManifest children | `GoldenManifestWarnings`, `GoldenManifestDecisions`, `GoldenManifestDecisionEvidenceLinks`, `GoldenManifestDecisionNodeLinks`, `GoldenManifestProvenanceSourceFindings`, `GoldenManifestProvenanceSourceGraphNodes`, `GoldenManifestProvenanceAppliedRules` |
| ProductLearning link tables | `ProductLearningImprovementPlanArchitectureRuns`, `ProductLearningImprovementPlanSignalLinks`, `ProductLearningImprovementPlanArtifactLinks` |

Migration 129 is the most thorough denormalization migration in the codebase. It addresses every gap explicitly called out in migration 036's header comment.

---

## 2. Confirmed gaps — join-through-only or no-predicate tables

### CRITICAL — Agent execution lineage (no TenantId column)

These three tables form the agent execution spine. **None of them has a `TenantId` column. None has an RLS predicate. All are scoped exclusively by `RunId NVARCHAR(64)` with no foreign-key enforcement.**

| Table | Sensitive content | Risk |
|-------|------------------|------|
| `dbo.AgentTasks` | `Objective NVARCHAR(MAX)` (tenant architecture context sent to agents) | HIGH |
| `dbo.AgentResults` | `ResultJson NVARCHAR(MAX)` (full agent output — findings, recommendations) | HIGH |
| `dbo.AgentExecutionTraces` | `TraceJson`, `FullSystemPromptBlobKey`, `FullUserPromptBlobKey`, `FullResponseBlobKey` (full LLM prompts and responses — the most sensitive data in the system) | **CRITICAL** |

`AgentExecutionTraces` stores blob keys that point to the verbatim system prompt and user prompt sent to the LLM, and the verbatim LLM response. These payloads contain actual tenant architecture descriptions. A query that joins `AgentExecutionTraces` with an incorrect or injected `RunId` — or any direct table scan — returns another tenant's LLM conversation.

**Why this is structurally worse than the other gaps:** `RunId` is stored as `NVARCHAR(64)` (a legacy string key), not as `UNIQUEIDENTIFIER`. There is no foreign-key constraint from `AgentExecutionTraces.RunId` to `Runs.RunId`. The application can write or query these rows without the DB enforcing any referential integrity to a known run.

**Recommended fix:** Add `TenantId UNIQUEIDENTIFIER NOT NULL` (brownfield: add as NULL, backfill via `AgentTasks.RunId` → `Runs.TenantId`, delete orphans, alter to NOT NULL, add RLS predicate). Block predicates are essential here since the data is write-once from the agent runtime.

---

### HIGH — `dbo.FindingReviewEvents` (migration 121)

`FindingReviewEvents` was created in migration 121 **after** the rename (108) and **before** wave 2 (129). It has `TenantId UNIQUEIDENTIFIER NOT NULL`, `WorkspaceId NOT NULL`, `ProjectId NOT NULL`. However, migration 129's exhaustive enumeration of tables to add to `ArchLucidTenantScope` does not include it.

This table records human review decisions (approve/reject findings), reviewer user IDs, and review notes. It is both sensitive and clearly in-scope for RLS. The `TenantId` column is there — the only thing missing is the `ALTER SECURITY POLICY` statement.

**Recommended fix:**

```sql
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingReviewEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'FindingReviewEvents')
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingReviewEvents,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingReviewEvents AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingReviewEvents AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingReviewEvents BEFORE DELETE;
END;
```

---

### HIGH — `dbo.ImportedArchitectureRequests` (migration 122)

Same window as `FindingReviewEvents`. Has `TenantId NOT NULL`, `WorkspaceId NOT NULL`, `ProjectId NOT NULL`, and stores `RequestJson NVARCHAR(MAX)` (uploaded architecture specs). Not added to the policy in 129.

**Recommended fix:** Same `ALTER SECURITY POLICY` pattern as above.

---

### MEDIUM — `dbo.ComparisonRecords` (migration 002)

The oldest tenant-data table in the schema. Created before the RLS scheme existed. Has **no TenantId column at all** and no FK constraint to `Tenants`. Contains `PayloadJson NVARCHAR(MAX)` with diff/comparison content between runs. Scoped only by `LeftRunId` / `RightRunId` (both `NVARCHAR(64)`, no FK to `Runs`).

This table pre-dates `dbo.Runs` and may be a legacy artifact, but it should be audited:
- If it is still written to by the application, it is a leakage vector.
- If it is effectively retired, it should be explicitly documented as such in `MULTI_TENANT_RLS.md`.

---

### LOW — `dbo.AgentOutputEvaluationResults` (migration 063)

No TenantId column. Contains only aggregate evaluation scores (floats) plus `RunId NVARCHAR(64)`. Low sensitivity — scores are not architecture content — but it is unscoped. Apply same denorm treatment if the score data itself is considered tenant-proprietary.

---

## 3. Structural weakness — nullable denorm columns from wave 1 (046)

Migration 046 added `TenantId`, `WorkspaceId`, `ScopeProjectId/ProjectId` as **`NULLABLE`** to `ContextSnapshots`, `FindingsSnapshots`, and `GoldenManifestAssumptions`, then backfilled from a JOIN.

Migration 118 (governance tables) demonstrated the correct follow-up pattern: add as NULL → backfill → delete orphans → `ALTER COLUMN ... NOT NULL`.

**Migration 046 never ran the final `NOT NULL` upgrade step.**

The consequence: the application can write a new `ContextSnapshot` or `FindingsSnapshot` with `TenantId = NULL`. The RLS predicate evaluates `NULL = TRY_CONVERT(...)` → UNKNOWN → row is invisible to all non-bypass sessions. This is "fail-closed" in the sense that no tenant can see the row, but it silently orphans the row from all queries, which could surface as application bugs or data-loss symptoms rather than a security breach. More importantly, `FindingRecords` (added in 129) is backfilled from `FindingsSnapshots.TenantId`. If a snapshot was written with NULL TenantId after 046 ran but before the application enforced the column, downstream finding records will also have NULL TenantId.

**Recommended fix:** Add a migration that upgrades these three columns to `NOT NULL` (with the same orphan-delete guard that 118 used), and add NOT NULL constraints or application-layer assertions for new writes.

---

## 4. Block predicate coverage is inconsistent on parent tables

Migration 036 only adds `FILTER` predicates (read filtering). Block predicates (preventing cross-tenant writes) were added retroactively:
- 070: `UsageEvents` (AFTER INSERT, AFTER UPDATE, BEFORE DELETE)
- 104: `FindingFeedback` (same)
- 118: Governance tables (same)
- 129: All wave-2 child tables (same)

The **26 original parent tables in 036** — `Runs`, `GoldenManifests`, `ArtifactBundles`, `AuditEvents`, `AlertRules`, `AlertRecords`, etc. — have only FILTER predicates. They have no BLOCK predicates preventing a misconfigured application session from writing a row with the wrong `TenantId`. This means:

> A session context bug (e.g., wrong `af_tenant_id` set in `SESSION_CONTEXT`) would be filtered at read time but would not be blocked at write time for the core parent tables.

This is medium-severity: the write would go to the wrong tenant's partition, and then be invisible to both tenants (the writer sees it; the victim can't read it via the filter), but data integrity is still corrupted.

---

## 5. Summary risk matrix

| Table / Group | TenantId column | RLS predicate | Block predicate | Risk level |
|--------------|----------------|--------------|----------------|------------|
| 26 parent tables (036) | ✅ NOT NULL | ✅ FILTER | ❌ | MEDIUM |
| ContextSnapshots, FindingsSnapshots, GoldenManifestAssumptions (046) | ✅ NULLABLE | ✅ FILTER | ❌ | MEDIUM |
| UsageEvents (070) | ✅ NOT NULL | ✅ FILTER | ✅ BLOCK | ✅ OK |
| FindingFeedback (104) | ✅ NOT NULL | ✅ FILTER | ✅ BLOCK | ✅ OK |
| Governance tables (118) | ✅ NOT NULL | ✅ FILTER | ✅ BLOCK | ✅ OK |
| ~35 wave-2 child tables (129) | ✅ NULLABLE backfill | ✅ FILTER | ✅ BLOCK | MEDIUM (nullability) |
| **AgentTasks, AgentResults, AgentExecutionTraces** | ❌ none | ❌ none | ❌ none | **CRITICAL** |
| **FindingReviewEvents** | ✅ NOT NULL | ❌ none | ❌ none | HIGH |
| **ImportedArchitectureRequests** | ✅ NOT NULL | ❌ none | ❌ none | HIGH |
| ComparisonRecords | ❌ none | ❌ none | ❌ none | MEDIUM |
| AgentOutputEvaluationResults | ❌ none | ❌ none | ❌ none | LOW |
| AdminNotifications | ❌ (by design — operator table) | ❌ intentional | N/A | ✅ OK |

---

## 6. Recommended migration sequence

Implement as a single new migration (130-series) to be included before next release:

1. **`AgentTasks` / `AgentResults` / `AgentExecutionTraces`** — add `TenantId`, `WorkspaceId`, `ScopeProjectId` as `NULL`; backfill via `JOIN Runs ON AgentTasks.RunId = CAST(Runs.RunId AS NVARCHAR(64))` chain; delete orphans; `ALTER COLUMN NOT NULL`; add FILTER + BLOCK predicates. This is the highest-priority fix.

2. **`FindingReviewEvents`** — one `ALTER SECURITY POLICY` statement (TenantId already NOT NULL).

3. **`ImportedArchitectureRequests`** — one `ALTER SECURITY POLICY` statement (TenantId already NOT NULL).

4. **Nullable upgrade** — `ALTER COLUMN TenantId NOT NULL` on `ContextSnapshots`, `FindingsSnapshots`, `GoldenManifestAssumptions` after verifying no NULL rows remain.

5. **Block predicates on 036 parent tables** — add BLOCK predicates (AFTER INSERT, AFTER UPDATE, BEFORE DELETE) to the 26 original parent tables.

6. **`ComparisonRecords` audit** — determine if still written. If yes, denorm + predicate. If no, add `is_deprecated: true` annotation in `MULTI_TENANT_RLS.md`.

---

## 7. Reference

- `docs/security/MULTI_TENANT_RLS.md` — canonical RLS documentation
- Migration 036 comment: explicit admission of three gaps (ConversationMessages, ContextSnapshots child tables, PolicyPackVersions) — all closed by migration 129
- Migration 129 comment references `docs/security/MULTI_TENANT_RLS.md §9`

> **Scope:** For backend engineers owning CustomerSuccess persistence; documents Azure SQL + Dapper architecture, RLS, scaling, and hardening paths for `ArchLucid.Persistence/CustomerSuccess` and core models—not end-user guides, OpenAPI contracts, or ORM-centric designs.

# CustomerSuccess Module — Persistent, Scalable Design

**Date:** 2026-05-03  
**Scope:** `ArchLucid.Persistence/CustomerSuccess/` + `ArchLucid.Core/CustomerSuccess/`  
**Status:** Design review + incremental hardening path

---

## 1. Objective

Define the authoritative Azure-native persistence architecture for the CustomerSuccess
module, identify current defects and scalability ceilings, and provide a concrete
hardening roadmap using Azure SQL + Dapper (no ORM).

---

## 2. Assumptions

- Azure SQL (General Purpose or Business Critical tier) is the primary store —
  already in use via `ISqlConnectionFactory`.
- Row-Level Security (RLS) is enforced on all tenant-scoped tables via
  `IRlsSessionContextApplicator`; the bypass ambient
  (`SqlRowLevelSecurityBypassAmbient`) is reserved for leader-elected maintenance
  workers only.
- A read replica connection factory (`IReadReplicaQueryConnectionFactory`) already
  exists for read-heavy queries.
- Health score refresh is a scheduled background operation (timer-triggered),
  currently running in-process.
- Scale target: ~500 tenants at V1 GA, up to ~5,000 at Series A; scoring loop
  must not degrade linearly with tenant count.

---

## 3. Constraints

- No heavy ORM (EF Core, NHibernate). Dapper + raw SQL is the standard.
- Private endpoints only — no public SQL Server exposure.
- Each class in its own file.
- RLS bypass must never leak into tenant-scoped request paths.
- Scoring dimensions are deterministic and stateless — safe to recalculate idempotently.

---

## 4. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│  Request path (authenticated tenant user)                            │
│                                                                      │
│  ┌─────────────────────────────────────────────────┐                 │
│  │  API / Application Layer                        │                 │
│  │  • ICorePilotTeamChecklistRepository  (RW)      │                 │
│  │  • ITenantCustomerSuccessRepository   (Read)    │                 │
│  │  • IOperatorStickinessSnapshotReader  (Read)    │                 │
│  └──────────────┬───────────────────┬──────────────┘                 │
│                 │ primary writes    │ read queries                   │
│                 ▼                   ▼                                │
│         Azure SQL Primary    Azure SQL Read Replica                  │
│         (dbo.CorePilotTeamChecklist, dbo.ProductFeedback)            │
│         (dbo.TenantHealthScores)                                     │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│  Background scoring worker (leader-elected, RLS bypass)              │
│                                                                      │
│  Azure Functions Timer Trigger (or App Service BackgroundService)    │
│  └─► RefreshAllTenantHealthScoresAsync                               │
│       └─► Set-based SQL MERGE (single round-trip, all tenants)       │
│            └─► dbo.TenantHealthScores (upsert)                       │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 5. Component Breakdown

### 5.1 ICorePilotTeamChecklistRepository → SqlCorePilotTeamChecklistRepository

**Current state:** Correct. Uses `MERGE` for upsert, applies RLS, parameterizes all
inputs, validates `stepIndex` range (0–3) at the repository boundary.

**Issues:**

| # | Finding | Severity |
|---|---------|----------|
| CS-01 | `Row` inner class has multi-line `{ get; init; }` properties — violates `CSharp-SimpleProperties-OneLine.mdc` | Low |
| CS-02 | `stepIndex` domain validation (0–3) lives in the SQL repo, not in the domain model or `CorePilotChecklistStepRow` | Medium |

**Fix for CS-02:** Add a `StepIndexRange` constant or `IsValid(int)` method to the
domain model so the SQL layer can delegate rather than re-invent the rule.

---

### 5.2 ITenantCustomerSuccessRepository → SqlTenantCustomerSuccessRepository

**Current state:** Health score reads and product feedback inserts are correct. The
`RefreshAllTenantHealthScoresAsync` method is architecturally sound in intent but has a
critical scalability problem.

**Issues:**

| # | Finding | Severity |
|---|---------|----------|
| CS-03 | `RefreshAllTenantHealthScoresAsync` issues **6+ separate COUNT queries per tenant** in a sequential `foreach` loop — O(N·Q) round-trips; at 500 tenants this is 3,000+ network round-trips per refresh cycle | Critical |
| CS-04 | `TenantHealthScoreRecord` is a `class` with `init` properties — should be a `sealed record` per project patterns | Low |
| CS-05 | `TenantHealthScoreSqlRow` (private record) and `SignalAggRow` are correct but `UpdatedUtc` is `DateTime` not `DateTimeOffset`, requiring a `new DateTimeOffset(row.UpdatedUtc, TimeSpan.Zero)` conversion — fragile if the column ever changes timezone encoding | Medium |

**Fix for CS-03 (Priority 1):** Replace the in-process per-tenant loop with a single
set-based SQL operation. See §6 (Data Flow) for the recommended stored procedure shape.

---

### 5.3 IOperatorStickinessSnapshotReader → SqlOperatorStickinessSnapshotReader

**Current state:** Executes two large multi-subquery SELECT statements cleanly. Good
use of `CommandDefinition` for cancellation.

**Issues:**

| # | Finding | Severity |
|---|---------|----------|
| CS-06 | Does **not** inject or apply `IRlsSessionContextApplicator` — if called from a tenant-scoped request context, tenant isolation relies solely on `WHERE TenantId = @TenantId` parameters. A future call without the parameter guard would silently cross tenant boundaries | High |
| CS-07 | `ToDateTimeOffset` method name says `DateTimeOffset` but returns `DateTime?` — misleading and bug-prone | Low |
| CS-08 | `OperatorSignalsRow` and `FunnelRow` inner classes use multi-line `{ get; init; }` properties (same as CS-01) | Low |
| CS-09 | `ToInt(object? v)` defensive converter suggests SQL is returning `int` or `long` inconsistently — this should be pinned to `COUNT_BIG` → `bigint` everywhere and deserialized as `long`, then cast once cleanly | Low |

**Fix for CS-06:** Route `GetOperatorSignalsAsync` and `GetFunnelSnapshotAsync` through
the read replica connection factory (these are pure reads) **and** apply RLS. The stickiness
reader should accept `IRlsSessionContextApplicator` in its primary constructor.

---

### 5.4 InMemory* Implementations

All three in-memory implementations are correctly implemented as no-ops for offline/test
use. `InMemoryCorePilotTeamChecklistRepository` uses a `lock(_gate)` correctly.

No structural issues; these do not need to change.

---

## 6. Data Flow

### Write Path — Checklist Step Update

```
API Controller
  → ICorePilotTeamChecklistRepository.UpsertAsync(tenantId, workspaceId, projectId, stepIndex, ...)
    → ISqlConnectionFactory.CreateOpenConnectionAsync()
    → IRlsSessionContextApplicator.ApplyAsync()   ← enforces tenant row filter
    → MERGE dbo.CorePilotTeamChecklist            ← single round-trip
```

### Write Path — Product Feedback

```
API Controller
  → ITenantCustomerSuccessRepository.InsertProductFeedbackAsync(submission)
    → ISqlConnectionFactory.CreateOpenConnectionAsync()
    → IRlsSessionContextApplicator.ApplyAsync()
    → INSERT dbo.ProductFeedback                  ← single round-trip
```

### Read Path — Stickiness Signals (recommended future state)

```
API Controller
  → IOperatorStickinessSnapshotReader.GetOperatorSignalsAsync(tenantId, ...)
    → IReadReplicaQueryConnectionFactory.CreateOpenConnectionAsync()   ← read replica
    → IRlsSessionContextApplicator.ApplyAsync()                        ← ADD THIS
    → single multi-subquery SELECT                                     ← already correct
```

### Background Scoring Refresh (recommended future state)

Replace the per-tenant sequential loop with a **single set-based MERGE** stored
procedure that sources its inputs from existing table aggregates in one pass:

```sql
-- Conceptual shape of dbo.sp_TenantHealthScores_BatchRefresh
-- Called once; walks all tenants using CTEs, no C# loop required.

WITH TenantWorkspaces AS (
    -- resolve one canonical workspace per tenant
    SELECT t.TenantId, w.WorkspaceId, w.DefaultProjectId
    FROM   dbo.Tenants t
    CROSS APPLY (
        SELECT TOP (1) WorkspaceId, DefaultProjectId
        FROM   dbo.TenantWorkspaces tw
        WHERE  tw.TenantId = t.TenantId
        ORDER  BY tw.CreatedUtc
    ) w
),
Signals AS (
    SELECT
        tw.TenantId, tw.WorkspaceId, tw.DefaultProjectId AS ProjectId,
        -- engagement
        SUM(CASE WHEN r.CreatedUtc >= DATEADD(DAY,-7,SYSUTCDATETIME()) THEN 1 ELSE 0 END) AS Runs7d,
        ...
    FROM TenantWorkspaces tw
    LEFT JOIN dbo.Runs r ON r.TenantId = tw.TenantId ...
    LEFT JOIN dbo.AuditEvents ae ON ae.TenantId = tw.TenantId ...
    GROUP BY tw.TenantId, tw.WorkspaceId, tw.DefaultProjectId
)
MERGE dbo.TenantHealthScores AS t
USING (
    SELECT
        TenantId, WorkspaceId, ProjectId,
        dbo.fn_EngagementScore(Runs7d, Commits7d, Actors7d)   AS EngagementScore,
        ...
    FROM Signals
) AS s ON t.TenantId = s.TenantId AND t.WorkspaceId = s.WorkspaceId AND t.ProjectId = s.ProjectId
WHEN MATCHED THEN UPDATE SET ...
WHEN NOT MATCHED THEN INSERT ...;
```

**Trade-off:** Scoring logic moves partly into SQL scalar functions. The alternative
(keeping scoring in C# `TenantHealthScoringCalculator`) requires a table-valued
parameter (TVP) approach: compute all scores in C#, then `MERGE` all rows in one batch.
The TVP approach is recommended because it keeps `TenantHealthScoringCalculator`
testable and pure.

**Recommended TVP approach:**

```csharp
// 1. Collect all signal counts in one SQL batch (CTE, no scoring math)
// 2. Run scores through TenantHealthScoringCalculator (pure C# — already tested)
// 3. SqlBulkCopy or TVP MERGE all scored rows in one round-trip
```

This keeps the scoring calculator testable without SQL dependencies and avoids
duplicating business logic in a scalar function.

---

## 7. Security Model

| Control | Mechanism | Gap |
|---------|-----------|-----|
| Tenant row isolation (reads) | RLS via `IRlsSessionContextApplicator` on primary | `SqlOperatorStickinessSnapshotReader` missing (CS-06) |
| Tenant row isolation (background) | `SqlRowLevelSecurityBypassAmbient` — scoped, ambient | No gap; correct pattern |
| Network | Azure SQL Private Endpoint only; no public exposure | No gap |
| Credentials | Managed Identity via `DefaultAzureCredential` (enforced by `SqlConnectionFactory`) | No gap |
| Least privilege | App identity has `db_datawriter` + `db_datareader` on specific schemas only | Review: does the worker identity need cross-tenant SELECT? Yes — document this explicitly |
| Audit | `dbo.ProductFeedback` captures user ID per submission | `dbo.CorePilotTeamChecklist` captures `UpdatedByUserId` — good |

---

## 8. Scalability Path

### Milestone 1 — V1 GA (~50–200 tenants)

The current implementation is **sufficient** for V1 with the following minimal fixes:
- Fix CS-06 (RLS on stickiness reader).
- Fix the sequential refresh loop (CS-03) — even at 50 tenants, 300 round-trips per
  refresh cycle is fragile. Use a single raw SQL aggregation query that returns all
  tenant signal rows, compute scores in C#, then `SqlBulkCopy` the scored rows.

### Milestone 2 — Growth (~500–1,000 tenants)

- Move `RefreshAllTenantHealthScoresAsync` to a dedicated **Azure Functions Timer
  Trigger** (isolated process, not in-process with the API). This decouples scoring
  compute from request latency.
- Route `GetOperatorSignalsAsync` and `GetFunnelSnapshotAsync` to the **read replica**
  (`IReadReplicaQueryConnectionFactory`) — these are pure reads with no side effects.
- Add composite covering indexes (see §8.1).

### Milestone 3 — Scale (~5,000+ tenants)

- Partition `dbo.TenantHealthScores` by hash of `TenantId` (Azure SQL elastic pools
  or Hyperscale tier).
- Consider a **materialized scoring cache** — write health scores to Azure Cache for
  Redis on refresh completion; reads return from Redis with a 5-minute TTL fallback
  to SQL. This eliminates read pressure from dashboard polling.
- For `dbo.ProductFeedback`, append-only ingestion via **Azure Service Bus** queue →
  Functions consumer → SQL insert; decouples feedback submission from SQL write latency.

### 8.1 Recommended Index Additions

```sql
-- CorePilotTeamChecklist: support listing by project scope
CREATE INDEX IX_CorePilotTeamChecklist_Scope
    ON dbo.CorePilotTeamChecklist (TenantId, WorkspaceId, ProjectId, StepIndex)
    INCLUDE (IsCompleted, UpdatedUtc, UpdatedByUserId);

-- TenantHealthScores: point lookup by scope
CREATE UNIQUE INDEX UQ_TenantHealthScores_Scope
    ON dbo.TenantHealthScores (TenantId, WorkspaceId, ProjectId);

-- ProductFeedback: tenant timeline query
CREATE INDEX IX_ProductFeedback_Tenant_Created
    ON dbo.ProductFeedback (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC)
    INCLUDE (Score, FindingRef);
```

---

## 9. Operational Considerations

### Scoring Refresh Idempotency

`sp_TenantHealthScores_Upsert` and any batch replacement use `MERGE` — safe to
re-run on failure without side effects.

### Feedback Write Durability

`dbo.ProductFeedback` inserts are synchronous and transactional. At V1 this is fine.
At scale, move to a durable queue (Service Bus) before the SQL insert to prevent
feedback loss during database maintenance windows.

### Health Score Staleness SLA

The default refresh cadence should target **≤ 15 minutes** staleness (e.g. timer
trigger every 10 minutes). Document this SLA in the trust center.

### Terraform Resource Alignment

The following Azure resources back this module:

```hcl
# Already expected in infra/
resource "azurerm_mssql_server" "primary"    { ... }
resource "azurerm_mssql_database" "archlucid" { sku_name = "GP_Gen5_4" }
resource "azurerm_private_endpoint" "sql"    { ... }  # no public exposure

# Add for scoring worker
resource "azurerm_function_app" "scoring_worker" {
  # Timer trigger; isolated process; Managed Identity assigned
}
resource "azurerm_redis_cache" "health_scores" {
  # Milestone 3 only; SKU = Basic at first, Standard for HA
  sku_name = "Standard"
  family   = "C"
  capacity = 1
}
```

---

## 10. Immediate Code Fixes (Defect List)

These can be applied as a single focused PR without architectural change:

| ID | File | Fix |
|----|------|-----|
| CS-01 | `SqlCorePilotTeamChecklistRepository.cs` | Collapse `Row` inner class properties to one line each |
| CS-04 | `TenantHealthScoreRecord.cs` | Convert `class` → `sealed record` |
| CS-05 | `SqlTenantCustomerSuccessRepository.cs` | Store `UpdatedUtc` as `DateTimeOffset` from SQL or use `DateTime2` column with `AT TIME ZONE` |
| CS-06 | `SqlOperatorStickinessSnapshotReader.cs` | Inject `IRlsSessionContextApplicator`; call `ApplyAsync` before each query |
| CS-07 | `SqlOperatorStickinessSnapshotReader.cs` | Rename `ToDateTimeOffset` → `ToNullableUtcDateTime` to match its actual return type |
| CS-08 | `SqlOperatorStickinessSnapshotReader.cs` | Collapse `OperatorSignalsRow` and `FunnelRow` inner class properties to one line each |
| CS-09 | `SqlOperatorStickinessSnapshotReader.cs` | Use `long` for all COUNT columns in row types; remove `ToInt(object?)` defensive converter |
| CS-03 | `SqlTenantCustomerSuccessRepository.cs` | Replace per-tenant sequential loop with bulk signal collection + TVP MERGE (see §6) |

---

## Appendix A — Current Module Map

```
ArchLucid.Core/CustomerSuccess/
├── ICorePilotTeamChecklistRepository.cs     interface + CorePilotChecklistStepRow record
├── ITenantCustomerSuccessRepository.cs      interface
├── IOperatorStickinessSnapshotReader.cs     interface
├── TenantHealthScoringCalculator.cs         pure static scoring logic (testable)
├── TenantHealthScoreRecord.cs               materialized score shape (→ convert to record)
├── OperatorStickinessModels.cs              OperatorStickinessSignals + PilotFunnelSnapshot records
└── ProductFeedbackSubmission.cs             submission DTO

ArchLucid.Persistence/CustomerSuccess/
├── SqlCorePilotTeamChecklistRepository.cs   production (MERGE upsert, RLS)
├── InMemoryCorePilotTeamChecklistRepository.cs   test/offline no-op
├── SqlTenantCustomerSuccessRepository.cs    production (health scores + feedback)
├── InMemoryTenantCustomerSuccessRepository.cs    test/offline no-op
├── SqlOperatorStickinessSnapshotReader.cs   production (multi-subquery SELECT)
└── InMemoryOperatorStickinessSnapshotReader.cs   test/offline no-op
```

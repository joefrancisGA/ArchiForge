# Change Set 58R — product learning dashboard and improvement triage

## 1. Objective

Give product and pilot stakeholders a **disciplined, queryable trail** of how ArchiForge outputs are received: what is trusted, rejected, or repeatedly revised, and which **repeat patterns** deserve engineering investment — **without** autonomous adaptation or silent policy changes in this change set.

## 2. Assumptions

- Feedback is **human-entered** (operators, pilots, or internal product roles), not inferred from model logits alone.
- **Tenant / workspace / project** scope continues to partition data; aggregation stays within scope unless a later prompt adds cross-tenant analytics (explicitly out of scope here).
- **SQL Server** is the system of record when `ArchiForge:StorageProvider` is `Sql`; **InMemory** uses the same repository interface for local dev.

## 3. Constraints

- **C#** only for application code; **Dapper** for SQL access; **no Entity Framework**.
- **Deterministic** list ordering (newest first, stable tie-break on `SignalId`).
- **No autonomous adaptation**: inserts do not alter prompts, rule packs, or agent configuration.
- **Reuse** existing scope columns and migration/DbUp patterns.

## 4. Architecture overview

**Nodes:** API (future prompts), **product-learning repository**, SQL table **`ProductLearningPilotSignals`**, optional UI dashboard (later).  
**Edges:** Human judgment → persisted signal → read APIs / views → triage UI.  
**Boundaries:** Distinct from **advisory `RecommendationRecords`** / **`RecommendationLearningProfiles`** (those score advisory outputs). 58R targets **cross-cutting pilot feedback** on manifests, artifacts, and runs.

## 5. Component breakdown

| Layer | Responsibility |
|--------|----------------|
| **Contracts** (`ArchiForge.Contracts.ProductLearning`) | Stable strings + `ProductLearningPilotSignalRecord` DTO. |
| **Persistence** | `IProductLearningPilotSignalRepository`, Dapper + in-memory implementations. |
| **SQL** | DbUp `031_*.sql` + `ArchiForge.sql` parity. |
| **API / UI** | *Prompt 2+* — REST surface, aggregation queries, dashboard page. |

## 6. Data flow

1. **Write (future):** authenticated caller supplies scope + disposition + subject + optional pattern key → `InsertAsync`.
2. **Read:** `ListRecentForScopeAsync` returns the latest N rows for triage (cap 500).
3. **Rollups (future):** SQL view or grouped query on `PatternKey` × `Disposition` for “repeat patterns.”

## 7. Security model

- Rows are **scope-scoped**; API endpoints (when added) must enforce the same **tenant/workspace/project** as existing governance APIs.
- **No secrets** in `DetailJson` by convention; operators should not paste credentials.
- Optional `ArchitectureRunId` FK ensures referential integrity when a string run id is supplied.

## 8. Operational considerations

- **DbUp** applies `031_ProductLearningPilotSignals.sql` on API startup against SQL Server.
- **Persistence bootstrap** (`ArchiForge.sql`) creates the same objects on greenfield databases.
- **Indexes** support scope + time, scope + disposition, and filtered **pattern** lookups.

---

## Prompt log

### Prompt 1 — persistence foundation

- Added **`ProductLearningPilotSignals`** table (disposition CHECK, triage CHECK, optional FK to **`ArchitectureRuns`**).
- Added **contracts**, **Dapper + in-memory repositories**, **DI registration** for Sql and InMemory storage.
- **Tests:** in-memory repository unit tests.
- **Docs:** `CHANGE_SET_58R.md`, `DATA_MODEL.md`, `SQL_SCRIPTS.md` catalog.

**Next prompt (suggested):** HTTP API (scoped POST/GET), authorization aligned with operator/admin roles, and optional aggregate DTO for pattern × disposition counts.

### Prompt 2 — aggregation and triage domain models

- **Added** explicit DTO classes (no logic): `FeedbackAggregate`, `ArtifactOutcomeTrend`, `ImprovementOpportunity`, `LearningDashboardSummary`, `TriageQueueItem` under `ArchiForge.Contracts/ProductLearning/`.
- **Next:** repository/query methods and application service to populate these models from `ProductLearningPilotSignals` (and optional joins).

### Prompt 3 — SQL/Dapper aggregation queries

- **Extended** `IProductLearningPilotSignalRepository` with aggregation methods over **`ProductLearningPilotSignals`** (no new tables).
- **Dapper:** explicit CTE/grouped SQL + internal row DTOs (`ProductLearningPilotSignalSqlRows.cs`); in-memory path uses shared **`ProductLearningSignalAggregations`** rules so behavior matches SQL.
- **Added** `RepeatedCommentTheme` contract for deterministic comment-prefix rollups.
- **Tests:** in-memory repository coverage for aggregates, top reject/revise, comment themes, opportunity thresholds.

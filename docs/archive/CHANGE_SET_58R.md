# Change Set 58R — product learning dashboard and improvement triage

## 1. Objective

Give product and pilot stakeholders a **disciplined, queryable trail** of how ArchLucid outputs are received: what is trusted, rejected, or repeatedly revised, and which **repeat patterns** deserve engineering investment — **without** autonomous adaptation or silent policy changes in this change set.

## 2. Assumptions

- Feedback is **human-entered** (operators, pilots, or internal product roles), not inferred from model logits alone.
- **Tenant / workspace / project** scope continues to partition data; aggregation stays within scope unless a later prompt adds cross-tenant analytics (explicitly out of scope here).
- **SQL Server** is the system of record when `ArchLucid:StorageProvider` is `Sql`; **InMemory** uses the same repository interface for local dev.

## 3. Constraints

- **C#** only for application code; **Dapper** for SQL access; **no Entity Framework**.
- **Deterministic** list ordering (newest first, stable tie-break on `SignalId`).
- **No autonomous adaptation**: inserts do not alter prompts, rule packs, or agent configuration.
- **Reuse** existing scope columns and migration/DbUp patterns.

## 4. Architecture overview

**Nodes:** **Product-learning repository**, SQL table **`ProductLearningPilotSignals`**, **read APIs** (`/v1/product-learning/...`), **operator UI** (**Pilot feedback** page), optional future HTTP write for pilots.  
**Edges:** Human judgment → persisted signal → aggregation services → dashboard / export → triage discussion.  
**Boundaries:** Distinct from **advisory `RecommendationRecords`** / **`RecommendationLearningProfiles`** (those score advisory outputs). 58R targets **cross-cutting pilot feedback** on manifests, artifacts, and runs.

## 5. Component breakdown

| Layer | Responsibility |
|--------|----------------|
| **Contracts** (`ArchLucid.Contracts.ProductLearning`) | Stable strings + `ProductLearningPilotSignalRecord` DTO. |
| **Persistence** | `IProductLearningPilotSignalRepository`, Dapper + in-memory implementations. |
| **SQL** | DbUp `031_*.sql` + `ArchLucid.sql` parity. |
| **API** | `ProductLearningController`: summary, opportunities, trends, triage queue, triage report (`markdown` / `json`). |
| **UI** | Operator shell **Pilot feedback** (`/product-learning`), export links; nav distinct from **Learning** (recommendation learning). |
| **Docs** | [PRODUCT_LEARNING.md](PRODUCT_LEARNING.md) — operator & product-owner workflow. |

## 6. Data flow

1. **Write:** Integrators insert rows via **`IProductLearningPilotSignalRepository`** (scope, disposition, subject, optional pattern key, comment, run link). A first-party **HTTP POST** for pilots may follow in a later change.
2. **Aggregate:** Repository methods + **`IProductLearningFeedbackAggregationService`** / **`IProductLearningDashboardService`** build rollups, trends, ranked opportunities, triage queue (deterministic ordering).
3. **Expose:** **`GET /v1/product-learning/*`** and operator **Pilot feedback** page; **report** endpoints emit concise Markdown/JSON triage summaries (not full raw comments).

## 7. Security model

- Rows are **scope-scoped**; read/report endpoints use the same **tenant/workspace/project** resolution as other operator APIs (`ReadAuthority`).
- **No secrets** in `DetailJson` by convention; operators should not paste credentials.
- Optional `ArchitectureRunId` FK ensures referential integrity when a string run id is supplied.

## 8. Operational considerations

- **DbUp** applies `031_ProductLearningPilotSignals.sql` on API startup against SQL Server.
- **Persistence bootstrap** (`ArchLucid.sql`) creates the same objects on greenfield databases.
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

- **Added** explicit DTO classes (no logic): `FeedbackAggregate`, `ArtifactOutcomeTrend`, `ImprovementOpportunity`, `LearningDashboardSummary`, `TriageQueueItem` under `ArchLucid.Contracts/ProductLearning/`.
- **Next:** repository/query methods and application service to populate these models from `ProductLearningPilotSignals` (and optional joins).

### Prompt 3 — SQL/Dapper aggregation queries

- **Extended** `IProductLearningPilotSignalRepository` with aggregation methods over **`ProductLearningPilotSignals`** (no new tables).
- **Dapper:** explicit CTE/grouped SQL + internal row DTOs (`ProductLearningPilotSignalSqlRows.cs`); in-memory path uses shared **`ProductLearningSignalAggregations`** rules so behavior matches SQL.
- **Added** `RepeatedCommentTheme` contract for deterministic comment-prefix rollups.
- **Tests:** in-memory repository coverage for aggregates, top reject/revise, comment themes, opportunity thresholds.

### Prompt 4 — product learning / triage service layer

- **Contracts:** `ProductLearningScope`, `ProductLearningTriageOptions`, `ProductLearningAggregationSnapshot`, and service interfaces (`IProductLearningFeedbackAggregationService`, `IProductLearningImprovementOpportunityService`, `IProductLearningDashboardService`).
- **Persistence:** `ProductLearningFeedbackAggregationService`, `ProductLearningImprovementOpportunityService`, `ProductLearningDashboardService`, `ProductLearningOpportunityScoring` (deterministic scoring helpers).
- **Repository:** `CountSignalsInScopeAsync`, `CountDistinctArchitectureRunsWithSignalsAsync` for accurate dashboard totals.
- **DI:** registered scoped (SQL) / singleton (in-memory) alongside `IProductLearningPilotSignalRepository`.
- **Tests:** dashboard + count smoke tests.

### Prompts 5–8 (summary)

- **HTTP API** for dashboard slices (`summary`, `improvement-opportunities`, `artifact-outcome-trends`, `triage-queue`) with query validation.
- **Operator UI** dashboard + **export** (`report`, `report/file`).
- **Focused tests** (`ChangeSet=58R` / `ProductLearning` filters): aggregation, ranking, parser, API, report builder, URL helpers.

### Prompt 9 — documentation

- **Added** [PRODUCT_LEARNING.md](PRODUCT_LEARNING.md) (capture, dashboard, opportunities, export, owner guidance).
- **Updated** [PILOT_GUIDE.md](PILOT_GUIDE.md), [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md), [README.md](../README.md), [archlucid-ui/README.md](../archlucid-ui/README.md), this file (overview §4–§7, component table, prompt log).

### Coherence / cleanup pass (post–Prompt 9)

- **Aggregation:** `GetSnapshotAsync` no longer calls `ListTopRejectedRevisedArtifactRollupsAsync` — that slice was never consumed by dashboard, opportunities, or export (extra SQL work only). `TopRejectedRevisedRollups` on the snapshot stays **empty** until a future feature uses it; documented on the contract and in [DATA_MODEL.md](DATA_MODEL.md).
- **Dashboard notes:** Removed the summary line that duplicated KPI chip counts (less noise in the expandable “How to read” list).
- **Naming:** Clarified `ProductLearningTriageReportDocument.DistinctRunsReviewed` ↔ `LearningDashboardSummary.DistinctRunsTouched` in XML; `TopRejectedRevisedTake` option documented as unused by aggregation today.
- **Docs:** [PRODUCT_LEARNING.md](PRODUCT_LEARNING.md) states the UI issues **four** aligned GETs per refresh.

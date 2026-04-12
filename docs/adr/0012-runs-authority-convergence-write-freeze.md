# ADR 0012: Runs / authority convergence — legacy `ArchitectureRuns` removal

- **Status:** Completed (2026-04-12)
- **Date:** 2026-04-11 (accepted); **closure:** 2026-04-12
- **Extends / closes:** [0002-dual-persistence-architecture-runs-and-runs.md](0002-dual-persistence-architecture-runs-and-runs.md) (now **Superseded**)

## Context

The product previously maintained **dual persistence**: authority **`dbo.Runs`** ( **`UNIQUEIDENTIFIER` `RunId`** ) and legacy **`dbo.ArchitectureRuns`** (string **`RunId`** ). Coordinator tables used string run ids; migration **047** dropped inbound FKs from those tables to **`ArchitectureRuns`** so **`dbo.Runs`** could become the sole header without a type-matched FK.

## Decision (final)

1. **Remove** **`IArchitectureRunRepository`**, **`ArchitectureRunRepository`**, **`InMemoryArchitectureRunRepository`**, and all product writes to **`dbo.ArchitectureRuns`**.
2. **Drop** **`dbo.ArchitectureRuns`** in migration **`049_DropArchitectureRunsTable.sql`** (idempotent **`DROP TABLE`** after **047**).
3. **Master DDL** **`ArchLucid.sql`** — greenfield installs no longer create **`ArchitectureRuns`**; lifecycle strings live on **`dbo.Runs.LegacyRunStatus`** (migration **048**).
4. **Remove** dual-persistence health checks (**`dual_persistence_consistency`**, **`dual_persistence_row_reconciliation`**) and the CI pragma guard **`assert_architecture_run_write_pragma.py`**.
5. Retain the **`ArchitectureRun`** / **`ArchitectureRunDetail`** **DTOs** in **`ArchLucid.Contracts`** — they model API shape, not the dropped table.

## Inventory — historical write paths (all retired)

The table in revision **2026-04-11** listed **`CoordinatorService`**, **`DemoSeedService`**, repositories, and orchestrator bridges. As of **2026-04-12**:

- **`CoordinatorService`** inserts **no** legacy row; it patches **`dbo.Runs`** only.
- **`DemoSeedService`** seeds **`dbo.Runs`** and coordinator data only.
- **`DapperProductLearningPlanningPlanLinkRepository`** validates run ids against **`dbo.Runs`** (not **`ArchitectureRuns`**).
- SQL contract seed **`ArchitectureCommitTestSeed`** inserts **`dbo.Runs`** rows ( **`N`** format run id + fixed scope GUIDs).

## Appendix: Foreign keys that referenced `dbo.ArchitectureRuns` (migration 047)

Dropped by **`ArchLucid.Persistence/Migrations/047_DropForeignKeysToArchitectureRuns.sql`**. See that file for the explicit constraint list. Migration **049** then removes **`dbo.ArchitectureRuns`** itself (including **`FK_ArchitectureRuns_Request`**).

## Consequences

- **Positive:** One run header table; no drift probes between two stores; simpler operator mental model.
- **Negative:** Brownfield databases must apply **047** before **049**; historical DbUp scripts **001–048** remain immutable except additive policy — **049** is the drop.

## Links

- `docs/DATA_CONSISTENCY_MATRIX.md`
- [0002-dual-persistence-architecture-runs-and-runs.md](0002-dual-persistence-architecture-runs-and-runs.md)

## Audit method (closure)

- **`rg IArchitectureRunRepository`** and **`rg ArchitectureRunRepository`** across **`*.cs`** — expect **no** matches outside **`docs/`** and historical **`*.sql`** migrations.

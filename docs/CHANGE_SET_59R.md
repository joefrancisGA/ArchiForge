# Change Set 59R — learning-to-planning bridge

## 1. Objective

Turn **58R product-learning aggregates** into **structured improvement themes** and **bounded, human-reviewable improvement plans**, with **explicit links** to runs, artifacts, pilot feedback, and triage context—using **deterministic** rules and **no autonomous system mutation**.

## 2. Assumptions

- Operators or integrators **materialize** themes and plans (or a future service does so **explicitly** under policy), rather than the runtime silently changing prompts or packs.
- **Opportunity IDs** from live `ImprovementOpportunity` projections may be **ephemeral** today; persisted themes use stable **`ThemeKey`** plus optional **`SourceAggregateKey`** / **`PatternKey`** for traceability.

## 3. Constraints

- **C#**, **SQL Server**, **Dapper**; no Entity Framework.
- **No changes** to core generation/evaluation logic in 59R.
- **Scoped** data same as **`ProductLearningPilotSignals`**.

## 4. Architecture overview

**Nodes:** SQL tables for themes and plans, junction tables for links, **`IProductLearningPlanningRepository`**.  
**Edges:** Theme ← plan → (runs, signals, artifacts).  
**Flows (future prompts):** read 58R snapshot → derive themes → derive plans + priority explanation → optional HTTP/UI.

## 5. Prompt log

### Prompt 1 — persistence foundation

- **DbUp** `032_ProductLearningPlanningBridge.sql` + **`ArchiForge.sql`** parity.
- **Contracts** under `ArchiForge.Contracts/ProductLearning/Planning/`.
- **Persistence:** `IProductLearningPlanningRepository`, `DapperProductLearningPlanningRepository`, `InMemoryProductLearningPlanningRepository`, validation + JSON helpers.
- **DI:** registered in `ArchiForgeStorageServiceCollectionExtensions` (scoped SQL / singleton in-memory).
- **Tests:** `ProductLearningPlanningRepositoryTests`.
- **Docs:** `SQL_SCRIPTS.md`, `DATA_MODEL.md`, this file.

**Next prompt (suggested):** deterministic **theme derivation** service consuming `IProductLearningImprovementOpportunityService` / aggregation snapshot; stable **`ThemeKey`** builder; optional **plan draft** builder with **priority score + explanation** from frequency, severity, trust.

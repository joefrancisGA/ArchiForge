# SQL scripts — reference & operations

This document is the **canonical guide** to every SQL artifact in ArchiForge: what each file does, how it is executed, how the pieces relate, and how to change schema safely.

**Related:** [DATA_MODEL.md](DATA_MODEL.md) (tables and domains at a glance) · [README.md](../README.md) (database setup & DbUp) · [TEST_STRUCTURE.md](TEST_STRUCTURE.md) (`DatabaseMigrationScriptTests`)

---

## 1. Why there are two SQL pathways

ArchiForge uses **two** mechanisms for SQL Server schema (by design):

| Pathway | When it runs | Engine | Script source | Purpose |
|--------|----------------|--------|----------------|--------|
| **DbUp migrations** | API startup when `ConnectionStrings:ArchiForge` is set | SQL Server | `ArchiForge.Data/Migrations/*.sql` embedded in **ArchiForge.Data** | **Authoritative upgrades** for deployed and test databases; ordered, transactional, logged. |
| **Persistence schema bootstrap** | First use of Dapper SQL persistence (same DB as API typically) | SQL Server | `ArchiForge.Data/SQL/ArchiForge.sql` copied to **ArchiForge.Persistence** output as `Scripts/ArchiForge.sql` | Ensures **authority + decisioning** objects exist; runs **full** consolidated DDL in `GO` batches (see §3). |

**Important:**

- **Production / dev SQL Server:** Schema evolution is driven by **migrations**. The consolidated `ArchiForge.sql` is still run by Persistence bootstrap and should stay **aligned** with migrations + authority DDL, but **do not** rely on it alone for long-lived DB upgrades.
- **Integration tests:** `WebApplicationFactory` hosts use **SQL Server** (e.g. `localhost` with a per-run database name from **`ArchiForgeApiFactory`**). **DbUp runs** on test host startup against that database, same as production code paths.

---

## 2. File locations (quick inventory)

| Path | Role |
|------|------|
| `ArchiForge.Data/SQL/ArchiForge.sql` | SQL Server **consolidated** schema (API + authority + decisioning). Source of truth for **greenfield** / manual runs / Persistence bootstrap copy. |
| `ArchiForge.Data/SQL/README.md` | Short pointer to this doc for repo browsers. |
| `ArchiForge.Data/Migrations/001_*.sql` … `022_*.sql` | Incremental **DbUp** scripts (SQL Server); see §4 catalog. |
| `ArchiForge.Data/Migrations/README.md` | Short pointer + naming rule for DbUp ordering. |
| `ArchiForge.Persistence` output | `Scripts/ArchiForge.sql` — MSBuild **linked copy** of `ArchiForge.Data/SQL/ArchiForge.sql` (`CopyToOutputDirectory`). |

There is **no** remaining `001_AuthorityStore.sql` under Persistence; authority DDL lives inside `ArchiForge.sql`.

---

## 3. `ArchiForge.sql` (SQL Server consolidated)

### 3.1 Purpose

- **Single readable file** describing the **entire** SQL Server surface area used by ArchiForge today: legacy API/agent/commit tables, export/comparison/decision nodes, and the **authority** (`Runs` with `UNIQUEIDENTIFIER`), **Dapper** repositories, and **decisioning** (recommendations, advisory, digests, alerts, composite rules, policy packs).
- **Idempotent** for objects it creates: safe to run multiple times on a database where objects may already exist from DbUp or an older run.

### 3.2 How it is executed

- **`SqlSchemaBootstrapper`** (`ArchiForge.Persistence`) reads the file as text, splits on **`GO`** (line-based, case-insensitive), and executes each batch with Dapper against the **ArchiForge** SQL connection.
- Path resolution: `Path.Combine(assemblyDir, "Scripts", "ArchiForge.sql")` where the assembly is **ArchiForge.Persistence** (see `ArchiForgeStorageServiceCollectionExtensions`).
- **Requirement:** `ArchiForge.sql` must use **`GO`** batch separators compatible with that splitter (batch per logical unit).

### 3.3 Idempotency rules (what “safe to re-run” means)

| Pattern | Meaning |
|---------|--------|
| `IF OBJECT_ID(N'dbo.Table', N'U') IS NULL` + `CREATE TABLE` | Table created only if missing. |
| Inline **`INDEX … NONCLUSTERED`** inside `CREATE TABLE` | Indexes are created **with** the table on first creation only. If the table already exists without them, **this script does not add missing indexes** (same limitation as before when indexes were separate statements inside the same `IF` block). |
| `IF NOT EXISTS (…sys.foreign_keys…)` + `ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY` | Repairs **missing FKs** on **existing** tables (legacy or partial restores). |
| No `COL_LENGTH` / `ALTER ADD` for normal columns in consolidated script | **Column** changes for existing deployments belong in **DbUp migrations**; consolidated file assumes **greenfield `CREATE TABLE`** or migrations already applied. |

### 3.4 Document structure (sections)

Read the file top-down; major comment banners include:

1. **`/* ---- Core ---- */`** — `ArchitectureRequests`, `ArchitectureRuns` (string `RunId` for API), FK to requests.
2. **`/* ---- Agents ---- */`** — `AgentTasks`, `AgentResults`, FK batches.
3. **`/* ---- Manifest / evidence ---- */`** — `GoldenManifestVersions`, `EvidenceBundles`, `DecisionTraces`, `AgentEvidencePackages`, `AgentExecutionTraces`.
4. **`/* ---- RunExportRecords ---- */`** — Export records linked to `ArchitectureRuns`.
5. **`/* ---- ComparisonRecords ---- */`** — Comparisons, replay payloads, label/tags, FKs.
6. **`/* ---- Decision Engine v2 ---- */`** — `DecisionNodes`, `AgentEvaluations`.
7. **`/* ---- Authority / Dapper … */`** — **`dbo.Runs`** (`UNIQUEIDENTIFIER` **RunId** — **not** the same as `ArchitectureRuns.RunId`), snapshots, manifests, bundles, audit, provenance, conversations, then decisioning tables (`RecommendationRecords`, advisory, digests, alerts, policy packs, etc.).

### 3.5 Two different “run” tables (common confusion)

| Table | `RunId` type | Used by |
|-------|----------------|--------|
| **`dbo.ArchitectureRuns`** | `NVARCHAR(64)` | Legacy API orchestration, agents, exports, comparisons, decision nodes. |
| **`dbo.Runs`** | `UNIQUEIDENTIFIER` | Authority pipeline, Dapper `IRunRepository`, context/graph/findings/manifest/bundle chain. |

They are **different domains**; names overlap conceptually but not at the database type level.

### 3.6 Indexes

- All **nonclustered** secondary indexes are declared **inline** on `CREATE TABLE` using SQL Server syntax:  
  `INDEX IX_Name NONCLUSTERED (col1, col2 DESC) [WHERE (…)]`  
- **Filtered** indexes match prior behavior (e.g. nullable snapshot id columns, `ComparisonRecords.Label`).

### 3.7 When to change `ArchiForge.sql`

- **Always** when you add or change objects that Persistence bootstrap must create on a **fresh** database.
- **Always** add or extend a **DbUp migration** for SQL Server deployments that already exist.

---

## 4. DbUp migrations (`ArchiForge.Data/Migrations/`)

### 4.1 Mechanics

- Scripts are **`EmbeddedResource`** in **ArchiForge.Data** (`Migrations\*.sql` only — no ad-hoc SQL under `SQL\` is picked up by DbUp).
- **DbUp** (`DatabaseMigrator.Run`) selects only embedded names that contain **`.Migrations.`** and end with **`.sql`**, so only `ArchiForge.Data/Migrations/*.sql` run.
- Ordering is **lexicographic by embedded resource name** — keep the filename prefix pattern **`001_`**, **`002_`**, … (see `DatabaseMigrationScriptTests`).

### 4.2 Catalog (one file = one version step)

| File | Summary |
|------|--------|
| **001_InitialSchema.sql** | Base API schema: requests, runs, agents, manifests, evidence, decision traces, export records, core indexes/FKs. |
| **002_ComparisonRecords.sql** | `ComparisonRecords` table for stored comparisons / replay payloads. |
| **003_ComparisonRecords_LabelAndTags.sql** | Adds `Label`, `Tags` on `ComparisonRecords`. |
| **004_DecisionNodes_And_Evaluations.sql** | `DecisionNodes`, `AgentEvaluations`. |
| **005–007** | `ArchitectureRuns` snapshot id columns (`ContextSnapshotId`, `GraphSnapshotId`, `ArtifactBundleId`). |
| **008–016** | Recommendations, advisory, digests, alerts, routing, composite rules, policy packs, scoped assignments. |
| **017_GovernanceWorkflow.sql** | Governance approval / promotion / environment activation tables. |
| **017_GraphSnapshots_ParentTables.sql** | Authority **`Runs`**, **`ContextSnapshots`**, **`GraphSnapshots`** (parent of **`GraphSnapshotEdges`** FK). |
| **018_GraphSnapshotEdges.sql** | Denormalized graph edges table + index. |
| **019_RetrievalIndexingOutbox.sql** | Post-commit retrieval indexing outbox. |
| **020_PerformanceIndexes_HotLists.sql** | Hot-list indexes (e.g. scoped **`Runs`** lists). |
| **021_ArchitectureRunIdempotency.sql** | **`ArchitectureRunIdempotency`** for **`Idempotency-Key`** on create run. |
| **022_GraphSnapshotEdges_IndexKeyLength.sql** | Index shape fix for **`GraphSnapshotEdges`** (1700-byte key limit). |
| **023–030** | Relational snapshot children, performance indexes, idempotency, retrieval outbox, governance workflow extras, archival flags, RLS pilot on **`dbo.Runs`** (superseded by 036), etc. |
| **031_ProductLearningPilotSignals.sql** | **58R:** Scoped pilot/product signals (trust / reject / revise / follow-up) with optional **`PatternKey`** for aggregation. |
| **036_RlsArchiforgeTenantScope.sql** | RLS **`rls.ArchiforgeTenantScope`** on all scope-keyed authority tables (replaces pilot **`RunsScopeFilter`**). See **`docs/security/MULTI_TENANT_RLS.md`**. |
| **032_ProductLearningPlanningBridge.sql** | **59R:** Improvement themes, bounded plans (`BoundedActionsJson`), links to **`ArchitectureRuns`**, **`ProductLearningPilotSignals`**, and authority bundle artifacts / pilot hints. |

**Note:** Authority-chain tables also appear in **`ArchiForge.sql`** for Persistence bootstrap parity.

### 4.3 Adding a new migration `0NN_…`

1. Create `ArchiForge.Data/Migrations/0NN_YourChange.sql` (idempotent `IF` / `IF NOT EXISTS` patterns preferred).
2. Update **`ArchiForge.sql`** with the same objects/columns/indexes for greenfield parity.
3. Run tests; optionally extend **`DatabaseMigrationScriptTests`** if you add new ordering rules.
4. Update §4.2 in this file.

---

## 5. Change checklist (schema work)

Treat this checklist as a **definition of done** for every schema change. Do not merge without completing each applicable item.

### Required for every SQL change

- [ ] **DbUp migration:** new `ArchiForge.Data/Migrations/0NN_*.sql` for SQL Server incremental change. Use `IF NOT EXISTS` / `IF OBJECT_ID IS NULL` patterns; migrations must be idempotent.
- [ ] **`ArchiForge.Data/SQL/ArchiForge.sql`:** same objects, columns, and indexes as the migration — keeps greenfield provisioning in parity.
- [ ] **Migration catalog:** update §4.2 of this file with the new migration number and description.

### Required when schema changes affect data access

- [ ] **C# repositories / contracts:** update Dapper queries, `IRepository` methods, and any affected DTOs in `ArchiForge.Contracts` or `ArchiForge.Data`.
- [ ] **`docs/DATA_MODEL.md`:** update the conceptual data model to reflect new or modified tables/columns.

### Required when schema changes affect seeding or demos

- [ ] **`DemoSeedService`:** if demo data references the new table or column, update `EnsureXxxAsync` to include it.
- [ ] **`DemoSeedServiceTests`:** verify the idempotency test still passes after the change.
- [ ] **`DatabaseMigrationScriptTests`:** extend if new ordering or naming rules need to be enforced.

### CI gate

Before opening a PR with SQL changes, run the full local pre-push loop from `docs/CI_MIGRATION_CHECKLIST.md`.

---

## 6. Troubleshooting

| Symptom | Things to check |
|---------|------------------|
| **“Schema script not found”** (Persistence) | `ArchiForge.Persistence` build output contains **`Scripts/ArchiForge.sql`**; verify `ArchiForge.Persistence.csproj` link to `..\ArchiForge.Data\SQL\ArchiForge.sql`. |
| **Missing tables on SQL Server** | DbUp errors on startup (API logs); run migrations manually in order if needed. Persistence bootstrap only runs when SQL storage is registered. |
| **Duplicate or wrong migration order** | Embedded resource names must sort correctly (`010` before `011`). |

---

## 7. Security & operations

- Scripts contain **no secrets**. Connection strings live in configuration (User Secrets, env, Key Vault, etc.).
- **Production:** Prefer controlled migration runs (CI/CD or DBA) over ad-hoc execution of `ArchiForge.sql`, unless you intentionally use it for greenfield provisioning.

---

## 8. Versioning

- Update the **migration catalog** (§4.2) when adding `0NN_*.sql`.
- This document’s last migration line should stay in sync with the highest `ArchiForge.Data/Migrations/0NN_*.sql` file.

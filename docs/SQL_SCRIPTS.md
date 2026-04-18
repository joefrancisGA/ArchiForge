# SQL scripts — reference & operations

This document is the **canonical guide** to every SQL artifact in ArchLucid: what each file does, how it is executed, how the pieces relate, and how to change schema safely.

**Related:** [DATA_MODEL.md](DATA_MODEL.md) (tables and domains at a glance) · [README.md](../README.md) (database setup & DbUp) · [TEST_STRUCTURE.md](TEST_STRUCTURE.md) (`DatabaseMigrationScriptTests`)

---

## 1. Why there are two SQL pathways

ArchLucid uses **two** mechanisms for SQL Server schema (by design):

| Pathway | When it runs | Engine | Script source | Purpose |
|--------|----------------|--------|----------------|--------|
| **DbUp migrations** | API startup when **`ConnectionStrings:ArchLucid`** is set | SQL Server | `ArchLucid.Persistence/Migrations/*.sql` (+ optional **`Migrations/Baseline/`**) embedded in **ArchLucid.Persistence** | **Authoritative upgrades** for deployed and test databases; ordered, transactional, logged. |
| **Persistence schema bootstrap** | First use of Dapper SQL persistence (same DB as API typically) | SQL Server | `ArchLucid.Persistence/Scripts/ArchLucid.sql` copied to **ArchLucid.Persistence** output as `Scripts/ArchLucid.sql` | Ensures **authority + decisioning** objects exist; runs **full** consolidated DDL in `GO` batches (see §3). |

**Important:**

- **`appsettings.Advanced.json`** (optional, loaded in **`ArchLucid.Api/Program`**) is chained **after** the default configuration providers, so it can override earlier environment variables (for example **`ArchLucid:Persistence:AllowRlsBypass=false`** while **`SqlServer:RowLevelSecurity:ApplySessionContext`** is **true**). The API then re-applies **`AddEnvironmentVariables()`** so **`ARCHLUCID_ALLOW_RLS_BYPASS`** and **`ArchLucid__Persistence__AllowRlsBypass`** from CI or hosts still enable coordinated RLS break-glass for **`SqlRowLevelSecurityBypassAmbient.Enter()`** during schema bootstrap.
- **API / Worker startup (`ArchLucidPersistenceStartup`):** runs **DbUp first**, then **`SqlSchemaBootstrapper`** (`ArchLucid.sql`). If bootstrap ran first on an **empty** catalog, `ArchLucid.sql` would create objects that migration **`001`** also creates; DbUp would then see an empty **`SchemaVersions`** journal and fail with **“There is already an object named …”**. DbUp-first matches **`DatabaseMigrator.Run`**-only integration setups (e.g. **`ARCHLUCID_SQL_TEST`** full regression).
- **Production / dev SQL Server:** Schema evolution is driven by **migrations**. The consolidated `ArchLucid.sql` is still run after DbUp and should stay **aligned** with migrations + authority DDL, but **do not** rely on it alone for long-lived DB upgrades.
- **Integration tests:** `WebApplicationFactory` hosts use **SQL Server** (e.g. `localhost` with a per-run database name from **`ArchLucidApiFactory`**). **DbUp** runs on test host startup against that database, same as production code paths.

---

## 2. File locations (quick inventory)

| Path | Role |
|------|------|
| `ArchLucid.Persistence/Scripts/ArchLucid.sql` | SQL Server **consolidated** schema (API + authority + decisioning). Source of truth for **greenfield** / manual runs / Persistence bootstrap copy. |
| `ArchLucid.Persistence/Scripts/README.md` | Short pointer to this doc for repo browsers. |
| `ArchLucid.Persistence/Migrations/001_*.sql` … `022_*.sql` | Incremental **DbUp** scripts (SQL Server); see §4 catalog. |
| `ArchLucid.Persistence/Migrations/README.md` | Short pointer + naming rule for DbUp ordering. |
| `ArchLucid.Persistence` output | `Scripts/ArchLucid.sql` — MSBuild **linked copy** of `ArchLucid.Persistence/Scripts/ArchLucid.sql` (`CopyToOutputDirectory`). |

There is **no** remaining `001_AuthorityStore.sql` under Persistence; authority DDL lives inside `ArchLucid.sql`.

---

## 3. `ArchLucid.sql` (SQL Server consolidated)

### 3.1 Purpose

- **Single readable file** describing the **entire** SQL Server surface area used by ArchLucid today: legacy API/agent/commit tables, export/comparison/decision nodes, and the **authority** (`Runs` with `UNIQUEIDENTIFIER`), **Dapper** repositories, and **decisioning** (recommendations, advisory, digests, alerts, composite rules, policy packs).
- **Idempotent** for objects it creates: safe to run multiple times on a database where objects may already exist from DbUp or an older run.

### 3.2 How it is executed

- **`SqlSchemaBootstrapper`** (`ArchLucid.Persistence`) reads the file as text, splits on **`GO`** (line-based, case-insensitive), and executes each batch with Dapper against the **application** SQL connection (from `ISqlConnectionFactory`).
- Path resolution: `Path.Combine(assemblyDir, "Scripts", "ArchLucid.sql")` where the assembly is **ArchLucid.Persistence** (see `ArchLucidStorageServiceCollectionExtensions`).
- **Requirement:** `ArchLucid.sql` must use **`GO`** batch separators compatible with that splitter (batch per logical unit).

### 3.3 Idempotency rules (what “safe to re-run” means)

| Pattern | Meaning |
|---------|--------|
| `IF OBJECT_ID(N'dbo.Table', N'U') IS NULL` + `CREATE TABLE` | Table created only if missing. |
| Inline **`INDEX … NONCLUSTERED`** inside `CREATE TABLE` | Indexes are created **with** the table on first creation only. If the table already exists without them, **this script does not add missing indexes** (same limitation as before when indexes were separate statements inside the same `IF` block). |
| `IF NOT EXISTS (…sys.foreign_keys…)` + `ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY` | Repairs **missing FKs** on **existing** tables (legacy or partial restores). |
| No `COL_LENGTH` / `ALTER ADD` for normal columns in consolidated script | **Column** changes for existing deployments belong in **DbUp migrations**; consolidated file assumes **greenfield `CREATE TABLE`** or migrations already applied. |

### 3.4 Document structure (sections)

Read the file top-down; major comment banners include:

1. **`/* ---- Core ---- */`** — `ArchitectureRequests` (requests are still keyed here; run header is **`dbo.Runs`**).
2. **`/* ---- Agents ---- */`** — `AgentTasks`, `AgentResults`, FK batches.
3. **`/* ---- Manifest / evidence ---- */`** — `GoldenManifestVersions`, `EvidenceBundles`, `DecisionTraces`, `AgentEvidencePackages`, `AgentExecutionTraces`.
4. **`/* ---- RunExportRecords ---- */`** — Export records linked by string `RunId` (correlates with **`dbo.Runs.RunId`** as **N** hex).
5. **`/* ---- ComparisonRecords ---- */`** — Comparisons, replay payloads, label/tags, FKs.
6. **`/* ---- Decision Engine v2 ---- */`** — `DecisionNodes`, `AgentEvaluations`.
7. **`/* ---- Authority / Dapper … */`** — **`dbo.Runs`** (`UNIQUEIDENTIFIER` **RunId** — sole persisted run header), snapshots, manifests, bundles, audit, provenance, conversations, then decisioning tables (`RecommendationRecords`, advisory, digests, alerts, policy packs, etc.).

### 3.5 Run identity (post–049)

| Layer | `RunId` shape | Notes |
|-------|----------------|------|
| **`dbo.Runs.RunId`** | `UNIQUEIDENTIFIER` | Authority header; lifecycle strings may appear on **`LegacyRunStatus`**. |
| Coordinator tables (`AgentTasks`, `GoldenManifestVersions`, …) | `NVARCHAR(64)` | Logical correlation key — same value as **`dbo.Runs.RunId`** formatted **`N`** (no dashes). **No FK** to a second run table after **047**/**049**. |

### 3.6 Indexes

- All **nonclustered** secondary indexes are declared **inline** on `CREATE TABLE` using SQL Server syntax:  
  `INDEX IX_Name NONCLUSTERED (col1, col2 DESC) [WHERE (…)]`  
- **Filtered** indexes match prior behavior (e.g. nullable snapshot id columns, `ComparisonRecords.Label`).

### 3.7 When to change `ArchLucid.sql`

- **Always** when you add or change objects that Persistence bootstrap must create on a **fresh** database.
- **Always** add or extend a **DbUp migration** for SQL Server deployments that already exist.

---

## 4. DbUp migrations (`ArchLucid.Persistence/Migrations/`)

### 4.0 Greenfield baseline (`Migrations/Baseline/`)

- **`Migrations/Baseline/000_Baseline_2026_04_17.sql`** is a **single forward script** that replays the cumulative effect of migrations **`001`–`050`** (mechanical union of those files; regenerate if that band changes).
- **`GreenfieldBaselineMigrationRunner`** (called from **`DatabaseMigrator.Run`** before DbUp) runs whenever **`dbo.SchemaVersions`** does **not** yet record **`001_InitialSchema`** — including a **non-empty** journal missing that row (shared CI drift). It **replays embedded migrations `001`–`050` in order** (same batches as DbUp would) when tenant tables are absent, then **stamps** those script names into **`SchemaVersions`** so DbUp continues at **`051`** onward. If **`ArchitectureRequests`** already exists but **`dbo.AuditEvents`** does not (empty or inconsistent journal, partial CI catalog), it **stamps** (and **replays `035`–`050` only** when **`AuditEvents`** is still missing) so DbUp does not hit `060`+ DDL against a missing **`AuditEvents`** table or re-execute **`001`** DDL. Tenant presence is detected with **`OBJECT_ID` on `dbo` and on `QUOTENAME(SCHEMA_NAME()) + '.ArchitectureRequests'`** (001 uses an unqualified **`CREATE TABLE`**, so the target is the session default schema), plus **`sys.objects`** in **`SCHEMA_NAME()`** for any non-system name collision (e.g. synonym) that would still block **`001`** replay. The checked-in **`000_Baseline_*.sql`** file is a **documentation / audit union** of those scripts (regenerate with the repo script when `001`–`050` change); runtime does **not** split that mega-file on `GO` (comment-safe).
- **Brownfield / existing databases** that already ran **`001`** keep the normal incremental path; the baseline file is **not** executed.
- Baseline scripts are **not** part of the “latest ten rollback” CI guard (only root **`NNN_*.sql`** forward files are).

### 4.1 Mechanics

- Scripts are **`EmbeddedResource`** in **ArchLucid.Persistence** (`Migrations\*.sql` plus `Migrations\Baseline\*.sql` — consolidated `Scripts\ArchLucid.sql` is never picked up by DbUp).
- **DbUp** (`DatabaseMigrator.Run`) selects embedded names that contain **`.Migrations.`** and end with **`.sql`**, but **excludes** **`.Migrations.Baseline.`** (baseline is applied by **`GreenfieldBaselineMigrationRunner`**, not DbUp’s script provider).
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
| **047_DropForeignKeysToArchitectureRuns.sql** | Drops **15** FK constraints from coordinator / learning tables to **`dbo.ArchitectureRuns`** (see migration header for names). Does **not** add FKs to **`dbo.Runs`** (**`UNIQUEIDENTIFIER`** vs legacy **`NVARCHAR(64)`** **`RunId`**). |
| **049_DropArchitectureRunsTable.sql** | **`DROP TABLE dbo.ArchitectureRuns`** when present (after **047**). Greenfield **`ArchLucid.sql`** no longer creates **`ArchitectureRuns`**. |
| **050_PolicyPackChangeLog.sql** | Append-only **`dbo.PolicyPackChangeLog`** (policy pack / version / assignment mutations) plus RLS predicate when **`ArchiforgeTenantScope`** exists. |
| **051_AuditEvents_DenyUpdateDelete.sql** | When database role **`ArchLucidApp`** exists: **`DENY UPDATE`** and **`DENY DELETE`** on **`dbo.AuditEvents`** (append-only enforcement). Skips if role absent. See **`docs/AUDIT_COVERAGE_MATRIX.md`**. |

**Note:** Authority-chain tables also appear in **`ArchLucid.sql`** for Persistence bootstrap parity.

### 4.3 Adding a new migration `0NN_…`

1. Create `ArchLucid.Persistence/Migrations/0NN_YourChange.sql` (idempotent `IF` / `IF NOT EXISTS` patterns preferred).
2. Update **`ArchLucid.sql`** with the same objects/columns/indexes for greenfield parity.
3. Run tests; optionally extend **`DatabaseMigrationScriptTests`** if you add new ordering rules.
4. Update §4.2 in this file.

---

## 5. Change checklist (schema work)

Treat this checklist as a **definition of done** for every schema change. Do not merge without completing each applicable item.

### Required for every SQL change

- [ ] **DbUp migration:** new `ArchLucid.Persistence/Migrations/0NN_*.sql` for SQL Server incremental change. Use `IF NOT EXISTS` / `IF OBJECT_ID IS NULL` patterns; migrations must be idempotent.
- [ ] **`ArchLucid.Persistence/Scripts/ArchLucid.sql`:** same objects, columns, and indexes as the migration — keeps greenfield provisioning in parity.
- [ ] **Migration catalog:** update §4.2 of this file with the new migration number and description.

### Required when schema changes affect data access

- [ ] **C# repositories / contracts:** update Dapper queries, `IRepository` methods, and any affected DTOs in `ArchLucid.Contracts` or `ArchLucid.Persistence.Data.*`.
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
| **“Schema script not found”** (Persistence) | `ArchLucid.Persistence` build output contains **`Scripts/ArchLucid.sql`**; verify `ArchLucid.Persistence.csproj` includes **`Scripts\ArchLucid.sql`** with **`CopyToOutputDirectory`**. |
| **Missing tables on SQL Server** | DbUp errors on startup (API logs); run migrations manually in order if needed. Persistence bootstrap only runs when SQL storage is registered. |
| **Duplicate or wrong migration order** | Embedded resource names must sort correctly (`010` before `011`). |

---

## 7. Security & operations

- Scripts contain **no secrets**. Connection strings live in configuration (User Secrets, env, Key Vault, etc.).
- **Production:** Prefer controlled migration runs (CI/CD or DBA) over ad-hoc execution of `ArchLucid.sql`, unless you intentionally use it for greenfield provisioning.

---

## 8. Versioning

- Update the **migration catalog** (§4.2) when adding `0NN_*.sql`.
- This document’s last migration line should stay in sync with the highest `ArchLucid.Persistence/Migrations/0NN_*.sql` file.

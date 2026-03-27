# SQL scripts — reference & operations

This document is the **canonical guide** to every SQL artifact in ArchiForge: what each file does, how it is executed, how the pieces relate, and how to change schema safely.

**Related:** [DATA_MODEL.md](DATA_MODEL.md) (tables and domains at a glance) · [README.md](../README.md) (database setup & DbUp) · [TEST_STRUCTURE.md](TEST_STRUCTURE.md) (`DatabaseMigrationScriptTests`)

---

## 1. Why there are three SQL pathways

ArchiForge does **not** use a single script for all environments. Three mechanisms coexist by design:

| Pathway | When it runs | Engine | Script source | Purpose |
|--------|----------------|--------|----------------|--------|
| **DbUp migrations** | API startup (SQL Server connection only) | SQL Server | `ArchiForge.Data/Migrations/*.sql` embedded in **ArchiForge.Data** | **Authoritative upgrades** for deployed databases; ordered, transactional, logged. |
| **Persistence schema bootstrap** | First use of Dapper SQL persistence (same DB as API typically) | SQL Server | `ArchiForge.Data/SQL/ArchiForge.sql` copied to **ArchiForge.Persistence** output as `Scripts/ArchiForge.sql` | Ensures **authority + decisioning** objects exist; runs **full** consolidated DDL in `GO` batches (see §4). |
| **SQLite bootstrap** | First connection per distinct SQLite connection string (e.g. in-memory tests) | SQLite | `ArchiForge.Data/SQL/ArchiForge.Sqlite.sql` embedded in **ArchiForge.Data** | Test / local schema; **DbUp is skipped** when the connection string is SQLite. |

**Important:**

- **Production / dev SQL Server:** Schema evolution is driven by **migrations**. The consolidated `ArchiForge.sql` is still run by Persistence bootstrap and should stay **aligned** with migrations + authority DDL, but **do not** rely on it alone for long-lived DB upgrades.
- **Integration tests:** Often use **in-memory SQLite** → DbUp does not run; only `ArchiForge.Sqlite.sql` defines the shape of the test DB (for code paths using `SqliteConnectionFactory`).

---

## 2. File locations (quick inventory)

| Path | Role |
|------|------|
| `ArchiForge.Data/SQL/ArchiForge.sql` | SQL Server **consolidated** schema (API + authority + decisioning). Source of truth for **greenfield** / manual runs / Persistence bootstrap copy. |
| `ArchiForge.Data/SQL/ArchiForge.Sqlite.sql` | SQLite **consolidated** schema for tests/reference. |
| `ArchiForge.Data/SQL/README.md` | Short pointer to this doc for repo browsers. |
| `ArchiForge.Data/Migrations/001_*.sql` … `017_*.sql` | Incremental **DbUp** scripts (SQL Server); **`017_GovernanceWorkflow.sql`** adds governance approval / promotion / environment activation tables. |
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
- **Also** update **`ArchiForge.Sqlite.sql`** (if tests need the same concept on SQLite).
- **Always** add or extend a **DbUp migration** for SQL Server deployments that already exist.

---

## 4. `ArchiForge.Sqlite.sql` (SQLite consolidated)

### 4.1 Purpose

- Defines a **full** logical schema for **SQLite** used when `IDbConnectionFactory` is **`SqliteConnectionFactory`** (typically in-memory integration tests).
- **Not** executed by DbUp.

### 4.2 How it is executed

- Embedded as **`ArchiForge.Data.SQL.ArchiForge.Sqlite.sql`** (`ArchiForge.Data.csproj` sets `LogicalName`).
- On **first** connection for a given connection string, `SqliteConnectionFactory` loads the resource and runs the entire script with **`ExecuteNonQuery`** (multi-statement script).

### 4.3 Syntax & limitations vs SQL Server

- **`CREATE TABLE IF NOT EXISTS`** / **`CREATE INDEX IF NOT EXISTS`** — idempotent style appropriate for SQLite.
- **No `dbo.` schema**; table names are unqualified.
- **GUIDs / datetimes** stored as **`TEXT`** (ISO-8601 for times).
- **SQLite cannot** embed arbitrary named non-unique indexes inside `CREATE TABLE` like SQL Server; each table’s **`CREATE INDEX IF NOT EXISTS`** statements appear **immediately after** that table’s `CREATE TABLE` in the file (documented in the file header).
- **Foreign keys:** Application should enable `PRAGMA foreign_keys = ON` per connection where enforcement is required (see file header comment).

### 4.4 Alignment

- Comment in file references DbUp range **001–016** and parity with the authority/decisioning portion of `ArchiForge.sql`.
- When adding SQL Server objects, **evaluate** whether SQLite tests need matching tables/indexes (many Dapper repositories are SQL Server–only in production).

---

## 5. DbUp migrations (`ArchiForge.Data/Migrations/`)

### 5.1 Mechanics

- Scripts are **`EmbeddedResource`** in **ArchiForge.Data** (`Migrations\*.sql` plus the standalone SQLite reference script under `SQL\`, which is **not** under `.Migrations.`).
- **DbUp** (`DatabaseMigrator.Run`) selects only embedded names that contain **`.Migrations.`** and end with **`.sql`**, so only `ArchiForge.Data/Migrations/*.sql` run — not ad-hoc embedded SQL.
- Ordering is **lexicographic by embedded resource name** — keep the filename prefix pattern **`001_`**, **`002_`**, … (see `DatabaseMigrationScriptTests`).
- **`DatabaseMigrator.IsSqliteConnection`** skips DbUp for SQLite test strings and typical file-backed `*.db` / `*.sqlite` strings without `Server=` / `Initial Catalog=` (SQL Server uses DbUp).

### 5.2 Catalog (one file = one version step)

| File | Summary |
|------|--------|
| **001_InitialSchema.sql** | Base API schema: requests, runs, agents, manifests, evidence, decision traces, export records, core indexes/FKs. |
| **002_ComparisonRecords.sql** | `ComparisonRecords` table for stored comparisons / replay payloads. |
| **003_ComparisonRecords_LabelAndTags.sql** | Adds `Label`, `Tags` on `ComparisonRecords`. |
| **004_DecisionNodes_And_Evaluations.sql** | `DecisionNodes`, `AgentEvaluations`. |
| **005_ArchitectureRuns_ContextSnapshotId.sql** | `ArchitectureRuns.ContextSnapshotId`. |
| **006_ArchitectureRuns_GraphSnapshotId.sql** | `ArchitectureRuns.GraphSnapshotId`. |
| **007_ArchitectureRuns_ArtifactBundleId.sql** | `ArchitectureRuns.ArtifactBundleId`. |
| **008_RecommendationRecords.sql** | Recommendation workflow storage (scoped by tenant/workspace/project). |
| **009_RecommendationLearningProfiles.sql** | Learning profiles for recommendations. |
| **010_AdvisoryScheduling.sql** | Advisory scan schedules and executions. |
| **011_DigestDelivery.sql** | Digests, subscriptions, delivery attempts. |
| **012_Alerts.sql** | Alert rules and alert records. |
| **013_AlertRouting.sql** | Routing subscriptions and delivery attempts. |
| **014_CompositeAlertRules.sql** | Composite alert rules and conditions. |
| **015_PolicyPacks.sql** | Policy packs, versions, assignments (baseline assignment shape). |
| **016_PolicyPackAssignments_Scope.sql** | Adds **`ScopeLevel`**, **`IsPinned`** on `PolicyPackAssignments` + index (aligns with Dapper / consolidated `CREATE TABLE`). |

**Note:** Authority-chain tables (`dbo.Runs`, snapshots, etc.) are **not** introduced by migrations 001–007; they come from **`ArchiForge.sql`** via Persistence bootstrap (and are mirrored in consolidated scripts).

### 5.3 Adding migration `017_…`

1. Create `ArchiForge.Data/Migrations/017_YourChange.sql` (idempotent `IF` / `IF NOT EXISTS` patterns preferred).
2. Update **`ArchiForge.sql`** (and **`ArchiForge.Sqlite.sql`** if tests need it).
3. Run tests; optionally extend **`DatabaseMigrationScriptTests`** if you add new ordering rules.

---

## 6. Change checklist (schema work)

Treat this checklist as a **definition of done** for every schema change. Do not merge without completing each applicable item.

### Required for every SQL change

- [ ] **DbUp migration:** new `ArchiForge.Data/Migrations/0NN_*.sql` for SQL Server incremental change. Use `IF NOT EXISTS` / `IF OBJECT_ID IS NULL` patterns; migrations must be idempotent.
- [ ] **`ArchiForge.Data/SQL/ArchiForge.sql`:** same objects, columns, and indexes as the migration — keeps greenfield provisioning in parity.
- [ ] **`ArchiForge.Data/SQL/ArchiForge.Sqlite.sql`:** add equivalent SQLite DDL if any integration test must see the new schema. SQLite syntax differs (no `ALTER COLUMN`, no `NVARCHAR`).
- [ ] **Migration catalog:** update §5.2 of this file with the new migration number and description.

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

## 7. Troubleshooting

| Symptom | Things to check |
|---------|------------------|
| **“Schema script not found”** (Persistence) | `ArchiForge.Persistence` build output contains **`Scripts/ArchiForge.sql`**; verify `ArchiForge.Persistence.csproj` link to `..\ArchiForge.Data\SQL\ArchiForge.sql`. |
| **Missing tables on SQL Server** | DbUp errors on startup (API logs); run migrations manually in order if needed. Persistence bootstrap only runs when SQL storage is registered. |
| **SQLite tests fail with “no such table”** | Embedded resource name / `ArchiForge.Sqlite.sql` content; ensure new tables added to SQLite script. |
| **Duplicate or wrong migration order** | Embedded resource names must sort correctly (`010` before `011`). |

---

## 8. Security & operations

- Scripts contain **no secrets**. Connection strings live in configuration (User Secrets, env, Key Vault, etc.).
- **Production:** Prefer controlled migration runs (CI/CD or DBA) over ad-hoc execution of `ArchiForge.sql`, unless you intentionally use it for greenfield provisioning.

---

## 9. Versioning

- Update the **migration catalog** (§5.2) and any **“Aligns with migrations …”** comments in `ArchiForge.Sqlite.sql` when adding `017+`.
- This document’s last migration line should stay in sync with the highest `ArchiForge.Data/Migrations/0NN_*.sql` file.

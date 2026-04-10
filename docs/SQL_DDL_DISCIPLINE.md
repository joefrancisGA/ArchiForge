# SQL DDL discipline (single source of truth)

## Objective

Keep **SQL Server** schema discoverable and provisionable from one consolidated script while still supporting **ordered, transactional upgrades** for long-lived databases.

## Assumptions

- Production and shared dev databases evolve via **DbUp** embedded scripts under **`ArchLucid.Persistence/Migrations/`** (`DatabaseMigrator`).
- Greenfield SQL Server installs, Persistence **bootstrap**, and human operators may run **`ArchLucid.Persistence/Scripts/ArchLucid.sql`** (batched by `GO`, idempotent `IF OBJECT_ID` / `IF NOT EXISTS` patterns).
- **Integration tests** use **SQL Server** (per-test databases); **DbUp** runs on test host startup (see **`ArchLucid.Api.Tests`** / **`TEST_STRUCTURE.md`**).

## Constraints

- **One consolidated SQL Server DDL file per logical database:** **`ArchLucid.sql`** (not split per feature area for the canonical reference).
- **Additive migrations** use new **`NNN_Description.sql`** files; DbUp order is **lexicographic on the embedded resource name**—keep **`NNN_`** prefixes zero-padded.

## Architecture overview

| Artifact | Role |
|----------|------|
| **`Migrations/*.sql`** | Brownfield deltas applied in order by DbUp. |
| **`ArchLucid.sql`** | Full reference + bootstrap parity (includes objects that also appeared first in migrations, e.g. outbox **019**, indexes **020**, idempotency **021**). |

## Component breakdown

- **`ArchLucid.Persistence.Data.*`** — embeds migrations, ships SQL files, exposes **`DatabaseMigrator`**.
- **`ArchLucid.Persistence`** — MSBuild **link** copies **`ArchLucid.sql`** to output **`Scripts/ArchLucid.sql`** for **`SqlSchemaBootstrapper`** (see **`ArchLucidStorageServiceCollectionExtensions`**).

## Data flow

1. **New column/table/index:** add **`ArchLucid.Persistence/Migrations/NNN_....sql`** (idempotent `IF NOT EXISTS` where possible).
2. **Mirror** the same logical object into **`ArchLucid.sql`**.
3. Run **`DatabaseMigrator`** in CI or locally against SQL Server test instances (see **`ArchLucid.Persistence.Tests`** / **`TEST_STRUCTURE.md`**).

## Security model

- DDL files contain **no secrets**; connection strings stay in configuration / Key Vault (see **`docs/CONFIGURATION_KEY_VAULT.md`**).
- **SMB / port 445:** storage access patterns remain private-endpoint aligned per workspace rules—not DDL-specific.

## Operational considerations

- **Drift detection:** Compare migration list to sections appended in **`ArchLucid.sql`** when reviewing PRs (this document’s inventory below).
- **Rollback:** DbUp does not auto-generate down scripts; use **`docs/runbooks/MIGRATION_ROLLBACK.md`** and **`NEXT_REFACTORINGS.md`** item **249**.

## Migration inventory (SQL Server, embedded)

| Script | Purpose |
|--------|---------|
| `001_InitialSchema.sql` – `029_...` | API + authority + decisioning deltas (see `Migrations/README.md` and **`docs/SQL_SCRIPTS.md`** §4.2). **`028_ArchivalSoftFlags.sql`**: nullable **`ArchivedUtc`** on **`Runs`**, **`ArchitectureDigests`**, **`ConversationThreads`** (skipped when table absent). **`029_PolicyPackAssignments_ArchivedUtc.sql`**: **`ArchivedUtc`** on **`PolicyPackAssignments`**. |

**Consolidated script parity:** **`ArchLucid.sql`** includes later migration semantics in trailing sections so bootstrap matches migrated databases.

## Cost / scalability / reliability

- **Cost:** Index **020** trades small storage for fewer scans on **`dbo.Runs`** list-by-project queries.
- **Scalability:** Idempotency table is keyed by scope + 32-byte hash; volume is bounded by distinct client keys.
- **Reliability:** Idempotency replay avoids duplicate **`ArchitectureRuns`** for retries; cross-store atomicity with authority **`dbo.Runs`** is **best-effort** (documented in **`API_CONTRACTS.md`**).

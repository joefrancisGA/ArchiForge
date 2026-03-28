# SQL DDL discipline (single source of truth)

## Objective

Keep **SQL Server** schema discoverable and provisionable from one consolidated script while still supporting **ordered, transactional upgrades** for long-lived databases.

## Assumptions

- Production and shared dev databases evolve via **DbUp** embedded scripts under **`ArchiForge.Data/Migrations/`** (`DatabaseMigrator`).
- Greenfield SQL Server installs, Persistence **bootstrap**, and human operators may run **`ArchiForge.Data/SQL/ArchiForge.sql`** (batched by `GO`, idempotent `IF OBJECT_ID` / `IF NOT EXISTS` patterns).
- **Integration tests** use **SQL Server** (per-test databases); **DbUp** runs on test host startup (see **`ArchiForge.Api.Tests`** / **`TEST_STRUCTURE.md`**).

## Constraints

- **One consolidated SQL Server DDL file per logical database:** **`ArchiForge.sql`** (not split per feature area for the canonical reference).
- **Additive migrations** use new **`NNN_Description.sql`** files; DbUp order is **lexicographic on the embedded resource name**—keep **`NNN_`** prefixes zero-padded.

## Architecture overview

| Artifact | Role |
|----------|------|
| **`Migrations/*.sql`** | Brownfield deltas applied in order by DbUp. |
| **`ArchiForge.sql`** | Full reference + bootstrap parity (includes objects that also appeared first in migrations, e.g. outbox **019**, indexes **020**, idempotency **021**). |

## Component breakdown

- **`ArchiForge.Data`** — embeds migrations, ships SQL files, exposes **`DatabaseMigrator`**.
- **`ArchiForge.Persistence`** — MSBuild **link** copies **`ArchiForge.sql`** to output **`Scripts/ArchiForge.sql`** for **`SqlSchemaBootstrapper`** (see **`ArchiForgeStorageServiceCollectionExtensions`**).

## Data flow

1. **New column/table/index:** add **`ArchiForge.Data/Migrations/NNN_....sql`** (idempotent `IF NOT EXISTS` where possible).
2. **Mirror** the same logical object into **`ArchiForge.sql`**.
3. Run **`DatabaseMigrator`** in CI or locally against SQL Server test instances (see **`ArchiForge.Persistence.Tests`** / **`TEST_STRUCTURE.md`**).

## Security model

- DDL files contain **no secrets**; connection strings stay in configuration / Key Vault (see **`docs/CONFIGURATION_KEY_VAULT.md`**).
- **SMB / port 445:** storage access patterns remain private-endpoint aligned per workspace rules—not DDL-specific.

## Operational considerations

- **Drift detection:** Compare migration list to sections appended in **`ArchiForge.sql`** when reviewing PRs (this document’s inventory below).
- **Rollback:** DbUp does not auto-generate down scripts; document manual rollback in runbooks if needed (**`NEXT_REFACTORINGS.md`** §249).

## Migration inventory (SQL Server, embedded)

| Script | Purpose |
|--------|---------|
| `001_InitialSchema.sql` – `022_...` | API + authority + decisioning deltas (see `Migrations/README.md` and **`docs/SQL_SCRIPTS.md`** §4.2). |

**Consolidated script parity:** **`ArchiForge.sql`** includes later migration semantics in trailing sections so bootstrap matches migrated databases.

## Cost / scalability / reliability

- **Cost:** Index **020** trades small storage for fewer scans on **`dbo.Runs`** list-by-project queries.
- **Scalability:** Idempotency table is keyed by scope + 32-byte hash; volume is bounded by distinct client keys.
- **Reliability:** Idempotency replay avoids duplicate **`ArchitectureRuns`** for retries; cross-store atomicity with authority **`dbo.Runs`** is **best-effort** (documented in **`API_CONTRACTS.md`**).

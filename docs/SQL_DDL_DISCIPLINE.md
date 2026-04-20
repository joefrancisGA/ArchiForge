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
4. **API / Worker:** **`ArchLucidPersistenceStartup`** applies **DbUp** before **`SqlSchemaBootstrapper`** so greenfield catalogs get a populated **`SchemaVersions`** journal before idempotent **`ArchLucid.sql`** (see **`SQL_SCRIPTS.md`** §1).

## Security model

- DDL files contain **no secrets**; connection strings stay in configuration / Key Vault (see **`docs/CONFIGURATION_KEY_VAULT.md`**).
- **SMB / port 445:** storage access patterns remain private-endpoint aligned per workspace rules—not DDL-specific.

## Operational considerations

- **Drift detection:** Compare migration list to sections appended in **`ArchLucid.sql`** when reviewing PRs (this document’s inventory below).
- **Azure automatic tuning:** When **`enable_sql_automatic_tuning`** is on in **`infra/terraform-sql-failover/`**, **`createIndex`** / **`dropIndex`** can change live catalogs without a repo migration. Treat portal **recommendations** and **`sys.indexes`** diffs as inputs: promote kept indexes into **`Migrations/`** + **`ArchLucid.sql`**, or set tuning options to **`Off`** / **`Default`** per server if you require strict DDL-only control.
- **Rollback:** DbUp does not auto-generate down scripts; use **`docs/runbooks/MIGRATION_ROLLBACK.md`** and **`NEXT_REFACTORINGS.md`** item **249**.
- **Dashboard-grade lists on hot-write tables:** Prefer **covering nonclustered indexes** (key columns for filter + sort, **`INCLUDE`** for projected columns) so list plans avoid **key lookups** into the clustered index while writers hold locks on those pages. For operator UI / picker queries that already tolerate **read-replica staleness**, **`WITH (NOLOCK)`** on the **`FROM`** clause avoids **shared-lock** waits behind concurrent inserts/updates. Do **not** apply **`NOLOCK`** to transactional reads (**`GetByIdAsync`**, commit pipelines, optimistic concurrency).

## Migration inventory (SQL Server, embedded)

| Script | Purpose |
|--------|---------|
| `001_InitialSchema.sql` – `029_...` | API + authority + decisioning deltas (see `Migrations/README.md` and **`docs/SQL_SCRIPTS.md`** §4.2). **`028_ArchivalSoftFlags.sql`**: nullable **`ArchivedUtc`** on **`Runs`**, **`ArchitectureDigests`**, **`ConversationThreads`** (skipped when table absent). **`029_PolicyPackAssignments_ArchivedUtc.sql`**: **`ArchivedUtc`** on **`PolicyPackAssignments`**. |
| `060_QueryCoverage_Indexes.sql` | Supplemental indexes (audit event type, conversation threads, governance, recommendations, **`IX_Runs_ArchiveRetention`**, policy packs, …). |
| `061_RunsScopeCreatedUtcCoveringIndex.sql` | Drop/recreate **`IX_Runs_Scope_CreatedUtc`** with **`INCLUDE`** list columns for **`dbo.Runs`** dashboard lists (avoids PK key lookups under concurrent writes). |
| `084_PageCompression_AuditEvents_AgentExecutionTraces.sql` | **`ALTER INDEX ALL … REBUILD WITH (DATA_COMPRESSION = PAGE)`** on **`dbo.AuditEvents`** and **`dbo.AgentExecutionTraces`** when any rowstore partition is not already PAGE (idempotent). Rollback: **`Rollback/R084_*.sql`**. |
| `085_PageCompression_Runs.sql` | Same **PAGE** rebuild for **`dbo.Runs`** (clustered + all NC indexes). Highest read/cache impact; plan apply window. Rollback: **`Rollback/R085_*.sql`**. |
| `087_PageCompression_DecisionTraces.sql` | Same **PAGE** rebuild for **`dbo.DecisionTraces`** (clustered + all NC indexes). Append-mostly trace stream; plan apply window. Rollback: **`Rollback/R087_*.sql`**. |
| `088_PageCompression_DecisioningTraces.sql` | Same **PAGE** rebuild for **`dbo.DecisioningTraces`** (authority decision trace + JSON). Rollback: **`Rollback/R088_*.sql`**. |
| `089_PageCompression_UsageEvents.sql` | Same **PAGE** rebuild for **`dbo.UsageEvents`** (metering). Rollback: **`Rollback/R089_*.sql`**. |
| `090_PageCompression_AlertRecords_AlertDeliveryAttempts.sql` | Same **PAGE** rebuild for **`dbo.AlertRecords`** and **`dbo.AlertDeliveryAttempts`**. Rollback: **`Rollback/R090_*.sql`**. |

**Consolidated script parity:** **`ArchLucid.sql`** includes later migration semantics in trailing sections so bootstrap matches migrated databases.

## Cost / scalability / reliability

- **Cost:** Index **020** trades small storage for fewer scans on **`dbo.Runs`** list-by-project queries.
- **Cost (rowstore compression):** Migrations **084**–**090** extend **PAGE** compression across audit, traces, runs, decision traces, metering (**`UsageEvents`**), and alert history; estimate with **`sp_estimate_data_compression_savings`** before large-catalog applies; confirm SKU supports compression. **Outbox** tables (**`IntegrationEventOutbox`**, **`RetrievalIndexingOutbox`**, **`AuthorityPipelineWorkOutbox`**) are intentionally excluded until workload-specific analysis — see **`docs/SQL_OUTBOX_TABLES_COMPRESSION.md`**.
- **Scalability:** Idempotency table is keyed by scope + 32-byte hash; volume is bounded by distinct client keys.
- **Reliability:** Idempotency replay avoids duplicate run headers for retries; authority **`dbo.Runs`** is the durable correlation point (documented in **`API_CONTRACTS.md`**).

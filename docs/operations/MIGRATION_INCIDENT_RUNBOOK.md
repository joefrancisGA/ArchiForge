# Migration incident runbook (DbUp)

## Objective

Give operators a concise playbook when embedded SQL migrations (`ArchLucid.Persistence/Migrations/*.sql`) fail in production or staging, and clarify how journal drift, rollback scripts, and re-runs interact.

## Assumptions

- Host uses [`DatabaseMigrator`](ArchLucid.Persistence/Data/Infrastructure/DatabaseMigrator.cs) with **per-script transactions** (`WithTransactionPerScript()`).
- Journal table is **`dbo.SchemaVersions`** (DbUp default).
- Forward migrations are sequential and lexicographically ordered by embedded resource name (`NNN_Name.sql`).

## When a migration fails mid-run

1. **Expectation**: For a single script file, SQL Server runs batches (`GO`-separated) inside **one transaction**. If any batch fails, that script’s changes roll back and DbUp **does not** insert a row into `dbo.SchemaVersions` for that script.
2. **Remediation**: Fix the underlying cause (disk space, permission, bad data violating a new constraint, lock timeout), then **re-run the application deploy / migration step**. DbUp retries scripts that are not recorded as applied.
3. **Idempotency**: Migrations 051+ are written with `IF NOT EXISTS` / `COL_LENGTH … IS NULL` guards so a successful retry after a rolled-back failure does not rely on partial side effects.

## Identifying state from `dbo.SchemaVersions`

Query applied scripts:

```sql
SELECT ScriptName, Applied
FROM dbo.SchemaVersions
ORDER BY Applied;
```

The **`ScriptName`** value is the full embedded resource name (for example containing `124_FindingRecords_FilterIndexes`). Compare ordering with [`DatabaseMigrator.GetOrderedMigrationResourceNames()`](ArchLucid.Persistence/Data/Infrastructure/DatabaseMigrator.cs) in source when diagnosing ordering bugs.

## Journal drift (empty or missing rows while objects exist)

If `dbo.SchemaVersions` was truncated, restored without migration history, or otherwise inconsistent with physical objects:

1. [`GreenfieldBaselineMigrationRunner.TryApplyBaselineAndStampThrough050`](ArchLucid.Persistence/Data/Infrastructure/GreenfieldBaselineMigrationRunner.cs) runs **before** DbUp and can stamp `001–050` or replay subsets when audit/core tables are missing (see XML remarks on that type).
2. A subsequent [`DatabaseMigrator.Run`](ArchLucid.Persistence/Data/Infrastructure/DatabaseMigrator.cs) applies `051+` scripts; guarded DDL is safe when objects already exist.

Integration coverage: `JournalDriftBaselineRepairSqlIntegrationTests` in `ArchLucid.Persistence.Tests`.

## Rollback scripts (`Migrations/Rollback/`)

- These files are **manual** rollback aids. **DbUp does not execute them automatically.**
- Use only with backups and change windows; many rollbacks drop columns or indexes and may be destructive.
- Presence is validated for representative scripts in `MigrationRollbackScriptsSqlIntegrationTests`.

## Failure injection / transaction semantics

Automated coverage uses DbUp directly with a two-batch script that `RAISERROR`s in the second batch (`DbUpPerScriptTransactionRollbackSqlIntegrationTests`) to assert the first batch’s DDL does not persist after failure.

## Re-applying a single migration after journal repair

If a row for one script is removed from `dbo.SchemaVersions` but the physical migration already ran, the next `DatabaseMigrator.Run` may attempt that script again. Forward scripts must remain idempotent; replay scenarios for `127` and `129` are covered in `MigrationReplayIdempotencySqlIntegrationTests`.

## Governance baseline (`038_GovernanceWorkflow`)

The baseline runner treats [`038_GovernanceWorkflow.sql`](ArchLucid.Persistence/Migrations/038_GovernanceWorkflow.sql) as **non-idempotent** when replaying incremental files; if governance tables already exist, that script file may be skipped during repair replay so duplicate `CREATE TABLE` does not run (see remarks on `GreenfieldBaselineMigrationRunner`). Do not hand-edit historical `001–028` migration files per repository policy.

## Non-transactional operations

- **`ALTER DATABASE`** (for example read-committed snapshot options) is applied outside DbUp’s transaction where required — see `TryEnableReadCommittedSnapshotIfNeeded` in [`DatabaseMigrator`](ArchLucid.Persistence/Data/Infrastructure/DatabaseMigrator.cs).
- Very large backfills may hit **timeouts**; failure rolls back the script transaction but may require increasing timeout or running during a maintenance window — treat as operational, not schema corruption.

## Operational checklist

1. Capture SQL error text and failing script name from deploy logs.
2. Inspect `dbo.SchemaVersions` ordering vs. repo migration list.
3. Prefer **fix root cause + re-run** over manual rollback unless product asks for explicit downgrades.
4. If using `Migrations/Rollback/*.sql`, run in a controlled session, verify constraints and app compatibility afterward.

## Escalation

Route unresolved migration failures to the owning backend team with: failing script identifier, full SqlException text, row count from `dbo.SchemaVersions`, and whether journal drift or restore operations occurred recently.

# `ArchLucid.Persistence/Scripts`

| File | Role |
|------|------|
| **`ArchLucid.sql`** | SQL Server **consolidated** schema (API + authority + decisioning). Copied to build output for **`SqlSchemaBootstrapper`**. |
| **`Maintenance/QueryStore-ArchLucid-hotpaths.sql`** | Optional **read-only** Query Store rankings (duration / logical reads / ArchLucid table slice). Run against production-like workload DB after telemetry confirms slow paths. |

**Full documentation:** [../../docs/SQL_SCRIPTS.md](../../docs/library/SQL_SCRIPTS.md) (execution pathways, migration catalog, change checklist, troubleshooting).

DbUp incremental scripts live in **`../Migrations/`**, not in this folder.

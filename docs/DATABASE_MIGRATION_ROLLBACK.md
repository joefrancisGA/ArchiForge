# Database migration rollback (manual)

## Objective

Provide **operator-owned** reverse scripts for recent forward-only DbUp migrations when a deploy must be backed out before the next forward fix ships.

## Assumptions

- Rollbacks run against the **same** SQL Server catalog the API uses (`ConnectionStrings:ArchLucid`).
- Operators have **maintainer** privileges and a **verified backup** (point-in-time or full) before destructive steps.

## Constraints

- Historical migrations **001–028** are **frozen** (do not edit). New DDL continues via **`ArchLucid.sql`** + new numbered files under `ArchLucid.Persistence/Migrations/`.
- DbUp does **not** execute rollback files; they live under **`sql/rollbacks/`** for explicit, reviewed use.

## Architecture overview

| Node | Role |
|------|------|
| Operator / DBA | Chooses scope, runs `sql/rollbacks/*.sql` in order |
| `sql/rollbacks/` | Idempotent T-SQL reversing **061–065** |
| `ArchLucid.Persistence/Migrations/` | Canonical forward history |

## Component breakdown

- **`sql/rollbacks/README.md`** — index of rollback ↔ forward mapping.
- **`sql/rollbacks/061_rollback.sql` … 065_rollback.sql`** — drop index/column/table created by the matching forward migration.

## Data flow

1. Stop API **and** worker instances that write to the database (avoid schema drift during rollback).
2. Take a backup (or confirm PITR window).
3. Apply rollbacks **newest first** (065 → 061) only for migrations that were applied in the bad deploy.
4. Redeploy the **previous** application version that matches the schema **without** those forward migrations, or forward-fix with a new migration after analysis.

## Security model

- Run scripts only over **private** connectivity to SQL (no public SQL endpoints).
- Treat rollback output as sensitive if it touches tables that may contain tenant-scoped content.

## Operational considerations

- **Reliability:** Test rollbacks in a **clone** of production before production execution.
- **Cost:** Rollbacks that drop indexes can increase read I/O until indexes are recreated.
- **Scalability:** Dropping covering indexes may slow list endpoints under load — plan a maintenance window.

## Procedure (checklist)

1. Identify the **last good** migration version (DbUp journal / deployment log).
2. Open **`sql/rollbacks/README.md`** and select only the rollback files that reverse migrations **after** that version.
3. Execute each selected script once; verify `sys.columns` / `sys.indexes` / `sys.tables` match expectations.
4. Run smoke: **`/health/ready`**, create run in simulator, commit path if applicable.

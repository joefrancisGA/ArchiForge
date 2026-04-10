# SQL migration rollback (DbUp / ArchLucid.Persistence)

## Objective

Describe how operators recover when a **forward-only** DbUp migration is wrong, partially applied, or must be undone in an emergency. This complements **`docs/SQL_DDL_DISCIPLINE.md`** (item **249**).

## Assumptions

- Production schema evolves via **embedded scripts** under **`ArchLucid.Persistence/Migrations/`**, applied in lexicographic order by **`DatabaseMigrator`**.
- **`ArchLucid.sql`** is the consolidated reference for greenfield bootstrap; brownfield servers may have run the same logical change through a numbered migration first.
- **DbUp does not ship “down” scripts**; rollback is a **manual** DBA operation with a **restore-first** bias.

## Constraints

- Prefer **point-in-time restore** (PITR) or database **snapshot revert** over hand-written `ALTER TABLE DROP` when data loss or referential integrity is unclear.
- Any manual DDL must respect **FK order** (drop children before parents when removing columns/tables).
- Never expose **SMB (port 445)** for backups; use private endpoints and controlled networks for backup storage.

## Architecture overview

| Component | Role |
|-----------|------|
| **`Migrations/NNN_*.sql`** | Ordered, idempotent-forward deltas. |
| **`ArchLucid.sql`** | Full bootstrap parity (includes post-migration sections). |
| **Backup / PITR** | Primary rollback mechanism for production. |

## Data flow (rollback decision)

1. **Detect failure** — migration job fails mid-script, app health checks fail, or incorrect DDL shipped.
2. **Stop traffic** — scale App Service to zero or disable the API until the database state is known-good.
3. **Choose path:**
   - **A. Restore** — restore DB to pre-migration backup / PITR (recommended when migration already committed destructive changes).
   - **B. Forward fix** — ship a **new** migration that repairs schema/data (recommended when restore is too costly and drift is understood).
   - **C. Manual reverse DDL** — only for **additive** migrations (e.g. new nullable column) where dropping the column is provably safe; document the exact `ALTER` in the incident record.

## Security model

- Rollback operations use **least-privilege** DBA accounts; application runtime accounts must not own schema changes.
- Audit who ran rollback DDL and link to **change / incident** ticket.

## Operational considerations

- **After restore:** Re-run DbUp from a clean baseline only if the **`SchemaVersions`** (or DbUp journal) table matches the restored DB; mismatches require **manual journal alignment** (expert-only).
- **028 archival columns example:** `ArchivedUtc` on **`dbo.Runs`**, **`dbo.ArchitectureDigests`**, **`dbo.ConversationThreads`** is nullable and additive. Reversing it is `ALTER TABLE ... DROP COLUMN ArchivedUtc` **only** if no app version depends on the column (coordinate blue/green).
- **Test environments:** Prefer **throwaway database** restore or recreate from **`ArchLucid.sql`** + migrations over editing production.

## Cost / scalability / reliability

- **Cost:** PITR and long retention increase storage; balance against RPO/RTO in **`docs/runbooks/SLO_PROMETHEUS_GRAFANA.md`** themes.
- **Scalability:** Large tables: `DROP COLUMN` can be size-sensitive; plan maintenance windows.
- **Reliability:** Document **RPO** (how much data you accept to lose) before choosing restore vs forward fix.

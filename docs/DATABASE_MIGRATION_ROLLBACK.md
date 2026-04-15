# Database migration rollback scripts

Forward schema changes ship via DbUp under `ArchLucid.Persistence/Migrations/`. **DbUp does not run rollback scripts.**

**Rollback scripts** live in `ArchLucid.Persistence/Migrations/Rollback/` as `RNNN_Description.sql`, paired with the forward script `NNN_Description.sql`. They are **operator-only**: run manually with `sqlcmd` or SSMS during a controlled recovery when a deployment must be reversed.

## Guard

CI enforces that the **five most recent** numbered forward migrations each have at least one matching `Rollback/RNNN_*.sql` file (`scripts/ci/assert_rollback_scripts_exist.py`).

## Risk

Rollback scripts that `DROP TABLE` or `DROP COLUMN` **destroy data**. Use only with backups and an approved runbook.

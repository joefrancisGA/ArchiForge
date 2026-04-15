# Manual SQL rollbacks (last N forward migrations)

These scripts **reverse** the corresponding numbered migrations under `ArchLucid.Persistence/Migrations/`.
They are **not** run by DbUp automatically.

**Warnings**

- Executing a rollback may **delete data** created after the forward migration ran.
- Run in a **maintenance window** with backups.
- Apply rollbacks in **reverse numeric order** (newest first) only when undoing a bad deploy.

| Rollback file | Reverses migration |
|---------------|-------------------|
| `065_rollback.sql` | `065_AgentExecutionTraces_InlineFallbackFailed_Index.sql` |
| `064_rollback.sql` | `064_AgentExecutionTrace_InlineFallbackFailed.sql` |
| `063_rollback.sql` | `063_AgentOutputEvaluationResults.sql` |
| `062_rollback.sql` | `062_AgentExecutionTrace_InlineFullPrompts.sql` |
| `061_rollback.sql` | `061_RunsScopeCreatedUtcCoveringIndex.sql` |

See **[docs/DATABASE_MIGRATION_ROLLBACK.md](../docs/DATABASE_MIGRATION_ROLLBACK.md)** for procedure and risk notes.

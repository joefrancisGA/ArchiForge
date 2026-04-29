# Manifest finalization transaction (authority commit)

## Objective

Persist the **decision trace** and **golden manifest**, then atomically **finalize the run** (`dbo.Runs`), append a **durable audit** row, and enqueue an **integration outbox** message so downstream consumers see a consistent review trail.

## Flow (SQL path)

1. `BEGIN` transaction (via `IArchLucidUnitOfWork`).
2. `SELECT … FROM dbo.Runs WITH (UPDLOCK, ROWLOCK)` — serializes concurrent finalizers for the same run; supports idempotent early return when ` LegacyRunStatus = 'Committed'`.
3. `INSERT dbo.DecisioningTraces` + `INSERT dbo.GoldenManifests` (+ relational slices via `IGoldenManifestRepository`).
4. `EXEC dbo.sp_FinalizeManifest` — `UPDATE dbo.Runs` with optimistic `RowVersionStamp` match; `INSERT dbo.AuditEvents` (`ManifestFinalized`); `INSERT dbo.IntegrationEventOutbox` (`com.archlucid.manifest.finalized.v1`).
5. `COMMIT`.

## Database artifacts

- **Filtered unique index** `UQ_GoldenManifests_RunId_Active` — at most one non-archived golden manifest per `RunId` (implemented in [`ArchLucid.sql`](../../ArchLucid.Persistence/Scripts/ArchLucid.sql) and DbUp `120_ManifestFinalizationSpAndIndex.sql`).
- **`dbo.sp_FinalizeManifest`** — run header + audit + outbox only; manifest rows must already exist in the same transaction.

## In-memory / legacy path

When `IArchLucidUnitOfWork.SupportsExternalTransaction` is false, `ManifestFinalizationService` uses sequential repository calls plus `IAuditService` and `IIntegrationEventOutboxRepository` without a shared SQL transaction (test / in-memory parity; not crash-safe across failures).

## Code map

| Component | Location |
|-----------|----------|
| Orchestration entry | [`AuthorityDrivenArchitectureRunCommitOrchestrator`](../../ArchLucid.Application/Runs/Orchestration/AuthorityDrivenArchitectureRunCommitOrchestrator.cs) |
| Service | [`ManifestFinalizationService`](../../ArchLucid.Application/Runs/Finalization/ManifestFinalizationService.cs) |
| DI registration | [`ServiceCollectionExtensions.ApplicationPipeline.cs`](../../ArchLucid.Host.Composition/Startup/ServiceCollectionExtensions.ApplicationPipeline.cs) |
| Audit constant | `AuditEventTypes.ManifestFinalized` |
| Integration type | `IntegrationEventTypes.ManifestFinalizedV1` |
| JSON Schema | [`schemas/integration-events/manifest-finalized.v1.schema.json`](../../schemas/integration-events/manifest-finalized.v1.schema.json) |

## Security, reliability, cost

- **Security:** Scope predicates on `SELECT`/`UPDATE` (`TenantId`, `WorkspaceId`, `ScopeProjectId`); RLS on outbox/audit unchanged.
- **Reliability:** `UPDLOCK` + `RowVersionStamp` + unique index prevent duplicate manifests and lost updates under concurrency.
- **Cost:** One extra stored procedure round-trip per commit; negligible vs decision engine work.

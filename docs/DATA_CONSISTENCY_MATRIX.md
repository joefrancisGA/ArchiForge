# Data consistency matrix

**Last reviewed:** 2026-04-17 (trial lifecycle hard purge: `SqlTenantHardPurgeService` deletes tenant-scoped `dbo` rows in bounded batches; `dbo.AuditEvents` retained; see `TenantHardPurgeServiceSqlIntegrationTests`; prior **2026-04-16** — run archival cascades include ArtifactBundles, AgentExecutionTraces, ComparisonRecords **ArchivedUtc** when migration **073** is applied; see `SqlRunRepositoryArchivalCascadeTests`, `SqlRunRepositoryArchivalExtendedCascadeTests`)

This document states **what consistency guarantees callers should assume** for major aggregates. It complements `docs/SQL_DDL_DISCIPLINE.md` and `docs/API_CONTRACTS.md`.

## Objective

Make explicit which paths are **strongly consistent** (read-your-writes within a transaction), **transactionally outboxed** (eventually processed), or **eventually aligned** (cross-service).

## Assumptions

- Primary authority state lives in **`dbo.Runs`** and related authority tables scoped by tenant/workspace/project.
- Coordinator-facing tables use string **`RunId`** (no-dash hex) as a **logical** correlation key aligned with **`dbo.Runs.RunId`**; referential integrity to **`dbo.Runs`** is application-enforced (migration **047** dropped legacy FKs to **`ArchitectureRuns`**; migration **049** dropped the legacy table).
- A strongly typed **`RunId`** value object exists in **`ArchLucid.Core.Identity`** for gradual adoption at API and persistence boundaries; most code paths still use **`Guid`** today.

## Matrix

| Aggregate / flow | Consistency | Mechanism | Notes |
|------------------|------------|-----------|--------|
| Create run + authority pipeline | Per-connection transactional | SQL transactions in orchestrator | Committed rows visible after successful commit. |
| Run optimistic concurrency | Row-level | `ROWVERSION` on `dbo.Runs` (and selected tables) | Conflicting updates → `409` with conflict problem type. |
| Retrieval indexing after commit | Eventual | Transactional enqueue + worker processing | Enqueue is tied to commit transaction where configured; indexer may lag. |
| Idempotency key on create run | Scoped replay-safe | Hash of body + scope keys | Treat as **best-effort** under extreme duplicate-key races; authority **`dbo.Runs`** is the durable header. |
| Demo trusted-baseline seed | Transactional per repository | **`IRunRepository.SaveAsync`** / **`UpdateAsync`** on **`dbo.Runs`** plus coordinator rows | No legacy table write path. |
| Multi-tenant isolation (SQL) | Defense in depth | RLS policies + `SESSION_CONTEXT` when `SqlServer:RowLevelSecurity:ApplySessionContext=true` | Not every table carries scope columns; see `docs/security/MULTI_TENANT_RLS.md`. |
| Trial lifecycle → hard purge (DPA) | Eventual / operator-retryable | `TrialLifecycleSchedulerHostedService` + `TrialLifecycleTransitionEngine` + `SqlTenantHardPurgeService` (`SqlRowLevelSecurityBypassAmbient`) | Transitions are idempotent per `TryRecordTrialLifecycleTransitionAsync`; purge runs in `DELETE TOP` loops; `dbo.AuditEvents` excluded from purge; failed purge leaves `TrialStatus=Deleted` for retry. See `docs/runbooks/TRIAL_LIFECYCLE.md`. |
| Policy pack assignments | Per-row transactional | SQL writes | `ROWVERSION` on assignments supports future optimistic flows. |
| LLM completion cache | Best-effort | Distributed/memory cache | Cache hits do not consume Azure usage; stale entries TTL-bound. |
| Hot-path read cache (runs, golden manifests, policy pack metadata) | Read-through + TTL | `IHotPathReadCache` (memory or Redis; see `HotPathCache:*`) | **Does not cache list endpoints** (e.g. runs list). **Single-row writes** remove the matching key (`Save`/`Update` on runs; `Save` on manifests; `Create`/`Update` on policy packs). **Bulk run archival** (`ArchiveRunsCreatedBeforeAsync`) removes **each archived run’s** cache key using `OUTPUT` scope columns so operators do not see archived runs until TTL expiry. Remaining risk: TTL-bound staleness if data changes **outside** these repository methods (ad-hoc SQL, future writers). |

## Runs authority convergence (complete)

Dual persistence (**`ArchitectureRuns`** vs **`Runs`**) is **retired** in supported deployments:

- **ADR 0012** — **Completed** (2026-04-12): **`IArchitectureRunRepository`** and **`dbo.ArchitectureRuns`** removed; reads and writes use **`IRunRepository`** / **`dbo.Runs`** (see `docs/adr/0012-runs-authority-convergence-write-freeze.md`).
- **ADR 0002** — **Superseded** by ADR 0012 completion; historical note retained in `docs/adr/0002-dual-persistence-architecture-runs-and-runs.md`.

## Read-replica staleness expectations

When read replica routing is enabled (via **`ReadReplicaRoutedConnectionFactory`** and **`SqlServerOptions`**), read-only queries may hit an Azure SQL **readable secondary**. Writes always go to the **primary**. That path is **eventually consistent**: a successful write on the primary may not appear on a replica-bound read for a short interval.

| Scenario | Expected lag | Mitigation |
|----------|-------------|------------|
| Normal steady-state | Usually under **5 seconds** | Acceptable for list views, dashboards, and search-style reads |
| Heavy write burst (bulk archival, large seed) | **10–30 seconds** or more | Operators should wait and refresh; **`IHotPathReadCache`** reduces perceived lag for single-row reads that go through cache-invalidating repository methods |
| Geo-dr / failover group failover | **Minutes** (RPO/RPO per Azure SLA) | Follow database failover runbooks; app health checks reflect DB readiness |

### Which queries may hit the replica?

Services resolved through **`ReadReplicaRoutedConnectionFactory`** (per route enum, e.g. authority run list, governance resolution reads, golden manifest lookup) use the replica connection when configured. Examples include run list/search, some governance dashboard reads, and read-only manifest lookups.

Single-row hot-path reads may still be satisfied from **`IHotPathReadCache`**, which is invalidated on documented write paths; TTL remains a back-stop if data changes outside those writers.

### Queries that should stay on the primary

- All **`INSERT` / `UPDATE`** paths inside **`IArchLucidUnitOfWork`**
- Read-your-writes inside an open transaction (UoW connection)
- Health probes that must reflect primary connectivity

### Operator guidance

If a list view looks stale immediately after a write, wait briefly and refresh. For suspected replica issues during bulk operations, temporarily disable replica routing (**`ReadReplica:Enabled=false`**) only with operational awareness of added primary load.

## Archival cascade (runs)

| Area | Behavior today | Notes |
|------|----------------|--------|
| **`dbo.Runs`** | **`ArchivedUtc`** soft-archive on bulk archival | Primary visibility gate for run lists that respect archival |
| **`dbo.GoldenManifests` / `dbo.FindingsSnapshots`** | **`ArchivedUtc`** set in the **same transaction** as parent **`dbo.Runs`** bulk / by-id archival | Migration **`066_GoldenManifestsFindingsSnapshots_ArchivedUtc.sql`**; **`SqlRunRepository`** batch |
| **`dbo.ContextSnapshots` / `dbo.GraphSnapshots` / `dbo.DecisioningTraces`** | **`ArchivedUtc`** set in the **same transaction** as parent **`dbo.Runs`** bulk / by-id archival (RunId-aligned) | Migration **`067_ContextGraphDecisioning_ArchivedUtc.sql`**; **`SqlRunRepository`** batch; integration coverage in **`ArchLucid.Persistence.Tests/SqlRunRepositoryArchivalCascadeTests.cs`** |
| **`dbo.ArtifactBundles` / `dbo.AgentExecutionTraces` / `dbo.ComparisonRecords`** | **`ArchivedUtc`** set in the **same transaction** as parent **`dbo.Runs`** bulk / by-id archival (RunId-aligned; comparison rows match **`TRY_CAST(LeftRunId/RightRunId)`** to archived run ids) | Migration **`073_ArtifactBundlesAgentTracesComparisons_ArchivedUtc.sql`**; **`SqlRunRepository`** batch; integration coverage in **`SqlRunRepositoryArchivalExtendedCascadeTests.cs`** |
| **Coordinator artifacts** | Application-enforced consistency | Treat archived authority runs as **logically** inactive; do not assume every child FK is nulled automatically |
| **Hot-path cache** | **`ArchiveRunsCreatedBeforeAsync`** removes per-run keys via **`OUTPUT`** scope columns | See matrix row **Hot-path read cache** |

**Operator expectation:** golden manifest, findings snapshot, context snapshot, graph snapshot, decisioning trace, artifact bundle, agent execution trace, and comparison rows tied to an archived run carry **`ArchivedUtc`** alongside **`dbo.Runs`** when those columns exist in the catalog; list/detail APIs that filter on run archival should treat matching child **`ArchivedUtc`** as aligned for the families above.

**Transaction pattern:** **`IArchLucidUnitOfWork`** / **`IArchLucidUnitOfWorkFactory`** are the standard for mutating authority SQL in one transaction. A repo-wide search shows **no** `TransactionScope` usage in product `.cs` sources (as of 2026-04-14); coordinator orchestrators use the same UoW pattern for create/commit persistence. Prefer UoW for new writes.

## Operational consistency signals

| Signal | Type | Notes |
|--------|------|--------|
| **`run_golden_manifest_consistency`** (readiness) | Health check | **`RunGoldenManifestConsistencyHealthCheck`**: non-archived **`dbo.Runs`** with **`GoldenManifestId`** set but no matching **`dbo.GoldenManifests`** row → **Degraded**. Skipped when storage is InMemory. |
| **`DataConsistencyOrphanProbeHostedService`** | Background timer | SQL only; configurable via **`DataConsistency:OrphanProbeEnabled`** / **`OrphanProbeIntervalMinutes`**. Counts **`dbo.ComparisonRecords`** with parsable **`LeftRunId`** missing from **`dbo.Runs`**; logs warning and emits **`archlucid_data_consistency_orphans_detected_total`** (detection-only). |

## Related

- `docs/adr/0002-dual-persistence-architecture-runs-and-runs.md`
- `docs/adr/0012-runs-authority-convergence-write-freeze.md`
- `docs/ONBOARDING_HAPPY_PATH.md`

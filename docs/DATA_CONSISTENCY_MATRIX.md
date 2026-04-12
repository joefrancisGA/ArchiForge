# Data consistency matrix

**Last reviewed:** 2026-04-12 (migration **049** — **`dbo.ArchitectureRuns`** dropped; **`dbo.Runs`** is the sole persisted run header; dual-persistence health checks removed)

This document states **what consistency guarantees callers should assume** for major aggregates. It complements `docs/SQL_DDL_DISCIPLINE.md` and `docs/API_CONTRACTS.md`.

## Objective

Make explicit which paths are **strongly consistent** (read-your-writes within a transaction), **transactionally outboxed** (eventually processed), or **eventually aligned** (cross-service).

## Assumptions

- Primary authority state lives in **`dbo.Runs`** and related authority tables scoped by tenant/workspace/project.
- Coordinator-facing tables use string **`RunId`** (no-dash hex) as a **logical** correlation key aligned with **`dbo.Runs.RunId`**; referential integrity to **`dbo.Runs`** is application-enforced (migration **047** dropped legacy FKs to **`ArchitectureRuns`**; migration **049** dropped the legacy table).

## Matrix

| Aggregate / flow | Consistency | Mechanism | Notes |
|------------------|------------|-----------|--------|
| Create run + authority pipeline | Per-connection transactional | SQL transactions in orchestrator | Committed rows visible after successful commit. |
| Run optimistic concurrency | Row-level | `ROWVERSION` on `dbo.Runs` (and selected tables) | Conflicting updates → `409` with conflict problem type. |
| Retrieval indexing after commit | Eventual | Transactional enqueue + worker processing | Enqueue is tied to commit transaction where configured; indexer may lag. |
| Idempotency key on create run | Scoped replay-safe | Hash of body + scope keys | Treat as **best-effort** under extreme duplicate-key races; authority **`dbo.Runs`** is the durable header. |
| Demo trusted-baseline seed | Transactional per repository | **`IRunRepository.SaveAsync`** / **`UpdateAsync`** on **`dbo.Runs`** plus coordinator rows | No legacy table write path. |
| Multi-tenant isolation (SQL) | Defense in depth | RLS policies + `SESSION_CONTEXT` when `SqlServer:RowLevelSecurity:ApplySessionContext=true` | Not every table carries scope columns; see `docs/security/MULTI_TENANT_RLS.md`. |
| Policy pack assignments | Per-row transactional | SQL writes | `ROWVERSION` on assignments supports future optimistic flows. |
| LLM completion cache | Best-effort | Distributed/memory cache | Cache hits do not consume Azure usage; stale entries TTL-bound. |
| Hot-path read cache (runs, golden manifests, policy pack metadata) | Read-through + TTL | `IHotPathReadCache` (memory or Redis; see `HotPathCache:*`) | **Does not cache list endpoints** (e.g. runs list). **Single-row writes** remove the matching key (`Save`/`Update` on runs; `Save` on manifests; `Create`/`Update` on policy packs). **Bulk run archival** (`ArchiveRunsCreatedBeforeAsync`) removes **each archived run’s** cache key using `OUTPUT` scope columns so operators do not see archived runs until TTL expiry. Remaining risk: TTL-bound staleness if data changes **outside** these repository methods (ad-hoc SQL, future writers). |

## Runs authority convergence (complete)

Dual persistence (**`ArchitectureRuns`** vs **`Runs`**) is **retired** in supported deployments:

- **ADR 0012** — **Completed** (2026-04-12): **`IArchitectureRunRepository`** and **`dbo.ArchitectureRuns`** removed; reads and writes use **`IRunRepository`** / **`dbo.Runs`** (see `docs/adr/0012-runs-authority-convergence-write-freeze.md`).
- **ADR 0002** — **Superseded** by ADR 0012 completion; historical note retained in `docs/adr/0002-dual-persistence-architecture-runs-and-runs.md`.

## Related

- `docs/adr/0002-dual-persistence-architecture-runs-and-runs.md`
- `docs/adr/0012-runs-authority-convergence-write-freeze.md`
- `docs/ONBOARDING_HAPPY_PATH.md`

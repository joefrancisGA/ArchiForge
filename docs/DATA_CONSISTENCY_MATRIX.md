# Data consistency matrix

**Last reviewed:** 2026-04-09 (hot-path cache + archival eviction)

This document states **what consistency guarantees callers should assume** for major aggregates. It complements `docs/SQL_DDL_DISCIPLINE.md` and `docs/API_CONTRACTS.md`.

## Objective

Make explicit which paths are **strongly consistent** (read-your-writes within a transaction), **transactionally outboxed** (eventually processed), or **eventually aligned** (legacy dual-write).

## Assumptions

- Primary authority state lives in **`dbo.Runs`** and related authority tables scoped by tenant/workspace/project.
- **`dbo.ArchitectureRuns`** (legacy string `RunId`) may still participate in some flows; operators should treat it as **compatibility**, not the sole source of truth for new integrations.

## Matrix

| Aggregate / flow | Consistency | Mechanism | Notes |
|------------------|------------|-----------|--------|
| Create run + authority pipeline | Per-connection transactional | SQL transactions in orchestrator | Committed rows visible after successful commit. |
| Run optimistic concurrency | Row-level | `ROWVERSION` on `dbo.Runs` (and selected tables) | Conflicting updates → `409` with conflict problem type. |
| Retrieval indexing after commit | Eventual | Transactional enqueue + worker processing | Enqueue is tied to commit transaction where configured; indexer may lag. |
| Idempotency key on create run | Scoped replay-safe | Hash of body + scope keys | Documented caveat: extreme races may not span **both** legacy and authority stores atomically — treat as **best-effort** across dual persistence. |
| Multi-tenant isolation (SQL) | Defense in depth | RLS policies + `SESSION_CONTEXT` when `SqlServer:RowLevelSecurity:ApplySessionContext=true` | Not every table carries scope columns; see `docs/security/MULTI_TENANT_RLS.md`. |
| Policy pack assignments | Per-row transactional | SQL writes | `ROWVERSION` on assignments supports future optimistic flows. |
| LLM completion cache | Best-effort | Distributed/memory cache | Cache hits do not consume Azure usage; stale entries TTL-bound. |
| Hot-path read cache (runs, golden manifests, policy pack metadata) | Read-through + TTL | `IHotPathReadCache` (memory or Redis; see `HotPathCache:*`) | **Does not cache list endpoints** (e.g. runs list). **Single-row writes** remove the matching key (`Save`/`Update` on runs; `Save` on manifests; `Create`/`Update` on policy packs). **Bulk run archival** (`ArchiveRunsCreatedBeforeAsync`) removes **each archived run’s** cache key using `OUTPUT` scope columns so operators do not see archived runs until TTL expiry. Remaining risk: TTL-bound staleness if data changes **outside** these repository methods (ad-hoc SQL, future writers). |

## Deprecation: dual persistence (`ArchitectureRuns` vs `Runs`)

See **docs/adr/0012-runs-authority-convergence-write-freeze.md** for the complete write call site inventory.

**Status:** Converge new features on **`dbo.Runs`** and Dapper repositories. **`ArchitectureRuns`** exists for historical and CLI/adjacent flows.

### Named milestone: **RunsAuthorityConvergence**

| Gate | Date (aggressive default) | Meaning |
|------|---------------------------|---------|
| **Write freeze** | **2026-09-30** | No new product features or net-new code paths may **write** to **`dbo.ArchitectureRuns`**. Hotfixes to existing writers require an **ADR** and a dated removal task. |
| **Read convergence** | **2026-12-31** | All first-party readers that can switch to **`dbo.Runs`** / GUID **`/v1`** flows **must** switch; remaining `ArchitectureRuns` reads listed in a single tracking epic. |
| **Legacy removal target** | **2027-03-31** | **`ArchitectureRuns`** is **read-only or removed** from supported deployments (org choice: empty table vs drop), unless the epic is **explicitly extended** by ADR with a new named date. |

These dates are **planning defaults** for the product repo; your organization may tighten them in internal runbooks. They replace the vague “after two major releases” horizon so security and SRE reviews have a **single named target**.

**Actions for teams:**

1. Prefer APIs and jobs that resolve runs by **GUID** from **`/v1/...`** responses.
2. When adding persistence, avoid new **`ArchitectureRuns`** dependencies without an ADR (`docs/adr/`).
3. Track remaining readers with a periodic codebase search for `ArchitectureRuns` / legacy `RunId` string keys.
4. Tag work items **`RunsAuthorityConvergence`** so release notes and audits can filter progress.

## Related

- `docs/adr/0002-dual-persistence-architecture-runs-and-runs.md`
- `docs/adr/0012-runs-authority-convergence-write-freeze.md` — **complete write call site inventory** for **`IArchitectureRunRepository`** / **`dbo.ArchitectureRuns`** (production audit)
- `docs/ONBOARDING_HAPPY_PATH.md`

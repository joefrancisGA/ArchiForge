# Persistence Project Split

**Date:** 7 April 2026

## Objective

Split `ArchLucid.Persistence` (formerly 234 .cs files) into two focused projects to reduce cognitive load and enforce clear boundaries between data access and service coordination.

## Assumptions

- The split is structural only — no behavioral changes, no public API surface changes.
- `ArchLucid.Persistence.Runtime` (25 files) remains unchanged and references both new projects.
- 18 downstream projects reference `ArchLucid.Persistence`; some now also reference `Persistence.Coordination`.

## Constraints

- No circular references: `Coordination → Persistence` (one-way). `Runtime → both`.
- One class per file (existing convention preserved).
- Build and all 1,391 unit tests pass after the split.

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│  Downstream consumers                       │
│  (Api, Application, Host.*, Backfill.Cli…)  │
└────────┬───────────────────┬────────────────┘
         │                   │
         ▼                   ▼
┌────────────────┐  ┌────────────────────────────┐
│ArchLucid.      │  │ArchLucid.                  │
│Persistence     │◄─│ Persistence.Coordination   │
│(164 files)     │  │ (70 files)                 │
│                │  │                            │
│ Pure data      │  │ Service coordination:      │
│ access:        │  │ Compare, Replay, Evolution,│
│ Dapper repos,  │  │ ProductLearning, Retrieval,│
│ SQL queries,   │  │ Backfill, Compliance,      │
│ models,        │  │ Diagnostics, Caching       │
│ connections,   │  │ options/resolver            │
│ type handlers, │  │                            │
│ migrations,    │  │ Composes repositories,     │
│ serialization, │  │ processes outbox entries,   │
│ blob envelope  │  │ evaluates diffs, runs      │
│ models,        │  │ replay, manages backfill,  │
│ caching        │  │ product-learning analytics │
│ abstractions   │  └────────────────────────────┘
└────────────────┘           │
         ▲                   │
         │                   ▼
┌────────────────────────────────────────────┐
│ArchLucid.Persistence.Runtime (25 files)   │
│ Blob store impls (Azure, Local, Null),    │
│ hot-path cache impls (Memory, Distributed),│
│ authority pipeline orchestration,          │
│ data archival coordinator, UoW factories   │
└────────────────────────────────────────────┘
```

## Component Breakdown

### ArchLucid.Persistence (164 files) — pure data access

| Folder | Files | Purpose |
|--------|-------|---------|
| `Connections/` | 17 | ISqlConnectionFactory, RLS, resilient wrappers, read-replica routing |
| `Data/Infrastructure/` | 6 | SqlConnectionFactory, DatabaseMigrator, type handlers |
| `Data/Repositories/` | 62 | All I*Repository interfaces, Dapper/InMemory implementations, DTOs |
| `Repositories/` | 14 | Sql*Repository, Caching*Repository, storage rows, mappers |
| `Findings/` | 4 | Relational read, legacy JSON reader, storage row, payload codec |
| `ContextSnapshots/` | 3 | Relational read, legacy JSON reader, storage row |
| `GraphSnapshots/` | 2 | Relational read, edge constants |
| `GoldenManifests/` | 2 | Relational read, storage row |
| `ArtifactBundles/` | 4 | Relational read, JSON readers, storage row |
| `BlobStore/` | 6 | IArtifactBlobStore, ArtifactLargePayloadOptions, blob envelope models, offload evaluator |
| `Caching/` | 2 | IHotPathReadCache, HotPathCacheKeys |
| `Serialization/` | 4 | JsonEntitySerializer, graph converters, audit options |
| `Governance/` | 8 | Dapper/InMemory policy pack repos, CachingPolicyPackRepository |
| `Conversation/` | 6 | Dapper/InMemory conversation repos |
| `Provenance/` | 4 | Sql/InMemory provenance repos |
| `Queries/` | 11 | Dapper/InMemory query services, DTOs, mappers |
| Other | ~15 | Models, Interfaces, Sql schema, Options, RelationalRead |

### ArchLucid.Persistence.Coordination (70 files) — services and coordination

| Folder | Files | Purpose |
|--------|-------|---------|
| `Compare/` | 6 | AuthorityCompareService, run/manifest comparison results, diff types |
| `Replay/` | 6 | AuthorityReplayService, replay request/result/mode/validation |
| `Evolution/` | 8 | CandidateChangeSetService, evolution repositories (Dapper + InMemory), deterministic IDs |
| `ProductLearning/` | 29 | Dashboard, feedback aggregation, improvement opportunities, triage reports, pilot signals, planning |
| `Retrieval/` | 6 | RetrievalIndexingOutboxProcessor, outbox repo interfaces/impls |
| `Backfill/` | 8 | SqlRelationalBackfillService, CutoverReadinessService, interfaces/models |
| `Compliance/` | 1 | PolicyFilteredComplianceRulePackProvider |
| `Diagnostics/` | 4 | IOutboxOperationalMetricsReader, InMemory/Dapper impls, metrics snapshot |
| `Caching/` | 2 | HotPathCacheProviderResolver, HotPathCacheOptions |

## Data Flow

- **Coordination → Persistence**: Coordination services call repository interfaces and SQL helpers defined in Persistence.
- **Runtime → Coordination + Persistence**: Runtime provides concrete blob store and cache implementations for interfaces defined in Persistence, and references options/resolvers in Coordination.
- **Downstream → both**: API controllers and host composition reference whichever project provides the types they need.

## Security Model

No changes — all SQL connections, RLS enforcement, and blob store credentials remain in Persistence and Runtime as before.

## Operational Considerations

- **Circular reference guard**: `ArchLucid.Persistence.Coordination` must never reference `ArchLucid.Persistence.Runtime`. The dependency flows one way: `Runtime → Coordination → Persistence`.
- **InternalsVisibleTo**: `ArchLucid.Persistence` exposes internals to `ArchLucid.Persistence.Coordination` (for backfill access to `internal` repository methods like `BackfillRelationalSlicesAsync`).
- **Dockerfile**: The API Dockerfile includes a `COPY` line for `ArchLucid.Persistence.Coordination/*.csproj`.
- **Future evolution**: If Coordination grows further, individual subfolder groups (e.g., ProductLearning) could be extracted into their own projects.

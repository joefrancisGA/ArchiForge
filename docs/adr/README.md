# Architecture Decision Records (ADR)

**Last reviewed:** 2026-04-04

Short, durable decisions for ArchLucid. Each file is **immutable** once accepted; supersede with a new ADR rather than rewriting history.

| ADR | Title |
|-----|--------|
| [0001](0001-hosting-roles-api-worker-combined.md) | Hosting roles: Api, Worker, Combined |
| [0002](0002-dual-persistence-architecture-runs-and-runs.md) | Dual persistence: ArchitectureRuns vs dbo.Runs |
| [0003](0003-sql-rls-session-context.md) | SQL RLS and SESSION_CONTEXT |
| [0004](0004-transactional-outbox-retrieval-indexing.md) | Transactional outbox for retrieval indexing |
| [0005](0005-llm-completion-pipeline.md) | LLM completion pipeline, cache, quota, metrics |
| [0006](0006-url-path-api-versioning.md) | URL-path API versioning (`/v1`) |
| [0007](0007-effective-governance-merge.md) | Effective governance merge (policy pack resolution) |
| [0008](0008-alert-dedupe-scopes.md) | Alert deduplication scopes |
| [0009](0009-digest-delivery-failure-semantics.md) | Digest delivery failure semantics |
| [0010](0010-dual-manifest-trace-repository-contracts.md) | Dual manifest and decision-trace repository contracts |
| [0011](0011-inmemory-vs-sql-storage-provider.md) | `ArchLucid:StorageProvider` — InMemory vs Sql |
| [0012](0012-runs-authority-convergence-write-freeze.md) | Runs convergence — `ArchitectureRuns` write freeze inventory |

**When to add an ADR:** Cross-cutting choice affecting security, data, or ops; multiple valid alternatives; cost of reversal is high.

**Numbering rule:** Next ADR gets the next sequential number. Never reuse a number; never share a number between two files.

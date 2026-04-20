> **Scope:** ADR 0004: Transactional outbox for retrieval indexing - full detail, tables, and links in the sections below.

# ADR 0004: Transactional outbox for retrieval indexing

- **Status:** Accepted
- **Date:** 2026-04-04

## Context

Retrieval indexing must not commit “run succeeded” without a durable record of indexing work, or the system can lose index updates on crashes.

## Decision

Enqueue retrieval indexing work **inside the same SQL transaction** as authority commit where the storage provider supports it.

## Consequences

- **Positive:** Atomic handoff from commit to indexer; aligns with outbox pattern.
- **Negative:** In-memory test doubles use a separate code path; integration tests should exercise SQL. See ADR 0011 for storage-provider branching.

## Links

- `docs/DATA_CONSISTENCY_MATRIX.md`

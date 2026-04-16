# Runbook: Provenance indexing — failure modes and retries (v1)

**Last reviewed:** 2026-04-16

## Flow

After a successful authority commit, **retrieval indexing** may run via **`IRetrievalRunCompletionIndexer`** / **`RetrievalRunCompletionIndexer`**, building documents from manifest, artifacts, and **`DecisionProvenanceGraph`**.

## Failure modes

- **Indexing throws:** The committed run remains authoritative; search/RAG may lag until indexing is retried manually or a future outbox/retry job exists (see **`NEXT_REFACTORINGS.md`** outbox items).
- **OpenAI / embedding outages:** Same as above — symptom is empty or stale retrieval results for the run.

## Observability

- Tracing: **`ArchLucid.Retrieval.Index`** **`ActivitySource`** (tag **`archlucid.run_id`**) when OTLP tracing includes **`AddSource`** for ArchLucid sources.

## Mitigation (v1)

1. Identify the **`runId`** and check logs around commit time for indexing exceptions.
2. If a **re-index** API or job exists in your deployment, trigger it for the run; otherwise re-run a controlled **replay/commit** in lower environments only (avoid duplicate production commits).

## Hardening backlog

- **Outbox** + worker for post-commit indexing (at-least-once delivery).
- **Circuit breaker** around embedding clients with backoff.

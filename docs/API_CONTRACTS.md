# API contracts (notable behaviors)

## Comparison replay — verify mode

`POST /v1/architecture/comparisons/{comparisonRecordId}/replay` with `replayMode: verify` regenerates the comparison and compares it to the stored payload.

| Outcome | HTTP | Notes |
|--------|------|--------|
| Match | **200** | Replay artifact returned; `X-ArchiForge-VerificationPassed: true` |
| Drift | **422** | `application/problem+json`, `type` … `#comparison-verification-failed`, optional **`driftDetected`**, **`driftSummary`** |

Clients must not assume verify failure returns 200 with a JSON body flag.

## End-to-end run compare — missing run

`GET`/`POST` routes under `/v1/architecture/run/compare/end-to-end/...` that resolve runs by ID return **404** with problem type **`#run-not-found`** when a referenced run does not exist (not generic `#resource-not-found`).

## Commit run — conflict

`POST /v1/architecture/run/{runId}/commit` returns **409 Conflict** with problem type **`#conflict`** when the run is in Failed status, already committed, or otherwise not in a state that allows commit.

## Comparison replay — request validation

The replay endpoint body (`format`, `replayMode`, `profile`, `persistReplay`) is validated with FluentValidation. Invalid values (e.g. unsupported format or replayMode) return **400 Bad Request** with problem details describing validation errors.

## OpenAPI / .NET 10

Swagger documents the comparison replay **422** response, **404** with `#run-not-found` on run/compare and comparisons routes, and **409** with `#conflict` on commit. The codebase does not use deprecated `WithOpenApi`; use operation filters / transformers for per-operation docs.

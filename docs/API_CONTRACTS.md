# API contracts (notable behaviors)

## API versioning

- **URL path:** Major version is in the path: **`/v1/...`** (see controller routes `v{version:apiVersion}`).
- **Default:** Version **1.0** is assumed when not specified; clients should still use **`/v1`** in URLs.
- **Discovery:** Responses can include **`api-supported-versions`** / **`api-deprecated-versions`** per [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning) options (`ReportApiVersions`).

## Correlation ID

- Optional request header **`X-Correlation-ID`**: if present, the API echoes it on the response and uses it for logging/tracing context; if absent, a value is generated (e.g. from the ASP.NET Core trace identifier).

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

The **batch replay** endpoint body (`comparisonRecordIds`, `format`, `replayMode`, `profile`, `persistReplay`) is also validated; empty `comparisonRecordIds` or invalid format/replayMode/profile return **400** with validation errors.

## OpenAPI / .NET 10

Swagger documents the comparison replay **422** response, **404** with `#run-not-found` on run/compare and comparisons routes, and **409** with `#conflict` on commit. The codebase does not use deprecated `WithOpenApi`; use operation filters / transformers for per-operation docs.

## Create run — `ArchitectureRequest` (context ingestion fields)

`POST` routes that accept **`ArchitectureRequest`** (e.g. create run) may include optional ingestion fields in addition to **`Description`** / **`SystemName`**:

| Field | Type | Notes |
|-------|------|--------|
| `inlineRequirements` | `string[]` | Each entry becomes a canonical **Requirement** (see `docs/CONTEXT_INGESTION.md`). Max **100** items, each max **4000** chars. |
| `documents` | object[] | Inline uploads: **`name`**, **`contentType`**, **`content`** (not multipart files). Max **50** documents. **`contentType`** must be supported (currently **`text/plain`**, **`text/markdown`**). **`content`** max **500000** chars. |
| `policyReferences` | `string[]` | Max **100** items, each max **500** chars → **PolicyControl** objects. |
| `topologyHints` | `string[]` | Max **100** items, each max **2000** chars. |
| `securityBaselineHints` | `string[]` | Max **100** items, each max **2000** chars. |

Validation is performed with **FluentValidation** (`ArchitectureRequestValidator`, `ContextDocumentRequestValidator`). Invalid payloads return **400** with problem details.

If a document’s content type is not supported by any registered parser, ingestion may still record **warnings** on the persisted **`ContextSnapshot`** (`warnings`) when that path is hit (e.g. non-HTTP callers). Normal API clients receive **400** before ingest for unknown document content types.

Full pipeline behavior: **`docs/CONTEXT_INGESTION.md`**.

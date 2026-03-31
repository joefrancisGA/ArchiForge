# API contracts (notable behaviors)

## API versioning

- **URL path:** Major version is in the path: **`/v1/...`** (see controller routes `v{version:apiVersion}`).
- **Default:** Version **1.0** is assumed when not specified; clients should still use **`/v1`** in URLs.
- **Discovery:** Responses can include **`api-supported-versions`** / **`api-deprecated-versions`** per [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning) options (`ReportApiVersions`).

## Operator artifacts (`/v1/api/artifacts`)

- **List** `GET …/manifests/{manifestId}`: **200** with a JSON **array** (possibly empty) when the manifest exists in scope. Items are ordered **by name (case-insensitive), then artifact id** (stable for UI tables and bundle ZIP entry order).
- **Bundle** `GET …/manifests/{manifestId}/bundle`: **404** with **`#manifest-not-found`** when the manifest is unknown/out of scope; **404** with **`#resource-not-found`** when the manifest exists but there is no bundle or zero artifacts (list may still return `[]`). Use **problem `type` / `title`**, not status code alone.
- **Descriptor / file download** under `…/artifact/{artifactId}`: manifest must be in scope; missing artifact → **404** **`#resource-not-found`**.

UI alignment: **`docs/operator-shell.md`**.

## Correlation ID

- Optional request header **`X-Correlation-ID`**: if present, the API echoes it on the response and uses it for logging/tracing context; if absent, a value is generated (e.g. from the ASP.NET Core trace identifier).
- **Audit rows:** **`IAuditService`** (API host) stamps **`AuditEvent.CorrelationId`** from the **`correlation.id`** tag on the current or ancestor **`Activity`** (same value the middleware sets on the request activity), then falls back to **`HttpContext.TraceIdentifier`**, then the innermost activity id. Background advisory scans use a synthetic id of the form **`advisory-schedule:{scheduleId}`** on the advisory activity so digest/alert audits remain joinable in logs.

## Problem details (`application/problem+json`) — extensions

- **`extensions.errorCode`**: stable uppercase code for clients and automation.
- **`extensions.supportHint`** (56R): optional, concise **next step** for operators; complements **`detail`**. No stack traces or secrets — use logs with **`X-Correlation-ID`** / **`RunId`** for deep diagnosis. See **`docs/TROUBLESHOOTING.md`**.

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

### Security schemes (Swashbuckle)

When **`ArchiForgeAuth:Mode`** is **`JwtBearer`**, **`/swagger/v1/swagger.json`** includes **`components.securitySchemes.Bearer`** (HTTP bearer, JWT) and **document-level `security`** defaulting to that scheme, with text derived from **`ArchiForgeAuth:Audience`** and **`ArchiForgeAuth:Authority`** where set (Microsoft Entra). When **`ArchiForgeAuth:Mode`** is **`ApiKey`**, the document includes **`ApiKey`** (**`X-Api-Key`** header). **`DevelopmentBypass`** does not add these schemes (local ergonomics). Schema **ids** use the CLR **full type name** so colliding short names (e.g. two `DecisionTrace` types) do not break generation.

## Create run — `ArchitectureRequest` (context ingestion fields)

`POST` routes that accept **`ArchitectureRequest`** (e.g. create run) may include optional ingestion fields in addition to **`Description`** / **`SystemName`**:

| Field | Type | Notes |
|-------|------|--------|
| `inlineRequirements` | `string[]` | Each entry becomes a canonical **Requirement** (see `docs/CONTEXT_INGESTION.md`). Max **100** items, each max **4000** chars. |
| `documents` | object[] | Inline uploads: **`name`**, **`contentType`**, **`content`** (not multipart files). Max **50** documents. **`name`** and **`contentType`** must be non-empty and not whitespace-only (max **500** / **255** chars). **`contentType`** must be in **`ArchiForge.ContextIngestion.SupportedContextDocumentContentTypes.All`** (today: **`text/plain`**, **`text/markdown`**). **`content`** must not be null; max **500000** chars (empty string allowed). |
| `policyReferences` | `string[]` | Max **100** items, each max **500** chars → **PolicyControl** objects. |
| `topologyHints` | `string[]` | Max **100** items, each max **2000** chars. |
| `securityBaselineHints` | `string[]` | Max **100** items, each max **2000** chars. |
| `infrastructureDeclarations` | object[] | Max **50** items. Each: **`name`**, **`format`** (`json` \| `simple-terraform`), **`content`** (payload string; JSON document or Terraform-like `resource "type" "name"` lines). Validated by **`InfrastructureDeclarationRequestValidator`**. |

Validation is performed with **FluentValidation** (`ArchitectureRequestValidator`, `ContextDocumentRequestValidator`, **`InfrastructureDeclarationRequestValidator`**). Invalid payloads return **400** with problem details.

If a document’s content type is not supported by any registered parser, ingestion may still record **warnings** on the persisted **`ContextSnapshot`** (`warnings`) when that path is hit (e.g. non-HTTP callers). Normal API clients receive **400** before ingest for unknown document content types.

Full pipeline behavior: **`docs/CONTEXT_INGESTION.md`**.

## Create run — `Idempotency-Key` (optional)

`POST /v1/architecture/request` accepts optional header **`Idempotency-Key`** (trimmed, max **256** characters).

| Situation | HTTP | Notes |
|-----------|------|--------|
| First successful create with key | **201 Created** | Mapping stored for `(tenant, workspace, project, SHA-256(key))`. |
| Retry with same key and **same** request body fingerprint | **200 OK** | Same JSON as create; response includes **`Idempotency-Replayed: true`**. |
| Same key, **different** body | **409 Conflict** | Problem type **`#conflict`**; message explains key reuse with different payload. |

Fingerprint is **SHA-256** of the canonical **`ArchitectureRequest`** JSON using the same **`ContractJson.Default`** options as **`ArchitectureRequests.RequestJson`** persistence. Clients must send byte-identical JSON (modulo insignificant whitespace is **not** normalised—use a stable serializer).

**Scope:** Keys are isolated per **`x-tenant-id` / `x-workspace-id` / `x-project-id`** (or JWT claims) via **`IScopeContextProvider`**.

**Concurrency:** Under extreme parallel duplicate-key pressure, a losing request may roll back legacy **`ArchitectureRuns`** rows while authority-side **`dbo.Runs`** work may already be committed; operators should treat idempotency as **retry-safe** for typical client behaviour, not a distributed two-phase guarantee across both stores. See **`docs/SQL_DDL_DISCIPLINE.md`**.

## Policy packs (`/v1/policy-packs`)

Governance is packaged as **versioned, assignable** bundles. Pack **content** is JSON matching **`PolicyPackContentDocument`**: `complianceRuleIds`, `complianceRuleKeys` (string rule IDs matching file-based compliance rules), `alertRuleIds`, `compositeAlertRuleIds`, `advisoryDefaults`, `metadata` (see **`ArchiForge.Decisioning.Governance.PolicyPacks`**).

| Method | Path | Notes |
|--------|------|--------|
| `POST` | `/v1/policy-packs` | Create pack + initial **unpublished** version **1.0.0**. Requires **ExecuteAuthority**. |
| `POST` | `/v1/policy-packs/{policyPackId}/publish` | **Upserts** a **published** version row for `(pack, version)`; updates pack **Active** and **CurrentVersion**. Re-publishing the same version updates **ContentJson** in place (no duplicate rows). **`version`** must be **SemVer 2**-style (`MAJOR.MINOR.PATCH`, optional pre-release/build, optional leading `v`). |
| `POST` | `/v1/policy-packs/{policyPackId}/assign` | Assigns a **version** with optional **`scopeLevel`** (`Tenant` \| `Workspace` \| `Project`, default **Project**) and **`isPinned`**. Tenant assignments store `workspaceId`/`projectId` as empty GUIDs; workspace assignments store `projectId` as empty. **`version`** must match the same **SemVer 2** rules as publish. **404** `#policy-pack-version-not-found` if that version does not exist for the pack. |
| `GET` | `/v1/policy-packs` | List packs for scope. |
| `GET` | `/v1/policy-packs/{policyPackId}/versions` | List versions for a pack. |
| `GET` | `/v1/policy-packs/effective` | Resolved **enabled** assignments → pack metadata + **ContentJson** per entry. |
| `GET` | `/v1/policy-packs/effective-content` | **Merged** document: union of IDs (distinct), **advisoryDefaults** / **metadata** last-wins per key. |

**Validation:** Create / publish / assign bodies are validated with **FluentValidation**. Invalid JSON in `initialContentJson` or `contentJson`, unknown `packType`, or empty `version` returns **400** with problem details (same style as other validated endpoints).

**Effective governance, compliance, and alerts:** When merged **`alertRuleIds`** or **`compositeAlertRuleIds`** is **non-empty**, simple and composite **alert evaluation** restricts rules to those IDs. When **`complianceRuleIds`** and **`complianceRuleKeys`** are both **empty**, compliance uses the full file-based rule pack; otherwise evaluation uses only rules matching those keys or GUID **RuleId** values. **Empty alert lists** mean *no pack filter* for alerts (all enabled rules in scope still run). Advisory scans load merged content **once** per run, copy **`advisoryDefaults`** onto **`ImprovementPlan.PolicyPackAdvisoryDefaults`**, and pass merged content on **`AlertEvaluationContext.EffectiveGovernanceContent`** so simple and composite evaluation do not each reload it.

**Alerts / digest / tuning / simulation** routes use the same URL versioning pattern, e.g. **`/v1/alerts`**, **`/v1/alert-rules`**, **`/v1/composite-alert-rules`**, **`/v1/alert-simulation/...`**, **`/v1/alert-tuning/...`**, **`/v1/alert-routing-subscriptions`**, **`/v1/digest-subscriptions`**.

Compliance filtering in API requests follows **`IScopeContextProvider`**. For **advisory scheduled scans**, the runner pushes an **ambient `ScopeContext`** (see **`AmbientScopeContext`**) for the duration of the scan so scoped services (including filtered compliance) see the schedule’s tenant/workspace/project even without an HTTP request.

**Multiple assignments:** Assignments applicable to the project context include **tenant-wide**, matching **workspace**, and matching **project** rows (see **`PolicyPackAssignment.ScopeLevel`**). Enabled rows are listed in **`AssignedUtc` descending** order on **`GET .../effective`**. **`GET .../effective-content`** applies hierarchical resolution (not a naive union). Inspect **`GET /v1/governance-resolution`** for **decisions**, **conflicts**, and **notes**.

## Governance resolution (`/v1/governance-resolution`)

| Method | Path | Notes |
|--------|------|--------|
| `GET` | `/v1/governance-resolution` | Returns **`EffectiveGovernanceResolutionResult`**: **`effectiveContent`**, per-item **`decisions`** (winner pack, scope level, reason), **`conflicts`** (duplicate definitions / value conflicts), and **`notes`**. Emits audit **`GovernanceResolutionExecuted`**; **`GovernanceConflictDetected`** when **`conflicts`** is non-empty. Requires **ReadAuthority**. |

**Rate limiting:** Governance and alert **`/v1/...`** controllers use the **`fixed`** window unless noted elsewhere (e.g. **expensive** / **replay** on architecture flows). See **README.md** (Rate limiting table) and **`RateLimiting:*`** configuration keys.

Scope defaults for dev/tests: **`ScopeIds`** (**`x-tenant-id`**, **`x-workspace-id`**, **`x-project-id`** optional).

## Create run and commit — retries and idempotency (v1)

**Current behavior (v1):**

- **`POST .../architecture/run`** (create + execute authority chain) and **`POST .../architecture/run/{runId}/commit`** do **not** accept an **`Idempotency-Key`** header. Each successful call performs real work and persists new state.
- Clients that retry on timeouts or **`5xx`** must tolerate **at-most-once** semantics unless they implement their own idempotency (e.g. cache the returned **`runId`** and avoid issuing a second create for the same logical operation, or use conditional checks before commit).

**Recommended client pattern:**

1. Create run → capture **`runId`** from the response body.
2. On ambiguous failure (gateway timeout, connection reset), **GET** run detail or list if your integration supports it before creating another run.
3. Commit only when the run is in an expected phase; handle **`409`** **`#conflict`** as the authoritative “already committed / invalid phase” signal.

**Future (backlog):** optional **`Idempotency-Key`** on create and/or commit, backed by a short-lived server-side store, to make safe retries first-class without duplicate runs.

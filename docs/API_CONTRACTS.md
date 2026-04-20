> **Scope:** API contracts (notable behaviors) - full detail, tables, and links in the sections below.

# API contracts (notable behaviors)

**Error bodies (RFC 9457 Problem Details, obsoletes RFC 7807):** See **[API_ERROR_CONTRACT.md](API_ERROR_CONTRACT.md)** for Problem+JSON shape, stable **`type`** URIs, and correlation behavior.

## API versioning

- **URL path:** Major version is in the path: **`/v1/...`** (see controller routes `v{version:apiVersion}`).
- **Alternate readers:** Clients may also send **`api-version`** as a query string or request header (same major.minor as the URL segment, e.g. **`1.0`**) — wired in **`ArchLucid.Api/Startup/MvcExtensions.cs`** via **`ApiVersionReader.Combine`** alongside **`UrlSegmentApiVersionReader`**.
- **Default:** Version **1.0** is assumed when not specified; clients should still use **`/v1`** in URLs.
- **Discovery:** Responses can include **`api-supported-versions`** / **`api-deprecated-versions`** per [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning) options (`ReportApiVersions`).

## Deprecation policy

- **Headers:** When **`ApiDeprecation:Enabled`** is true, successful responses may include **`Deprecation`** and **`Sunset`** (and optional **`Link`** with relation `deprecation`) per product configuration (`ApiDeprecation:*` in appsettings).
- **Sunset:** Treat **`Sunset`** as the earliest date after which the version may be removed; plan client upgrades before that date.
- **Breaking changes:** Ship breaking changes only in a **new major** API version (new path prefix, e.g. `/v2/...`), keep **`v1`** stable for the published sunset window, and document migration in release notes.
- **Non-breaking:** Minor additive changes (new optional fields, new endpoints under the same major version) do not require a new major version; prefer OpenAPI diff + contract tests to catch accidental breaks.

## Contract artifacts

| Artifact | Location | Purpose |
|----------|----------|---------|
| OpenAPI (Microsoft document) | Served at **`/openapi/v1.json`**; snapshot in **`ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json`** | CI fails on unexpected HTTP contract drift (`OpenApiContractSnapshotTests`). Regenerate: `ARCHLUCID_UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractSnapshotTests`. |
| OpenAPI (Swashbuckle) | **`/swagger/v1/swagger.json`** | Interactive docs and optional APIM import (`infra/terraform`). |
| AsyncAPI (webhooks) | **`docs/contracts/archlucid-asyncapi-2.6.yaml`** | Documents **outbound** alert/digest webhook JSON and optional HMAC header. |
| Bruno collection | **`contracts/bruno/`** | Manual smoke requests (health, OpenAPI, admin diagnostics); set **`local`** environment `baseUrl` and **`apiKey`** (or switch auth to JWT in Bruno for Entra). |

**Operator narrative:** `docs/ONBOARDING_HAPPY_PATH.md` (request → commit → retrieval). **Consistency guarantees:** `docs/DATA_CONSISTENCY_MATRIX.md`. **Admin / runbooks:** `docs/OPERATIONS_ADMIN.md`, `docs/OPERATIONS_LLM_QUOTA.md`. **ADRs:** `docs/adr/README.md`.

## Operator artifacts (`/v1/artifacts`)

- **List** `GET /v1/artifacts/manifests/{manifestId}`: **200** with a JSON **array** (possibly empty) when the manifest exists in scope. Items are ordered **by name (case-insensitive), then artifact id** (stable for UI tables and bundle ZIP entry order).
- **Bundle** `GET /v1/artifacts/manifests/{manifestId}/bundle`: **404** with **`#manifest-not-found`** when the manifest is unknown/out of scope; **404** with **`#resource-not-found`** when the manifest exists but there is no bundle or zero artifacts (list may still return `[]`). Use **problem `type` / `title`**, not status code alone.
- **Run export ZIP** `GET /v1/artifacts/runs/{runId}/export`: committed run only; see OpenAPI for response shape.
- **Descriptor / file download** under `/v1/artifacts/manifests/{manifestId}/artifact/{artifactId}` (and `…/descriptor`): manifest must be in scope; missing artifact → **404** **`#resource-not-found`**.

UI alignment: **`docs/operator-shell.md`**.

## Explain (`/v1/explain`)

All routes require **ReadAuthority** and use versioned paths under **`/v1/explain`**. Response JSON uses **camelCase** property names.

| Method | Path | Response | Notes |
|--------|------|----------|--------|
| `GET` | **`/v1/explain/runs/{runId}/explain`** | **`ExplanationResult`** | Stakeholder narrative + `structured` envelope, optional provenance and top-level `confidence`. **404** if run/manifest missing in scope. |
| `GET` | **`/v1/explain/runs/{runId}/aggregate`** | **`RunExplanationSummary`** | Same nested **`explanation`** as above, plus **`themeSummaries`**, **`overallAssessment`**, **`riskPosture`**, and manifest/findings **counts**. **404** if run/manifest missing in scope. |
| `GET` | **`/v1/explain/compare/explain`** | **`ComparisonExplanationResult`** | Query: **`baseRunId`**, **`targetRunId`**. **404** if either run lacks a golden manifest in scope. |

Schema and posture rules: **`docs/EXPLANATION_SCHEMA.md`**. Operator UI: run detail **Explanation** section calls **`getRunExplanationSummary`** (`archlucid-ui`).

## Pilots (`/v1/pilots`)

Sponsor- and pilot-facing read models. All routes require **ReadAuthority** and live under **`/v1/pilots`**.

| Method | Path | Response | Notes |
|--------|------|----------|-------|
| `GET` | **`/v1/pilots/runs/{runId}/first-value-report`** | **`text/markdown`** | One-page Markdown summary (run metadata, findings counts, decision trace excerpt, baseline placeholders). **404** when the run id is unknown. |

CLI: `archlucid first-value-report <runId> [--save]` (see **`docs/CLI_USAGE.md`**).

## List pagination (runs and alerts)

Several list endpoints support **two response shapes** so existing clients keep working:

| Query | Response shape | Notes |
|-------|----------------|--------|
| **`GET /v1/authority/projects/{projectId}/runs`** with **`take`** only (no **`page`**) | JSON **array** of run summaries | **`take`** clamped **1–200** (default **20**). Newest first. |
| Same path with **`page`** set | **`PagedResponse&lt;RunSummaryResponse&gt;`**: **`items`**, **`totalCount`**, **`page`**, **`pageSize`**, **`hasMore`** | **`page`** is one-based. **`pageSize`** clamped **1–200** (default **50**, same as **`PaginationDefaults.DefaultPageSize`**). Server uses **`OFFSET`/`FETCH`** (SQL) with a matching **`COUNT`** for **`totalCount`**. |
| **`GET /v1/alerts`** with **`take`** only (no **`page`**) | JSON **array** of **`AlertRecord`** | **`take`** clamped **1–500** (default **100**). |
| Same path with **`page`** set | **`PagedResponse&lt;AlertRecord&gt;`** | Same pagination rules as other **`page`**/**`pageSize`** endpoints. |

The operator UI **Runs** page uses **`page`** + **`pageSize`** (legacy **`take`** in the query string is still read as **`pageSize`** when **`pageSize`** is omitted). **Alerts** uses **`page`** + **`pageSize`** from the client.

## Bulk operator endpoints (partial success)

These return **200** with a JSON body that lists **per-id outcomes** (succeeded vs failed). Failed rows include a **reason** or **message**; they do not fail the entire request.

| Method | Path | Policy | Cap | Notes |
|--------|------|--------|-----|--------|
| `POST` | **`/v1/admin/runs/archive-by-ids`** | **AdminAuthority** | 100 run ids | Body: **`{ "runIds": ["…"] }`**. Response: **`RunArchiveByIdsResult`** (`succeededRunIds`, `failed[]`). |
| `POST` | **`/v1/governance/approval-requests/batch-review`** | **ExecuteAuthority** | 50 ids | Body: **`decision`** `approve` \| `reject`, **`approvalRequestIds`**, optional **`reviewComment`** / **`reviewedBy`**. Response: **`GovernanceBatchReviewResponse`**. |
| `POST` | **`/v1/alerts/acknowledge-batch`** | **ExecuteAuthority** | 100 alert ids | Body: **`{ "alertIds": ["…"], "comment": "…" }`**. Scope must match each alert. Response: **`AlertsAcknowledgeBatchResponse`**. |

See also **`docs/CONTROLLER_AREA_MAP.md`**. Existing **`POST /v1/admin/runs/archive-batch`** (cutoff by **`createdBeforeUtc`**) remains available.

## Correlation ID

- Optional request header **`X-Correlation-ID`**: if present, the API echoes it on the response and uses it for logging/tracing context; if absent, a value is generated (e.g. from the ASP.NET Core trace identifier).
- **`application/problem+json`:** error bodies include **`correlationId`** (same value as **`X-Correlation-ID`**) so operators can triage from saved JSON when headers are missing. The global exception handler still includes legacy **`traceId`** for the same identifier.
- **Operator UI proxy** (`/api/proxy/*`): proxy-generated errors (bad upstream URL, 413, 502, 429) include **`correlationId`** in the JSON body and **`X-Correlation-ID`** on the response; one-line **`archlucid-ui-proxy`** server logs may repeat **`correlationId`** for log correlation.
- **Audit rows:** **`IAuditService`** (API host) stamps **`AuditEvent.CorrelationId`** from the **`correlation.id`** tag on the current or ancestor **`Activity`** (same value the middleware sets on the request activity), then falls back to **`HttpContext.TraceIdentifier`**, then the innermost activity id. Background advisory scans use a synthetic id of the form **`advisory-schedule:{scheduleId}`** on the advisory activity so digest/alert audits remain joinable in logs.

## Problem details (`application/problem+json`) — extensions

- **`extensions.errorCode`**: stable uppercase code for clients and automation.
- **`extensions.supportHint`** (56R): optional, concise **next step** for operators; complements **`detail`**. No stack traces or secrets — use logs with **`X-Correlation-ID`** / **`correlationId`** in the body / **`RunId`** for deep diagnosis. See **`docs/TROUBLESHOOTING.md`**.

## Trial exhausted or over quota (**402 Payment Required**)

When the tenant is on an **Active** trial that has **expired**, or **run** / **seat** limits are reached, **mutating** routes under **ExecuteAuthority** / **AdminAuthority** return **402** with **`Content-Type: application/problem+json`** and:

- **`type`:** `https://archlucid.dev/problem/trial-expired`
- **`title`:** human-readable summary (e.g. trial limit reached)
- **`status`:** **402**
- **`extensions.traceCompleteness`**, **`extensions.correlationId`**, **`extensions.trialReason`** (`Expired` \| `RunsExceeded` \| `SeatsExceeded`), **`extensions.daysRemaining`**

**Example (trimmed):**

```json
{
  "type": "https://archlucid.dev/problem/trial-expired",
  "title": "Trial limit reached",
  "status": 402,
  "detail": "The tenant trial does not allow this write.",
  "traceCompleteness": "Full",
  "correlationId": "00-abc123-01",
  "trialReason": "Expired",
  "daysRemaining": 0
}
```

**Reads** under **ReadAuthority** (e.g. explain, GET run detail) are **not** blocked by this rule so operators can still inspect existing work. **Conversion** uses **`POST /v1/tenant/convert`** (annotated to skip the trial write gate). See **`docs/security/TRIAL_LIMITS.md`** and **ADR [0014](adr/0014-trial-enforcement-boundary.md)**.

## Comparison replay — verify mode

`POST /v1/architecture/comparisons/{comparisonRecordId}/replay` with `replayMode: verify` regenerates the comparison and compares it to the stored payload.

| Outcome | HTTP | Notes |
|--------|------|--------|
| Match | **200** | Replay artifact returned; `X-ArchLucid-VerificationPassed: true` |
| Drift | **422** | `application/problem+json`, `type` … `#comparison-verification-failed`, optional **`driftDetected`**, **`driftSummary`** |

Clients must not assume verify failure returns 200 with a JSON body flag.

## End-to-end run compare — missing run

`GET`/`POST` routes under `/v1/architecture/run/compare/end-to-end/...` that resolve runs by ID return **404** with problem type **`#run-not-found`** when a referenced run does not exist (not generic `#resource-not-found`).

## Commit run — success, idempotency, and conflicts

`POST /v1/architecture/run/{runId}/commit` returns **200 OK** with the golden manifest when the run is **already committed** and the stored manifest can be loaded (**idempotent retry** safe for clients).

It returns **409 Conflict** with problem type **`#conflict`** when the run is in **Failed** status, not ready to commit (e.g. missing agent results / wrong phase), or other state rules block commit — but **not** solely because a prior commit already succeeded.

When **`ArchLucid:Governance:PreCommitGateEnabled`** is **true** and an enabled policy pack assignment has **`BlockCommitOnCritical`**, the same route may return **409** with problem type **`#governance-pre-commit-blocked`** (RFC 9457 **`extensions.blockingFindingIds`**, optional **`extensions.policyPackId`**) if the run’s findings snapshot contains **Critical** severity findings. See **`docs/PRE_COMMIT_GOVERNANCE_GATE.md`**.

## Comparison replay — request validation

The replay endpoint body (`format`, `replayMode`, `profile`, `persistReplay`) is validated with FluentValidation. Invalid values (e.g. unsupported format or replayMode) return **400 Bad Request** with problem details describing validation errors.

The **batch replay** endpoint (`POST /v1/architecture/comparisons/replay/batch`) validates the same replay fields as single replay, plus `comparisonRecordIds`. **Maximum list size** is **`ComparisonReplay:Batch:MaxComparisonRecordIds`** (default **50**, startup-validated between **1** and **500**). Duplicate IDs are processed once, in first-seen order.

**Success (200):** response is **`application/zip`** containing **`batch-replay-manifest.json`** (successes, failures, processed IDs) and one folder per successful comparison ID with the replay artifact. If some IDs fail and at least one succeeds, **`X-ArchLucid-Batch-Partial: true`** is set.

**All IDs fail:** **422 Unprocessable Entity** with problem type **`#batch-replay-all-failed`** and **`extensions.errorCode`** **`BATCH_REPLAY_ALL_FAILED`** (no ZIP body).

## OpenAPI / .NET 10

Swagger documents the comparison replay **422** response, **404** with `#run-not-found` on run/compare and comparisons routes, and **409** with `#conflict` or `#governance-pre-commit-blocked` on commit when applicable. The codebase does not use deprecated `WithOpenApi`; use operation filters / transformers for per-operation docs.

### Security schemes (Swashbuckle)

When **`ArchLucidAuth:Mode`** is **`JwtBearer`**, **`/swagger/v1/swagger.json`** includes **`components.securitySchemes.Bearer`** (HTTP bearer, JWT) and **document-level `security`** defaulting to that scheme, with text derived from **`ArchLucidAuth:Audience`** and **`ArchLucidAuth:Authority`** where set (Microsoft Entra). When **`ArchLucidAuth:Mode`** is **`ApiKey`**, the document includes **`ApiKey`** (**`X-Api-Key`** header). **`DevelopmentBypass`** does not add these schemes (local ergonomics). Schema **ids** use the CLR **full type name** so colliding short names (e.g. two `DecisionTrace` types) do not break generation.

## Create run — `ArchitectureRequest` (context ingestion fields)

`POST` routes that accept **`ArchitectureRequest`** (e.g. create run) may include optional ingestion fields in addition to **`Description`** / **`SystemName`**:

| Field | Type | Notes |
|-------|------|--------|
| `inlineRequirements` | `string[]` | Each entry becomes a canonical **Requirement** (see `docs/CONTEXT_INGESTION.md`). Max **100** items, each max **4000** chars. |
| `documents` | object[] | Inline uploads: **`name`**, **`contentType`**, **`content`** (not multipart files). Max **50** documents. **`name`** and **`contentType`** must be non-empty and not whitespace-only (max **500** / **255** chars). **`contentType`** must be in **`ArchLucid.ContextIngestion.SupportedContextDocumentContentTypes.All`** (today: **`text/plain`**, **`text/markdown`**). **`content`** must not be null; max **500000** chars (empty string allowed). |
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

**Concurrency:** Under extreme parallel duplicate-key pressure, idempotency is **retry-safe** for typical client behaviour but not a serializable global guarantee; authority state is **`dbo.Runs`** and related coordinator rows. See **`docs/SQL_DDL_DISCIPLINE.md`**.

## Policy packs (`/v1/policy-packs`)

Governance is packaged as **versioned, assignable** bundles. Pack **content** is JSON matching **`PolicyPackContentDocument`**: `complianceRuleIds`, `complianceRuleKeys` (string rule IDs matching file-based compliance rules), `alertRuleIds`, `compositeAlertRuleIds`, `advisoryDefaults`, `metadata` (see **`ArchLucid.Decisioning.Governance.PolicyPacks`**).

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

**Rate limiting:** Governance and alert **`/v1/...`** controllers use the **`fixed`** window unless noted elsewhere (e.g. **expensive** / **replay** on architecture flows). **`fixed`** and **`expensive`** policies apply **per role segment × client IP**; base limits come from **`RateLimiting:FixedWindow:*`** / **`RateLimiting:Expensive:*`**, with optional multipliers under **`RateLimiting:RoleMultipliers`**. See **README.md** (Rate limiting table) and **`docs/SECURITY.md`** (role-aware limits).

Scope defaults for dev/tests: **`ScopeIds`** (**`x-tenant-id`**, **`x-workspace-id`**, **`x-project-id`** optional).

## Create run and commit — retries and idempotency (v1)

**Current behavior (v1):**

- **`POST .../architecture/request`** (create run) may use an **`Idempotency-Key`** header per API semantics; **`POST .../architecture/run/{runId}/commit`** does **not** accept that header.
- **Commit** is **idempotent** when the run is **already committed**: a repeat **`POST …/commit`** returns **200 OK** with the same golden manifest (safe retries after timeouts).
- **Create** and the **first** successful commit still perform durable writes; clients that retry ambiguous **create** failures should **GET** run detail/list before issuing a second create for the same logical operation.

**Recommended client pattern:**

1. Create run → capture **`runId`** from the response body.
2. On ambiguous failure (gateway timeout, connection reset), **GET** run detail or list if your integration supports it before creating another run.
3. Commit when the run is **ReadyForCommit**; on ambiguous failure, **GET** run detail — if already **Committed**, a retry **`POST …/commit`** is safe (**200**). Treat **`409`** **`#conflict`** as **invalid phase** (e.g. not executed, **Failed**, missing results), not as “must never retry commit.”

**Future (backlog):** optional **`Idempotency-Key`** on create and/or commit, backed by a short-lived server-side store, to make safe retries first-class without duplicate runs.

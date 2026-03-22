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
| `documents` | object[] | Inline uploads: **`name`**, **`contentType`**, **`content`** (not multipart files). Max **50** documents. **`contentType`** must be in **`ArchiForge.ContextIngestion.SupportedContextDocumentContentTypes.All`** (today: **`text/plain`**, **`text/markdown`**). **`content`** max **500000** chars. |
| `policyReferences` | `string[]` | Max **100** items, each max **500** chars → **PolicyControl** objects. |
| `topologyHints` | `string[]` | Max **100** items, each max **2000** chars. |
| `securityBaselineHints` | `string[]` | Max **100** items, each max **2000** chars. |
| `infrastructureDeclarations` | object[] | Max **50** items. Each: **`name`**, **`format`** (`json` \| `simple-terraform`), **`content`** (payload string; JSON document or Terraform-like `resource "type" "name"` lines). Validated by **`InfrastructureDeclarationRequestValidator`**. |

Validation is performed with **FluentValidation** (`ArchitectureRequestValidator`, `ContextDocumentRequestValidator`, **`InfrastructureDeclarationRequestValidator`**). Invalid payloads return **400** with problem details.

If a document’s content type is not supported by any registered parser, ingestion may still record **warnings** on the persisted **`ContextSnapshot`** (`warnings`) when that path is hit (e.g. non-HTTP callers). Normal API clients receive **400** before ingest for unknown document content types.

Full pipeline behavior: **`docs/CONTEXT_INGESTION.md`**.

## Policy packs (`/v1/policy-packs`)

Governance is packaged as **versioned, assignable** bundles. Pack **content** is JSON matching **`PolicyPackContentDocument`**: `complianceRuleIds`, `complianceRuleKeys` (string rule IDs matching file-based compliance rules), `alertRuleIds`, `compositeAlertRuleIds`, `advisoryDefaults`, `metadata` (see **`ArchiForge.Decisioning.Governance.PolicyPacks`**).

| Method | Path | Notes |
|--------|------|--------|
| `POST` | `/v1/policy-packs` | Create pack + initial **unpublished** version **1.0.0**. Requires **ExecuteAuthority**. |
| `POST` | `/v1/policy-packs/{policyPackId}/publish` | **Upserts** a **published** version row for `(pack, version)`; updates pack **Active** and **CurrentVersion**. Re-publishing the same version updates **ContentJson** in place (no duplicate rows). **`version`** must be **SemVer 2**-style (`MAJOR.MINOR.PATCH`, optional pre-release/build, optional leading `v`). |
| `POST` | `/v1/policy-packs/{policyPackId}/assign` | Assigns a **version string** to the **current scope** (tenant/workspace/project from headers/claims). **`version`** must match the same **SemVer 2** rules as publish. **404** `#policy-pack-version-not-found` if that version does not exist for the pack. |
| `GET` | `/v1/policy-packs` | List packs for scope. |
| `GET` | `/v1/policy-packs/{policyPackId}/versions` | List versions for a pack. |
| `GET` | `/v1/policy-packs/effective` | Resolved **enabled** assignments → pack metadata + **ContentJson** per entry. |
| `GET` | `/v1/policy-packs/effective-content` | **Merged** document: union of IDs (distinct), **advisoryDefaults** / **metadata** last-wins per key. |

**Validation:** Create / publish / assign bodies are validated with **FluentValidation**. Invalid JSON in `initialContentJson` or `contentJson`, unknown `packType`, or empty `version` returns **400** with problem details (same style as other validated endpoints).

**Effective governance, compliance, and alerts:** When merged **`alertRuleIds`** or **`compositeAlertRuleIds`** is **non-empty**, simple and composite **alert evaluation** restricts rules to those IDs. When **`complianceRuleIds`** and **`complianceRuleKeys`** are both **empty**, compliance uses the full file-based rule pack; otherwise evaluation uses only rules matching those keys or GUID **RuleId** values. **Empty alert lists** mean *no pack filter* for alerts (all enabled rules in scope still run). Advisory scans load merged content **once** per run, copy **`advisoryDefaults`** onto **`ImprovementPlan.PolicyPackAdvisoryDefaults`**, and pass merged content on **`AlertEvaluationContext.EffectiveGovernanceContent`** so simple and composite evaluation do not each reload it.

**Alerts / digest / tuning / simulation** routes use the same URL versioning pattern, e.g. **`/v1/alerts`**, **`/v1/alert-rules`**, **`/v1/composite-alert-rules`**, **`/v1/alert-simulation/...`**, **`/v1/alert-tuning/...`**, **`/v1/alert-routing-subscriptions`**, **`/v1/digest-subscriptions`**.

Compliance filtering in API requests follows **`IScopeContextProvider`**. For **advisory scheduled scans**, the runner pushes an **ambient `ScopeContext`** (see **`AmbientScopeContext`**) for the duration of the scan so scoped services (including filtered compliance) see the schedule’s tenant/workspace/project even without an HTTP request.

**Multiple assignments:** Enabled assignments for the same scope are returned in **`AssignedUtc` descending** order. **`GET .../effective`** lists each resolved pack separately; **`GET .../effective-content`** merges all of them (union of ID lists, **`complianceRuleKeys`** / **`complianceRuleIds`** distinct, **`advisoryDefaults`** / **`metadata`** last-wins per key).

**Rate limiting:** Governance and alert **`/v1/...`** controllers use the **`fixed`** window unless noted elsewhere (e.g. **expensive** / **replay** on architecture flows). See **README.md** (Rate limiting table) and **`RateLimiting:*`** configuration keys.

Scope defaults for dev/tests: **`ScopeIds`** (**`x-tenant-id`**, **`x-workspace-id`**, **`x-project-id`** optional).

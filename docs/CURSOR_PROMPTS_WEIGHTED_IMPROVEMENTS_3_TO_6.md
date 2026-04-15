# Cursor prompts — Weighted quality assessment Improvements 3–6

Canonical assessment: [`QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`](QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md) § *Six Best Improvements*.

Improvement 3 detail lives in [`CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md`](CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md) (rename, single `.sln`, legacy sunset, archive superseded quality docs). This file adds **Improvements 4–6** and a **verification bundle** for Improvement 3.

---

## Improvement 3 — Verification bundle `rename-artifacts-verify-session`

Run after any rename or doc shuffle:

1. Repo root: exactly **`ArchLucid.sln`** — `Get-ChildItem -Filter *.sln` (or `dir *.sln`).
2. No **`ArchiForge/`** directory tree with product **`.cs`** sources (allow CI allowlists, Terraform addresses, RLS names per Improvement 3 prompt file).
3. `dotnet build ArchLucid.sln -c Release --nologo`
4. Spot-check **`docs/CONFIG_BRIDGE_SUNSET.md`** date ↔ **`ArchLucidLegacyConfigurationWarnings.LegacyConfigurationKeysHardEnforcementNoEarlierThan`**.

---

## Improvement 4 — Prompt `v1-requirements-traceability-matrix`

**Objective:** Lightweight **requirements → evidence** map for V1 (not a full ALM tool).

1. Open [`V1_SCOPE.md`](V1_SCOPE.md) §§ 2–5 (in-scope capabilities, happy path, release criteria).
2. Create or extend **[`V1_REQUIREMENTS_TEST_TRACEABILITY.md`](V1_REQUIREMENTS_TEST_TRACEABILITY.md)** with a table: **V1 clause** | **Primary docs** | **Representative tests / scripts** | **Notes**.
3. Prefer existing suites: `ArchLucid.Api.Tests`, `ArchLucid.Persistence.Tests`, `ArchLucid.Decisioning.Tests`, `release-smoke.ps1`, `v1-rc-drill.ps1` — cite **folders or trait filters**, not every test name.
4. **Orphan / archival traceability:** Document **`DataConsistencyOrphanProbeHostedService`** (detection-only), metric **`archlucid_data_consistency_orphans_detected_total`**, and operator remediation (see **`docs/runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md`**). Do not change historical migrations **001–028**; new DDL only via **`ArchLucid.sql`** + new numbered migration if schema changes are required.

---

## Improvement 4 — Prompt `comparison-orphan-probe-and-remediation`

**Objective:** Close the loop between **run lifecycle** and **`ComparisonRecords`** integrity.

1. Ensure the orphan probe covers **both** **`LeftRunId`** and **`RightRunId`** GUID references missing from **`dbo.Runs`** (detection-only; tagged metrics).
2. Keep **[`infra/prometheus/archlucid-alerts.yml`](../infra/prometheus/archlucid-alerts.yml)** § `archlucid-data-consistency` descriptions aligned with probe behavior.
3. Maintain **[`docs/runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md`](runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md)** with idempotent **preview** and **delete** SQL for operators (RLS / scope discipline: run under maintainer role as per your deployment).

---

## Improvement 5 — Prompt `finding-explainability-narrative`

**Objective:** User-facing **readable narrative** from persisted **`ExplainabilityTrace`** (still deterministic; no LLM).

1. Add a small **`FindingExplainabilityNarrativeBuilder`** (or equivalent) in **`ArchLucid.Decisioning`** that turns trace lists + metadata into a **plain-text or Markdown** block.
2. Expose on **`GET .../findings/{findingId}/explainability`** via a new field on **`FindingExplainabilityResult`** (e.g. **`NarrativeText`**).
3. Unit tests in **`ArchLucid.Decisioning.Tests`**: empty trace, fully populated trace, partial lists.
4. **Faithfulness trend:** Add or extend a Prometheus **`archlucid-explainability`** alert group for **`archlucid_explanation_aggregate_faithfulness_fallback_total`** (rate/increase threshold tuned for your environment).

---

## Improvement 6 — Prompt `first-run-wizard-parity`

**Objective:** Wizard matches **V1 happy path** and docs.

1. UI: **`archlucid-ui`** — **`/runs/new`**, wizard schema (**`wizard-schema.ts`**), presets (**`wizard-presets`**), navigation copy (**`ShellNav`**, **`runs/page.tsx`**).
2. API: **`POST /v1/architecture/request`** body parity with **`CreateArchitectureRunRequestPayload`** / OpenAPI.
3. Align **[`PILOT_GUIDE.md`](PILOT_GUIDE.md)** and **[`operator-shell.md`](operator-shell.md)** so “first run” steps reference the same entry points.
4. Optional: empty-state deep-link from dashboard to **`/runs/new`** when no runs exist.

---

## Improvement 6 — Prompt `rfc9457-controller-sweep`

**Objective:** **Problem+JSON** ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457), obsoletes RFC 7807) for client-visible errors (not empty **`NotFound()`** / raw **`Conflict(object)`** where a typed problem is clearer). *(Legacy bookmark: `rfc7807-controller-sweep`.)*

1. Prefer existing extensions in **`ArchLucid.Api/ProblemDetails/ProblemDetailsExtensions.cs`** (`NotFoundProblem`, `ConflictProblem`, `BadRequestProblem`, …).
2. Grep: `return NotFound();`, `return Conflict(`, `return BadRequest();` under **`ArchLucid.Api/Controllers`** — convert to **`this.*Problem`** with stable **`ProblemTypes.*`**.
3. Update **`ProducesResponseType`** metadata where the status body type changes.
4. Run: `dotnet test ArchLucid.Api.Tests -c Release` (or targeted controller tests).

---

## Objective / assumptions / constraints

| | |
|--|--|
| **Objective** | Improvements 3–6 reduce cognitive load, improve traceability, explainability, and API usability. |
| **Assumptions** | **`ArchLucid.sln`** is the product entry; Sql persistence available for optional integration tests. |
| **Constraints** | Historical SQL migrations **001–028** frozen; Terraform **7.5–7.8** deferred per **[`ARCHLUCID_RENAME_CHECKLIST.md`](ARCHLUCID_RENAME_CHECKLIST.md)**; do not expose SMB/445. |

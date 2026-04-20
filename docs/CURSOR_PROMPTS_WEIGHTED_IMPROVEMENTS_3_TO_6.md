> **Scope:** Cursor prompts — Weighted quality assessment Improvements 3–6 - full detail, tables, and links in the sections below.

# Cursor prompts — Weighted quality assessment Improvements 3–6

**Last developed:** 2026-04-17.

Canonical assessment: [`QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`](QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md) § *Six Best Improvements*.

Improvement 3 detail lives in [`CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md`](CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md) (rename, single `.sln`, legacy sunset, archive superseded quality docs). This file adds **Improvements 4–6**, a **verification bundle** for Improvement 3, and **forward-looking** prompts where the original batch is already largely implemented.

### Slug index (bookmark-friendly)

| Slug | Improvement | Purpose |
|------|-------------|---------|
| `rename-artifacts-verify-session` | 3 | Post-session regression checks |
| `terraform-monitoring-phase-7-5-addresses` | 3 | Rename remaining `archiforge` Terraform **addresses** in monitoring stack |
| `di-registration-map-drift` | 3 | Keep `DI_REGISTRATION_MAP.md` truthful vs `Host.Composition` |
| `stale-archiforge-grep-hygiene` | 3 | Grep hygiene for misleading product paths in docs/UI |
| `v1-requirements-traceability-matrix` | 4 | Matrix rows vs `V1_SCOPE.md` |
| `v1-traceability-gap-pass` | 4 | Second pass: every V1 release criterion has ≥1 evidence link |
| `comparison-orphan-probe-and-remediation` | 4 | Probe + Prometheus + runbook alignment |
| `outbox-traceability-sli-row` | 4 | Document outbox convergence SLI next to matrix |
| `finding-explainability-narrative` | 5 | Narrative builder + API field (if not already shipped) |
| `explainability-openapi-contract-parity` | 5 | OpenAPI snapshot + client for `NarrativeText` |
| `explainability-faithfulness-alert-tuning` | 5 | Tune Prometheus rules with runbook thresholds |
| `first-run-wizard-parity` | 6 | Wizard vs pilot docs |
| `rfc9457-controller-sweep` | 6 | Problem+JSON across controllers |
| `rfc9457-controller-batch` | 6 | Bounded batch (N files) + tests |
| `wizard-playwright-first-run` | 6 | E2E happy path `/runs/new` |
| `problem-details-guard-expand` | 6 | Extend Roslyn/guard tests for new anti-patterns |

---

## Improvement 3 — Verification bundle `rename-artifacts-verify-session`

Run after any rename or doc shuffle:

1. Repo root: exactly **`ArchLucid.sln`** — `Get-ChildItem -Filter *.sln` (or `dir *.sln`).
2. No **`ArchiForge/`** directory tree with product **`.cs`** sources (allow CI allowlists, Terraform addresses, RLS names per Improvement 3 prompt file).
3. `dotnet build ArchLucid.sln -c Release --nologo`
4. Spot-check **`docs/CONFIG_BRIDGE_SUNSET.md`** date ↔ **`ArchLucidLegacyConfigurationWarnings.LegacyConfigurationKeysHardEnforcementNoEarlierThan`**.

---

## Improvement 3 — Prompt `terraform-monitoring-phase-7-5-addresses`

**Objective:** Continue **Phase 7.5** for **`infra/terraform-monitoring/`** the same way as root **`infra/terraform`** APIM: Terraform **`moved`** blocks + resource label renames so **state** migrates without replacing Azure objects.

**Context:** Root **`infra/terraform`** APIM already uses **`moved_archlucid_apim.tf`** + **`azurerm_api_management.archlucid`**. Monitoring still references historical labels (see **`docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md`** § `terraform-monitoring`).

**Steps:**

1. `rg "archiforge" infra/terraform-monitoring --glob "*.tf"` — inventory every **resource address** (not comments, not metric strings in JSON).
2. For each renamed resource pair, add **`moved { from = … to = … }`** in a dedicated **`moved_archlucid_monitoring.tf`** (or split by concern), then rename the **`resource`** blocks and **all** internal references (`grafana_folder`, `azurerm_dashboard_grafana`, `azurerm_monitor_alert_prometheus_rule_group`, outputs).
3. From **`infra/terraform-monitoring`**: `terraform init -backend=false` then **`terraform validate`**. Fix any reference cycles.
4. Update **`docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md`** table to mark monitoring rows as **IaC `moved` available** and link the new file.
5. **`docs/ARCHLUCID_RENAME_CHECKLIST.md`** — progress log row; leave **§7.5** unchecked until all planned stacks are done **or** split checklist into per-stack checkboxes if the team prefers.

**Constraints:** Never `terraform destroy` to “fix” names; do not change Grafana JSON metric names (`archlucid_*`) unless OTel parity is explicitly in scope.

---

## Improvement 3 — Prompt `di-registration-map-drift`

**Objective:** Reduce **cognitive load** when wiring new services — `DI_REGISTRATION_MAP.md` should match **`ArchLucid.Host.Composition`**.

**Steps:**

1. Read **`docs/DI_REGISTRATION_MAP.md`** structure (sections per host / feature area).
2. Grep **`ArchLucid.Host.Composition`** for `AddScoped`, `AddSingleton`, `AddHostedService`, `TryAddEnumerable` — build a short delta list vs the doc (added registrations not documented, or documented entries removed from code).
3. Update **`docs/DI_REGISTRATION_MAP.md`** in one PR: add missing rows, strike or relocate removed ones, cross-link to the real extension method file (path + method name).
4. Optional: add **`ArchLucid.Host.Composition.Tests`** test that fails if a curated list of “public DI entrypoints” (e.g. `ServiceCollectionExtensions.*.cs`) is renamed without updating the doc — only if you can do it without brittle string scans.

**Acceptance:** A new contributor can answer “where is X registered?” from the doc in one hop.

---

## Improvement 3 — Prompt `stale-archiforge-grep-hygiene`

**Objective:** Eliminate **misleading** `ArchiForge` / `archiforge` references in **product-facing** prose and UI copy while preserving intentional literals (allowlist, RLS object names, Terraform `moved` `from` addresses, historical migration file names).

**Steps:**

1. Run **`scripts/ci/archiforge-rename-allowlist*.txt`** workflow mentally: read header comments in those files so you do not “fix” allowed literals.
2. `rg -i "archiforge|ArchiForge" archlucid-ui/src docs --glob "!**/archive/**"` — triage hits:
   - **User-visible** strings → **ArchLucid** product naming.
   - **Env var names** in docs → prefer **`ARCHLUCID_*`** with a footnote if legacy is still read for warnings only.
3. For each change, ensure **`docs/ARCHLUCID_RENAME_CHECKLIST.md`** progress log has a one-line entry if you touched rename-adjacent material.
4. `dotnet build ArchLucid.sln -c Release`; **`npm test`** (or **`npm run test`**) under **`archlucid-ui`** if UI strings changed.

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

## Improvement 4 — Prompt `v1-traceability-gap-pass`

**Objective:** Second pass on **`docs/V1_REQUIREMENTS_TEST_TRACEABILITY.md`** so **every** numbered V1 release criterion / capability in **`docs/V1_SCOPE.md`** has at least one **test**, **script**, or **runbook** citation (or an explicit **“gap — planned”** note with owner).

**Steps:**

1. Extract a checklist of clauses from **`docs/V1_SCOPE.md`** (use headings + bullet ids if present; otherwise stable paraphrase ≤ 80 chars per row).
2. For each clause, locate the best existing evidence path (prefer **`Suite=Core`** tests, integration tests under `ArchLucid.*.Tests`, **`scripts/ci/*.py`**, **`release-smoke.ps1`**, **`docs/runbooks/*`**).
3. Update the matrix: no empty “Representative tests” unless the Notes column documents the gap and links a tracking item (e.g. **`docs/NEXT_REFACTORINGS.md`** row).
4. Add **`scripts/ci/assert_v1_traceability.py`** (or extend it) only if you can keep it low-flake — otherwise keep enforcement human-gated via PR review.

**Acceptance:** A release manager can trace **V1_SCOPE → tests/docs** in under 30 minutes.

---

## Improvement 4 — Prompt `outbox-traceability-sli-row`

**Objective:** Tie **async reliability** to the same traceability story as user-visible features: document **integration event outbox** SLI/SLO next to the matrix.

**Steps:**

1. Read **`docs/API_SLOS.md`** (or equivalent) for **outbox oldest age** / convergence language; read **`infra/prometheus/archlucid-slo-rules.yml`** / **`archlucid-alerts.yml`** for the recording rule and alert names.
2. Add one row to **`docs/V1_REQUIREMENTS_TEST_TRACEABILITY.md`**: clause “Integration events delivered reliably” → metrics **`archlucid:slo:integration_event_outbox_oldest_age_seconds`** (or current name) → tests **`IntegrationEventOutboxProcessorTests`** (or cite actual class) → runbook **`docs/runbooks/*`** if retries/DLQ are operator-facing.
3. If metric names in code differ from docs by more than spelling, fix **docs** or **metric registration** in the smallest coherent change set.

---

## Improvement 5 — Prompt `finding-explainability-narrative`

**Objective:** User-facing **readable narrative** from persisted **`ExplainabilityTrace`** (still deterministic; no LLM).

1. Add a small **`FindingExplainabilityNarrativeBuilder`** (or equivalent) in **`ArchLucid.Decisioning`** that turns trace lists + metadata into a **plain-text or Markdown** block.
2. Expose on **`GET .../findings/{findingId}/explainability`** via a new field on **`FindingExplainabilityResult`** (e.g. **`NarrativeText`**).
3. Unit tests in **`ArchLucid.Decisioning.Tests`**: empty trace, fully populated trace, partial lists.
4. **Faithfulness trend:** Add or extend a Prometheus **`archlucid-explainability`** alert group for **`archlucid_explanation_aggregate_faithfulness_fallback_total`** (rate/increase threshold tuned for your environment).

---

## Improvement 5 — Prompt `explainability-openapi-contract-parity`

**Objective:** If **`FindingExplainabilityResult.NarrativeText`** (or equivalent) ships in the API, **OpenAPI + contract tests** must reflect it so **`ArchLucid.Api.Client`** and external consumers do not drift.

**Steps:**

1. Locate **`GET`** handler for finding explainability and the response DTO in **`ArchLucid.Contracts`** / API project.
2. Regenerate or hand-edit **`Contracts/openapi-v1.contract.snapshot.json`** per repo convention; ensure **`OpenApiContractSnapshotTests`** (or **`OpenApiContractInvariantsTests`**) cover the new property name and nullability.
3. Rebuild **`ArchLucid.Api.Client`** (NSwag) if generated; ensure CI **`dotnet-fast-core`** path still passes.
4. Add **`ArchLucid.Api.Tests`** test: response **Content-Type** `application/problem+json` is *not* expected here — assert **`application/json`** and parse body includes narrative field when trace exists.

**Acceptance:** OpenAPI snapshot PR cannot merge without the new field present.

---

## Improvement 5 — Prompt `explainability-faithfulness-alert-tuning`

**Objective:** Make **`archlucid-explainability`** alerts **actionable** (clear runbook, threshold tied to traffic), not noisy.

**Steps:**

1. Open **`infra/prometheus/archlucid-alerts.yml`** — find rules touching **`faithfulness`**, **`explainability`**, or **`ArchLucidExplanationFaithfulnessFallbackTrend`** (names may vary slightly; `rg explainability infra/prometheus`).
2. For each rule: add **`annotations.runbook_url`** (or repo-relative path in docs if that is your convention) and ensure **`for:`** duration matches SLO tier.
3. Document in **`docs/API_SLOS.md`** or **`docs/EXPLAINABILITY_TRACE_COVERAGE.md`** what operator action is expected on first page.
4. Run **`scripts/ci`** Python tests if any script validates alert file shape.

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

## Improvement 6 — Prompt `rfc9457-controller-batch`

**Objective:** Same as **`rfc9457-controller-sweep`**, but **bounded** so PRs stay reviewable (e.g. one API **area** or ≤ **8 controller files** per change set).

**Steps:**

1. Pick a batch using **`docs/CONTROLLER_AREA_MAP.md`** (e.g. **Advisory** + **Alerts** only).
2. Grep only those files: `return NotFound();`, `return Conflict(`, `return BadRequest();`, `return StatusCode(` with ambiguous bodies.
3. Convert to **`this.NotFoundProblem`**, **`this.ConflictProblem`**, **`this.BadRequestProblem`** (or existing helpers in **`ArchLucid.Api/ProblemDetails`**), reusing **`ProblemTypes.*`** constants.
4. Update **`ProducesResponseType`** / **`SwaggerResponse`** attributes where return type meaningfully changes.
5. `dotnet test ArchLucid.Api.Tests/ArchLucid.Api.Tests.csproj -c Release --filter "FullyQualifiedName~<YourNewOrUpdatedTests>"` then full **`ArchLucid.Api.Tests`** if time allows.

**Acceptance:** No bare `NotFound()` left in the chosen batch; CI green.

---

## Improvement 6 — Prompt `wizard-playwright-first-run`

**Objective:** Lock the **first-run** UX with a **Playwright** spec so wizard regressions are caught in **`archlucid-ui`** CI.

**Steps:**

1. Locate existing E2E config (`.github/workflows/ci.yml` Tier **3b**, `archlucid-ui` **`npm run test:e2e`**).
2. Add **`e2e/first-run-wizard.spec.ts`** (name may vary): navigate to **`/runs/new`**, select a **preset** (if applicable), fill required fields with **test-id** selectors, **stub** or target **mock API** per project pattern, assert success navigation or summary panel.
3. Keep runtime under **90s** on CI; skip if `process.env.CI` lacks credentials only if an existing pattern already does this — document skip reason in the spec header.
4. Update **`docs/FIRST_RUN_WIZARD.md`** (or **`FIRST_RUN_WALKTHROUGH.md`**) with “Automated coverage: …” linking the spec path.

---

## Improvement 6 — Prompt `problem-details-guard-expand`

**Objective:** Extend static / unit **guard tests** so new controller code cannot reintroduce bare MVC results where Problem+JSON is required.

**Steps:**

1. Open **`ArchLucid.Api.Tests`** (or **`ArchLucid.Architecture.Tests`**) — find **`ApiControllerProblemDetailsSourceGuardTests`** (or similarly named guard).
2. Add patterns the grep should flag next: e.g. **`return StatusCode(404)`**, **`Results.NotFound`**, **`TypedResults.NotFound`** if those appear in the codebase and should be unified.
3. Run the guard test alone; fix **existing** violations only in the files you touch, or split PR: **(a)** widen guard, **(b)** fix violations — prefer **(b)** first if the guard expansion would explode scope.

**Acceptance:** Guard test documents forbidden patterns in its class XML doc; `dotnet test` passes.

---

## Objective / assumptions / constraints

| | |
|--|--|
| **Objective** | Improvements 3–6 reduce cognitive load, improve traceability, explainability, and API usability. |
| **Assumptions** | **`ArchLucid.sln`** is the product entry; Sql persistence available for optional integration tests. |
| **Constraints** | Historical SQL migrations **001–028** frozen; **Phase 7.5** is incremental (**root `infra/terraform` APIM** may already use **`moved`** blocks — see **`infra/terraform/moved_archlucid_apim.tf`**); **`infra/terraform-monitoring`** and checklist **§7.6–7.8** may remain deferred; do not expose SMB/445. |

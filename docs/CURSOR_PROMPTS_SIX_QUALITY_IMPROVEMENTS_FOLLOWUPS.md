> **Scope:** Cursor prompts — follow-ups to the six quality improvements - full detail, tables, and links in the sections below.

# Cursor prompts — follow-ups to the six quality improvements

**Purpose:** Paste-ready **second-wave** prompts that assume the primary work from [`CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md`](CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md) is either **done** or **stable**, and you want to deepen correctness, operability, or governance without re-running the whole original brief.

**How to use:** Open a new Agent chat, paste **one** fenced block. One logical batch per session unless the user expands scope. Respect `.cursor/rules/ArchLucid-Rename.mdc` and **never rewrite historical SQL migrations 001–028** — only `ArchLucid.sql` + **new** migrations when schema changes are required.

**Suggested pairing:** Each **Follow-up N** maps to **Prompt N** in the parent doc.

---

## Follow-up 1 — k6 artifacts as regression signals + flake triage playbook

```
You are working in the ArchLucid repo.

**Goal:** Treat k6 summary JSON as a **first-class regression signal** beyond merge-blocking pass/fail: store comparable metrics across runs, document flake triage, and optionally wire a lightweight comparison step for PR vs main (or weekly burst vs CI smoke).

**Prerequisites (verify):**
- `tests/load/ci-smoke.js`, `tests/load/k6-api-smoke.js`, and (if present) `tests/load/per-tenant-burst.js` + `scripts/ci/assert_k6_ci_smoke_summary.py`.
- `.github/workflows/ci.yml` k6 jobs and any scheduled k6 workflow(s).
- `docs/LOAD_TEST_BASELINE.md`, `docs/PERFORMANCE.md`.

**What to do:**

1. **Artifact discipline:** Ensure every k6 workflow uploads a **named** summary JSON artifact and that `scripts/ci/print_k6_summary_metrics.py` output is copied into the GitHub **job summary** (idempotent — skip if already there).
2. **Regression helper (optional):** Add `scripts/ci/compare_k6_summary.py` (or extend an existing script) that takes two summary JSON paths and exits non-zero if `http_req_duration` p(95) regresses beyond a **documented** threshold (e.g. +25% relative, with floor/ceiling caps). Document usage in `docs/PERFORMANCE.md` — do **not** make it merge-blocking until baselines exist.
3. **Flake playbook:** Add a short subsection to `docs/PERFORMANCE.md` or `docs/TEST_EXECUTION_MODEL.md`: “If k6-ci-smoke flakes” — checklist: rate limits, SQL cold start, runner contention, Retry-After behavior, link to live E2E `live-api-client` 429 retry patterns if relevant.
4. **Cross-links:** From `docs/API_SLOS.md` (or synthetic probe runbook), reference k6 as **capacity** signal, not SLO substitute, unless product decides otherwise.

**Constraints:**
- Do not point scripts at production URLs by default.
- No `ConfigureAwait(false)` in new tests.

**Done when:** Docs describe how to compare two runs; CI artifacts remain easy to find; optional comparator script is tested on two fixture JSON files under `ArchLucid.Api.Tests` or `scripts/ci` self-test pattern you choose.
```

---

## Follow-up 2 — Golden-set catalog governance + explanation fixtures

```
You are working in the ArchLucid repo.

**Goal:** After structural golden-set tests exist for `ParsedResultJson`, grow a **catalog** with explicit ownership: versioning, naming, and at least one **explanation** / aggregate fixture so explainability regressions are caught without LLM calls.

**Prerequisites (verify):**
- `docs/AGENT_OUTPUT_EVALUATION.md`, `docs/AGENT_TRACE_FORENSICS.md`.
- Existing fixtures under `ArchLucid.AgentRuntime.Tests/Fixtures/` (or equivalent) + evaluator tests.

**What to do:**

1. **Catalog doc:** Add `docs/testing/AGENT_EVALUATION_GOLDEN_CATALOG.md` (or a subsection in `AGENT_OUTPUT_EVALUATION.md`) listing each fixture: intent, last updated date, which evaluator(s) consume it, and “must fail if …” invariant in one line.
2. **New fixtures:** Add 1–2 explanation or aggregate JSON fixtures (deterministic) that exercise faithfulness / schema validation paths already in product code — no network.
3. **Tests:** Wire fixtures into tests that mirror the structural golden pattern; keep runtime small.
4. **CI policy:** Document whether golden-set tests are **Tier 2** always-on or a **label** — match actual `ci.yml` / `TEST_STRUCTURE.md`.

**Constraints:**
- Do not add LLM calls in CI for this task.
- Prefer extending existing evaluators over new packages.

**Done when:** Catalog lists every golden file; new tests fail if a field is stripped intentionally; docs match CI tier.
```

---

## Follow-up 3 — Audit operator ergonomics: saved views + export parity

```
You are working in the ArchLucid repo.

**Goal:** Deepen `/audit` after search filters exist: **saved filter presets** (localStorage or profile API if one exists — prefer local-only unless a backend contract already exists), and **export parity** so CSV/JSON export uses the **same** query parameters as on-screen search (no hidden divergence).

**Prerequisites (verify):**
- `archlucid-ui/src/app/audit/` and `GET /v1/audit/search` / export routes in `ArchLucid.Api`.
- `docs/AUDIT_COVERAGE_MATRIX.md`, `docs/API_CONTRACTS.md`.

**What to do:**

1. **UI:** Implement 2–3 named presets (e.g. “Governance”, “Runs I touched”) mapping to known filter combinations; clear UX for reset; a11y preserved.
2. **Export:** Trace server-side export code path — ensure parameters match search; add an API test that export + search return consistent row counts for the same scope (within documented `maxRows` caps).
3. **Docs:** Update `docs/operator-shell.md` audit subsection; add one row to the matrix if new audit **event types** are surfaced in UI filters.

**Constraints:**
- Do not log raw PII in new client-side debug statements.
- Export must remain bounded (existing caps).

**Done when:** Manual operator checklist in doc is 5 steps or fewer; tests prove search/export parity for at least one representative filter set.
```

---

## Follow-up 4 — Phase 7.5 dry-run: disposable workspace + automated inventory

```
You are working in the ArchLucid repo.

**Goal:** De-risk Phase 7.5 (`terraform state mv`) by (1) maintaining an **automated inventory** of legacy Terraform **resource addresses** still containing `archiforge` (or agreed pattern), and (2) documenting a **dry-run** sequence for a disposable workspace clone — **no** remote state mutation in CI.

**Prerequisites (verify):**
- `docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md`, `docs/ARCHLUCID_RENAME_CHECKLIST.md` §7.5–7.8.
- `infra/**` Terraform roots.

**What to do:**

1. **Inventory script:** Add `scripts/ci/assert_terraform_legacy_address_inventory.py` (or extend an existing guard) that fails CI only when **new** `archiforge`-addressed resources appear without an allowlist entry (allowlist shrinks over time as moves complete — document policy).
2. **Runbook:** Append a “Dry-run lab” section: create temp clone, `terraform init`, `terraform state list | …`, apply **one** `state mv` template, `terraform plan` must be clean — all **local** or sandbox subscription the user owns.
3. **Checklist:** Update Phase 7.5 checkbox notes with “inventory script added” and link.

**Constraints:**
- Do **not** run `terraform apply` / state mutation against shared production state in automation.
- One batch per session: script **or** runbook expansion if large.

**Done when:** CI or local `pre-commit` path is documented; runbook has dry-run lab; checklist cross-links.
```

---

## Follow-up 5 — Coverage: per-hotspot file map + FsCheck shrinking budget

```
You are working in the ArchLucid repo.

**Goal:** After global coverage gates exist, publish a **maintainer-facing map** of the lowest-covered **hotspot files** (above floor but risky), and tighten **FsCheck** tests with explicit **size** / iteration caps so `Suite=Core` stays fast.

**Prerequisites (verify):**
- `scripts/ci/assert_merged_line_coverage_min.py`, `docs/CODE_COVERAGE.md`.
- Existing FsCheck tests and `Directory.Packages.props` package versions.

**What to do:**

1. **Map:** From the latest Cobertura artifact (or documented local command), generate `docs/testing/COVERAGE_HOTSPOTS.md` listing top N **product** `.cs` files below a chosen threshold (e.g. 10 points above floor) with owner suggestions “Decisioning | Persistence | …”.
2. **FsCheck:** Audit properties for unbounded generators; add `Arb.Default.WithSize(...)` or equivalent where needed; document default size in `docs/TEST_STRUCTURE.md`.
3. **Optional:** Add one **targeted** unit test file for the worst hotspot if it is a pure function — do not boil the ocean.

**Constraints:**
- No `ConfigureAwait(false)` in tests.
- Do not lower coverage floors in this prompt; only clarify or raise with justification.

**Done when:** Hotspot doc exists and is linked from `CODE_COVERAGE.md` or `TEST_STRUCTURE.md`; FsCheck runtime stable in local `dotnet test` sample.
```

---

## Follow-up 6 — RLS + keys operational drills (tabletop + automation hooks)

```
You are working in the ArchLucid repo.

**Goal:** Turn `RLS_RISK_ACCEPTANCE.md` and `API_KEY_ROTATION.md` into **executable drills**: a tabletop checklist for security/engineering pairs, plus minimal **automation hooks** (scripts or CI **manual** workflow) that verify read paths still enforce scope after rotation **simulations**.

**Prerequisites (verify):**
- `docs/security/RLS_RISK_ACCEPTANCE.md`, `docs/runbooks/API_KEY_ROTATION.md`.
- `ArchLucid.Api.Tests` or integration tests for RLS / API key if present.

**What to do:**

1. **Tabletop doc:** Add `docs/runbooks/DRILL_RLS_AND_API_KEYS.md` with quarterly steps: verify RLS on for env, attempt cross-tenant read negative test, rotate overlapping keys on staging, grep for deprecated key usage.
2. **Automation hook:** Add a **workflow_dispatch-only** GitHub workflow (or `dotnet test` filter target) that runs a **small** subset of tests tagged for RLS/API key — document secrets required; default skip if secrets missing.
3. **Cross-links:** `SECURITY.md` → drill doc; threat model row for “operator key leak” → rotation runbook.

**Constraints:**
- Do not weaken Production guards or enable RLS bypass by default.
- Never expose SMB/445 publicly (repo security rule).

**Done when:** Drill doc exists; optional workflow is opt-in and documented; no secrets committed.
```

---

## Follow-up 7 (cross-cutting) — Live E2E + k6 coherence

```
You are working in the ArchLucid repo.

**Goal:** Align **Playwright live-api** helpers and **k6** load scripts on shared semantics: base URL env vars, rate-limit expectations, and 429/retry policy **documentation** so operators and CI maintainers see one story.

**Prerequisites (verify):**
- `archlucid-ui/e2e/helpers/live-api-client.ts`, `docs/LIVE_E2E_HAPPY_PATH.md`.
- `tests/load/*.js`, `docs/PERFORMANCE.md`, `.github/workflows/ci.yml` (k6 + live jobs).

**What to do:**

1. **Doc table:** Add a small matrix to `docs/TEST_EXECUTION_MODEL.md` (or `LIVE_E2E_HAPPY_PATH.md`): columns **Tool** (Playwright vs k6), **BASE_URL env**, **Auth**, **Rate limit note**, **429 strategy**.
2. **Code comment:** At top of `live-api-client.ts` and one representative k6 script, cross-link to that doc anchor.
3. **No behavior change required** unless you find a real bug; prefer documentation + one trivial comment fix.

**Constraints:**
- Do not point load tests at production.

**Done when:** Single doc section is canonical; both stacks reference it.
```

---

## Suggested order

| Order | Follow-up | Why |
|------|-----------|-----|
| 1 | #7 (coherence) | Cheap; reduces confusion before deeper work. |
| 2 | #4 (Phase 7.5 dry-run) | Reduces rename risk before execution. |
| 3 | #6 (drills) | Governance without code churn. |
| 4 | #1 (k6 regression) | Builds on stable CI k6. |
| 5 | #5 (coverage map) | Data-driven test investment. |
| 6 | #2 (golden catalog) | Scales eval assets safely. |
| 7 | #3 (audit UX) | Product-heavy; schedule when UI bandwidth exists. |

Parallelize **#7 + #6** if two agents are available.

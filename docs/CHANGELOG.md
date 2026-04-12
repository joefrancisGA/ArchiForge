# ArchLucid changelog

Release entries newest-first. Each section condenses the detailed prompt logs preserved in `docs/archive/`.

> **Design-session logs:** The full incremental prompt records live in
> `docs/archive/CHANGE_SET_55R_SUMMARY.md` through `CHANGE_SET_59R.md`.
> Read those when you need exact delivery scope or deferred-backlog decisions.

---

## 2026-04-12 — Audit export and retention policy

Added audit export endpoint (`GET /v1/audit/export`) with CSV/JSON support and 90-day range limit. Created audit retention policy document (`docs/AUDIT_RETENTION_POLICY.md`). Database-enforced append-only on `dbo.AuditEvents` (Migration **051**).

---

## 2026-04-12 — CI hardening

CI hardening: Simmy chaos tests now block PRs (burn-in complete). Per-package line coverage gate raised from 50% to 60%.

Added Schemathesis API fuzz testing as a scheduled CI workflow against the OpenAPI spec. Operator docs: `docs/API_FUZZ_TESTING.md`; execution model and test matrix updated for Tier 4 (ZAP + Schemathesis).

---

## 2026-04-12 — Aggregate run explanation

Added aggregate run explanation endpoint (`/v1/explain/runs/{runId}/aggregate`) with theme summaries, risk posture, confidence score, and explanation provenance. Surfaced in run detail UI.

---

## Phase 7 — ArchLucid rename (code-level)

**Area:** Rename / operator breaking changes  
**Summary:** Removed legacy **`ArchiForge*`** configuration keys, **`ARCHIFORGE_*`** / UI OIDC storage bridges, and renamed CLI manifest (`archlucid.json`), global tool command (`archlucid`), SQL DDL file (`ArchLucid.sql`), and dev Docker/compose defaults. **`com.archiforge.*` integration event type strings are no longer emitted or aliased** — only canonical **`com.archlucid.*`** types apply. See **`BREAKING_CHANGES.md`** for migration steps. Terraform resource **addresses** using the historical **`archiforge`** token remain until a planned `state mv` (checklist 7.5); the APIM backend URL **variable** is now **`archlucid_api_backend_url`**.

---

## 59R — Learning-to-planning bridge

**Area:** Product learning / planning  
**Key deliverables:**

- `032_ProductLearningPlanningBridge.sql` (DbUp) + `ArchLucid.sql` parity — SQL tables for improvement themes, plans, and junction links to runs/signals/artifacts.
- Contracts under `ArchLucid.Contracts/ProductLearning/Planning/`.
- `IProductLearningPlanningRepository`, Dapper + in-memory implementations, DI registration.
- Unit tests: `ProductLearningPlanningRepositoryTests`.
- Docs: `SQL_SCRIPTS.md`, `DATA_MODEL.md`, this file.

**Intentionally deferred:** deterministic theme-derivation service, plan-draft builder with priority score.

---

## 58R — Product learning dashboard and improvement triage

**Area:** Operator tooling / product feedback  
**Key deliverables:**

- `ProductLearningPilotSignals` SQL table + Dapper and in-memory repositories.
- Aggregation services: `IProductLearningFeedbackAggregationService`, `IProductLearningImprovementOpportunityService`, `IProductLearningDashboardService`.
- HTTP API: `GET /v1/product-learning/summary`, `/improvement-opportunities`, `/artifact-outcome-trends`, `/triage-queue`, `/report` (Markdown/JSON).
- Operator UI: **Pilot feedback** page (`/product-learning`), export links.
- Tests: aggregation, ranking, parser, API, report-builder (`ChangeSet=58R` / `ProductLearning` filter tags).
- Docs: `PRODUCT_LEARNING.md`; updated `PILOT_GUIDE.md`, `OPERATOR_QUICKSTART.md`, `README.md`.

**Constraints:** No autonomous adaptation; human-entered signals only; scoped to tenant/workspace/project.

---

## 57R — Operator-journey E2E (Playwright)

**Area:** UI test harness  
**Key deliverables:**

- `e2e/fixtures/` — typed JSON payloads aligned with all UI coercion helpers.
- `e2e/helpers/route-match.ts`, `register-operator-api-routes.ts`, `operator-journey.ts` — centralised route dispatch and journey navigation.
- Specs: `smoke`, `compare-proxy-mock`, `run-manifest-journey`, `compare-journey`, `compare-stale-input-warning`, `manifest-empty-artifacts`.
- `e2e/mock-archlucid-api-server.ts` + `e2e/start-e2e-with-mock.ts` — loopback HTTP mock on port 18765 for RSC pages; `playwright.config.ts` `webServer` updated.
- `tsx` devDependency for TS mock runner; `e2e/tsconfig.json` + `npm run typecheck:e2e`.
- `-RunPlaywright` flag added to `release-smoke.ps1` / `.cmd`.
- Docs: `archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md` (section 8 rewritten).

---

## 56R — Release-candidate hardening and pilot readiness

**Area:** Configuration, observability, packaging, operator docs  
**Key deliverables:**

- Fail-fast config validation (`ArchLucidConfigurationRules`) before DbUp; SQL connection required only when `StorageProvider=Sql`.
- `/health/live` (minimal) + `/health/ready` (DB, schema files, compliance pack, writable temp) + `/health` (all) with `DetailedHealthCheckResponseWriter` (enriched JSON including `version`, `commitSha`, `totalDurationMs`).
- Startup non-secret configuration snapshot log (toggle `Hosting:LogStartupConfigurationSummary`).
- `GET /version` endpoint (`VersionController`, `[AllowAnonymous]`): `application`, `informationalVersion`, `assemblyVersion`, `fileVersion`, `commitSha`, `runtimeFramework`, `environment`.
- `BuildProvenance` + `BuildInfoResponse` (Core): parses `CommitSha` from `+{sha}` suffix of informational version; CI stamps `SourceRevisionId=$(git rev-parse HEAD)`.
- API `ProblemSupportHints` (`extensions.supportHint`); CLI `CliOperatorHints` (`Next:` lines); UI proxy `502/503 supportHint`.
- `archlucid support-bundle` CLI command (folder + optional `--zip`): `README.txt`, `manifest.json` (v1.1 + `triageReadOrder`), `build.json`, `health.json`, `api-contract.json` (bounded OpenAPI probe), `config-summary.json`, `environment.json`, `workspace.json`, `references.json`, `logs.json`.
- Local scripts: `build-release`, `package-release`, `run-readiness-check`, `release-smoke` (`.cmd` + `.ps1`); `scripts/OperatorDiagnostics.ps1` (structured triage output).
- Release handoff artifacts in `artifacts/release/`: `metadata.json` (schema 1.1), `release-manifest.json`, `checksums-sha256.txt`, `PACKAGE-HANDOFF.txt`.
- Docs added: `PILOT_GUIDE.md`, `OPERATOR_QUICKSTART.md`, `TROUBLESHOOTING.md`, `RELEASE_LOCAL.md`, `RELEASE_SMOKE.md`, `CLI_USAGE.md`.

---

## 55R — Operator shell coherence

**Area:** UI shell  
**Key deliverables:**

- Shared navigation, breadcrumbs, and operator messaging patterns across home, runs, run/manifest detail, graph, compare, replay, and artifact review.
- Canonical manifest-scoped artifact URLs; `GET /runs/{runId}/artifacts/{artifactId}` resolves manifest then redirects.
- Compare page: sequential legacy-then-structured fetches; UI explains fetch order vs. on-page review order; optional AI explanation; stale-input warning when run IDs drift.
- Coercion/guard helpers for operator-facing JSON.
- Vitest smoke coverage: API wiring (list/descriptor/compare/explain), shell nav, key review components.

---

## How to add a changelog entry

1. Add a new `## <version> — <title>` section **above** the previous one.
2. Use the subsections: **Area**, **Key deliverables**, and (optionally) **Intentionally deferred**.
3. Keep entries to a navigable summary; put fine-grained prompt records in a new `docs/archive/CHANGE_SET_<id>.md` file and link from here.

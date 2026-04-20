> **Scope:** Test execution model (54R — release readiness) - full detail, tables, and links in the sections below.

# Test execution model (54R — release readiness)

This document is the **canonical reference** for how the ArchLucid product codebase (`ArchLucid.*` assemblies) classifies and runs automated tests. It aligns local scripts, contributor docs, and CI behavior.

**See also:** [TEST_STRUCTURE.md](TEST_STRUCTURE.md) (**54R operator cheat sheet** — copy-paste commands), [BUILD.md](BUILD.md) (SQL Server setup for tests), [API_FUZZ_TESTING.md](API_FUZZ_TESTING.md) (scheduled Schemathesis OpenAPI fuzz), [RELEASE_LOCAL.md](RELEASE_LOCAL.md) (**56R** — `build-release`, `package-release`, `run-readiness-check`), [RELEASE_SMOKE.md](RELEASE_SMOKE.md) (**56R** — `release-smoke` E2E gate).

> **Canonical entry point (2026-04-20).** Every tier below can be invoked from the repo root with the consolidated driver: **`.\test.ps1 -Tier <name>`** (PowerShell) or **`test.cmd <name>`** (cmd trampoline). Tier names: `Core`, `FastCore`, `Integration`, `SqlServerIntegration`, `Full`, `UiUnit`, `UiSmoke`, `Slow`. Run **`.\test.ps1 -ListTiers`** for the full list. The legacy `test-<tier>.cmd` / `test-<tier>.ps1` scripts still exist as **shims** that delegate to the consolidated driver and are scheduled for removal **after 2026-Q3** — new docs and runbooks should call the consolidated driver directly.

---

## Objectives

- **Predictable gates:** Everyone uses the same names (`Core`, `Fast core`, `Integration`, `SQL Server integration`, `Full regression`, `Operator UI unit`, `Operator UI e2e smoke`).
- **Fail-fast locally:** Run the smallest meaningful subset before pushing.
- **Authoritative CI:** The pipeline enforces **full regression** against SQL Server (**Dapper** + **DbUp**; **no Entity Framework** in tests or product DB path).
- **No product churn:** This change set only defines execution and documentation unless a test must be stabilized.

---

## Suite definitions

### 1. Core suite (“corset”)

**Meaning:** Curated **high-value regression** tests marked at **class** level. This is the smallest **intentional** product-critical belt (informally a “corset” around the release).

| Mechanism | xUnit trait |
|-----------|-------------|
| Filter | `Suite=Core` |

**Run (repo root):**

```bash
dotnet test ArchLucid.sln --filter "Suite=Core"
```

**Scripts:** `test-core.cmd` / `test-core.ps1`

**Notes:**

- **GitHub Actions** job **“.NET: fast core (corset)”** runs the same filter as the Fast core subset below (`Suite=Core&Category!=Slow&Category!=Integration`), which **includes** `OpenApiContractSnapshotTests` — any OpenAPI drift fails CI until the snapshot is regenerated.
- Not every test in the solution is (or should be) in Core. Adding `Suite=Core` is a deliberate choice.
- Some Core classes are also tagged `Category=Integration` or `Category=Slow`; they still run in the full Core filter.
- **OpenAPI contract snapshot:** `OpenApiContractSnapshotTests` (`ArchLucid.Api.Tests`, `Suite=Core`) compares live `GET /openapi/v1.json` (Microsoft `MapOpenApi` document) to `Contracts/openapi-v1.contract.snapshot.json` after `OpenApiJsonCanonicalizer` (stable across Windows/Linux). **Regenerate after intentional API surface changes:** `ARCHLUCID_UPDATE_OPENAPI_SNAPSHOT=1 dotnet test ArchLucid.Api.Tests --filter OpenApiContractSnapshotTests` (from repo root), then commit the updated JSON. Operator-oriented narrative: [OPENAPI_CONTRACT_DRIFT.md](OPENAPI_CONTRACT_DRIFT.md).

---

### 2. Fast core subset

**Meaning:** Core tests that are **not** marked slow and **not** full-stack HTTP integration. Use for quick feedback before a push.

| Mechanism | xUnit filter |
|-----------|----------------|
| Filter | `Suite=Core&Category!=Slow&Category!=Integration` |

**Run:**

```bash
dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

**Scripts:** `test-fast-core.cmd` / `test-fast-core.ps1`

**Does not include:**

- `WebApplicationFactory` / HTTP integration tests (`Category=Integration`).
- Tests tagged `Category=Slow`.

**May still require:** Local tools or optional services depending on which Core classes you hit; most Fast Core tests are in-memory or unit-style.

---

### 3. Integration suite (HTTP / host)

**Meaning:** Tests that drive the **real API host** (e.g. `WebApplicationFactory`), external HTTP semantics, or other **Integration**-category coverage.

| Mechanism | xUnit trait |
|-----------|-------------|
| Filter | `Category=Integration` |

**Run:**

```bash
dotnet test ArchLucid.sln --filter "Category=Integration"
```

**Scripts:** `test-integration.cmd` / `test-integration.ps1`

**Requires:** SQL Server available to **ArchLucid.Api.Tests** per [BUILD.md](BUILD.md) (local default `localhost`; CI uses the service container and per-factory databases).

---

### 4. SQL Server–first integration (Dapper / Persistence)

**Meaning:** Tests that assume **real SQL Server** (container or LocalDB), **Dapper**, and **DbUp** migrations — **no Entity Framework**. These validate persistence round-trips and relational behavior.

| Mechanism | xUnit trait |
|-----------|-------------|
| Filter | `Category=SqlServerContainer` |
| Primary project | `ArchLucid.Persistence.Tests` |

**Run:**

```bash
dotnet test ArchLucid.Persistence.Tests --filter "Category=SqlServerContainer"
```

**Scripts:** `test-sqlserver-integration.cmd` / `test-sqlserver-integration.ps1`

**Requires:** `ARCHLUCID_SQL_TEST` set to a full ADO.NET connection string (see [BUILD.md](BUILD.md)), or Windows LocalDB. Resolution is centralized in **`ArchLucid.TestSupport`**. If SQL is unavailable, tests **skip** via `SkippableFact` / fixture checks where implemented.

This path is the **default** for “did we break Dapper + SQL DDL + migrations?” without spinning the full API host.

---

### 5. Full regression

**Meaning:** **Entire solution** test run — all projects, all traits unless skipped by the test framework.

**Run:**

```bash
dotnet test ArchLucid.sln
```

**Scripts:** `test-full.cmd` / `test-full.ps1`

**CI:** GitHub Actions runs this (Release configuration, with `ARCHLUCID_SQL_TEST` for Persistence tests). This is the **.NET release gate** alongside **Vitest** and **Playwright** UI jobs.

**Local parity:** `scripts/run-full-regression-docker-sql.ps1` / `.sh` starts Compose SQL and sets `ARCHLUCID_SQL_TEST` to the dev password from `docker-compose.yml` (see [BUILD.md](BUILD.md)).

---

### 6. Operator shell unit (Next.js + Vitest)

**Meaning:** **Fast, deterministic** component and pure-function tests for **archlucid-ui** (React Testing Library + jsdom). No browser install; suitable for every PR and local iteration.

| Mechanism | Location |
|-----------|----------|
| Runner | **Vitest** |
| Config | `archlucid-ui/vitest.config.ts`, `archlucid-ui/vitest.setup.ts` |
| Specs | `archlucid-ui/src/**/*.test.{ts,tsx}` (and `*.spec.{ts,tsx}` under `src/`) |

**Run (from `archlucid-ui/`):**

```bash
npm ci
npm test                 # one-shot (CI)
npm run test:watch       # local loop
```

**Repo root:** `test-ui-unit.cmd` / `test-ui-unit.ps1`

### 7. Operator shell — Vitest axe (components) and mock Playwright (on demand)

**Meaning:** **Tier 3b** (`ui-axe-components`) runs **Vitest + jest-axe** on a small **`src/accessibility/**`** suite (fast, no browser install). **Mock** Playwright journeys (compare/manifest smoke, etc.) use **`playwright.mock.config.ts`** via **`npm run test:e2e`** when you need them locally or in **`test-ui-smoke`**.

| Mechanism | Location |
|-----------|----------|
| Component axe | **Vitest** + **jest-axe** — `archlucid-ui/src/accessibility/*.test.tsx` |
| Mock browser | **Playwright** — `archlucid-ui/playwright.mock.config.ts`, specs under **`e2e/`** except **`live-api-*.spec.ts`** |
| Live browser (default config) | **Playwright** — `archlucid-ui/playwright.config.ts` — **`live-api-*.spec.ts`** only (see **§ merge gates** below) |

**Run Vitest axe (from `archlucid-ui/`):**

```bash
npm ci
npm run test:axe-components
```

**Run mock Playwright (from `archlucid-ui/`):**

```bash
npm ci
npx playwright install --with-deps chromium
npm run test:e2e
```

**Repo root:** `test-ui-smoke.cmd` / `test-ui-smoke.ps1` (Chromium install + **`npm run test:e2e`**, mock config).

Mock **`webServer`** runs **`npm run build`** and **`e2e/start-e2e-with-mock.ts`** (production Next on port **3000**).

---

## Combining filters (examples)

| Goal | Filter |
|------|--------|
| Everything except SQL container | `Category!=SqlServerContainer` |
| Everything except HTTP integration | `Category!=Integration` |
| Unit-only (where tagged) | `Category=Unit` |

---

## CI mapping (54R)

Workflow: `.github/workflows/ci.yml` — **tiered jobs** for clarity and fail-fast behavior.

| Tier | Job | What runs |
|------|-----|-----------|
| **0** | **`gitleaks`** | Full-history secret scan (`gacts/gitleaks@v1.3.2` + **`.gitleaks.toml`**). All other jobs **`needs: gitleaks`**. |
| **1** | **`dotnet-fast-core`** | Restore, vulnerable package audit, `dotnet build -c Release`, **CycloneDX** SBOM for **`ArchLucid.Api`** (artifact **`sbom-dotnet`**), context-ingestion DI guards, then `dotnet test` with `Suite=Core&Category!=Slow&Category!=Integration`. **No SQL** service (fast gate). |
| **1.5** | **`api-greenfield-boot`** | After Tier **1**. SQL Server service, **`CREATE DATABASE ArchLucidGreenfieldCi`** (empty catalog), **`dotnet run`** **`ArchLucid.Api`** with **`ArchLucid:StorageProvider=Sql`**, wait for **`/health/ready`**, assert **`dbo.SchemaVersions`** has rows (DbUp journal). **Blocking** — catches greenfield startup / DbUp vs bootstrap ordering bugs. See **`GreenfieldSqlBootIntegrationTests`** for the same path via **`WebApplicationFactory`**. |
| **2** | **`dotnet-full-regression`** | Runs **after** Tier **1** and **`api-greenfield-boot`**. Restore, build, SQL Server service container, `dotnet test ArchLucid.sln` with `ARCHLUCID_SQL_TEST` (entire solution). |
| **2b** | **`chaos-tests`** | Runs **after** Tier 2 passes. **Resilience: Simmy chaos tests** — `ArchLucid.AgentRuntime.Tests` and `ArchLucid.Persistence.Tests` filtered to Simmy/Chaos FQNs. **CI-blocking** (failures block the PR). See [CHAOS_TESTING.md](CHAOS_TESTING.md). |
| **3a** | **`ui-unit`** | `archlucid-ui`: `npm ci`, `npm run test` (Vitest / jsdom), **CycloneDX** npm SBOM (artifact **`sbom-npm`**). |
| **3b** | **`ui-axe-components`** | `archlucid-ui`: `npm ci`, **`npm run test:axe-components`** (Vitest + jest-axe on **`src/accessibility/**`**). Fast; no Playwright browsers. |
| **3c** | **`ui-e2e-live`** | After **`dotnet-full-regression`**. SQL service + **`docker run … sqlcmd`** creates empty **`ArchLucidLiveE2e`**. Then **ArchLucid.Api** (DevelopmentBypass) + Playwright **default `playwright.config.ts`**: **full** `live-api-*.spec.ts` suite (operator journeys + live axe). Job **`timeout-minutes: 35`**. **Merge-blocking.** See **[LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)**. |
| **3c′** | **`ui-e2e-live-apikey`** | After **`dotnet-full-regression`**. Separate SQL catalog **`ArchLucidLiveE2eApiKey`**, API with **`ArchLucidAuth:Mode=ApiKey`** + **`Authentication:ApiKey:*`**, env **`LIVE_API_KEY`** / **`LIVE_API_KEY_READONLY`**. Playwright subset: **`live-api-apikey-auth`**, **`live-api-journey`**, **`live-api-negative-paths`**. **`timeout-minutes: 25`**. **Merge-blocking.** See **[LIVE_E2E_AUTH_PARITY.md](LIVE_E2E_AUTH_PARITY.md)**. |
| **3c″** | **`ui-e2e-live-jwt`** | After **`dotnet-full-regression`**. Catalog **`ArchLucidLiveE2eJwt`**, API **`JwtBearer`** + **`JwtSigningPublicKeyPemPath`** + local issuer/audience; OpenSSL + **`mint_ci_jwt.py`**; Playwright env **`LIVE_JWT_TOKEN`** and **`ARCHLUCID_PROXY_BEARER_TOKEN`**. Same subset as **3c′** plus **`live-api-jwt-auth`**. **`continue-on-error: true`** (not a merge gate). See **[LIVE_E2E_JWT_SETUP.md](LIVE_E2E_JWT_SETUP.md)**. |
| **3d** | **`Performance: k6 API smoke (operator path)`** | After **`dotnet-full-regression`**. Create **`ArchLucidK6Smoke`**, start API via **`scripts/ci/start_api_for_k6.sh`** (exports **RLS break-glass** for **`SqlServer:RowLevelSecurity:ApplySessionContext`** in **`appsettings.Advanced.json`**), **native k6** `tests/load/k6-api-smoke.js` (5 VUs ~60s: ready + version + create run + authority runs list), **`assert_k6_ci_smoke_summary.py`** (p95 ≤ 2000 ms, failed rate ≤ 1%), artifact **`k6-smoke-results`** (summary JSON). Rate limits raised for this job. **Merge-blocking.** See **[PERFORMANCE.md](PERFORMANCE.md)** § k6 operator-path smoke, **[PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md)**. |

PRs must pass the **blocking** merge gates (Tier **0–3b**, **2b**, **3c**, **3c′**, and **3d** as wired in `ci.yml`). Tier **3c″** (JWT live subset) runs for signal only. Tier **3c** is the **only** merge-blocking operator **browser** journey gate against **live API + SQL**. Tier 2 (and 2b) are skipped automatically if Tier 1 fails (`needs: dotnet-fast-core`), saving SQL spin-up, full-suite time, and chaos runs on obvious breaks.

### Tier 4 — scheduled security testing (not per-PR)

These workflows run on a **weekly** cron (**Monday 06:00 UTC**) and **`workflow_dispatch`**. They are **not** merge gates for every pull request; runtime is typically **tens of minutes** (image build + scans).

| Tier | Workflow | What runs |
|------|----------|-----------|
| **4a** | **[`zap-baseline-strict-scheduled.yml`](../.github/workflows/zap-baseline-strict-scheduled.yml)** (**Security: ZAP baseline (scheduled, strict visibility)**) | OWASP ZAP **baseline** scan against the API container; strict rules. See [security/ZAP_BASELINE_RULES.md](security/ZAP_BASELINE_RULES.md). |
| **4b** | **[`schemathesis-scheduled.yml`](../.github/workflows/schemathesis-scheduled.yml)** (**Security: Schemathesis API fuzz (scheduled)**) | **Schemathesis** property-based fuzzing from **`/openapi/v1.json`**; JUnit artifact. See [API_FUZZ_TESTING.md](API_FUZZ_TESTING.md). |
| **4c** | **[`stryker-scheduled.yml`](../.github/workflows/stryker-scheduled.yml)** (**Stryker mutation testing (scheduled)**) | **Stryker.NET** per module; asserts score vs **`scripts/ci/stryker-baselines.json`** (**70.0** baseline per label after the latest ratchet, **0.10** pp tolerance). See [MUTATION_TESTING_STRYKER.md](MUTATION_TESTING_STRYKER.md). |
| **4d** | **[`k6-soak-scheduled.yml`](../.github/workflows/k6-soak-scheduled.yml)** (**Performance: k6 soak (scheduled)**) | **k6** `tests/load/soak.js` against **`ARCHLUCID_SOAK_BASE_URL`** (no-op when unset). **Not** a merge gate (`continue-on-error`). See [PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md). |
| **4e** | **[`live-e2e-nightly.yml`](../.github/workflows/live-e2e-nightly.yml)** (**Live E2E nightly**) | **03:30 UTC** (and `workflow_dispatch`): three jobs run the **full** `live-api-*.spec.ts` suite — DevelopmentBypass, ApiKey, and JwtBearer + local PEM (forks skipped). **Not** a per-PR merge gate. See [LIVE_E2E_AUTH_PARITY.md](LIVE_E2E_AUTH_PARITY.md). |

**Follow-on / re-run:** Use the Actions tab to **re-run failed jobs** only (e.g. retry e2e after a flake) without redefining workflows.

**Class-level hygiene:** New test classes should declare **`[Trait("Category", "Unit")]`**, **`Integration`**, **`SqlServerContainer`**, or **`Slow`** as appropriate so Fast core / integration filters stay meaningful.

Optional **local** sequence before a PR (preferred form using the consolidated driver):

1. `.\test.ps1 -Tier FastCore`
2. `.\test.ps1 -Tier SqlServerIntegration` (if you touched Persistence / SQL)
3. `.\test.ps1 -Tier Integration` (if you touched API / HTTP)
4. `.\test.ps1 -Tier UiUnit` or `npm test` in `archlucid-ui/` (if you touched `archlucid-ui` logic/components)
5. `.\test.ps1 -Tier UiSmoke` (if you touched `archlucid-ui` routes/build/e2e-relevant behavior)
6. `.\test.ps1 -Tier Full` before merge (or rely on CI)

> **Legacy form (still works via shims).** The `test-<tier>.cmd` / `test-<tier>.ps1` scripts remain as deprecated shims that forward to the consolidated driver. They will be removed after **2026-Q3**.

---

## Deferred in later 54R prompts

| Item | Status |
|------|--------|
| **Expand Playwright** coverage (navigation, critical flows, a11y) | Add specs under `archlucid-ui/e2e/`. |
| **Pin Node dependency graph further** (e.g. `npm audit fix`, Renovate) | `package-lock.json` is committed for `npm ci` in CI. |
| **Split .NET CI jobs** (parallel `build` vs `test` matrix) | Done: `gitleaks` + `dotnet-fast-core` + `dotnet-full-regression` + `chaos-tests` + `ui-unit` + `ui-axe-components`. |

---

## Constraints (solution)

- **C#** test stack (xUnit).
- **SQL Server + Dapper** for database tests; **no EF**.
- Prefer **small, surgical** changes when adjusting tests or CI.

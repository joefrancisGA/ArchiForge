# Test execution model (54R — release readiness)

This document is the **canonical reference** for how ArchiForge classifies and runs automated tests. It aligns local scripts, contributor docs, and CI behavior.

**See also:** [TEST_STRUCTURE.md](TEST_STRUCTURE.md) (**54R operator cheat sheet** — copy-paste commands), [BUILD.md](BUILD.md) (SQL Server setup for tests), [RELEASE_LOCAL.md](RELEASE_LOCAL.md) (**56R** — `build-release`, `package-release`, `run-readiness-check`).

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
dotnet test ArchiForge.sln --filter "Suite=Core"
```

**Scripts:** `test-core.cmd` / `test-core.ps1`

**Notes:**

- Not every test in the solution is (or should be) in Core. Adding `Suite=Core` is a deliberate choice.
- Some Core classes are also tagged `Category=Integration` or `Category=Slow`; they still run in the full Core filter.

---

### 2. Fast core subset

**Meaning:** Core tests that are **not** marked slow and **not** full-stack HTTP integration. Use for quick feedback before a push.

| Mechanism | xUnit filter |
|-----------|----------------|
| Filter | `Suite=Core&Category!=Slow&Category!=Integration` |

**Run:**

```bash
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
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
dotnet test ArchiForge.sln --filter "Category=Integration"
```

**Scripts:** `test-integration.cmd` / `test-integration.ps1`

**Requires:** SQL Server available to **ArchiForge.Api.Tests** per [BUILD.md](BUILD.md) (local default `localhost`; CI uses the service container and per-factory databases).

---

### 4. SQL Server–first integration (Dapper / Persistence)

**Meaning:** Tests that assume **real SQL Server** (container or LocalDB), **Dapper**, and **DbUp** migrations — **no Entity Framework**. These validate persistence round-trips and relational behavior.

| Mechanism | xUnit trait |
|-----------|-------------|
| Filter | `Category=SqlServerContainer` |
| Primary project | `ArchiForge.Persistence.Tests` |

**Run:**

```bash
dotnet test ArchiForge.Persistence.Tests --filter "Category=SqlServerContainer"
```

**Scripts:** `test-sqlserver-integration.cmd` / `test-sqlserver-integration.ps1`

**Requires:** `ARCHIFORGE_SQL_TEST` set to a full ADO.NET connection string (see [BUILD.md](BUILD.md)), or Windows LocalDB. Resolution is centralized in **`ArchiForge.TestSupport`**. If SQL is unavailable, tests **skip** via `SkippableFact` / fixture checks where implemented.

This path is the **default** for “did we break Dapper + SQL DDL + migrations?” without spinning the full API host.

---

### 5. Full regression

**Meaning:** **Entire solution** test run — all projects, all traits unless skipped by the test framework.

**Run:**

```bash
dotnet test ArchiForge.sln
```

**Scripts:** `test-full.cmd` / `test-full.ps1`

**CI:** GitHub Actions runs this (Release configuration, with `ARCHIFORGE_SQL_TEST` for Persistence tests). This is the **.NET release gate** alongside **Vitest** and **Playwright** UI jobs.

---

### 6. Operator shell unit (Next.js + Vitest)

**Meaning:** **Fast, deterministic** component and pure-function tests for **archiforge-ui** (React Testing Library + jsdom). No browser install; suitable for every PR and local iteration.

| Mechanism | Location |
|-----------|----------|
| Runner | **Vitest** |
| Config | `archiforge-ui/vitest.config.ts`, `archiforge-ui/vitest.setup.ts` |
| Specs | `archiforge-ui/src/**/*.test.{ts,tsx}` (and `*.spec.{ts,tsx}` under `src/`) |

**Run (from `archiforge-ui/`):**

```bash
npm ci
npm test                 # one-shot (CI)
npm run test:watch       # local loop
```

**Repo root:** `test-ui-unit.cmd` / `test-ui-unit.ps1`

### 7. Operator shell e2e smoke (Next.js + Playwright)

**Meaning:** A **minimal** browser check that the **archiforge-ui** app builds and the home route renders expected headings. Slower than Vitest; not a replacement for manual UX review.

| Mechanism | Location |
|-----------|----------|
| Runner | **Playwright** (`@playwright/test`) |
| Config | `archiforge-ui/playwright.config.ts` |
| Specs | `archiforge-ui/e2e/*.spec.ts` |

**Run (from `archiforge-ui/`):**

```bash
npm ci
npx playwright install --with-deps chromium
npm run test:e2e
```

**Repo root:** `test-ui-smoke.cmd` / `test-ui-smoke.ps1` (same Chromium install flags as CI).

The config’s `webServer` runs `npm run build && npm run start` (production server on port 3000) unless `CI` is unset and a server is already running (`reuseExistingServer`).

---

## Combining filters (examples)

| Goal | Filter |
|------|--------|
| Everything except SQL container | `Category!=SqlServerContainer` |
| Everything except HTTP integration | `Category!=Integration` |
| Unit-only (where tagged) | `Category=Unit` |

---

## CI mapping (54R)

Workflow: `.github/workflows/ci.yml` — **four jobs**, tiered for clarity and fail-fast behavior.

| Tier | Job | What runs |
|------|-----|-----------|
| **1** | **`dotnet-fast-core`** | Restore, vulnerable package audit, `dotnet build -c Release`, context-ingestion DI guards, then `dotnet test` with `Suite=Core&Category!=Slow&Category!=Integration`. **No SQL** service (fast gate). |
| **2** | **`dotnet-full-regression`** | Runs **after** Tier 1 passes. Restore, build, SQL Server service container, `dotnet test ArchiForge.sln` with `ARCHIFORGE_SQL_TEST` (entire solution). |
| **3a** | **`ui-unit`** | `archiforge-ui`: `npm ci`, `npm run test` (Vitest / jsdom). **Parallel** with Tier 1 on the same workflow event. |
| **3b** | **`ui-e2e-smoke`** | `archiforge-ui`: `npm ci`, Playwright Chromium, `npx playwright test` (build + start via Playwright `webServer`). **Parallel** with other jobs; browser-heavy. |

PRs must pass **all four** jobs. Tier 2 is skipped automatically if Tier 1 fails (`needs: dotnet-fast-core`), saving SQL spin-up and full-suite time on obvious breaks.

**Follow-on / re-run:** Use the Actions tab to **re-run failed jobs** only (e.g. retry e2e after a flake) without redefining workflows.

**Class-level hygiene:** New test classes should declare **`[Trait("Category", "Unit")]`**, **`Integration`**, **`SqlServerContainer`**, or **`Slow`** as appropriate so Fast core / integration filters stay meaningful.

Optional **local** sequence before a PR:

1. `test-fast-core.cmd`
2. `test-sqlserver-integration.cmd` (if you touched Persistence / SQL)
3. `test-integration.cmd` (if you touched API / HTTP)
4. `test-ui-unit.cmd` or `npm test` in `archiforge-ui/` (if you touched `archiforge-ui` logic/components)
5. `test-ui-smoke.cmd` (if you touched `archiforge-ui` routes/build/e2e-relevant behavior)
6. `test-full.cmd` before merge (or rely on CI)

---

## Deferred in later 54R prompts

| Item | Status |
|------|--------|
| **Expand Playwright** coverage (navigation, critical flows, a11y) | Add specs under `archiforge-ui/e2e/`. |
| **Pin Node dependency graph further** (e.g. `npm audit fix`, Renovate) | `package-lock.json` is committed for `npm ci` in CI. |
| **Split .NET CI jobs** (parallel `build` vs `test` matrix) | Done: `dotnet-fast-core` + `dotnet-full-regression` + `ui-unit` + `ui-e2e-smoke`. |

---

## Constraints (solution)

- **C#** test stack (xUnit).
- **SQL Server + Dapper** for database tests; **no EF**.
- Prefer **small, surgical** changes when adjusting tests or CI.

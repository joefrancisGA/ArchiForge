# Test execution model (54R — release readiness)

This document is the **canonical reference** for how ArchiForge classifies and runs automated tests. It aligns local scripts, contributor docs, and CI behavior.

**See also:** [TEST_STRUCTURE.md](TEST_STRUCTURE.md) (project-level detail), [BUILD.md](BUILD.md) (SQL Server setup for tests).

---

## Objectives

- **Predictable gates:** Everyone uses the same names (`Core`, `Fast core`, `Integration`, `SQL Server integration`, `Full regression`, `Operator UI smoke`).
- **Fail-fast locally:** Run the smallest meaningful subset before pushing.
- **Authoritative CI:** The pipeline enforces **full regression** against SQL Server (Dapper / real DB), not ORMs.
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

**CI:** GitHub Actions runs this (Release configuration, with `ARCHIFORGE_SQL_TEST` for Persistence tests). This is the **.NET release gate** alongside the operator UI job below.

---

### 6. Operator shell smoke (Next.js + Playwright)

**Meaning:** A **minimal** browser check that the **archiforge-ui** app builds and the home route renders expected headings. Not a replacement for manual UX review.

| Mechanism | Location |
|-----------|----------|
| Runner | **Playwright** (`@playwright/test`) |
| Config | `archiforge-ui/playwright.config.ts` |
| Specs | `archiforge-ui/e2e/*.spec.ts` |

**Run (from `archiforge-ui/`):**

```bash
npm ci
npx playwright install chromium   # local dev; CI uses --with-deps
npm run test:e2e
```

**Repo root scripts:** `test-ui-smoke.cmd` / `test-ui-smoke.ps1`

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

| Job / stage | What runs |
|-------------|-----------|
| **`build`** | Restore, vulnerable package audit, `dotnet build -c Release`, context-ingestion DI guards. |
| **Test — fast core** | `dotnet test` with `Suite=Core&Category!=Slow&Category!=Integration` (**fail-fast**, no SQL service required for this filter). |
| **Test — full regression** | `dotnet test ArchiForge.sln` with `ARCHIFORGE_SQL_TEST` → SQL Server service container. |
| **`operator-ui-smoke`** | `archiforge-ui`: `npm install`, Playwright Chromium, `npx playwright test` (build + start via Playwright `webServer`). |

PRs must pass **both** jobs. Fast core runs before full regression in the same job so obvious breaks fail without waiting for SQL-heavy suites.

**Class-level hygiene:** New test classes should declare **`[Trait("Category", "Unit")]`**, **`Integration`**, **`SqlServerContainer`**, or **`Slow`** as appropriate so Fast core / integration filters stay meaningful.

Optional **local** sequence before a PR:

1. `test-fast-core.cmd`
2. `test-sqlserver-integration.cmd` (if you touched Persistence / SQL)
3. `test-integration.cmd` (if you touched API / HTTP)
4. `test-ui-smoke.cmd` (if you touched `archiforge-ui`)
5. `test-full.cmd` before merge (or rely on CI)

---

## Deferred in later 54R prompts

| Item | Status |
|------|--------|
| **Expand Playwright** coverage (navigation, critical flows, a11y) | Add specs under `archiforge-ui/e2e/`. |
| **Pin Node dependency graph further** (e.g. `npm audit fix`, Renovate) | `package-lock.json` is committed for `npm ci` in CI. |
| **Split .NET CI jobs** (parallel `build` vs `test` matrix) | Optional latency win; current workflow is two jobs (`build` + `operator-ui-smoke`). |

---

## Constraints (solution)

- **C#** test stack (xUnit).
- **SQL Server + Dapper** for database tests; **no EF**.
- Prefer **small, surgical** changes when adjusting tests or CI.

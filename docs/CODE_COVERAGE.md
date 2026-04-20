> **Scope:** Code coverage (CI and local) - full detail, tables, and links in the sections below.

# Code coverage (CI and local)

## Objective

Describe how **line/branch coverage** is collected in CI and how to reproduce reports locally.

## Strict profile (product target)

The long-term merge-blocking target (ratchet goal) is:

- **Merged line ≥ 79%**
- **Merged branch ≥ 63%**
- **Per-product-package line ≥ 63%** for every gated **`ArchLucid.*`** assembly with coverable lines

**Compliance status:** **`.github/workflows/ci.yml`** (job **`.NET: full regression (SQL)`**) enforces the **strict profile** below on merged Cobertura. **PRs may fail** until merged line, merged branch, and every gated product **`ArchLucid.*`** package meet the floors (including **`ArchLucid.Jobs.Cli`** — no per-package skip). Before this ratchet, a typical green run was approximately **76.2%** merged line and **60.4%** merged branch with **`ArchLucid.Api`** near **~60%** per-package line — expect **`dotnet-full-regression`** to fail until tests lift those numbers.

**Measured snapshot (local, 2026-04-20).** A **Release** `dotnet test ArchLucid.sln --settings coverage.runsettings --collect:"XPlat Code Coverage"` run on a Windows developer machine, merged with **`dotnet tool run reportgenerator`** into **`coverage-report-final/Cobertura.xml`**, produced:

| Scope | Value | Notes |
|-------|------:|-------|
| **Merged line** | **72.95%** | Below strict **79%** floor — **not** CI-equivalent (see below). |
| **Merged branch** | **58.71%** | Below strict **63%** floor. |
| **`ArchLucid.Api` line** | **60.79%** | Below per-package **63%** floor; still well below the Improvement 4 aspirational **79%** uplift target. |
| **`ArchLucid.Persistence` line** | **39.66%** | Below per-package **63%** floor (unchanged gap vs prior notes). |

**Caveats for this snapshot:** fifteen tests failed on the same machine (**13** in **`ArchLucid.Api.Tests`**, one each in **`ArchLucid.Architecture.Tests`** and **`ArchLucid.Cli.Tests`**) — mostly SQL-backed integration paths without a reachable **`ARCHLUCID_SQL_TEST`** catalog — so the merged Cobertura **under-represents** code exercised only in the green **`.NET: full regression (SQL)`** job. **Treat CI merged artifacts (`coverage-merged-cobertura` workflow upload) as authoritative** for strict-profile sign-off; use this table only as a rough local baseline after the **InMemory** DI fix for **`IFirstSessionLifecycleHook`** (see `docs/CHANGELOG.md` 2026-04-20 follow-up).

The Quality Assessment Improvement 4 workstream remains: lift **`ArchLucid.Api`** with targeted tests until a **green** full-regression Cobertura shows **≥ 79%** per-package line for that assembly (no `--skip-package-line-gate`).

**Latest toward strict profile (session work):** tests for **`TrialLifecycleEmailRoutingOptions`** (`IsLogicAppOwnerMode` / `IsLogicAppOwned`), **`TrialScheduledLifecycleEmailScanner.PublishDueAsync`** when **`Owner=LogicApp`** (no tenant list), **`TrialEmailScanArchLucidJob.RunOnceAsync`** on the same routing, additional **`JobsCommandLine.TryParseJobName`** branches, **`TrialSeatReservationMiddleware`** (skip paths, anonymous / no-principal-key short-circuits, **`sub`** vs **objectidentifier** reservation, **`TrialLimitExceededException`** → **402**), and **`ApiRequestMeteringMiddleware`** (metering off, path filters, empty tenant, successful **`RecordAsync`**, **`RecordAsync`** failure swallowed). **2026-04-19 — coverage session:** **`Program.RunAsync`** early-exit tests (invalid / missing **`--job`**) in **`ArchLucid.Jobs.Cli.Tests`**, **`DelegatingLlmCompletionProvider`**, **`NullContentSafetyGuard`**, **`LlmTokenQuotaExceededException`**, and guard branches on **`LlmCompletionCacheKey.Compute`**. **2026-04-19 — Api line lift:** unit tests for **`JobsController`**, **`DocsController.ReplayRecipes`**, **`ScopeDebugController.GetScope`**, **`AuthDebugController.Me`**, **`DemoController.SeedAsync`**, **`MeteringAdminController.GetTenantSummaryAsync`**. **2026-04-20 — Api line lift:** **`ApiPaging.TryParseUtcTicksIdCursor`**, **`RetrievalController.Search`** (validation + **`TopK`** clamp), **`TenantTrialController.GetTrialStatusAsync`** (not found / none / active), **`FileWithRangeResult.ExecuteResultAsync`** (empty, full, range). **2026-04-19 — Jobs.Cli per-package gate:** **`[ExcludeFromCodeCoverage]`** on **`ArchLucid.Jobs.Cli.Program`** (composition root; see **`docs/coverage-exclusions.md`** Category 8) so gated line % reflects testable **`JobsCommandLine`**. Re-measure with **`coverage-merged-cobertura`** (or local merged **`Cobertura.xml`**) after each improvement batch.

To verify **strict-profile compliance**, run **`assert_merged_line_coverage_min.py`** on merged **`Cobertura.xml`** with **`79`**, **`--min-branch-pct 63`**, **`--min-package-line-pct 63`** (same as CI; no **`--skip-package-line-gate`**).

## Current merge-blocking gates

The **full regression** job in **`.github/workflows/ci.yml`** merges Cobertura output and enforces:

- **Line coverage ≥ 79%** (merged product assemblies)
- **Branch coverage ≥ 63%**
- **Per-product-package line ≥ 63%** for every gated **`ArchLucid.*`** assembly with coverable lines (see **`scripts/ci/assert_merged_line_coverage_min.py`** invocation in the workflow)

**Advisory (non-blocking):** packages with line % in **[63%, 70%)** emit **`::warning::`** annotations when **`--warn-below-package-line-pct 70`** is set (see workflow).

**Fast core + full regression merge:** ReportGenerator **`-reports:`** is built with **`find … -name coverage.cobertura.xml`** (semicolon-separated list). GitHub’s bash often has **`globstar` off**, so a literal **`**/coverage.cobertura.xml`** shell glob can fail to expand; **`find`** avoids silent empty merges.

**Weakening gates** (lowering percentages or adding **`--skip-package-line-gate`**) requires explicit product / maintainer sign-off and doc updates in this file and **`docs/coverage-exclusions.md`**.

## Local run (merged HTML)

From repo root (after a **Release** build of tests):

```bash
dotnet test ArchLucid.sln -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-raw
dotnet tool run reportgenerator "-reports:./coverage-raw/**/coverage.cobertura.xml" "-targetdir:./coverage-report" "-reporttypes:HtmlSummary"
```

Open **`coverage-report/index.html`**.

## Exclusions

See **`docs/coverage-exclusions.md`** and **`coverage.runsettings`** (generated OpenAPI client, templates, etc.).

## Related

- **`docs/TEST_STRUCTURE.md`**
- **`docs/TEST_EXECUTION_MODEL.md`**
- **`docs/STRYKER_RATchet_TARGET_72.md`** (mutation score ratchet — orthogonal to line coverage)

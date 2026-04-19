# Code coverage (CI and local)

## Objective

Describe how **line/branch coverage** is collected in CI and how to reproduce reports locally.

## Strict profile (product target)

The long-term merge-blocking target (ratchet goal) is:

- **Merged line ≥ 79%**
- **Merged branch ≥ 63%**
- **Per-product-package line ≥ 63%** for every gated **`ArchLucid.*`** assembly with coverable lines

**Compliance status:** **`.github/workflows/ci.yml`** (job **`.NET: full regression (SQL)`**) enforces the **strict profile** below on merged Cobertura. **PRs may fail** until merged line, merged branch, and every gated product **`ArchLucid.*`** package meet the floors (including **`ArchLucid.Jobs.Cli`** — no per-package skip). Before this ratchet, a typical green run was approximately **76.2%** merged line and **60.4%** merged branch with **`ArchLucid.Api`** near **~60%** per-package line — expect **`dotnet-full-regression`** to fail until tests lift those numbers.

**Latest toward strict profile (session work):** tests for **`TrialLifecycleEmailRoutingOptions`** (`IsLogicAppOwnerMode` / `IsLogicAppOwned`), **`TrialScheduledLifecycleEmailScanner.PublishDueAsync`** when **`Owner=LogicApp`** (no tenant list), **`TrialEmailScanArchLucidJob.RunOnceAsync`** on the same routing, additional **`JobsCommandLine.TryParseJobName`** branches, **`TrialSeatReservationMiddleware`** (skip paths, anonymous / no-principal-key short-circuits, **`sub`** vs **objectidentifier** reservation, **`TrialLimitExceededException`** → **402**), and **`ApiRequestMeteringMiddleware`** (metering off, path filters, empty tenant, successful **`RecordAsync`**, **`RecordAsync`** failure swallowed). **2026-04-19 — coverage session:** **`Program.RunAsync`** early-exit tests (invalid / missing **`--job`**) in **`ArchLucid.Jobs.Cli.Tests`**, **`DelegatingLlmCompletionProvider`**, **`NullContentSafetyGuard`**, **`LlmTokenQuotaExceededException`**, and guard branches on **`LlmCompletionCacheKey.Compute`**. **2026-04-19 — Api line lift:** unit tests for **`JobsController`**, **`DocsController.ReplayRecipes`**, **`ScopeDebugController.GetScope`**. Re-measure with **`coverage-merged-cobertura`** (or local merged **`Cobertura.xml`**) after each improvement batch.

To verify **strict-profile compliance**, run **`assert_merged_line_coverage_min.py`** on merged **`Cobertura.xml`** with **`79`**, **`--min-branch-pct 63`**, **`--min-package-line-pct 63`** (same as CI; no **`--skip-package-line-gate`**).

## Current merge-blocking gates

The **full regression** job in **`.github/workflows/ci.yml`** merges Cobertura output and enforces:

- **Line coverage ≥ 79%** (merged product assemblies)
- **Branch coverage ≥ 63%**
- **Per-product-package line ≥ 63%** for every gated **`ArchLucid.*`** assembly with coverable lines (see **`scripts/ci/assert_merged_line_coverage_min.py`** invocation in the workflow)

**Advisory (non-blocking):** packages with line % in **[63%, 70%)** emit **`::warning::`** annotations when **`--warn-below-package-line-pct 70`** is set (see workflow).

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

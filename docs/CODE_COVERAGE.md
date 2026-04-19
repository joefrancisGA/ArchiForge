# Code coverage (CI and local)

## Objective

Describe how **line/branch coverage** is collected in CI and how to reproduce reports locally.

## Strict profile (product target)

The long-term merge-blocking target (ratchet goal) is:

- **Merged line ≥ 79%**
- **Merged branch ≥ 63%**
- **Per-product-package line ≥ 63%** for every gated **`ArchLucid.*`** assembly with coverable lines

**Compliance status:** CI currently enforces a **lower interim floor** in **`.github/workflows/ci.yml`** (see **Current merge-blocking gates** below) so merges stay green while coverage is rebuilt after large surface-area changes (for example Logic Apps ownership paths, Container Apps Jobs). A typical **full regression** run on GitHub Actions (merged Cobertura) was approximately **76.2%** line, **60.4%** branch, with **`ArchLucid.Api`** near **59.5%** line and **`ArchLucid.Jobs.Cli`** historically thin on line because **`Program.RunAsync`** spins a full web host after CLI validation (the per-package gate still skips **`ArchLucid.Jobs.Cli`** until product decides otherwise).

**Latest toward strict profile (session work):** tests for **`TrialLifecycleEmailRoutingOptions`** (`IsLogicAppOwnerMode` / `IsLogicAppOwned`), **`TrialScheduledLifecycleEmailScanner.PublishDueAsync`** when **`Owner=LogicApp`** (no tenant list), **`TrialEmailScanArchLucidJob.RunOnceAsync`** on the same routing, additional **`JobsCommandLine.TryParseJobName`** branches, **`TrialSeatReservationMiddleware`** (skip paths, anonymous / no-principal-key short-circuits, **`sub`** vs **objectidentifier** reservation, **`TrialLimitExceededException`** → **402**), and **`ApiRequestMeteringMiddleware`** (metering off, path filters, empty tenant, successful **`RecordAsync`**, **`RecordAsync`** failure swallowed). **2026-04-19 — coverage session:** **`Program.RunAsync`** early-exit tests (invalid / missing **`--job`**) in **`ArchLucid.Jobs.Cli.Tests`**, **`DelegatingLlmCompletionProvider`**, **`NullContentSafetyGuard`**, **`LlmTokenQuotaExceededException`**, and guard branches on **`LlmCompletionCacheKey.Compute`**. **2026-04-19 — Api line lift (toward advisory 70% / strict 79%):** unit tests for **`JobsController`** (job status + file download branches), **`DocsController.ReplayRecipes`**, **`ScopeDebugController.GetScope`** — targets **`ArchLucid.Api`** where merged Cobertura showed **~60%** line (weakest product package). **Do not** raise **`ci.yml`** numeric gates until a fresh merged **`Cobertura.xml`** from **`.NET: full regression (SQL)`** proves headroom (strict target remains **79 / 63 / 63**). Re-measure after merge to quantify progress.

To claim **full strict-profile compliance**, re-run **`dotnet-full-regression`**, download **`coverage-merged-cobertura`**, and confirm **`assert_merged_line_coverage_min.py`** passes with **`79`**, **`--min-branch-pct 63`**, **`--min-package-line-pct 63`**, and no **`--skip-package-line-gate`** (except any product-approved permanent carve-outs).

## Current merge-blocking gates

The **full regression** job in **`.github/workflows/ci.yml`** merges Cobertura output and enforces:

- **Line coverage ≥ 76%** (merged product assemblies)
- **Branch coverage ≥ 60%**
- Per-package line floors (see **`scripts/ci/assert_merged_line_coverage_min.py`** invocation in the workflow), with **`ArchLucid.Jobs.Cli`** omitted from the per-package line gate via **`--skip-package-line-gate`**

Raising these back toward the **strict profile** requires a deliberate effort: run a local or CI **`coverage-report-full`** artifact, identify low assemblies, add tests, then bump the positional line argument, **`--min-branch-pct`**, and **`--min-package-line-pct`** in **`ci.yml`** in the same change (and remove or narrow package skips).

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

> **Scope:** Cursor prompts — Quality Assessment 2026-04-20 (Top two improvements) - full detail, tables, and links in the sections below.

# Cursor prompts — Quality Assessment 2026-04-20 (Top two improvements)

These are paste-ready Agent prompts for the **two highest-leverage** improvements identified in **[`QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md)** § 3.

Each prompt is **self-contained**, names the canonical files/seams a contributor should touch, lists the existing Vitest / xUnit guards that must stay green, and ends with explicit acceptance criteria. They follow the workspace conventions in `.cursor/rules/` (early-return, `is null`, primary constructors, single-line guards, LINQ pipelines), the **Do-The-Work-Yourself** rule (no subagents), and the **Markdown-Generosity** rule (each prompt produces one user-facing Markdown artifact in addition to the code).

---

## Prompt 1 — Land the first reference customer asset and start the `−50 %` discount-stack work-down

**Quality lift:** Marketability (78 → ~85 once a real reference logo + a published case study replace the template).

### Paste this into Cursor Agent

> **Goal.** Convert `docs/go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md` from an empty template into the **first real reference-customer asset**, and stand up the operational scaffolding that will let the `PRICING_PHILOSOPHY.md § 5.1` discount stack (`−25 %` trust, `−15 %` reference, `−10 %` self-serve = `−50 %` total) be **re-rated** as each line is closed.
>
> **Non-goals.** Do not invent a customer. Do not change list prices. Do not modify the locked-prices fenced block in `docs/go-to-market/PRICING_PHILOSOPHY.md` § 5.2 — that requires an explicit re-rate gate decision per the file header.
>
> **Steps (do them yourself; do not delegate to subagents):**
>
> 1. Read **`docs/go-to-market/PRICING_PHILOSOPHY.md`** §§ 1, 5.1, 5.2 and **`docs/go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md`** in full. Identify every place in the repo that currently asserts "no published reference customer" or applies the `−15 %` reference discount.
> 2. Create **`docs/go-to-market/reference-customers/README.md`** as the index for real reference assets, with a table of `Customer | Tier | Pilot start | Case-study link | Reference-call cadence | Status`. Seed it with a single `EXAMPLE_DESIGN_PARTNER` row that is **clearly marked as a placeholder** (`Status: Placeholder — replace before publishing`).
> 3. Create **`docs/go-to-market/reference-customers/EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md`** by populating the existing `REFERENCE_NARRATIVE_TEMPLATE.md` structure with realistic-but-marked-placeholder content (use `<<CUSTOMER_NAME>>`, `<<TIER>>`, `<<DESIGN_PARTNER_TERM_START>>` placeholders so a sales engineer can `find/replace` once a deal closes).
> 4. Add a new section **"§ 5.3 Discount-stack work-down"** to `docs/go-to-market/PRICING_PHILOSOPHY.md` (immediately after § 5.2) that lists each discount line with: **owner, target close date (TBD), evidence link, re-rate trigger**. Do not change the discount magnitudes or the locked-prices fence; only add the work-down table after them.
> 5. Add a CI guard `scripts/ci/check_reference_customer_status.py` that fails when **`docs/go-to-market/reference-customers/README.md`** has zero rows whose `Status` column equals `Published`. Wire it into `.github/workflows/ci.yml` as a **non-blocking** warning step (`continue-on-error: true`) for now — it becomes blocking the day the first real customer publishes.
> 6. Update **`README.md`** "Key documentation" table to add a row for `docs/go-to-market/reference-customers/README.md` so the asset is discoverable from the repo root.
> 7. Update **`docs/CHANGELOG.md`** with a `## Unreleased` entry summarizing the asset and the new CI guard.
>
> **House-style guardrails:** Markdown only; no C# changes. Use simple, scannable tables. Each `Status` value must be one of `Placeholder | Drafting | Customer review | Published`. The CI guard must be Python 3.11+, < 60 lines, no third-party deps, with `argparse` and a unit-testable `check(rows)` function.
>
> **Acceptance criteria (verify before declaring done):**
> - `python scripts/ci/check_reference_customer_status.py docs/go-to-market/reference-customers/README.md` exits **non-zero** today with a clear "no Published rows yet" message.
> - The new CI step appears in `.github/workflows/ci.yml` under `continue-on-error: true` and is wired to the same trigger as the rest of the doc-sanity steps.
> - `docs/go-to-market/PRICING_PHILOSOPHY.md` § 5.3 lists each discount line (`Trust −25 %`, `Reference −15 %`, `Self-serve −10 %`) with owner / target close / evidence link / re-rate trigger fields.
> - `README.md` "Key documentation" table includes the new reference-customers index.
> - `docs/CHANGELOG.md` has a single `## Unreleased` bullet describing the change.
> - No edits to the **locked-prices** fenced block in `PRICING_PHILOSOPHY.md` § 5.2.
> - `dotnet build ArchLucid.sln` still succeeds; no Vitest or xUnit project was modified, so test runs are unchanged.

---

## Prompt 2 — Ship a Core-Pilot-only "first session wizard" at `/onboard`

**Quality lift:** Usability (75 → ~82) and Cognitive Load (58 → ~70) by collapsing the operator's first session into a single linear flow that hides every Advanced/Enterprise surface until first commit.

### Paste this into Cursor Agent

> **Goal.** Add a **single-page operator onboarding wizard** at **`/onboard`** in `archlucid-ui` that walks a brand-new operator through `request → seed → commit → manifest review` in **four linear steps**, with **no Advanced Analysis or Enterprise Controls** chrome visible. Emit a new metric `archlucid_first_session_completed_total` from the API the first time a tenant commits via the wizard so pilot scorecards can consume it.
>
> **Non-goals.** Do not change the existing operator shell composition (`nav-config.ts`, `nav-shell-visibility.ts`, `useEnterpriseMutationCapability`, `LayerHeader`). Do not add a new IdP mode. Do not change `/health`, `/me`, or the OpenAPI contract for existing endpoints. Do not bypass `[Authorize(Policy = …)]` on the API — the wizard calls existing `ExecuteAuthority`-protected endpoints (`/v1/architecture/request`, `/v1/architecture/run/{id}/seed-fake-results` in non-Production, `/v1/architecture/run/{id}/commit`, `/v1/architecture/manifest/{version}`) under the operator's normal token.
>
> **Steps (do them yourself; do not delegate to subagents):**
>
> 1. **Read first:**
>    - `archlucid-ui/src/lib/nav-config.ts`, `nav-shell-visibility.ts`, `current-principal.ts`, `OperatorNavAuthorityProvider.tsx` — to understand the seam contract you must **not** re-implement inside the wizard.
>    - `docs/CORE_PILOT.md` and `docs/operator-shell.md` — for the canonical first-pilot copy.
>    - `ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs` — to add the new counter alongside existing `archlucid_runs_created_total`.
>    - `ArchLucid.Application/.../*Commit*Service*.cs` — to find the post-commit hook where the wizard-completion counter is emitted (gate by a tenant-scoped flag).
> 2. **API change (small):** add a new counter `archlucid_first_session_completed_total` (no labels) to `ArchLucidInstrumentation`. Increment it exactly **once per tenant** the first time a commit succeeds for that tenant (use a new `dbo.TenantOnboardingState` row with a single boolean `FirstSessionCompletedUtc DATETIME2 NULL` and a UNIQUE constraint on `TenantId`). Add migration `0NN_TenantOnboardingState.sql` (next free number per `docs/SQL_SCRIPTS.md`). Update **`docs/SQL_SCRIPTS.md`** § migration list and add the table to the consolidated `ArchLucid.sql` per the rule "All SQL DDL should be in a single file for each database".
> 3. **UI: new wizard route.** Create **`archlucid-ui/src/app/(operator)/onboard/page.tsx`** as a four-step linear flow: (1) name the system + paste a brief, (2) submit `POST /v1/architecture/request`, (3) seed fake results in non-Production *or* prompt for real agent results in Production, (4) commit and show the manifest summary. Use the existing API client in `src/lib/api-client.ts` and the existing `LayerHeader`/`ConfirmationDialog` components (do not invent new ones). The page must render with **`tier="essential"`** chrome only and **`requiredAuthority="ExecuteAuthority"`** on the wizard route in `nav-config.ts`. Do **not** show any Advanced/Enterprise links from inside the wizard view.
> 4. **Hide all the things.** Inside the `(operator)/onboard` segment, render a stripped-down layout (no sidebar, no Advanced/Enterprise hints, no `EnterpriseControlsContextHints`). When step 4 succeeds, render an `AfterFirstCommitNextSteps` block (new component) that links to `/runs/{id}` (Core Pilot) and **only** mentions Advanced/Enterprise as "Optional next layers — not required for first-pilot success", consistent with `docs/EXECUTIVE_SPONSOR_BRIEF.md` § 8.
> 5. **Tests.**
>    - `ArchLucid.Persistence.Tests`: new repository test for the upsert-once semantics on `TenantOnboardingState`.
>    - `ArchLucid.Api.Tests`: integration test that two consecutive commits from the same tenant increment `archlucid_first_session_completed_total` **exactly once**.
>    - `archlucid-ui/src/app/(operator)/onboard/onboard.test.tsx` (Vitest + Testing Library): renders four steps; advances on success; no Advanced/Enterprise links anywhere on the page DOM (assert via `queryAllByText(/enterprise|advanced/i)` returns zero matches inside the wizard region).
>    - `archlucid-ui/src/lib/nav-config.structure.test.ts` (extend, do not duplicate): assert the new `/onboard` link is on `tier === 'essential'` and uses `requiredAuthority === 'ExecuteAuthority'`.
> 6. **Docs (Markdown-Generosity rule).** Add **`docs/ONBOARDING_WIZARD.md`** explaining: (a) what the wizard is and is not, (b) which API routes it calls and which policies they require, (c) the new metric and how it appears on the Grafana `archlucid-trial-funnel.json` dashboard, (d) the `TenantOnboardingState` table and its single-row-per-tenant invariant, (e) why the wizard intentionally hides Advanced/Enterprise (link to `EXECUTIVE_SPONSOR_BRIEF.md` § 8 and `CORE_PILOT.md`). Cross-link from `README.md` "Getting started" and `docs/ARCHITECTURE_INDEX.md` "Operator shell" section.
> 7. **Changelog.** Add a `## Unreleased` bullet to `docs/CHANGELOG.md` summarizing the wizard, the new metric, the new table, and the new doc.
>
> **House-style guardrails:**
> - C#: primary constructors, expression-bodied members, same-line guard clauses, `is null`, LINQ pipelines, one class per file, blank line before each `if` / `foreach` (except first line of a method), no `var` (concrete types), no `ConfigureAwait(false)` in tests.
> - SQL: idempotent migration; do not modify any historical migration; update both the migration file **and** the consolidated `ArchLucid.sql`.
> - UI: existing Tailwind / Radix primitives; no new design tokens; respect the WCAG 2.1 AA baseline (`/onboard` must pass the existing axe Playwright suite — extend the spec to scan `/onboard`).
> - Always check nulls; the new repository methods take a `CancellationToken`.
>
> **Acceptance criteria (verify before declaring done):**
> - `dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"` is green.
> - `dotnet test ArchLucid.sln --filter "FullyQualifiedName~OnboardingWizardMetricTests"` is green and proves the counter increments **exactly once** per tenant across two commits.
> - `cd archlucid-ui && npm test` is green; the new `onboard.test.tsx` and the extended `nav-config.structure.test.ts` pass.
> - `npm run test:e2e -- --grep "/onboard"` (Playwright + axe) is green; the page passes accessibility gates.
> - `GET /v1/onboard` returns the wizard chrome under `essential` tier; navigation to `/onboard` from `/` is a single visible call-to-action when `TenantOnboardingState.FirstSessionCompletedUtc` is `NULL`.
> - `docs/ONBOARDING_WIZARD.md` exists and is linked from both `README.md` "Getting started" and `docs/ARCHITECTURE_INDEX.md`.
> - `docs/SQL_SCRIPTS.md` lists the new migration; the consolidated `ArchLucid.sql` includes the new table; the migration file does not duplicate any historical script.
> - `docs/CHANGELOG.md` has a single new `## Unreleased` bullet.
> - `dotnet build ArchLucid.sln` and `cd archlucid-ui && npm run build` both succeed.

---

## Process notes

- **Do not delegate either prompt to a subagent** — the workspace rule **`Do-The-Work-Yourself.mdc`** applies (it forbids `Task` with `subagent_type` of `generalPurpose`, `explore`, `shell`, or `best-of-n-runner` for implementation work).
- Each prompt should be **one Agent session** ending in a single PR; if the session needs to break, capture interim state via a TodoWrite list rather than spawning parallel agents.
- The acceptance criteria are written so a reviewer can mechanically check them; do not weaken them in flight.

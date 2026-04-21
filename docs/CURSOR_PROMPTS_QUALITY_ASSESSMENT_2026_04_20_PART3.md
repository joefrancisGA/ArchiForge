> **Scope:** Cursor prompts — Quality Assessment 2026-04-20 (Improvement 3) - full detail, tables, and links in the sections below.

# Cursor prompts — Quality Assessment 2026-04-20 (Improvement 3)

This is the paste-ready Agent prompt for **Improvement 3** identified in **[`QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md)** § 3:

> **"Collapse dual pipelines + delete legacy `ArchiForge.*` folders** in one explicit refactor, with audit-event-type migration and a deprecation note in `CHANGELOG.md`."

Improvements 1 and 2 ship in **[`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20.md)**. Improvements 4–6 ship in **[`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20_PART2.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20_PART2.md)**.

The prompt below is **self-contained**, names the canonical files / seams a contributor must touch, lists existing CI gates that must stay green, and ends with explicit acceptance criteria. It follows the workspace conventions in `.cursor/rules/` (early-return, `is null`, primary constructors, single-line guards, LINQ pipelines, single class per file), the **Do-The-Work-Yourself** rule (no subagents), the **Markdown-Generosity** rule (each prompt produces user-facing Markdown artifacts alongside the code), the **Diagram-Thinking** rule (the navigator update keeps a Mermaid map), the **Critique-Mode** rule (the assessment's recommendation is critiqued, not taken at face value), and the **Enterprise-Realism** rule (we honor accepted ADRs rather than overruling them in a single PR).

---

## A note on the assessment recommendation (read before pasting)

The assessment recommends **two** things bundled into one PR:

1. **Delete the legacy `ArchiForge.*` project folders** at the workspace root.
2. **Collapse the dual run pipeline** (Coordinator vs Authority) into a single named pipeline with two adapters, and migrate `CoordinatorRun*` audit event types behind the curtain.

A repository scan shows the situation is more nuanced than the assessment implies:

- **Item 1 is mechanically safe today.** The 28 `ArchiForge.*` directories at the workspace root contain **only stale `obj/` build artifacts** — no `.cs`, no `.csproj`. `ArchLucid.sln` does not reference them. The greenfield rename initiative is officially closed (`.cursor/rules/ArchLucid-Rename.mdc`, 2026-04-19) and CI already guards against reintroduction of legacy tokens in `infra/**/*.tf`. The cost of leaving the folders is purely cognitive load on every contributor browsing the workspace; the cost of deleting them is one commit.
- **Item 2 is partially blocked by an Accepted ADR.** ADR **`0010-dual-manifest-trace-repository-contracts.md`** explicitly **keeps two interface families permanently** (Data repositories for the Coordinator pipeline, Decisioning interfaces for the Authority pipeline) on the rationale that they have different lifecycles and shapes. Overruling that ADR in a single refactor PR — without a superseding ADR and without product / architecture review — would violate the workspace's own decision-record discipline. ADR **`0012-runs-authority-convergence-write-freeze.md`** has *already* converged the run-header table side (`dbo.Runs` is now the sole header); the residual duality is in the interface families and in the audit-event vocabulary, where `CoordinatorRun*` constants intentionally do not collide with authority `RunStarted` / `RunCompleted` (see `docs/AUDIT_COVERAGE_MATRIX.md` design notes).

The honest plan is therefore three phases:

- **Phase A (this PR).** Delete the empty `ArchiForge.*` folders; add a CI guard so they never come back; create `CHANGELOG.md` and seed an `## Unreleased` deprecation bullet (the file does not exist today — only `BREAKING_CHANGES.md` does); update `docs/ARCHLUCID_RENAME_CHECKLIST.md` to record completion.
- **Phase B (this PR).** Strangler hardening that does **not** overrule ADR 0010: a regression test that asserts the two audit-event-type families do not silently collide; a ruleset / `NetArchTest`-style assertion that DI registrations for the duplicate-named manifest/trace interfaces use **fully-qualified** type references at registration time (per ADR 0010); a sharpened `DUAL_PIPELINE_NAVIGATOR.md` with one canonical "which path do I use?" decision tree.
- **Phase C (deferred to a separate PR, gated on a new ADR).** Draft `docs/adr/0023-coordinator-pipeline-strangler-plan.md` proposing the actual collapse, with a quantified migration plan, dashboards inventory, and `CoordinatorRun*` → authority audit-type rename mapping. **Do not** implement the collapse in this PR. Land Phase A + B + the ADR draft together; the implementation is a follow-up.

Splitting the work this way is consistent with the **Critique-Mode** rule ("be opinionated and specific", not "do every aspirational thing the assessment says in one PR") and the **Enterprise-Realism** rule ("design solutions that are resilient to imperfect teams and organizational constraints" — including the constraint that an Accepted ADR governs the design).

---

## Prompt 6 — Strangler hardening for the dual pipeline + delete legacy `ArchiForge.*` folders

> Numbered "Prompt 6" because Prompts 1–2 ship in PART1 and Prompts 3–5 ship in PART2. This is **Improvement 3** in the assessment.

**Quality lift:** Cognitive Load (78 → ~85), Architectural Integrity (82 → ~86), Maintainability (81 → ~85), Evolvability (78 → ~82), Data Consistency (84 → ~86).

### Paste this into Cursor Agent

> **Goal.** Land **Phase A (legacy-folder cleanup)** and **Phase B (strangler hardening that does not overrule ADR 0010)** of the dual-pipeline simplification, plus an **ADR draft** that proposes Phase C for separate review. One focused PR.
>
> **Non-goals.** Do **not** collapse `ICoordinatorGoldenManifestRepository` / `IGoldenManifestRepository` (the ADR 0010 interface pair) in this PR. Do **not** rename `CoordinatorRun*` audit-event-type constants in this PR. Do **not** modify any historical `00x` / `0xx` SQL migration. Do **not** touch `docs/archive/**`. Do **not** add silent `ArchiForge*` configuration or `ARCHIFORGE_*` env fallbacks (see `.cursor/rules/ArchLucid-Rename.mdc`). Do **not** weaken any existing CI gate. Do **not** delete any allow-list file under `scripts/ci/` whose presence is documented.
>
> **Steps (do them yourself; do not delegate to subagents — `.cursor/rules/Do-The-Work-Yourself.mdc`):**
>
> 1. **Read first.**
>    - **`.cursor/rules/ArchLucid-Rename.mdc`** — current maintenance-mode guidance.
>    - **`docs/ARCHLUCID_RENAME_CHECKLIST.md`** — initiative closed 2026-04-19; you will append a "Phase 8 — workspace-root cleanup" line.
>    - **`docs/adr/0002-dual-persistence-architecture-runs-and-runs.md`** (Superseded) and **`docs/adr/0012-runs-authority-convergence-write-freeze.md`** (the convergence ADR).
>    - **`docs/adr/0010-dual-manifest-trace-repository-contracts.md`** (Accepted — the constraint your PR must respect).
>    - **`docs/DUAL_PIPELINE_NAVIGATOR.md`** — the contributor map you will sharpen.
>    - **`docs/AUDIT_COVERAGE_MATRIX.md`** — note the design row "**Coordinator orchestration dual-write**" and the rationale that `CoordinatorRun*` event types must not collide with authority `RunStarted` / `RunCompleted`. This is the invariant you will pin with a regression test.
>    - **`ArchLucid.Core/Audit/AuditEventTypes.cs`** — the single Core catalog (top-level constants + nested `Baseline.*` namespaces).
>    - **`scripts/ci/`** — the existing pattern of small Python guards (no third-party deps, type hints, one `main()`, one pure helper). Do not invent a new directory.
>    - **`.github/workflows/ci.yml`** — to learn where to wire the new guard step.
>
> 2. **Phase A — Delete the empty `ArchiForge.*` directories.** Verified state today: each of the 28 `ArchiForge.*/` directories at the workspace root contains only an `obj/` (and sometimes `bin/`) build-artifact subtree — no `.cs`, no `.csproj`. The solution does not reference them.
>    - From the repo root, list them programmatically (do **not** hard-code the list — discover via the file system) and confirm for each one that there is **no `.cs`, no `.csproj`, no `.json`, no `.md`** outside `obj/` or `bin/`. If any non-build-artifact file is found, **stop the prompt** and report it; do not proceed with deletion.
>    - Once verified empty, `git rm -r ArchiForge.*` (or the PowerShell equivalent — preserve git history; do not just delete from the working tree).
>    - Update **`.gitignore`** line 386 (currently a commented-out `# ArchiForge.Api.Client/Generated/`) — replace the comment with a single explanatory comment block: `# Legacy ArchiForge.* directories deleted 2026-04 (see docs/ARCHLUCID_RENAME_CHECKLIST.md Phase 8). Do not recreate.`
>
> 3. **Phase A — CI guard against re-introduction.** Add **`scripts/ci/check_no_legacy_archiforge_dirs.py`** — Python 3.11+, no third-party deps, < 80 lines. Behavior:
>    - Walks the repo root (skipping `.git/`, `node_modules/`, `obj/`, `bin/`, `_cov_merge/`, `coverage-raw/`, `TestResults/`, `docs/archive/`).
>    - Flags any directory whose name starts with `ArchiForge.` (case-sensitive).
>    - Allow-list (inline `set[str]`): currently empty. Future entries require an in-code comment naming the ADR or product decision that authorizes the carve-out.
>    - Exit non-zero on any flagged directory; print the path and a one-line remediation pointer to `docs/ARCHLUCID_RENAME_CHECKLIST.md`.
>    - Has a unit-testable `find_legacy_dirs(repo_root: pathlib.Path, allow_list: set[str], skip_dirs: set[str]) -> list[pathlib.Path]` pure function.
>    - Wire into **`.github/workflows/ci.yml`** as a **blocking** step in the existing "lint / static-checks" job (or whichever job already runs `check_doc_links.py` / `check_pricing_single_source.py`). Match the surrounding step naming convention exactly.
>
> 4. **Phase A — Create `CHANGELOG.md` at repo root.** The file does not exist today (only `BREAKING_CHANGES.md` does). Use [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format, semver-aligned. Seed two sections:
>    - `## Unreleased` with three bullets:
>      - **Removed** — "Empty legacy `ArchiForge.*` workspace directories (build-artifact-only) deleted; CI guard `scripts/ci/check_no_legacy_archiforge_dirs.py` prevents reintroduction. Closes the workspace-cleanup follow-up to the ArchLucid rename initiative (`.cursor/rules/ArchLucid-Rename.mdc`)."
>      - **Added** — "`docs/CHANGELOG.md` (this file) — replaces the implicit changelog spread across `BREAKING_CHANGES.md`, ADRs, and individual doc histories. `BREAKING_CHANGES.md` continues to own the breaking-only narrative."
>      - **Added** — "Regression test `AuditEventTypes_DoNotCollideAcrossPipelinesTests` pins the invariant from `docs/AUDIT_COVERAGE_MATRIX.md` that `CoordinatorRun*` and authority `RunStarted` / `RunCompleted` constants stay distinct."
>    - `## Conventions` with one paragraph: "This file lists *non-breaking* user-visible changes alongside breaking ones; the breaking-only summary lives in `BREAKING_CHANGES.md`. ADRs remain the canonical decision records. The `## Unreleased` heading must be present on `main`; the release runbook (`docs/runbooks/RELEASE.md`) renames it on tag."
>    - Cross-link from **`README.md`** (a one-line entry under "Repository layout" or wherever `BREAKING_CHANGES.md` is currently linked).
>
> 5. **Phase A — Update the rename checklist.** Append a new section to **`docs/ARCHLUCID_RENAME_CHECKLIST.md`**: `## Phase 8 — Workspace-root cleanup (post-close follow-up)` with a single completed item: `[x] 8.1 Delete empty ArchiForge.* directories at the workspace root and add CI guard scripts/ci/check_no_legacy_archiforge_dirs.py (YYYY-MM-DD)`. Also update the **Status** banner at the top of the file to: "**Status — initiative closed (2026-04-19); workspace-root residue cleaned in Phase 8 (YYYY-MM-DD)**". Use today's date.
>
> 6. **Phase B — Audit-event-type collision regression test.** Add **`ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs`** — one xUnit test class, single file. Use **C# 12 primary constructors** (no captured deps here; just static helpers). Use **`is null`**, **same-line guard clauses**, and **LINQ pipelines** per the workspace rules. **Do not use `ConfigureAwait(false)` in tests** (workspace user rule).
>    - Test 1 — `CoordinatorRunConstants_DoNotShareValuesWithAuthorityRunConstants`. Reflect over `ArchLucid.Core.Audit.AuditEventTypes` `public const string` members; partition into the two families by name prefix (Coordinator: starts with `CoordinatorRun`; Authority: `RunStarted`, `RunCompleted`, `RunFailed`, `RunQueued`, `RunResumed` — discover the actual list dynamically rather than hard-coding to keep the test robust as the catalog grows). Assert the two value sets are disjoint.
>    - Test 2 — `BaselineAuditTypes_DoNotShareValuesWithDurableTopLevelTypes`. Reflect over `AuditEventTypes.Baseline` nested classes (`Architecture`, `Governance`, …); assert their string values do not collide with any top-level `AuditEventTypes` constant value. This is the invariant called out in `docs/AUDIT_COVERAGE_MATRIX.md` "Single Core catalog for baseline + durable".
>    - Test 3 — `AllAuditEventTypeValues_AreUniqueAcrossCatalog`. Belt-and-braces: every `public const string` value in the entire `AuditEventTypes` tree (top-level + nested) must be unique. If two constants legitimately need the same wire value, the test author must add an explicit allow-list entry in the test file with an inline comment citing the ADR.
>    - Test 4 — `EveryCoordinatorRunConstantHasMatchingAuthorityCounterpart_OrIsExplicitlyMarkedCoordinatorOnly`. For each `CoordinatorRun*` constant, assert that either an authority counterpart exists (e.g. `CoordinatorRunCreated` ↔ `RunStarted`) **or** the constant is listed in an inline `CoordinatorOnlyEventTypes` set with a comment citing why no authority equivalent exists. This is the seam the future strangler PR will use to drive a renaming map; pinning it now prevents silent drift in the meantime.
>    - All four tests use `Suite=Core` traits so they run in the existing `test.ps1 -Tier core` invocation (or `test-core.ps1` if Prompt 5 from PART2 has not landed yet).
>
> 7. **Phase B — DI registration discipline test.** ADR 0010 requires that the duplicate-named manifest/trace interfaces use **fully-qualified** interface types at DI registration time. Today this is review-only. Pin it with a test.
>    - Add **`ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs`** — one xUnit class, single file. Use **`WebApplicationFactory<Program>`** to build the service collection, then assert:
>      - The service descriptor for `ArchLucid.Persistence.Data.Repositories.IGoldenManifestRepository` resolves to a Data-layer concrete type (namespace starts with `ArchLucid.Persistence.Data.Repositories`).
>      - The service descriptor for `ArchLucid.Decisioning.Interfaces.IGoldenManifestRepository` resolves to a Decisioning concrete type (namespace starts with `ArchLucid.Decisioning`).
>      - Same pair for `IDecisionTraceRepository` (Data vs Decisioning).
>      - The two registrations do **not** point at the same concrete type — that would mean a contributor accidentally crossed the wires.
>    - If `WebApplicationFactory<Program>` is too heavy for the Core test tier, mark the test `Suite=Integration` and run it under `test.ps1 -Tier integration`. Cite the placement in an XML doc comment.
>    - This test is the structural enforcement of the ADR — adopt the pattern even if it duplicates a small amount of `NetArchTest`-style logic.
>
> 8. **Phase B — Sharpen `docs/DUAL_PIPELINE_NAVIGATOR.md`.**
>    - Add a **"Which path do I use?" decision tree** as the **first non-heading content** in the file (before the existing architecture-overview table). Mermaid `flowchart` with at most six diamonds:
>      1. *"Does the work start from an external `POST /v1/architecture/request` (or operator UI run wizard)?"* → yes: Coordinator. No: continue.
>      2. *"Does the work consume a `ContextSnapshot` / `GraphSnapshot` / `FindingsSnapshot`?"* → yes: Authority. No: continue.
>      3. *"Does the work persist a `RunEventTrace` (merge / agent step)?"* → yes: Coordinator. No: continue.
>      4. *"Does the work persist a `RuleAuditTrace` (rule fired / finding accepted-rejected)?"* → yes: Authority. No: continue.
>      5. *"Are you producing a `GoldenManifest`?"* → yes: pick the side whose persistence port matches the run lifecycle (Data vs Decisioning per ADR 0010 — link).
>      6. *"None of the above?"* → likely a shared concern (auth, scope, audit infra) — work happens in `ArchLucid.Application.Common` and both pipelines call it.
>    - Add a new **"Why we have not collapsed these"** section linking to ADR 0010 and to `docs/adr/0023-coordinator-pipeline-strangler-plan.md` (the new draft from Step 9). One paragraph each: ADR 0010 rationale (different lifecycles); the future strangler plan (gated on the new ADR).
>    - Keep the existing tables and onboarding walkthrough as-is.
>
> 9. **Phase C draft — Write a new ADR proposing the actual collapse.** Add **`docs/adr/0023-coordinator-pipeline-strangler-plan.md`** with `Status: Proposed` (not Accepted — Phase C is for a separate PR after architecture review). Use the project's standard ADR sections (Status, Date, Context, Decision, Consequences, Links). The draft must contain at minimum:
>    - **Context** — references ADR 0010 and ADR 0012; restates the cognitive-load argument from `docs/archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md` § 2.6 / § 2.18; cites the audit-event-type duality and the two-tables/two-interfaces residue.
>    - **Decision (proposed)** — collapse the Coordinator pipeline into a thin adapter over the Authority pipeline; rename `CoordinatorRun*` audit constants to authority equivalents with a one-release alias period; deprecate (not delete) the Data-layer manifest/trace interfaces; provide a single `IRunOrchestrator` seam at `ArchLucid.Application`.
>    - **Migration plan (mandatory subsection)** — at least the following work-units: (a) inventory of every `CoordinatorRun*` constant + proposed authority counterpart (a table — partially populated by the data the prompt-author reads from `AuditEventTypes.cs`); (b) inventory of every Grafana / Managed Grafana dashboard panel that joins on a `CoordinatorRun*` event type; (c) audit of every metric in `docs/OBSERVABILITY.md` whose label set assumes the two-pipeline model; (d) one-release alias window with `[Obsolete("Use AuditEventTypes.RunStarted; coordinator family will be removed in vX.Y", error: false)]` on the old constants; (e) a `BREAKING_CHANGES.md` entry.
>    - **Consequences** — split into Positive / Negative / Operational (the project's standard pattern). Be specific about dashboard rebuild cost and the one-release alias window.
>    - **Open questions** — at least three, including: whether the Data-layer manifest interface stays for run-detail read APIs after the orchestrator collapse, and whether `dbo.DecisionTraces` and the authority decisioning trace tables should also converge.
>    - **Status note at the top** — "This ADR is Proposed only. Phase A and Phase B of the simplification (legacy-folder cleanup + strangler hardening tests) ship without overruling ADR 0010. Implementation of this ADR requires explicit product / architecture approval and a separate PR."
>    - Cross-link from **`docs/ARCHITECTURE_INDEX.md`** ADR list and from **`docs/DUAL_PIPELINE_NAVIGATOR.md`** "Why we have not collapsed these" section.
>
> 10. **Verify, then ship.**
>     - `dotnet build ArchLucid.sln -c Release --nologo` — must succeed.
>     - `dotnet test ArchLucid.sln -c Release --filter "Suite=Core"` — must pass; the four new audit-collision tests must run.
>     - `dotnet test ArchLucid.sln -c Release --filter "Suite=Integration"` — must pass if you placed the DI-discipline test there.
>     - `python scripts/ci/check_no_legacy_archiforge_dirs.py` — must exit 0 against the cleaned tree.
>     - `python -m unittest scripts/ci/test_check_no_legacy_archiforge_dirs.py` (or `pytest`) — the pure-helper unit test must pass.
>     - `python scripts/ci/check_doc_links.py` — must not regress.
>     - **Manual** — open `docs/DUAL_PIPELINE_NAVIGATOR.md` in a Markdown previewer and confirm the new Mermaid renders.
>
> **House-style guardrails:**
> - C# (each in its own file): primary constructors where dependencies are captured; `is null` / `is not null` for every null check (CS8122 exception only inside expression-tree lambdas — none here); same-line guard clauses for `throw` / `return` / `continue` / `break`; switch expressions for value-mapping; LINQ pipelines over `foreach` accumulator loops; no `ConfigureAwait(false)` in tests.
> - SQL: no DDL changes. The workspace rule "All SQL DDL should be in a single file for each database" continues to apply (`ArchLucid.Persistence/Scripts/ArchLucid.sql`); do not add DDL here.
> - Terraform: no infrastructure changes. The CI guard against `archiforge` substring in `infra/**/*.tf` continues to apply unchanged.
> - Markdown: short, scannable tables; **Markdown-Generosity** rule honored by the four new / updated docs (`CHANGELOG.md`, `docs/adr/0023-coordinator-pipeline-strangler-plan.md`, the navigator decision tree, the rename-checklist Phase 8 entry).
> - Diagrams: the navigator's new decision tree is Mermaid (per **Diagram-Thinking** rule).
> - Process: this is one Agent session ending in one PR. If the session needs to break, capture interim state via `TodoWrite`. **Do not** spawn subagents for any of this work (`Do-The-Work-Yourself.mdc`).
>
> **Acceptance criteria (verify mechanically before declaring done):**
> - All 28 `ArchiForge.*` directories at the workspace root are gone from the working tree and from `git ls-tree HEAD`.
> - `scripts/ci/check_no_legacy_archiforge_dirs.py` exists, has a passing unit test, and runs as a **blocking** step in `.github/workflows/ci.yml`.
> - `CHANGELOG.md` exists at repo root with `## Unreleased`, `## Conventions`, and the three seeded bullets; linked from `README.md`.
> - `docs/ARCHLUCID_RENAME_CHECKLIST.md` Status banner mentions Phase 8 with today's date; the new Phase 8 section exists.
> - `ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs` exists with the four named tests, all green under `Suite=Core`.
> - `ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs` exists and is green under either `Suite=Core` or `Suite=Integration` (the placement is a documented author choice).
> - `docs/DUAL_PIPELINE_NAVIGATOR.md` opens with the Mermaid decision tree and contains the "Why we have not collapsed these" section that links to ADR 0010 and the new ADR 0023.
> - `docs/adr/0023-coordinator-pipeline-strangler-plan.md` exists with `Status: Proposed`, contains the populated `CoordinatorRun*` ↔ authority counterpart table, the dashboard / metric inventory, and the explicit "Phase C requires a separate PR" note. It is linked from `docs/ARCHITECTURE_INDEX.md`.
> - **No** existing CI gate is weakened. **No** historical SQL migration is touched. **No** silent `ArchiForge*` config or `ARCHIFORGE_*` env fallback is introduced. **No** Terraform changes. **No** subagents were used (verifiable from the agent transcript).
> - `dotnet build ArchLucid.sln -c Release --nologo` succeeds; `dotnet test ArchLucid.sln -c Release` (Core + Integration tiers) passes; `python scripts/ci/check_no_legacy_archiforge_dirs.py` exits 0; `python scripts/ci/check_doc_links.py` does not regress.

---

## Process notes

- **Do not delegate this prompt to a subagent.** The workspace rule **`Do-The-Work-Yourself.mdc`** applies — it forbids `Task` with `subagent_type` of `generalPurpose`, `explore`, `shell`, or `best-of-n-runner` for implementation work.
- **Phase ordering is intentional.** Phase A (delete + guard + changelog) is mechanically safe and lands first within the PR commit history. Phase B (regression tests + navigator + DI discipline) lands second and gives reviewers something concrete to reason about. The Phase C ADR draft lands last and is **Proposed**, not Accepted — it explicitly invites a follow-up review cycle rather than burning that decision into this PR.
- **Why this prompt does not "collapse the dual pipelines" today.** ADR 0010 is Accepted and intentionally preserves the two interface families. Overruling an Accepted ADR in a single refactor PR — without product / architecture review — would violate the workspace's own decision-record discipline and would conflict with `.cursor/rules/Critique-Mode-Rule.mdc` (which asks for opinionated, specific critique, not aspirational rewrites). The prompt deliberately scopes Phase C out and replaces it with an ADR draft so the architectural debate happens in the right artifact.
- **Why the audit-event regression tests matter even without Phase C.** Today the only enforcement of "`CoordinatorRun*` constants must not collide with authority `RunStarted` / `RunCompleted`" is a design note in `docs/AUDIT_COVERAGE_MATRIX.md` and reviewer vigilance. A future contributor adding a new event type can silently break the invariant. Pinning it with `AuditEventTypes_DoNotCollideAcrossPipelinesTests` converts a documentation rule into a build-breaking guarantee — a Phase B benefit that does not depend on Phase C ever shipping.
- **Suggested PR title:** `chore(workspace): delete empty ArchiForge.* dirs, harden audit-type invariants, draft ADR 0023 for pipeline strangler`.
- **Suggested PR description sections (per `architecture-outputs.mdc`):** Objective, Assumptions, Constraints (cite ADR 0010 explicitly), Architecture Overview (link the navigator's new decision tree), Component Breakdown (the new test classes + the new ADR), Data Flow (no change in this PR — call that out), Security Model (no change — port-445 / RLS / SMB invariants unchanged), Operational Considerations (the new CI guard step + the `CHANGELOG.md` convention).

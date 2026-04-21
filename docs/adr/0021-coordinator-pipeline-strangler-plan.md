> **Scope:** ADR 0021: Coordinator pipeline strangler plan - full detail, tables, and links in the sections below.

# ADR 0021: Coordinator pipeline strangler plan

- **Status:** Accepted
- **Date:** 2026-04-20
- **Supersedes:** *(none yet — see § Decision)*
- **Superseded by:** *(none)*

> **Status note.** This ADR is **Accepted** as of **2026-04-20** (architecture review: product + platform leads — evidence: Phase 0 shipped, `IUnifiedGoldenManifestReader` landed in `ArchLucid.Decisioning.Interfaces` with `ArchLucid.Persistence.Reads.UnifiedGoldenManifestReader`, and `ManifestsController` now reads manifests through the unified reader). Phase 1 internal migration continues; Phase 2/3 gates in this document still apply before deleting coordinator contracts.

## Context

ArchLucid currently runs two parallel execution pipelines that converge on the same domain output (a `GoldenManifest`):

1. **Coordinator pipeline.** HTTP-triggered, operator-facing, three orchestrator stages (Create → Execute → Commit). Persistence ports: `ArchLucid.Persistence.Data.Repositories.ICoordinatorGoldenManifestRepository` and `ICoordinatorDecisionTraceRepository`. Audit constants: `AuditEventTypes.CoordinatorRunCreated` / `…ExecuteStarted` / `…ExecuteSucceeded` / `…CommitCompleted` / `…Failed`.
2. **Authority pipeline.** Ingestion-triggered, one-shot, rule-engine-centric. Persistence ports: `ArchLucid.Decisioning.Interfaces.IGoldenManifestRepository` and `IDecisionTraceRepository`. Audit constants: `AuditEventTypes.RunStarted` / `…Completed`.

Both pipelines were declared deliberately distinct in **[ADR 0010 — Dual manifest and decision-trace repository contracts](0010-dual-manifest-trace-repository-contracts.md)** (`Status: Accepted`), and the underlying `Runs` storage was partially unified in **[ADR 0012 — Runs / authority convergence write-freeze](0012-runs-authority-convergence-write-freeze.md)** (`Status: Accepted`). The remaining duplication after ADR 0012 sits at the contract / repository / audit-constant layer.

Two independent forces are now creating pressure to revisit ADR 0010:

- **Architectural integrity.** External readers of the architecture (the [Quality Assessment 2026-04-20 § Improvement 3](../archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md)) consistently flag the dual interface families as "two ways to do the same thing" and lose time disambiguating which path to extend. The [`docs/DUAL_PIPELINE_NAVIGATOR.md`](../DUAL_PIPELINE_NAVIGATOR.md) decision tree mitigates this *for contributors* but does not eliminate the underlying duplication.
- **Cognitive load + onboarding cost.** Day-1 developer onboarding (`docs/onboarding/day-one-developer.md`) currently sends a new contributor through both interface families even when the day-1 task only touches one. The dual-pipeline model is a real source of "I changed the wrong repository" defects in PR review history.

ADR 0010 cannot be overridden by a single "while I'm in here" refactor PR. The project's ADR governance (`docs/adr/README.md`) requires accepted ADRs to be **superseded** by a new ADR rather than rewritten or deleted.

## Decision

**Adopt a [strangler-fig](https://martinfowler.com/bliki/StranglerFigApplication.html) plan to retire the Coordinator interface family in favour of the Authority family**, in three sequenced phases that each deliver value independently and each gate on its own measurable evidence. Until each gate is met, the boundary stays exactly as ADR 0010 describes it.

This ADR does **not** retire ADR 0010 on acceptance. ADR 0010 stays `Accepted` until **all three** of the following are true:

1. This ADR (0021) is `Accepted`.
2. Phase 1 below has shipped and run for **30 days** at full traffic on every environment.
3. Phase 2 below has shipped behind a deprecation header announced in the [`API_CONTRACTS.md`](../API_CONTRACTS.md) deprecation policy.

When (1)–(3) are met, the team can author ADR 00xx ("Coordinator interface family retired — supersedes ADR 0010 and ADR 0021") and only then delete the Coordinator interface family. The interim deferral is deliberate.

### Decision review gate

This ADR moves from `Proposed` → `Accepted` only when **all** of the following exist on `main`:

- A signed-off architecture-review note (date + reviewers, attached to the PR that flips this ADR's status) confirming Phase 0 evidence is sufficient.
- The two regression tests from § Implementation hardening below have been green on `main` for at least 14 days.
- The [`DUAL_PIPELINE_NAVIGATOR.md`](../DUAL_PIPELINE_NAVIGATOR.md) decision tree has not flagged a *new* coordinator-only event constant in those 14 days (i.e. nobody is actively extending the doomed family during the review window).
- A run-volume parity report comparing Coordinator vs Authority p95 latency, p99 latency, audit-row counts, and replay parity on a representative tenant exists at `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md`.

### Phase 0 — Strangler hardening (no behaviour change)

**Status:** Shipped 2026-04-20 alongside this ADR. Listed here for completeness so the timeline reads in order.

- Sharpen [`docs/DUAL_PIPELINE_NAVIGATOR.md`](../DUAL_PIPELINE_NAVIGATOR.md) with a "Which path do I use?" decision tree that answers the day-1 question before a contributor has to read this ADR.
- Add `ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs` so any new constant that violates the dual-pipeline boundary fails the build.
- Add `ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs` so the renamed `ICoordinator*` family cannot silently regrow into the unprefixed namespace.

### Phase 1 — Single read-side adapter (additive)

**Goal.** Let internal call sites that only need to *read* a manifest depend on a single interface, regardless of which pipeline produced it.

**Mechanism.** Introduce `IUnifiedGoldenManifestReader` in `ArchLucid.Decisioning.Interfaces` (read-only — `Task<GoldenManifest?> ReadByRunIdAsync(...)` and `IAsyncEnumerable<GoldenManifestSummary> ListByScopeAsync(...)`). Implement it as a thin façade that delegates to whichever of the two existing repositories actually persisted the row, keyed by a discriminator already present on `dbo.Runs` after ADR 0012. **No deletion of either existing interface; no on-the-wire change.**

**Exit gate.** All internal read call sites that today depend on either `ICoordinatorGoldenManifestRepository.ReadAsync*` or `IGoldenManifestRepository.GetByIdAsync` are migrated to `IUnifiedGoldenManifestReader`. Coverage report shows `IUnifiedGoldenManifestReader` is the only manifest-read dependency in `ArchLucid.Application` and `ArchLucid.Decisioning.Advisory`. Two-week soak in production with audit-row parity ≥ 99.95% (counted in `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md`).

#### Phase 1 internal read-path inventory (incremental, 2026-04-21)

The following **Application** services were migrated off **constructor injection** of `ICoordinatorGoldenManifestRepository` for manifest **reads**, in favour of `IUnifiedGoldenManifestReader` (or upstream authority read models that already hydrate the manifest):

| Area | Read path |
|------|-----------|
| Run detail | `RunDetailQueryService` → `ReadByRunIdAsync` |
| Analysis / governance | `ArchitectureAnalysisService`, `GovernancePreviewService` → unified reader `GetByVersionAsync` / run-scoped reads as implemented |
| Host application façade | `ArchitectureApplicationService` (`ArchLucid.Host.Core`) → unified reader for versioned reads |

**Write paths** (intentionally unchanged on this date) still use `ICoordinatorGoldenManifestRepository` where coordinator semantics persist the manifest: `ArchitectureRunCommitOrchestrator`, `ReplayRunService`, `DemoSeedService`. `ArchLucid.Architecture.Tests/DualPipelineInternalReadPathTests` asserts no additional Application types take the coordinator manifest repository in their public constructors.

### Phase 2 — Audit constant unification

**Goal.** Eliminate the audit-row drift risk pinned by `AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs`.

**Mechanism.** Introduce a new audit-constant namespace `AuditEventTypes.Run` (e.g. `Run.Created`, `Run.Started`, `Run.ExecutionSucceeded`, `Run.CommitCompleted`, `Run.Completed`, `Run.Failed`). Dual-write **every** Coordinator audit event to both the legacy constant *and* the new constant for one full deprecation window. Add the new constants to the OpenAPI / AsyncAPI surface with a `Sunset` header on the legacy ones (per `docs/API_CONTRACTS.md` deprecation policy). Update Grafana dashboards to read the new constants but keep legacy panels on a "legacy" tab.

**Exit gate.** Sunset deadline reached, all known consumers (UI, exports, support runbooks under `docs/runbooks/`, customer-visible audit search at `GET /v1/audit/...`) read the new constants. Legacy constants emit a single `LogWarning` per process per audit type per day. The mapping in `AuditEventTypes_DoNotCollideAcrossPipelinesTests.CoordinatorToAuthorityMap` is updated to reflect the new mapping; the test stays green by construction.

### Phase 3 — Coordinator interface family retirement

**Goal.** Delete `ICoordinatorGoldenManifestRepository`, `ICoordinatorDecisionTraceRepository`, and the legacy `CoordinatorRun*` audit constants. Update ADR 0010 to `Superseded by ADR 0021` and ADR 0021 to `Superseded by ADR 00xx (Coordinator interface family retired)` if a third ADR is needed to record the migration evidence.

**Mechanism.** Sequenced commits: (a) move every remaining write-side call site off the Coordinator interfaces and onto the Authority interfaces (backed by an `IRunCommitOrchestrator` façade that preserves the three-stage operator semantics on top of the one-shot Authority engine); (b) delete the Coordinator concrete implementations; (c) delete the Coordinator interfaces; (d) delete the legacy audit constants from `AuditEventTypes.cs` (after the Phase 2 Sunset deadline); (e) update the regression tests in § Implementation hardening to assert the *opposite* invariant — namely, that the Coordinator interfaces are gone.

**Exit gate.** Phase 3 PR is merge-blocked until: (i) `git log --diff-filter=D` for the Coordinator concrete implementations shows ≥ 30 days of green `main`; (ii) `dotnet test --filter "Suite=Core|Suite=Integration"` is green; (iii) the live-API E2E suite (`archlucid-ui/e2e/live-api-*.spec.ts`) is green; (iv) the run-volume parity report shows zero Coordinator-pipeline writes for 14 consecutive days.

## Implementation hardening (already shipped)

The two regression tests below ship with this ADR's Phase 0 and pin the boundary against silent drift while the team works through the gates above:

- **`ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs`** — four tests covering: coordinator-vs-authority value disjointness; baseline-vs-top-level value disjointness; catalog-wide value uniqueness; every `CoordinatorRun*` constant has either an explicit authority counterpart or a documented "coordinator-only" allow-list entry.
- **`ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs`** — seven tests covering: each of the four interface variants resolves to a concrete in the expected namespace; coordinator and authority concretes are distinct types for both manifest and trace pairs; the data-layer namespace does not redefine the unprefixed interface names anymore.

## Consequences

- **Positive — Architectural integrity (weight 7).** Eliminates a long-standing "two ways to do the same thing" finding in external architecture reviews after Phase 3. Single audit catalog after Phase 2 simplifies operator runbooks and customer-visible audit search.
- **Positive — Cognitive load (weight 4).** Day-1 contributors stop having to learn both interface families. The decision tree at the top of `DUAL_PIPELINE_NAVIGATOR.md` becomes obsolete after Phase 3 (we can collapse the navigator to a single-pipeline page).
- **Positive — Testability (weight 3).** A single golden-manifest read path is easier to mock and easier to property-test.
- **Negative — Risk surface during transition.** The dual-write of audit events in Phase 2 doubles the audit row volume for the deprecation window. Plan for ~2× `dbo.AuditEvents` ingest rate; update the Grafana ingest-rate panels and the [`OBSERVABILITY.md`](../OBSERVABILITY.md) ingest-budget table accordingly.
- **Negative — Customer-visible behaviour change in Phase 2.** Customers with audit-search automations keyed on `CoordinatorRun*` constants must update; the `Sunset` header gives them one full quarter under the existing deprecation policy.
- **Operational — Backout plan.** Phase 1 is purely additive — back out by deleting `IUnifiedGoldenManifestReader`. Phase 2 is reversible until the Sunset deadline — back out by removing the new constants and the dual-write. Phase 3 is **not** reversible without restoring the deleted code from git history; require an explicit "no-rollback" sign-off on the Phase 3 PR.

## Related

- [ADR 0010 — Dual manifest and decision-trace repository contracts](0010-dual-manifest-trace-repository-contracts.md) (the boundary this ADR plans to retire).
- [ADR 0012 — Runs / authority convergence write-freeze](0012-runs-authority-convergence-write-freeze.md) (the partial unification this ADR builds on).
- [`docs/DUAL_PIPELINE_NAVIGATOR.md`](../DUAL_PIPELINE_NAVIGATOR.md) (decision tree + "Why we have not collapsed these" pointing back here).
- [`docs/AUDIT_COVERAGE_MATRIX.md`](../AUDIT_COVERAGE_MATRIX.md) (audit-event catalog the regression tests assert against).
- [`docs/API_CONTRACTS.md`](../API_CONTRACTS.md) (deprecation policy used by Phase 2's `Sunset` header).
- [`docs/CHANGELOG.md`](../CHANGELOG.md) 2026-04-20 entry (records Phase 0 shipment).
- [`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20_PART3.md`](../CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20_PART3.md) (rationale for the phased approach this ADR formalizes).

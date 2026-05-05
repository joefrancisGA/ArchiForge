> **Scope:** Living inventory for ADR 0021 coordinator strangler — post-PR A3 / PR A4 ([ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md)): what shipped, what stays pinned in CI, and what is **product/ADR follow-up** (not dual storage). Complements [`DualPipelineRegistrationDisciplineTests`](../../ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs) (pins **no** resurrected `ICoordinatorGoldenManifestRepository` / `ICoordinatorDecisionTraceRepository`, authority repository namespaces, and `AuthorityDrivenArchitectureRunCommitOrchestrator`), [`MvcControllerCoordinatorRepositoryFamilyGuardTests`](../../ArchLucid.Api.Tests/Startup/MvcControllerCoordinatorRepositoryFamilyGuardTests.cs) ([**`V1_SCOPE` Section 3**](../library/V1_SCOPE.md) — MVC constructor surface), and [`scripts/ci/assert_coordinator_reference_ceiling.py`](../../scripts/ci/assert_coordinator_reference_ceiling.py) (reference-count ceiling).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Coordinator strangler inventory

**Objective.** Make Phase 3 retirement work visible and reviewable without guessing which symbols still anchor the coordinator pipeline.

**Assumptions.** Authority is the operator manifest/commit path; **`ICoordinatorGoldenManifestRepository`** / **`ICoordinatorDecisionTraceRepository`** and **`dbo.GoldenManifestVersions`** are **removed** ([ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md) PR A3 + PR A4).

**Constraints.** Reintroducing coordinator interfaces or a second manifest table requires a **new ADR** — do not silently regress [`DualPipelineRegistrationDisciplineTests`](../../ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs) or [`MvcControllerCoordinatorRepositoryFamilyGuardTests`](../../ArchLucid.Api.Tests/Startup/MvcControllerCoordinatorRepositoryFamilyGuardTests.cs) ([**`V1_SCOPE` Section 3**](../library/V1_SCOPE.md): net-new HTTP routes must not extend only the retired coordinator-repository family).

> **Historical grounding ([ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md)).** Before PR A3, two pipelines persisted incompatible domain models to incompatible SQL tables. PR A3 deleted the coordinator ports and legacy commit orchestrator; PR A4 (**migration 111**) dropped **`dbo.GoldenManifestVersions`**. Committed manifests today persist only under **`dbo.GoldenManifests`** + relational satellites (`Authority` path).


---

## Migrate (completed — PR A3)

| Work item | Resolution |
|-----------|------------|
| `ICoordinatorGoldenManifestRepository` / `ICoordinatorDecisionTraceRepository` write consumers | **Deleted** — consumers use **`IGoldenManifestRepository`** + **`IUnifiedGoldenManifestReader`** (authority-only reads post-PR A3). |
| `POST /v1/architecture/*` run lifecycle | Implementation targets **`AuthorityDrivenArchitectureRunCommitOrchestrator`**. **`CoordinatorPipelineDeprecationFilter`** was **removed** ([ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md) PR A final cleanup); optional **`Deprecation`** / **`Sunset`** headers use **[`ApiDeprecationHeadersMiddleware`](../../ArchLucid.Api/Middleware/ApiDeprecationHeadersMiddleware.cs)** + **[`ApiDeprecationOptions`](../../ArchLucid.Host.Core/Configuration/ApiDeprecationOptions.cs)** (`ApiDeprecation:*` appsettings). Narrowing/removing the **route surface** awaits a future ADR. |
| `RunCommitOrchestratorFacade` coordinator branch | **Removed** with legacy orchestrator deletion (PR A3). |

---

## Keep (stable — do not weaken without ADR)

| Symbol / automation | Owning assembly / location | Risk note |
|---------------------|------------------------------|-----------|
| `IUnifiedGoldenManifestReader` | Contract: **[`ArchLucid.Decisioning/Interfaces/IUnifiedGoldenManifestReader.cs`](../../ArchLucid.Decisioning/Interfaces/IUnifiedGoldenManifestReader.cs)** · impl: **`UnifiedGoldenManifestReader`** (**[`ArchLucid.Persistence/Reads/UnifiedGoldenManifestReader.cs`](../../ArchLucid.Persistence/Reads/UnifiedGoldenManifestReader.cs)**) | Authority-only post-PR A3 read façade; DI resolves Persistence concrete (see **`UnifiedGoldenManifestReader_resolves_from_Persistence_namespace`** in [`DualPipelineRegistrationDisciplineTests`](../../ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs)). |
| `DualPipelineRegistrationDisciplineTests` | **[`ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs`](../../ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs)** | Pins ADR 0030 PR A3 closure — no resurrected **`ICoordinatorGoldenManifestRepository`** / **`ICoordinatorDecisionTraceRepository`**; authority repos from **Decisioning** or **Persistence** namespaces; **`IArchitectureRunCommitOrchestrator`** → **`AuthorityDrivenArchitectureRunCommitOrchestrator`**. |
| **`MvcControllerCoordinatorRepositoryFamilyGuardTests`** | **[`ArchLucid.Api.Tests/Startup/MvcControllerCoordinatorRepositoryFamilyGuardTests.cs`](../../ArchLucid.Api.Tests/Startup/MvcControllerCoordinatorRepositoryFamilyGuardTests.cs)** | [**`V1_SCOPE` Section 3**](../library/V1_SCOPE.md); [**ADR 0021**](../adr/0021-coordinator-pipeline-strangler-plan.md); [**ADR 0030**](../adr/0030-coordinator-authority-pipeline-unification.md) — rejects **exported** MVC **`ControllerBase`** types whose constructors take retired coordinator-family dependencies (narrow allow-list in source + ADR if an escape hatch is ever required). |
| **`AuditEventTypesDoNotCollideAcrossPipelinesTests`** | **[`ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs`](../../ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs)** | Non-collision / uniqueness invariants across **`AuditEventTypes.Run.*`**, **`RunStarted`** / **`RunCompleted`**, **`Baseline`** nesting — aligned to [`AUDIT_COVERAGE_MATRIX.md`](../library/AUDIT_COVERAGE_MATRIX.md). |

---

## Completed in code (track ADR archival separately)

| Item | Resolution |
|------|------------|
| **`AuditEventTypes.CoordinatorRun*`** literals | **Removed** — regression guard **`Legacy_CoordinatorRun_audit_constants_are_removed_from_AuditEventTypes`** in **[`DependencyConstraintTests`](../../ArchLucid.Architecture.Tests/DependencyConstraintTests.cs)** (**only** remaining `CoordinatorRun` substring hits in **`*.cs`** are that test). |
| **`CoordinatorPipelineDeprecationFilter`** + **`CoordinatorPipelineDeprecatedAttribute`** + coordinator-only deprecation tests | **Deleted** per [ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md) header (**PR A final cleanup**). |

---

## Completed in docs (PR B — 2026-05-05)

| Item | Resolution |
|------|------------|
| Phase 3 **PR B** (audit-constant retirement checklist) | **Closed** on [ADR 0029](../adr/0029-coordinator-strangler-acceleration-2026-05-15.md) § Lifecycle § PR B; former working-surface file **`PHASE_3_PR_B_TODO.md`** and **`assert_pr_b_tracker_in_sync.py`** retired; [ADR 0010](../adr/0010-dual-manifest-trace-repository-contracts.md) + [ADR 0021](../adr/0021-coordinator-pipeline-strangler-plan.md) superseded by [ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md). |

---

## Remaining product / ADR follow-up

| Item | Notes |
|------|-------|
| Operator **`POST /v1/architecture/*`** lifecycle | Routes **persist** through PR A3/A4; **narrowing/removal** needs a **future ADR**. Deprecation signalling is **`ApiDeprecation:*`** appsettings (**not** a coordinator-only filter — see Migrate table). |

---

## Related automation

- Reference-count ceiling (non-test `.cs` hits vs baseline): `scripts/ci/assert_coordinator_reference_ceiling.py`
- Archived dual-path map: `docs/archive/dual-pipeline-navigator-superseded.md`
- Superseded completion scaffold: `docs/adr/0028-coordinator-strangler-completion.md` (**Superseded by** [ADR 0029](../adr/0029-coordinator-strangler-acceleration-2026-05-15.md))

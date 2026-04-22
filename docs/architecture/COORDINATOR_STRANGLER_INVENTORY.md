> **Scope:** Living inventory for ADR 0021 coordinator strangler — symbols and route families tagged migrate / keep / delete with ownership and risk notes. Complements `DualPipelineRegistrationDisciplineTests` (type-level allow list) and `scripts/ci/assert_coordinator_reference_ceiling.py` (reference-count ceiling).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Coordinator strangler inventory

**Objective.** Make Phase 3 retirement work visible and reviewable without guessing which symbols still anchor the coordinator pipeline.

**Assumptions.** Authority remains the supported long-term operator pipeline; coordinator contracts persist until exit gates in ADR 0021 / draft ADR 0028 clear.

**Constraints.** No runtime routing change from this document alone; owner-only dates and ADR 0022 state flips stay in `docs/PENDING_QUESTIONS.md` item 16.

> **2026-04-21 grounding-read finding ([ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md)).** The two pipelines persist **incompatible domain models** to **incompatible SQL tables** using **different decision engines**. Specifically:
>
> | Surface | Coordinator | Authority |
> |---------|-------------|-----------|
> | Manifest CLR type | `ArchLucid.Contracts.Manifest.GoldenManifest` (string `RunId`; services + datastores + relationships + governance + metadata) | `ArchLucid.Decisioning.Models.GoldenManifest` (Guid `ManifestId` + Guid scope triple; Topology / Security / Compliance / Cost / Constraints / UnresolvedIssues / Decisions / Provenance / Policy section objects) |
> | SQL table(s) | `dbo.GoldenManifestVersions` (single JSON blob keyed by string `ManifestVersion`; line 105 of `ArchLucid.sql`) | `dbo.GoldenManifests` + 6 phase-1 relational satellite tables (line 987 of `ArchLucid.sql`); keyed by Guid `ManifestId` + scope triple |
> | Decision engine | `IDecisionEngineService.MergeResults` + `IDecisionEngineV2.ResolveAsync` | One-shot rule-engine path |
> | Persistence port | `ICoordinatorGoldenManifestRepository` (`CreateAsync` / `GetByVersionAsync`) | `IGoldenManifestRepository` (`SaveAsync` / `GetByIdAsync`) |
>
> A single-PR PR A deletion (as originally framed in [ADR 0021](../adr/0021-coordinator-pipeline-strangler-plan.md) § Phase 3 mechanism (a) and accelerated by [ADR 0029](../adr/0029-coordinator-strangler-acceleration-2026-05-15.md)) is therefore **mechanically impossible**. The work is re-scoped into the **PR A0 → PR A4** sequence in [ADR 0030 § Component breakdown](../adr/0030-coordinator-authority-pipeline-unification.md). The Delete (blocked) section below stays in force until **PR A3** ships per ADR 0030 § Lifecycle.

---

## Migrate (target: authority façade or unified reader)

| Symbol / route family | Owning assembly | Last touched PR | Risk note |
|----------------------|-----------------|-----------------|----------|
| `ICoordinatorGoldenManifestRepository` write consumers outside unified reader | `ArchLucid.Application` | _PR link TBD_ | New readers must use `IUnifiedGoldenManifestReader`; extra write-side consumers extend Phase 3 scope. |
| `ICoordinatorDecisionTraceRepository` | `ArchLucid.Application`, `ArchLucid.Persistence` | _PR link TBD_ | Collapsing with authority trace port risks audit wire collisions (see ADR 0010). |
| `POST /v1/architecture/*` coordinator run lifecycle | `ArchLucid.Api` | _PR link TBD_ | Public clients still use string `RunId` path; migrate only with dual-run support and contract tests. |
| `RunCommitOrchestratorFacade` coordinator branch | `ArchLucid.Application` | _PR link TBD_ | Facade intentionally hides coordinator vs authority selection; changing split affects replay parity. |

---

## Keep (stable until superseding ADR)

| Symbol / route family | Owning assembly | Last touched PR | Risk note |
|----------------------|-----------------|-----------------|----------|
| `IUnifiedGoldenManifestReader` | `ArchLucid.Persistence` | _PR link TBD_ | Phase 1 read façade — expand HTTP consumers here, not new coordinator repos. |
| `DualPipelineRegistrationDisciplineTests` | `ArchLucid.Api.Tests` | _PR link TBD_ | Pins DI split + `ICoordinatorGoldenManifestRepository` allow list; do not weaken without ADR update. |
| `AuditEventTypes_DoNotCollideAcrossPipelinesTests` | `ArchLucid.Core.Tests` | _PR link TBD_ | Prevents coordinator/authority audit constant collisions on the wire. |
| `scripts/ci/coordinator_parity_probe.py` | repo root | _PR link TBD_ | Nightly parity row append to `COORDINATOR_TO_AUTHORITY_PARITY.md`. |

---

## Delete (blocked — requires exit gates)

| Symbol / route family | Owning assembly | Last touched PR | Risk note |
|----------------------|-----------------|-----------------|----------|
| `ICoordinatorGoldenManifestRepository` (interface) | `ArchLucid.Persistence` | _PR link TBD_ | **Do not delete** until ADR 0021 Phase 3 + replay/run-volume parity evidence; deleting early revives ADR 0010 collision class. |
| `ICoordinatorDecisionTraceRepository` | `ArchLucid.Persistence` | _PR link TBD_ | Same as manifest port — needs write-side façade and migration window. |
| Legacy `Coordinator*` HTTP route tree wholesale | `ArchLucid.Api` | _PR link TBD_ | Customer and demo scripts still target `/v1/architecture/*`; needs deprecation ADR and CLI cut-over. |

---

## Related automation

- Reference-count ceiling (non-test `.cs` hits vs baseline): `scripts/ci/assert_coordinator_reference_ceiling.py`
- Archived dual-path map: `docs/archive/dual-pipeline-navigator-superseded.md`
- Draft completion ADR scaffold: `docs/adr/0028-coordinator-strangler-completion.md`

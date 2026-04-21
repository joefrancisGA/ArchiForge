> **Scope:** Boundary tests — what's pinned today, what's still missing - full detail, tables, and links in the sections below.

# Boundary tests — what's pinned today, what's still missing

> **Scope:** Inventory of mechanical layer-boundary tests in `ArchLucid.Architecture.Tests` and the next ring of assertions to add. This document exists because Improvement #3 in `docs/archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_75_37.md` originally proposed creating the test project — that project already exists. The remaining work is the next ring, not the floor.

## What is pinned today (`ArchLucid.Architecture.Tests/DependencyConstraintTests.cs`)

### Tier 1 — foundation isolation

| Assertion | Mechanism |
|---|---|
| `Core_must_not_depend_on_any_solution_project` | NetArchTest `HaveDependencyOnAny(ForbiddenFromCore)` |
| `Contracts_must_not_depend_on_any_solution_project` | NetArchTest `HaveDependencyOnAny(ForbiddenFromContracts)` |
| `ContractsAbstractions_may_only_depend_on_Contracts` | NetArchTest `HaveDependencyOnAny(ForbiddenFromContractsAbstractions)` |

### Tier 2 — persistence sub-module DAG

| Assertion |
|---|
| `Coordination_must_not_reference_Runtime` |
| `Coordination_must_not_reference_Advisory` |
| `Coordination_must_not_reference_Alerts` |
| `Integration_must_not_reference_Runtime` |
| `Integration_must_not_reference_Advisory` |
| `Integration_must_not_reference_Alerts` |

### Tier 3 — hexagonal isolation

| Assertion |
|---|
| `Decisioning_must_not_depend_on_Persistence` |
| `KnowledgeGraph_must_not_depend_on_Persistence` |
| `ContextIngestion_must_not_depend_on_Persistence` |
| `ArtifactSynthesis_must_not_depend_on_Persistence` |

### Tier 4 — CLI isolation

| Assertion |
|---|
| `Cli_must_not_depend_on_Persistence` |
| `Cli_must_not_reference_Api_assembly` |

### Custom source-scanning lints

| Assertion |
|---|
| `Product_code_must_not_call_IIntegrationEventPublisher_PublishAsync_outside_authorized_wrappers` |
| `Application_references_Core_for_consolidated_audit_event_type_catalog` |

That is **17 assertions** with clear failure messages — strong floor.

## What is missing (the next ring)

### Tier 5 — API host isolation

| Proposed assertion | Why |
|---|---|
| `Api_must_not_depend_on_any_Persistence_assembly` | Today `ArchLucid.Api`'s controllers reach Application services, but nothing prevents a future controller from new-ing up a `Sql*` repository directly. NetArchTest `HaveDependencyOn("ArchLucid.Persistence")` on the Api assembly. |
| `Api_must_not_depend_on_AgentRuntime` | Same reason — agents are reached via `Application` only. |
| `Worker_must_not_depend_on_Api` | Workers should consume contracts and DI services, not host types. |

### Tier 6 — Application boundary

| Proposed assertion | Why |
|---|---|
| `Application_may_depend_on_Persistence_only_via_interfaces_in_Contracts_Abstractions` | This is the "Application talks to ports, not adapters" rule. **Risky:** `Application` legitimately ships some Persistence-shaped types; expect quarantine on first run. |
| `Application_must_not_depend_on_Host_Composition` | Composition is a host concern; Application should be host-agnostic. |

### Tier 7 — file-shape lints

| Proposed assertion | Why |
|---|---|
| `OneTopLevelPublicTypePerFile` | Repo rule "each class must be in its own file" — currently convention-only. Source scan over `*.cs` (skip `obj/`, `bin/`, generated-by-csproj). |
| `NoSiblingPersistenceCircularReferences` | Confirm the persistence sub-module DAG is acyclic by graph-walking the references at test time (defence in depth on top of Tier 2). |

## How to add a new assertion (3 steps)

1. Drop a single new `[Fact]` method into `DependencyConstraintTests.cs`. Keep one fact per rule for clear CI output.
2. If the rule needs a namespace allow/deny list, extend `ArchitectureConstraintNamespaces.cs`.
3. If the assertion fails on legitimate existing code, add `[Trait("Category", "Quarantine")]` and a `// TODO(architecture): <issue link>` comment so the test still runs but the violation is tracked, not blocking.

## Companion: project consolidation

`docs/PROJECT_CONSOLIDATION_PROPOSAL.md` is open. After landing the Tier 5 assertions, pick the two smallest sibling projects with the heaviest mutual coupling and merge them under an ADR (`docs/adr/0022-project-consolidation-step-1.md`). Update this roadmap when that lands.

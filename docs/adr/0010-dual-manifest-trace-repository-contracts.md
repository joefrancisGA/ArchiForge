> **Scope:** ADR 0010: Dual manifest and decision-trace repository contracts - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ADR 0010: Dual manifest and decision-trace repository contracts

- **Status:** Superseded by [ADR 0030 — Coordinator → Authority pipeline unification](0030-coordinator-authority-pipeline-unification.md) *(2026-05-05, PR B)* — the Coordinator repository interface family is retired; manifests and traces converge on the **Authority** (`ArchLucid.Decisioning.Interfaces`) persistence path.
- **Date:** 2026-04-04

## Context

ArchLucid persists golden manifests and decision traces in two different lifecycles:

1. **Run / commit pipeline (coordinator)** — manifests and traces are created and read with run-scoped APIs (`CreateAsync`, `GetByVersionAsync`, batch traces by run). These contracts live in **`ArchLucid.Persistence.Data.Repositories`** (`IGoldenManifestRepository`, `IDecisionTraceRepository`) and are implemented with Dapper against coordinator SQL tables, or with in-memory types when `ArchLucid:StorageProvider=InMemory`.

2. **Authority / decisioning persistence** — manifests and traces are saved and loaded with authority-oriented shapes (`SaveAsync`, scoped `GetByIdAsync`) for advisory and policy flows. These contracts live in **`ArchLucid.Decisioning.Interfaces`** and are registered in `AddArchLucidStorage`, with SQL or in-memory implementations that are distinct from the Data-layer types.

Both families use similar names, which risks mistaken DI registrations if interface types are not fully qualified at registration time.

## Decision

Keep **two interface families** permanently:

- **Data repositories** — coordinator execution, commit, replay, and governance features that need run/manifest versioning semantics.
- **Decisioning interfaces** — authority graph, policy packs, and related decisioning storage.

API startup registers Data-layer manifest/trace repositories inside **`RegisterCoordinatorDecisionEngineAndRepositories`**, using **fully qualified** interface types (e.g. `ArchLucid.Persistence.Data.Repositories.IGoldenManifestRepository`) so they do not collide with Decisioning registrations from **`AddArchLucidStorage`**.

## Consequences

- **Positive:** Clear boundaries between "run artifact" persistence and "authority" persistence; teams can evolve each without breaking the other.
- **Negative:** Developers must know which interface a feature should depend on; duplicate names require discipline at registration and in code review.
- **Operational:** In-memory hosts must register **both** Decisioning and Data in-memory implementations where features touch both paths (see ADR 0011).

## Related

- `docs/ARCHITECTURE_COMPONENTS.md` (dual manifest / trace section).
- ADR 0011 (storage provider branching).
- `docs/GLOSSARY.md` (golden manifest, decision trace entries).

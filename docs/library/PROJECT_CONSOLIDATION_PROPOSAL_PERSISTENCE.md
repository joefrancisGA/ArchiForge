> **Scope:** Persistence project fan-in consolidation (proposal) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Persistence project fan-in consolidation (proposal)

**Status:** Proposal (2026-04-20)  
**Predecessors:** [PERSISTENCE_SPLIT.md](PERSISTENCE_SPLIT.md) (April 2026 split narrative), [PROJECT_CONSOLIDATION_PROPOSAL.md](PROJECT_CONSOLIDATION_PROPOSAL.md) (documentation spine).

## 1. Objective

Reduce **six** shipping persistence assemblies (`ArchLucid.Persistence` + five `ArchLucid.Persistence.*` satellites, excluding tests) to **two** logical packages — **`ArchLucid.Persistence.Read`** and **`ArchLucid.Persistence.Write`** — while keeping **DbUp migrations**, **`ArchLucid.sql`**, and **RLS object names** untouched (rename checklist owns those).

## 2. Assumptions

- Consolidation is **structural** (project boundaries + namespaces), not a rewrite of SQL or repository semantics.
- Downstream projects (`ArchLucid.Api`, `ArchLucid.Application`, `ArchLucid.Coordinator`, `ArchLucid.Decisioning`, `ArchLucid.Worker`, CLIs) tolerate `ProjectReference` churn if each PR is small and CI stays green.
- `ArchLucid.Persistence.Tests` remains the primary regression harness for SQL-facing code.

## 3. Constraints

- **Do not** edit historical migration files **001–028**; only append new migrations and update `ArchLucid.Persistence/Scripts/ArchLucid.sql` per repo policy.
- **Do not** rename RLS identifiers in this initiative ([MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md)).
- **No circular references:** keep a DAG: `Read` ← `Write` is invalid; prefer **`Write` → `Read`** only if write paths need read types, otherwise keep both leaf-under-Host.

## 4. Current inventory (2026-04-20)

| Project | Approx. `.cs` files | Primary responsibility |
| --- | ---: | --- |
| `ArchLucid.Persistence` | 242 | Core Dapper repositories, DbUp wiring, scripts, foundational types |
| `ArchLucid.Persistence.Coordination` | 76 | Compare, replay, evolution, retrieval coordination repos |
| `ArchLucid.Persistence.Runtime` | 36 | Runtime glue consumed by host/worker |
| `ArchLucid.Persistence.Advisory` | 33 | Advisory / digest persistence |
| `ArchLucid.Persistence.Alerts` | 28 | Alert pipeline persistence |
| `ArchLucid.Persistence.Integration` | 19 | Integration-style persistence edges |

**Tests:** `ArchLucid.Persistence.Tests` (excluded from the two-target split — stays as the integration/unit home).

## 5. Proposed mapping (high level)

| Today | Proposed home |
| --- | --- |
| Pure SELECT/query repositories, list/detail readers, snapshot readers | **`Persistence.Read`** |
| INSERT/UPDATE paths, outbox writers, transactional orchestration repos, mutation-heavy coordination | **`Persistence.Write`** |
| Types used equally by read/write (e.g. DTOs, enums, SQL builders) | **`Persistence.Read`** first; **`Write` references `Read`** when unavoidable |

`Persistence.Runtime` either (a) merges into **`Write`** if it only hosts mutation/runtime adapters, or (b) remains a **thin shim** project for one release if host composition needs stable `InternalsVisibleTo` targets.

## 6. Downstream impact (initial)

| Consumer | Expected touch |
| --- | --- |
| `ArchLucid.Application` | ProjectReference swap; namespace `using` churn |
| `ArchLucid.Api` | Usually none beyond transitive restore noise |
| `ArchLucid.Coordinator` / `ArchLucid.Decisioning` | Direct repo interfaces may move namespaces |
| `ArchLucid.Worker` | Same as Application |
| `ArchLucid.Host.Composition` | DI registration files may need type relocations |

## 7. Migration sequence (keep CI green)

1. **Shim release (no behavior change):** introduce empty `ArchLucid.Persistence.Read` / `ArchLucid.Persistence.Write` projects that type-forward or re-export **one** moved namespace each PR (e.g. move `ArchLucid.Persistence.Data.X` → `ArchLucid.Persistence.Read.Data.X` with `TypeForwardedTo` only if needed — prefer plain moves + fixups).
2. **Move vertical slices:** pick one bounded area (e.g. Alerts) and move its repos + interfaces together; run `ArchLucid.Persistence.Tests` + affected integration tests.
3. **Retire empty satellite `.csproj`** files when file count hits zero; update solution filter folders.
4. **Final rename (optional):** collapse `ArchLucid.Persistence` core into `Read` if the name is redundant — only after all call sites read `Persistence.Read`.

## 8. Test impact

- Expect **`ArchLucid.Persistence.Tests`** namespace updates only when types move.
- **`ArchLucid.Api.Tests`** / **`ArchLucid.Application.Tests`** may need `using` fixes when they new-up concrete repos in tests.

## 9. Explicit non-goals

- No SMB / public file-share paths; storage stays Azure-native and private per existing ADRs.
- No Terraform `state mv` in this proposal (Phase 7.5 checklist).

## 10. Decision

Treat this document as **backlog shaping** until a release owner schedules the first shim PR. Update counts when file totals shift materially.

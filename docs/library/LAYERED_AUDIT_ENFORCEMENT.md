# Layered audit enforcement

## Objective

Raise the odds that **new mutation surfaces** (not just HTTP controllers) either emit **`IAuditService.LogAsync`** or are explicitly justified, without blocking teams with impossible guards on day one.

## What runs in CI today

| Guard | Purpose |
|-------|---------|
| `scripts/ci/assert_controller_mutations_have_audit.py` | Controllers issuing POST/PUT/PATCH/DELETE must call `LogAsync` (or controller allowlist) |
| `scripts/ci/assert_layered_audit_wiring_echo.py` | Repo-root tripwire ensuring critical **`AuditEventTypes.*`** literals remain reachable in known orchestrators after refactors |
| `ArchLucid.Application.Tests/Audit/BaselineMutationAuditDualWritePairingTests` | **`IBaselineMutationAuditService.RecordAsync`** call sites pair with **`LogAsync`/`TryLogAsync`** unless allowlisted filenames |

### Allowlists

| File | Intended use |
|------|----------------|
| `scripts/ci/controller_action_audit_allowlist.txt` | Rare controller exemptions where downstream services own audit (`FullyQualified.Class.Method`) |
| `scripts/ci/service_level_audit_echo_allowlist.txt` | Reserved companion for heuristic service/repo guards |

## Operational notes

**Pairing (#2)** favors **centralized durable echo** (`BaselineMutationAuditService`). Files that deliberately centralize echoes should justify themselves in **`AllowedBaselineOnlyFiles`** (keep that list microscopic).

**Echo script (#layer 3 lite)** deliberately uses **literal substring anchors** (`AuditEventTypes.RequestLocked`, …) — if you rename/move types, refresh `FILES_AND_MARKERS` in `scripts/ci/assert_layered_audit_wiring_echo.py`.

## Deferred work

Per `docs/library/AUDIT_COVERAGE_MATRIX.md`, **`ManifestSuperseded`** awaits a persisted supersession pathway; **`RequestReleased`** presently triggers when the authority commit path observes **zero remaining active runs** (`IRunRepository.CountActiveRunsForArchitectureRequestAsync`). If a future centralized engine marks runs **`Failed`** without passing through authority commit semantics, revisit whether an additional **`RequestReleased`** latch is warranted.

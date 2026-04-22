> **Scope:** Architecture team — captures the original scaffold for completing the coordinator → authority strangler. **Superseded by ADR 0029**; preserved for audit history. Not a current execution plan — read ADR 0029 for the live plan.
>
> **Status:** Superseded by [ADR 0029](0029-coordinator-strangler-acceleration-2026-05-15.md) (2026-04-21)
> **Supersedes / relates:** [0021-coordinator-pipeline-strangler-plan.md](0021-coordinator-pipeline-strangler-plan.md)

# ADR 0028 — Coordinator strangler completion (scaffold)

> **Superseded 2026-04-21.** The `_TODO (owner)_` placeholders in this scaffold (calendar date for Phase 3 completion + ADR 0022 state transition) were resolved by owner Q&A on 2026-04-21 and are now recorded in **[ADR 0029 — Coordinator strangler acceleration to 2026-05-15](0029-coordinator-strangler-acceleration-2026-05-15.md)**. Read ADR 0029 for the calendar date, the post-PR-A 30-day-soak-gate waiver rationale (pre-release context), the atomic surface area for the `Sunset:` constant change, and the lifecycle for ADR 0022. The exit-gate framing below remains conceptually correct but is **not** the operative decision record — ADR 0029 is.

## Objective

Document the **completion** criteria for ADR 0021 Phase 3 so the dual repository tax cannot return without an explicit ADR update.

## Assumptions

- Authority pipeline remains the long-term operator path.
- Coordinator HTTP and persistence ports remain until replay and run-volume parity evidence exists.

## Constraints

- **Owner-only:** completion calendar date, ADR 0022 state transition, auto-commit vs bot PR for parity markers (`docs/PENDING_QUESTIONS.md` item 16).
- No production routing change from this draft alone.

## Decision

_TODO (owner):_ record the agreed cut-over strategy (read façade expansion first, then write-side façade, then interface deletion).

## Consequences

- Positive: smaller DI surface; fewer dual-pipeline audit collision tests required.
- Negative: breaking change risk for CLI and integrators on `ArchitectureRuns` string ids unless dual-run bridge is retained for a window.

## Exit gates

1. **Replay parity:** coordinator vs authority replay suites green on representative tenant corpus.  
2. **Run volume / perf:** documented comparison within agreed SLO envelope.  
3. **Customer audit:** no regression in `AuditEventTypes` wire constants without `AuditEventTypes_DoNotCollideAcrossPipelinesTests` update.  
4. **Reference ceiling:** `scripts/ci/assert_coordinator_reference_ceiling.py` baseline **reduced** (counts go down), not silently raised.

## Completion date

_TODO (owner):_ calendar date for Phase 3 completion target.

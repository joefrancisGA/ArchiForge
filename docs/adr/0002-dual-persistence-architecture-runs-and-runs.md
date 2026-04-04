# ADR 0002: Dual persistence (ArchitectureRuns vs dbo.Runs)

- **Status:** Accepted (convergence in progress)
- **Date:** 2026-04-04

## Context

Historical **`dbo.ArchitectureRuns`** (string run id) coexists with authority **`dbo.Runs`** (GUID). Some idempotency and CLI paths still touch the legacy table.

## Decision

Treat **`dbo.Runs`** as the **authority source of truth** for new features. **`ArchitectureRuns`** remains for compatibility until fully migrated.

## Consequences

- **Positive:** Clear direction for new code; ROWVERSION and RLS target authority tables first.
- **Negative:** Idempotency and rare races cannot assume a single global transaction across both tables — see `docs/DATA_CONSISTENCY_MATRIX.md`.
- **Schedule:** Product default dates and epic tag **`RunsAuthorityConvergence`** are in **`docs/DATA_CONSISTENCY_MATRIX.md`** (write freeze, read convergence, legacy removal). Extend only via ADR.

## Links

- `docs/DATA_CONSISTENCY_MATRIX.md`

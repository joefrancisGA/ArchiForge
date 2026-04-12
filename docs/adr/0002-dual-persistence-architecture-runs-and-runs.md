# ADR 0002: Dual persistence (ArchitectureRuns vs dbo.Runs)

- **Status:** Superseded — see **ADR 0012** (completed 2026-04-12): legacy **`dbo.ArchitectureRuns`** and **`IArchitectureRunRepository`** removed; **`dbo.Runs`** is the sole run header table.
- **Date:** 2026-04-04

## Context (historical)

Historical **`dbo.ArchitectureRuns`** (string run id) coexisted with authority **`dbo.Runs`** (GUID). Idempotency and coordinator paths used the string key while authority artifacts used the GUID header.

## Decision (historical)

Treat **`dbo.Runs`** as the **authority source of truth** for new features. **`ArchitectureRuns`** remained for compatibility until fully migrated.

## Consequences (historical)

- **Positive:** Clear direction for new code; ROWVERSION and RLS target authority tables first.
- **Negative:** Idempotency and rare races could not assume a single global transaction across both tables — see archived **`docs/DATA_CONSISTENCY_MATRIX.md`** revisions pre-049.

## Links

- `docs/adr/0012-runs-authority-convergence-write-freeze.md` — completion record
- `docs/DATA_CONSISTENCY_MATRIX.md`

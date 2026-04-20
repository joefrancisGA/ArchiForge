> **Scope:** Stryker mutation score ratchet — target 72% - full detail, tables, and links in the sections below.

# Stryker mutation score ratchet — target 72%

## Why this exists

Raising **`scripts/ci/stryker-baselines.json`** and **`thresholds.break` / `thresholds.low`** in each `stryker-config*.json` to **72** without first achieving that score will fail **`.github/workflows/stryker-scheduled.yml`** (assert vs baseline minus tolerance).

## How to ratchet safely

1. Add or strengthen tests so surviving mutants decrease (negative-path E2E does not affect Stryker; add **unit/integration** coverage in the module under mutation).
2. From repo root (after `dotnet tool restore`):

   ```bash
   python3 scripts/ci/refresh_stryker_baselines.py
   ```

3. If every module reports **≥ 72.0** (floored one decimal), update all five `stryker-config*.json` blocks to `"low": 72, "break": 72` and set each **`mutationScore`** in **`stryker-baselines.json`** to the refreshed values (never lower without a product decision).
4. Push; confirm the scheduled Stryker workflow is green.

## Related

- **`docs/MUTATION_TESTING_STRYKER.md`** — full narrative.
- **`docs/TEST_STRUCTURE.md`** — Stryker matrix labels.

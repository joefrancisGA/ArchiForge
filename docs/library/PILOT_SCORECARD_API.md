> **Scope:** What exists today for **pilot scorecard** and **ROI baselines** in product vs manual spreadsheets.

# Pilot scorecard and ROI baselines

**Last reviewed:** 2026-04-27

## Shipped API (tenant-scoped, authenticated)

| Method | Route | Role |
|--------|-------|------|
| `GET` | `/v1/pilots/outcome-summary` | Trailing 30-day rollup (`PilotScorecardResponse`) for the current tenant. |
| `POST` | `/v1/pilots/scorecard` | JSON scorecard for a custom UTC window (`periodStart` / `periodEnd` in body). |

Implementation aggregates from `IRunRepository` in scope (runs in period, count with committed manifest). See `PilotScorecardBuilder` and `PilotsController` in the API project.

**Named `PilotBaselines` as a first-class persisted table** is **not** required for the above — executive ROI **manual baselines** (review hours, people per review) are stored on the **tenant** model for the ROI calculator (`DapperTenantRepository.UpdateBaseline*`). Use those fields for pilot “before” numbers; re-measure with the scorecard for “after” run volumes.

## Operations

- Broader pilot narrative: [`CORE_PILOT.md`](../CORE_PILOT.md)
- ROI model: [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md)
- If you add durable baseline snapshots in a future version, keep them **tenant-scoped** and **append-only** in line with the audit model.

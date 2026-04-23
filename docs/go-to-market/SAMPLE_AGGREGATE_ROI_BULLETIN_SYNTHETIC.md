> **Scope:** Synthetic aggregate ROI bulletin sample. **FORBIDDEN (repository hygiene):** Do not append this document to `docs/CHANGELOG.md`. Do not add a `## YYYY-MM-DD — ROI bulletin signed:` section for this synthetic artefact. Sign-off audit format applies only to real published bulletins (see `docs/go-to-market/AGGREGATE_ROI_BULLETIN_TEMPLATE.md`).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — aggregate review-cycle baseline bulletin (SYNTHETIC EXAMPLE)

**Quarter:** Q1-2026 (illustrative label only)
**Generated:** (static sample — not tied to a live SQL window)
**Qualifying tenants (N):** 5 (synthetic floor matching the real minimum-N gate)

## Headline numbers (tenant-supplied baseline hours only)

| Metric | Hours | Note |
|--------|------:|------|
| Mean | 22.4 | synthetic example — never published as a real bulletin |
| p50 | 20 | synthetic example — never published as a real bulletin |
| p90 | 46 | synthetic example — never published as a real bulletin |

## Interpretation guardrails

- These figures are **illustrative** so buyers can see table shape before **N ≥ 5** paying tenants with captured baselines exist.
- They are **not** ArchLucid runtime measurements and **not** SQL-sourced aggregates.
- Per-run sponsor deltas (findings histogram, audit counts, LLM calls) come from the same demo-seed story as `PilotRunDeltaComputer` in first-value reports; this bulletin slice only models **aggregate baseline hours** shape.

## Related

- [`AGGREGATE_ROI_BULLETIN_TEMPLATE.md`](AGGREGATE_ROI_BULLETIN_TEMPLATE.md)
- [`PILOT_ROI_MODEL.md`](../library/PILOT_ROI_MODEL.md)

> **Scope:** Quarterly **aggregate** ROI bulletin template for GTM and leadership — sanitized statistics only; not a vehicle for per-customer disclosure.

# Aggregate ROI bulletin — template (internal draft)

## Owner-approval gate (mandatory)

**No version of this bulletin may be published externally** (web, email to prospects, press, or partner decks) without **explicit owner sign-off** recorded per [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **27** (cadence, percentile bands, minimum N, signatory). Engineering may generate **drafts** from production using `archlucid roi-bulletin` (see [`docs/CLI_USAGE.md`](../CLI_USAGE.md)); publication remains **owner-only**.

## Minimum-N privacy guard

- The bulletin **must** aggregate **≥ 5 tenants** that have **tenant-supplied** `BaselineReviewCycleHours` captured in the reporting quarter (`BaselineReviewCycleCapturedUtc` window). Drafts **must refuse** to render public numbers below that threshold — the CLI and API return **400** / exit **UsageError** when `--min-tenants` is not met.
- **Never** attach a **per-tenant** row, customer name, or free-text baseline source string to the published bulletin body.

## Allowed statistics (this template)

| Statistic | Allowed? | Notes |
|-----------|----------|-------|
| Count of qualifying tenants | Yes | Integer only. |
| Mean baseline hours | Yes | Aggregate across qualifying tenants. |
| Median (p50) baseline hours | Yes | Use SQL `PERCENTILE_CONT(0.5)` semantics on the qualifying set. |
| p90 baseline hours | Yes | Upper tail for “heavy review culture” sensitivity; label as p90, not “max”. |
| Per-tenant baseline hours | **No** | Violates the privacy posture of this bulletin. |
| Measured time-to-commit per tenant | **No** | Same — aggregate measured stats belong in a **separate** engineering bulletin with its own owner gate. |

## Draft body skeleton (Markdown)

```markdown
# ArchLucid — aggregate review-cycle baseline bulletin (INTERNAL)

**Quarter:** Q?_-____
**Generated:** <UTC ISO timestamp>
**Qualifying tenants (N):** <integer ≥ 5>

## Headline numbers (tenant-supplied baseline hours only)

| Metric | Hours |
|--------|------:|
| Mean   | _._ |
| p50    | _._ |
| p90    | _._ |

## Interpretation guardrails

- These numbers describe **self-reported pre-ArchLucid review-cycle length** for tenants who **chose** to supply a baseline at signup — **not** ArchLucid runtime performance.
- “Before vs measured” product charts for any **single** tenant remain **in tenant-scoped operator surfaces** (value report / first-value report).

## Sign-off

| Role | Name | Date | Signature / link |
|------|------|------|------------------|
| Owner | | | |
```

## Automation

- **API:** `GET /v1/admin/roi-bulletin-preview?quarter=Q1-YYYY&minTenants=5` (AdminAuthority).
- **CLI:** `archlucid roi-bulletin --quarter Q1-YYYY [--min-tenants 5] [--out draft.md]` — uses `ARCHLUCID_API_KEY` with admin scope.

## Related

- [`TRIAL_BASELINE_PRIVACY_NOTE.md`](TRIAL_BASELINE_PRIVACY_NOTE.md) — how the per-tenant baseline field is used and *not* used.
- [`docs/PILOT_ROI_MODEL.md`](../PILOT_ROI_MODEL.md) — modeled default when prospects skip custom hours.

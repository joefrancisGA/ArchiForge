# Penetration test — remediation tracker

**Audience:** Security lead, engineering owners, release managers.

**Status:** Template — populate rows when vendor findings are triaged. Redacted summaries remain **NDA-gated** per [`README.md`](README.md) in this folder.

## Workflow

1. **Intake** — vendor assigns finding IDs; copy into this table.
2. **Triage** — severity, owner, target date.
3. **Fix** — link PRs in Notes; keep status `In Progress` until merged and verified.
4. **Verify** — re-test or vendor retest; set `Remediated` or `Accepted Risk` with rationale.
5. **Close** — never delete rows; append closure note in Notes.

Allowed **Status** values: `Open`, `In Progress`, `Remediated`, `Accepted Risk`, `Deferred`.

## Remediation table

| Finding ID | Severity | Title | Status | Owner | Target date (UTC) | Verification | Notes |
|------------|----------|-------|--------|-------|-------------------|----------------|-------|
| *(template)* | — | *(no rows yet — engagement in flight per [`2026-Q2-SOW.md`](2026-Q2-SOW.md))* | — | — | — | — | — |

## CI hygiene

`scripts/ci/assert_pen_test_remediation_no_stale.py` scans this table for **Open** rows whose **Target date** is in the past (warning) and for **Critical** + **Open** older than 30 days (blocking when strict mode is enabled). Wire the script in `.github/workflows/ci.yml`.

## Related

- [`2026-Q2-SOW.md`](2026-Q2-SOW.md)
- [`docs/trust-center.md`](../../trust-center.md)

> **Scope:** Penetration test redacted summaries — publication folder - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Penetration test redacted summaries — publication folder

**Audience:** Security leadership, procurement, and customer-facing sales engineering.

**Purpose:** This directory is the **single place** redacted third-party penetration test summaries land once an engagement completes. It mirrors the structure of [`PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md`](../PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md) but stores **customer-ready** copies with internal hostnames, credentials, and stack traces removed.

## Publication discipline

1. **Source of truth** remains the assessor’s full report (not committed here).
2. **Redaction:** follow the checklist in the template’s preamble; scrub internal URLs, personal emails, and repro steps that expose live tenant ids.
3. **Cadence:** publish within **10 business days** of customer approval (align with [`SECURITY.md`](../../library/SECURITY.md) acknowledgment SLA where applicable).
4. **Naming:** `YYYY-Qn-<customer-slug>-REDACTED.md` for published files; `YYYY-Qn-DRAFT.md` for placeholders awaiting vendor assignment.
5. **PGP:** when [`SECURITY.md`](../../library/SECURITY.md) publishes a disclosure encryption key, link it from the published summary header.

## Files

| File | Status |
| --- | --- |
| [`2026-Q2-SOW.md`](2026-Q2-SOW.md) | **Awarded** SoW — assessor **Aeronova Red Team LLC**; kickoff **2026-05-06** |
| [`2026-Q2-REDACTED-SUMMARY.md`](2026-Q2-REDACTED-SUMMARY.md) | Pre-report placeholder — replace severity/remediation tables after assessor redacted delivery |
| [`2026-Q2-DRAFT.md`](2026-Q2-DRAFT.md) | Legacy placeholder — superseded by SoW + redacted stub above when engaging assessors |
| [`../OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`](../OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md) | **Owner self-assessment** (not third-party) — interim buyer transparency; does not live in this folder once finalized |


## Related

- [`../PEN_TEST_SOW_TEMPLATE.md`](../PEN_TEST_SOW_TEMPLATE.md)
- [`../PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md`](../PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md)
- [`../../go-to-market/TRUST_CENTER.md`](../../go-to-market/TRUST_CENTER.md) — public **Recent assurance activity** table records engagement metadata (vendor, scope, completion date) without exposing redacted findings; this folder remains the in-repo working surface.
- [`../../SECURITY.md`](../../library/SECURITY.md)

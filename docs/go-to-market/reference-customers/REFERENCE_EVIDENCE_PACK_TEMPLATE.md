> **Scope:** One-page **template** for a single customer reference pack. Replace `<<…>>` placeholders. Every **computed** line must map to `pilot-run-deltas.json` produced by `archlucid reference-evidence` (or the admin ZIP).

> **Spine doc:** [Five-document onboarding spine](../../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Reference evidence pack — `<<CUSTOMER_NAME>>`

**Status:** Draft — internal only until legal sign-off.

---

## Logo

`<<LOGO_URI_OR_ATTACH>>`

---

## Problem statement (before ArchLucid)

`<<2_4_SENTENCES_CUSTOMER_VOICE>>`

---

## Measured deltas (from `pilot-run-deltas.json`)

> Fill from the CLI export. Property names refer to **camelCase** JSON from `GET /v1/pilots/runs/{runId}/pilot-run-deltas`. **Internal format-only sample:** [`samples/pilot-run-deltas.demo-tenant.json`](samples/pilot-run-deltas.demo-tenant.json) (must remain **demo tenant — replace before publishing** until a customer export replaces it).

| Metric | Value | JSON field |
|--------|------:|------------|
| Wall-clock request → committed manifest | `<<HH:MM:SS>>` | `timeToCommittedManifestTotalSeconds` (convert from seconds) |
| Manifest committed at (UTC) | `<<ISO8601>>` | `manifestCommittedUtc` |
| Run created at (UTC) | `<<ISO8601>>` | `runCreatedUtc` |
| Findings by severity | `<<TABLE_OR_BULLETS>>` | `findingsBySeverity[]` → `{ severity, count }` |
| Audit rows (run scope) | `<<N>>` (`<<lower bound if truncated>>`) | `auditRowCount`, `auditRowCountTruncated` |
| LLM completion calls (run scope) | `<<N>>` | `llmCallCount` |
| Top finding id / severity | `<<id>>` / `<<severity>>` | `topFindingId`, `topFindingSeverity` |
| Demo flag | `<<Yes/No>>` | `isDemoTenant` — **must be No** for external publication |

**Review-cycle hours saved** (if captured at signup): derive per [`PILOT_ROI_MODEL.md`](../../library/PILOT_ROI_MODEL.md) § 3.1 using tenant baseline fields — not duplicated in `pilot-run-deltas.json`; cite `GET /v1/tenant/trial-status` internally.

---

## Customer quote

> `<<ONE_SENTENCE_QUOTE>>`  
> — `<<NAME, TITLE>>`, `<<DATE>>`

**Redaction:** remove internal project codenames unless approved in Step 4 of [`REFERENCE_PUBLICATION_RUNBOOK.md`](REFERENCE_PUBLICATION_RUNBOOK.md).

---

## Screenshot

`<<SCREENSHOT_URI>>` — run detail or committed manifest view (no demo banner unless intentionally demo).

---

## Links

- Case study file: `<<PATH_TO_CASE_STUDY_MD>>`
- Evidence folder / ZIP on secure share: `<<INTERNAL_LINK>>`

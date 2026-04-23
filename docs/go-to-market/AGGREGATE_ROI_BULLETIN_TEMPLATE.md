> **Scope:** Quarterly **aggregate** ROI bulletin template for GTM and leadership — sanitized statistics only; not a vehicle for per-customer disclosure.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Aggregate ROI bulletin — template (internal draft)

## Owner-approval gate (mandatory)

**No version of this bulletin may be published externally** (web, email to prospects, press, or partner decks) without **explicit owner sign-off** recorded per [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **27**. Engineering may generate **drafts** from production using `archlucid roi-bulletin` (see [`docs/CLI_USAGE.md`](../library/CLI_USAGE.md)); publication remains **owner-only**.

**Resolved 2026-04-21 (item 27):**

| Decision | Value |
|----------|-------|
| **Minimum N for first issue** | **5** qualifying tenants |
| **Signatory** | **Owner-solo** sign-off (no CRO / GC co-sign required) |
| **Percentile bands** | **Mean + p50 + p90** all stay in v1 bulletins |
| **First publication window** | Opens **once at least one PLG tenant reaches `Status: Published`** in [`reference-customers/README.md`](reference-customers/README.md) (item 19) — the first published reference is the trigger to ship the first bulletin |
| **Repository of record for sign-off** | **Dedicated tagged section** in [`docs/CHANGELOG.md`](../CHANGELOG.md) — see § "Sign-off audit format (2026-04-21 owner Q&A follow-up)" below for the exact heading shape and `grep` recipe an auditor can run. |
| **Synthetic shape sample (not sign-off)** | Public Markdown + marketing page: [`SAMPLE_AGGREGATE_ROI_BULLETIN_SYNTHETIC.md`](SAMPLE_AGGREGATE_ROI_BULLETIN_SYNTHETIC.md) and `/example-roi-bulletin` — **never** append to CHANGELOG; no signed heading; illustrates artefact shape before N≥5. |

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

## Sign-off (owner-solo per 2026-04-21 decision)

| Role | Name | Date | CHANGELOG.md section anchor |
|------|------|------|-----------------------------|
| Owner | | | `#YYYY-MM-DD--roi-bulletin-signed-Q?-YYYY` (per § "Sign-off audit format" in the template) |
```

## Sign-off audit format (2026-04-21 owner Q&A follow-up)

To make owner-solo sign-off mechanically auditable, **every** published bulletin appends a dedicated section to [`docs/CHANGELOG.md`](../CHANGELOG.md) with a fixed heading shape:

```markdown
## YYYY-MM-DD — ROI bulletin signed: Q?-YYYY

**Bulletin:** Q?-YYYY (link to the rendered bulletin artifact)
**Qualifying tenants (N):** <integer ≥ 5>
**Statistics published:** Mean / p50 / p90 baseline review-cycle hours
**Owner sign-off:** <owner name> on <ISO date>
**Sign-off mechanism:** This `## …` section, committed by the owner directly on `main`, is the sign-off — no separate signature artifact, no co-signer.
```

**Audit recipe (one command):**

```bash
# List every signed bulletin, newest-first, with its date and quarter
rg -n '^## \d{4}-\d{2}-\d{2} — ROI bulletin signed: Q[1-4]-\d{4}$' docs/CHANGELOG.md
```

**Why a dedicated section (vs. a free-form sentence in another entry).** The fixed heading is greppable and survives `docs/CHANGELOG.md` reorganization. Auditors and Trust Center reviewers can produce the full historical sign-off log with a single `rg` invocation; no screenshots, no separate audit artifact, no risk of a sign-off being silently buried inside an unrelated entry. This is also why the Sign-off table column above is `CHANGELOG.md section anchor` rather than `Signature / link` — the section *is* the signature.

**No bulletin without a section.** A published bulletin without the matching `## YYYY-MM-DD — ROI bulletin signed: Q?-YYYY` section in `docs/CHANGELOG.md` is **out of policy** — the next quality assessment will flag it. There is no rollback path other than retracting the publication and recording the retraction in the same format.

## Automation

- **API:** `GET /v1/admin/roi-bulletin-preview?quarter=Q1-YYYY&minTenants=5` (AdminAuthority).
- **CLI:** `archlucid roi-bulletin --quarter Q1-YYYY [--min-tenants 5] [--out draft.md]` — uses `ARCHLUCID_API_KEY` with admin scope.
- **CLI (synthetic, no API):** `archlucid roi-bulletin --quarter Q1-YYYY --synthetic [--explain] [--out sample.md]` — fixed illustrative numbers for buyer education only; never eligible for CHANGELOG sign-off.

## Related

- [`TRIAL_BASELINE_PRIVACY_NOTE.md`](TRIAL_BASELINE_PRIVACY_NOTE.md) — how the per-tenant baseline field is used and *not* used.
- [`docs/PILOT_ROI_MODEL.md`](../library/PILOT_ROI_MODEL.md) — modeled default when prospects skip custom hours.

> **Scope:** First paying tenant (PLG) — ArchLucid reference case study template - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


## Owner substitution checklist — fill before customer review

| Placeholder | Real value needed | Typical source |
|-------------|-------------------|----------------|
| `<<CUSTOMER_NAME>>` | Legal customer name for external publication | Order form / MSA / billing entity |
| `<<TIER>>` | Commercial tier at conversion | Subscription record + [`PRICING_PHILOSOPHY.md` § 5.2](../PRICING_PHILOSOPHY.md#52-locked-price-table-do-not-edit-without-re-rate-gate-decision) |
| `<<TRIAL_START_DATE>>`, `<<CONVERSION_DATE>>`, `<<LAST_REVIEW_DATE>>` | UTC dates | CRM + Stripe/Marketplace subscription events |
| `<<INDUSTRY>>`, `<<TEAM_SIZE>>`, `<<CLOUD_POSTURE>>`, `<<ACQUISITION_CHANNEL>>` | Firmographics | Champion interview |
| `<<CHALLENGE_NARRATIVE>>`, `<<SOLUTION_NARRATIVE>>` | Approved prose | Internal interview notes (NDA) |
| `<<MINUTES_OR_HOURS>>`, `<<INTEGRATIONS>>` | Time-to-first-commit + wired connectors | Audit export + integration catalog |
| `<<BEFORE_TTFV>>`, `<<AFTER_TTFV>>`, `<<TTFV_SOURCE>>` | TTFV table | `pilot-run-deltas.json` + sponsor sign-off |
| `<<BEFORE_HOURS>>`, `<<AFTER_HOURS>>`, `<<HOURS_SOURCE>>` | Review-cycle hours | Tenant baseline (`GET /v1/tenant/trial-status`) + measured deltas |
| `<<CHAMPION_QUOTE>>`, `<<CHAMPION_NAME>>`, `<<CHAMPION_TITLE>>` | Quote pack | Written approval (email or Docusign) |
| `<<LOGO_RIGHTS>>`, `<<LOGO_LIMIT>>`, `<<REFERENCE_CALL_LIMIT>>` | Reference program terms | Legal / partnership |
| `<<REVIEW_DATE_1>>`, `<<REVIEWER_1>>`, `<<ACTION_1>>` | Internal review trail | GTM + customer success notes |

**Evidence pack:** use [`REFERENCE_EVIDENCE_PACK_TEMPLATE.md`](REFERENCE_EVIDENCE_PACK_TEMPLATE.md) with measured lines filled from committed `pilot-run-deltas` JSON — start from the **demo tenant** sample under [`samples/pilot-run-deltas.demo-tenant.json`](samples/pilot-run-deltas.demo-tenant.json) only as a **format scaffold** (every published cell must be replaced with customer-backed values; banner: **demo tenant — replace before publishing**).

# `<<CUSTOMER_NAME>>` — First self-serve paying tenant (PLG reference)

> **STATUS: PLACEHOLDER — PLG path.** This file supports the **ship trial first** motion: when the **first** tenant converts from self-serve trial to a **paid** subscription, populate this case study and move the matching row in [`README.md`](README.md) from **Placeholder** toward **Drafting**, then through **Customer review** to **Published** once the customer approves external use. Until then, every `<<...>>` token stays literal in git.

**Audience:** Prospective buyers evaluating product-led proof (time-to-value without a sales-led design partner).

**Tier at conversion:** `<<TIER>>` (see [`PRICING_PHILOSOPHY.md` § 5.2](../PRICING_PHILOSOPHY.md#52-locked-price-table-do-not-edit-without-re-rate-gate-decision))

**Trial started:** `<<TRIAL_START_DATE>>` (`YYYY-MM-DD`)

**Paid conversion date:** `<<CONVERSION_DATE>>` (`YYYY-MM-DD`)

**Last reviewed:** `<<LAST_REVIEW_DATE>>`

---

## Why PLG reference matters

This narrative answers: *"Has anyone paid without a bespoke pilot?"* Keep claims bounded to [`docs/V1_SCOPE.md`](../../library/V1_SCOPE.md) and the sponsor guardrails in [`docs/EXECUTIVE_SPONSOR_BRIEF.md`](../../EXECUTIVE_SPONSOR_BRIEF.md) § 8 (do not over-claim transformation or headcount reduction).

---

## Customer profile

| Attribute | Value |
|-----------|-------|
| Industry | `<<INDUSTRY>>` |
| Team size (architecture / platform) | `<<TEAM_SIZE>>` |
| Cloud posture | `<<CLOUD_POSTURE>>` |
| How they found ArchLucid | `<<ACQUISITION_CHANNEL>>` (e.g., search, GitHub, peer referral) |

---

## Challenge

`<<CHALLENGE_NARRATIVE>>` — Why they started a **trial** without a design-partner contract, and what had to be true before they entered a card or signed an order form.

---

## Solution

`<<SOLUTION_NARRATIVE>>` — Core Pilot path only unless they actually used Operate (analysis workloads) or Operate (governance and trust) (call those out as follow-on, not Day-1).

Concrete details:

- Time from signup to **first committed manifest** (cite `<<MINUTES_OR_HOURS>>` from audit / metrics).
- Whether they used **Docker pilot** vs **hosted SaaS trial** (pick one primary story).
- Integrations actually wired (`<<INTEGRATIONS>>`).

---

## Results

| Metric | Before (trial baseline) | After (paid) | Source |
|--------|-------------------------|--------------|--------|
| Time to first committed manifest | `<<BEFORE_TTFV>>` | `<<AFTER_TTFV>>` | `<<TTFV_SOURCE>>` |
| Hours per architecture review cycle | `<<BEFORE_HOURS>>` | `<<AFTER_HOURS>>` | `<<HOURS_SOURCE>>` |

Anchor metrics to [`PILOT_SUCCESS_SCORECARD.md`](../PILOT_SUCCESS_SCORECARD.md) definitions where possible.

---

## Quote

> *"`<<CHAMPION_QUOTE>>`"* — `<<CHAMPION_NAME>>`, `<<CHAMPION_TITLE>>`, `<<CUSTOMER_NAME>>`

**Quote rules:** same as [`EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md`](EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md) — written approval required before **Published**.

---

## Reference-availability commitments

| Channel | Commitment | Limit |
|---------|------------|-------|
| Logo on archlucid.net | `<<LOGO_RIGHTS>>` | `<<LOGO_LIMIT>>` |
| Reference calls | `<<REFERENCE_CALL_LIMIT>>` | At customer's discretion |

---

## Internal review history (do not publish)

| Date | Reviewer | Action |
|------|----------|--------|
| `<<REVIEW_DATE_1>>` | `<<REVIEWER_1>>` | `<<ACTION_1>>` |

When the [`README.md`](README.md) row reaches **Published**, strip placeholders, the PLACEHOLDER banner, and this review table — same discipline as the design-partner template.

> **Scope:** ArchLucid — Product-market fit validation tracker - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Product-market fit validation tracker

**Audience:** Product, sales, and leadership teams validating PMF hypotheses with pilot evidence.

**Last reviewed:** 2026-05-01

This is a **living document**. Use **anonymous pilot identifiers** only (**Pilot A**, **Pilot B**, …)—never customer, company, or employee names here. Populate rows as pilots execute; aggregate into §6 after synthesis.

**Measurement grounding:** Numeric and qualitative pilot measures align with **[PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md)** — **§3** (baseline before the pilot), **§4** (during the pilot), and **§4.1** (primary pilot metrics table: time to committed manifest, findings, LLM calls, audit rows, etc.). The PMF scorecard columns below are a **hypothesis lens** on top of that ROI story; they are not a second, competing metrics system.

---

## 1. PMF hypotheses

Derived from [POSITIONING.md](POSITIONING.md), [ROI_MODEL.md](ROI_MODEL.md), and [BUYER_PERSONAS.md](BUYER_PERSONAS.md).

| ID | Hypothesis | Scorecard metric | Validation threshold |
|----|-----------|------------------|---------------------|
| **H1** | Architecture reviews take > 40 architect-hours and ArchLucid reduces this by > 50% | Time per review (§2.1) | Before > 40 hrs, after < 20 hrs |
| **H2** | Governance workflows reduce compliance review cycles by > 30% | Governance turnaround (§2.3) | Before/after delta > 30% |
| **H3** | Audit trail eliminates "who approved this?" questions entirely | Audit trail coverage (§2.3) | 100% of reviews have structured approval record |
| **H4** | Finding quality (explainability + evidence) meets or exceeds manual review | Quality score (§2.2), qualitative interview (§3) | Score ≥ 3.5/5; champion rates "as good or better than manual" |
| **H5** | Time to first actionable output < 1 hour from tenant provisioning | Time-to-first-run (§2.4) | First run complete within 60 minutes of first login |

---

## 2. Evidence tracker

### 2.1 Identifiers and population cadence

| Rule | Detail |
|------|--------|
| **Pilot ID** | Use **Pilot A**, **Pilot B**, **Pilot C**, … in this file only. Map to real programs in a **separate** restricted system (CRM, pilot charter, or internal tracker) if names are required. |
| **Buyer-safe template** | **[PILOT_BUYER_SAFE_EVIDENCE_TEMPLATE.md](PILOT_BUYER_SAFE_EVIDENCE_TEMPLATE.md)** — copy row + ethics guardrails before updating §2.3. |
| **After each scorecard** | Within **5 business days** of a [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) review, update every row for that **Pilot ID** that was in scope. |
| **Internal dogfood** | You may fill **Pilot A** (or **Pilot B**) from **internal** runs first—aggregate numbers and qualitative signal are enough; do not invent customer outcomes. |
| **Quarterly** | Reconcile §2.2 with §6 **Current PMF status** and note the date in the §6 summary row (footer). |

### 2.2 Column semantics (Pending / Unknown / TBD)

| Column | How to fill |
|--------|-------------|
| **ICP score** | From [IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md) scoring, or **TBD** until scored. |
| **ICP segment** | Short code (e.g. **FS** / **Tech** / **HC**) or **TBD**. |
| **Baseline** | Pre-ArchLucid or pre-pilot measure, **TBD** if not yet collected, or **Unknown** if you deliberately skip baseline (note in **Notes**). |
| **Result** | Observed value after the pilot window, **TBD** if not finished, or **Unknown** if not measured. |
| **Status** | **Pending** (not started / no data), **Captured** (values or qualitative signal recorded), **Deferred** (pilot paused), or **N/A** (hypothesis not in scope for that pilot). |

No numeric **Result** or **Baseline** cells should be fabricated. If the team only has qualitative signal, put **See scorecard** in **Result** and keep **Status** = **Captured**.

### 2.3 Evidence rows

| Hypothesis | Pilot ID | ICP score | ICP segment | Scorecard metric | Baseline | Result | Status | Notes |
|------------|----------|-----------|-------------|------------------|----------|--------|--------|-------|
| H1 | **Pilot A** | TBD | TBD | Hours per review | TBD | TBD | Pending | Internal dogfood worksheets + PMF-safe updates: **[DOGFOOD_PILOT_KIT.md](../library/DOGFOOD_PILOT_KIT.md)**. Do not fabricate **Baseline**/**Result**. |
| H2 | **Pilot A** | TBD | TBD | Governance turnaround | TBD | TBD | Pending | Same — **[DOGFOOD_PILOT_KIT.md](../library/DOGFOOD_PILOT_KIT.md)** §4 (fill **Notes**/**Status**/ICP only until measured). |
| H3 | **Pilot A** | TBD | TBD | Audit trail coverage | TBD | TBD | Pending | Same — evidence only when pilot touched audit-style paths. |
| H4 | **Pilot A** | TBD | TBD | Quality rating | TBD | TBD | Pending | Same — qualitative **Captured** acceptable per §2.2 (**See scorecard**). |
| H5 | **Pilot A** | TBD | TBD | Time-to-first-run | TBD | TBD | Pending | **[DOGFOOD_PILOT_KIT.md](../library/DOGFOOD_PILOT_KIT.md)** + [PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md) **§4.1** “Time to committed manifest” when applicable — still **no invented numerics**. |
| H1 | **Pilot B** | TBD | TBD | Hours per review | TBD | TBD | Pending | Add **Pilot C** by copying a block when needed. |
| H2 | **Pilot B** | TBD | TBD | Governance turnaround | TBD | TBD | Pending | |
| H3 | **Pilot B** | TBD | TBD | Audit trail coverage | TBD | TBD | Pending | |
| H4 | **Pilot B** | TBD | TBD | Quality rating | TBD | TBD | Pending | |
| H5 | **Pilot B** | TBD | TBD | Time-to-first-run | TBD | TBD | Pending | |

---

## 3. Ethics / confidentiality (pre–reference-customer)

| Topic | Guidance |
|-------|----------|
| **This file** | Safe for **internal** use with anonymized **Pilot A/B** rows and **TBD** / **Pending**—no customer names, logos, or identifiable quotes. |
| **Aggregates** | Summarizing **ranges** or **counts** of pilots (e.g. “2 of 3 pilots met H4 threshold”) without naming customers is appropriate for internal GTM and investor conversations when your policy allows. |
| **External publication** | Named reference customers, logos, and published case studies remain governed by program policy and **[V1_DEFERRED.md](../library/V1_DEFERRED.md)** (reference-customer publication scope)—this tracker does **not** override that. |
| **Public / community excerpts** | **Smallest necessary** redaction: **no PII** in anything shared outside trusted pilot channels (no names, work email, employer, or quotes that imply identity). **Community-oriented** audience—systematic binning (ranges-only, rounded hours) is **optional**, not required unless a number or quote could re-identify someone. |
| **Demo / seed data** | If any number is copied from a **demo** tenant, follow **[PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md) §4.1.1** (non-negotiable redaction; do not treat demo figures as customer outcomes). |

---

## 4. Synthesis rules

| Status | Criteria |
|--------|----------|
| **Validated** | 3+ pilots with positive signal (meets or exceeds threshold) |
| **Invalidated** | 3+ pilots with negative signal (consistently misses threshold) |
| **Promising** | 1–2 pilots with positive signal; need more data |
| **Inconclusive** | Mixed results or insufficient data |

---

## 5. Product implications

| Hypothesis outcome | Product action |
|-------------------|----------------|
| **H1 validated** | Double down on analysis pipeline; consider time-to-review as a headline metric |
| **H1 invalidated** | Investigate — is the analysis useful but slow? Is manual baseline lower than expected? |
| **H2 validated** | Promote governance as a primary value pillar; invest in policy pack library |
| **H2 invalidated** | Governance may be a "nice to have" not a "must have" — re-examine tier gating |
| **H3 validated** | Audit trail is a competitive moat — never gate it behind higher tiers |
| **H4 invalidated** | Quality gaps in specific finding engines — target improvements to weakest engines |
| **H5 invalidated** | Onboarding friction too high — invest in provisioning automation and guided experience |

---

## 6. Go-to-market implications

| Hypothesis outcome | GTM action |
|-------------------|------------|
| **H1 validated** | Lead with "60% faster architecture reviews" in positioning and datasheet |
| **H2 validated** | Target compliance-driven buyers (financial services, healthcare) per ICP |
| **H3 validated** | Create case study focused on audit readiness |
| **H4 validated** | Publish finding quality comparison data (ArchLucid vs manual review) |
| **H5 invalidated** | Do not promise "5-minute time-to-value" until fixed; lead with guided pilot instead |

---

## 7. Current PMF status

| Hypothesis | Pilots completed | Signal | Overall status |
|------------|-----------------|--------|----------------|
| H1 | 0 | — | **Not yet tested** |
| H2 | 0 | — | **Not yet tested** |
| H3 | 0 | — | **Not yet tested** |
| H4 | 0 | — | **Not yet tested** |
| H5 | 0 | — | **Not yet tested** |

**Next action:** Execute first pilot with a strong-fit ICP customer ([IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md)), record measures per **[PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md)**, and move **Pilot A** rows from **Pending** to **Captured** when data exists.

**Last aggregate review:** _TBD_

---

## Related documents

| Doc | Use |
|-----|-----|
| [PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md) | Baseline (§3), during-pilot metrics (§4–§4.1), demo-number rules (§4.1.1) |
| [DOGFOOD_PILOT_KIT.md](../library/DOGFOOD_PILOT_KIT.md) | Internal Core Pilot worksheets; **Pilot A** row update discipline (notes-first, no fabricated baselines/results) |
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Measurement framework |
| [IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md) | ICP scoring for pilot selection |
| [ROI_MODEL.md](ROI_MODEL.md) | Value hypotheses grounding |
| [../PRODUCT_LEARNING.md](../library/PRODUCT_LEARNING.md) | Learning signals |
| [REFERENCE_NARRATIVE_TEMPLATE.md](REFERENCE_NARRATIVE_TEMPLATE.md) | Case study templates for validated hypotheses |
| [V1_DEFERRED.md](../library/V1_DEFERRED.md) | Deferred reference-customer publication scope (unchanged) |

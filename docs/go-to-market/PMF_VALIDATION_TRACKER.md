> **Scope:** ArchLucid — Product-market fit validation tracker - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Product-market fit validation tracker

**Audience:** Product, sales, and leadership teams validating PMF hypotheses with pilot evidence.

**Last reviewed:** 2026-04-15

This is a **living document**. Populate placeholder rows as pilots execute. Update after each pilot scorecard review.

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

| Hypothesis | Pilot | ICP score | ICP segment | Scorecard metric | Baseline | Result | Status |
|------------|-------|-----------|-------------|------------------|----------|--------|--------|
| H1 | _Pilot 1_ | _/45_ | _FS / Tech / HC_ | Hours per review | ___ hrs | ___ hrs | Pending |
| H2 | _Pilot 1_ | _/45_ | | Governance turnaround | ___ days | ___ days | Pending |
| H3 | _Pilot 1_ | _/45_ | | Audit trail coverage | __% | __% | Pending |
| H4 | _Pilot 1_ | _/45_ | | Quality rating | N/A | __/5 | Pending |
| H5 | _Pilot 1_ | _/45_ | | Time-to-first-run | N/A | ___ min | Pending |
| H1 | _Pilot 2_ | _/45_ | | Hours per review | ___ hrs | ___ hrs | Pending |
| H2 | _Pilot 2_ | _/45_ | | Governance turnaround | ___ days | ___ days | Pending |
| ... | ... | ... | ... | ... | ... | ... | ... |

---

## 3. Synthesis rules

| Status | Criteria |
|--------|----------|
| **Validated** | 3+ pilots with positive signal (meets or exceeds threshold) |
| **Invalidated** | 3+ pilots with negative signal (consistently misses threshold) |
| **Promising** | 1–2 pilots with positive signal; need more data |
| **Inconclusive** | Mixed results or insufficient data |

---

## 4. Product implications

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

## 5. Go-to-market implications

| Hypothesis outcome | GTM action |
|-------------------|------------|
| **H1 validated** | Lead with "60% faster architecture reviews" in positioning and datasheet |
| **H2 validated** | Target compliance-driven buyers (financial services, healthcare) per ICP |
| **H3 validated** | Create case study focused on audit readiness |
| **H4 validated** | Publish finding quality comparison data (ArchLucid vs manual review) |
| **H5 invalidated** | Do not promise "5-minute time-to-value" until fixed; lead with guided pilot instead |

---

## 6. Current PMF status

| Hypothesis | Pilots completed | Signal | Overall status |
|------------|-----------------|--------|----------------|
| H1 | 0 | — | **Not yet tested** |
| H2 | 0 | — | **Not yet tested** |
| H3 | 0 | — | **Not yet tested** |
| H4 | 0 | — | **Not yet tested** |
| H5 | 0 | — | **Not yet tested** |

**Next action:** Execute first pilot with a strong-fit ICP customer ([IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md)) and populate the evidence tracker.

---

## Related documents

| Doc | Use |
|-----|-----|
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Measurement framework |
| [IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md) | ICP scoring for pilot selection |
| [ROI_MODEL.md](ROI_MODEL.md) | Value hypotheses grounding |
| [../PRODUCT_LEARNING.md](../library/PRODUCT_LEARNING.md) | Learning signals |
| [REFERENCE_NARRATIVE_TEMPLATE.md](REFERENCE_NARRATIVE_TEMPLATE.md) | Case study templates for validated hypotheses |

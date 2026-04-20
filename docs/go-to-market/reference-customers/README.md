> **Scope:** ArchLucid — reference-customers index - full detail, tables, and links in the sections below.

# ArchLucid — reference-customers index

**Audience:** Marketing, sales, customer success, and product leadership.

**Purpose:** Single source of truth for **real**, **publishable** reference-customer assets. This file replaces "no published reference customer" as a discount-stack assumption (see [`PRICING_PHILOSOPHY.md` § 5.4](../PRICING_PHILOSOPHY.md#54-discount-stack-work-down)). The CI guard [`scripts/ci/check_reference_customer_status.py`](../../../scripts/ci/check_reference_customer_status.py) parses the table below and (today) **warns** when zero rows have `Status: Published`. The same guard becomes **merge-blocking** the day the first real customer is `Published`, at which point the **−15% reference discount** in [`PRICING_PHILOSOPHY.md` § 5.1](../PRICING_PHILOSOPHY.md#51-derivation-50-of-fair-value-basis) becomes a candidate for re-rate ([§ 5.3](../PRICING_PHILOSOPHY.md#53-re-rate-plan)).

**Distinct from [`REFERENCE_NARRATIVE_TEMPLATE.md`](../REFERENCE_NARRATIVE_TEMPLATE.md):** that file holds the **fictional but realistic** narrative templates for marketing copy. *This* directory holds **real customer-specific assets** (case studies populated with permission, with placeholders unwound, ready to publish externally).

---

## Status lifecycle

Every row in the table below moves through these states in order. A CI guard rejects any other value.

| Status | Meaning | Exit criteria |
|--------|---------|---------------|
| `Placeholder` | Empty seat for a future real customer; case-study file uses `<<...>>` placeholders | Real customer name + signed reference agreement |
| `Drafting` | Real customer named; case study being written internally | Customer-facing copy ready to send |
| `Customer review` | Customer reviewing the draft for legal / brand approval | Written approval to publish |
| `Published` | Live on archlucid.com / sales decks / Azure Marketplace listing | (terminal) |

A row that fails to move from `Customer review` to `Published` within 60 days should be downgraded back to `Drafting` and flagged in the next pricing review.

---

## Reference-customer table

| Customer | Tier | Pilot start | Case-study link | Reference-call cadence | Status |
|----------|------|-------------|-----------------|------------------------|--------|
| EXAMPLE_DESIGN_PARTNER | Professional (design-partner −50%) | TBD | [EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md](EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md) | TBD (target: quarterly) | Placeholder — replace before publishing |

> **CI guard contract:** the script reads only this table. The exact column order and the literal `Status` header text matter. Do not split the table across multiple sub-tables; add new rows to the bottom.

---

## How to add a real reference

1. **Copy** [`EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md`](EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md) to a new file named `<CUSTOMER_SLUG>_CASE_STUDY.md` (lowercase-hyphen-or-underscore slug; no spaces).
2. **Find/replace** every `<<CUSTOMER_NAME>>`, `<<TIER>>`, `<<DESIGN_PARTNER_TERM_START>>` and any other `<<...>>` placeholder with the real value. The existing pattern is intentional — it lets a sales engineer one-shot the substitution from a single deal-close email.
3. **Add a row** to the table above, with `Status: Drafting`.
4. **Move through the lifecycle** (Drafting → Customer review → Published) as approvals come in. Each transition gets a one-line entry in [`docs/CHANGELOG.md`](../../CHANGELOG.md) so finance/sales can re-rate the discount stack on a known cadence.
5. **When the first row reaches `Published`,** flip the CI guard from `continue-on-error: true` to a hard gate by removing that line from `.github/workflows/ci.yml` (the guard text is unchanged). This is the moment that authorizes a pricing-review trigger per [`PRICING_PHILOSOPHY.md` § 5.3](../PRICING_PHILOSOPHY.md#53-re-rate-plan).

---

## Related documents

| Doc | Use |
|-----|-----|
| [`PRICING_PHILOSOPHY.md` § 5.1](../PRICING_PHILOSOPHY.md#51-derivation-50-of-fair-value-basis) | Discount stack derivation (`−25%` trust, `−15%` reference, `−10%` self-serve = `−50%` total) |
| [`PRICING_PHILOSOPHY.md` § 5.3](../PRICING_PHILOSOPHY.md#53-re-rate-plan) | Re-rate gates that retire each discount line |
| [`PRICING_PHILOSOPHY.md` § 5.4](../PRICING_PHILOSOPHY.md#54-discount-stack-work-down) | Operational tracker — owner, target close, evidence link, re-rate trigger per discount line |
| [`REFERENCE_NARRATIVE_TEMPLATE.md`](../REFERENCE_NARRATIVE_TEMPLATE.md) | Three fictional-but-realistic narrative archetypes (FinServ / Tech / Healthcare) for marketing |
| [`PILOT_SUCCESS_SCORECARD.md`](../PILOT_SUCCESS_SCORECARD.md) | Metric definitions every published case study should populate |
| [`AZURE_MARKETPLACE_SAAS_OFFER.md`](../../AZURE_MARKETPLACE_SAAS_OFFER.md) | Where published references appear in the Marketplace listing copy |

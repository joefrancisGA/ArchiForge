> **Scope:** ArchLucid — Subscription order form (template) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Subscription order form (template)

**Important — not legal advice:** This is a **working template** to reduce friction for SMB-midmarket deals (< $50K ARR). It **does not** constitute legal advice. **Qualified legal counsel** must review and adapt it before use.

**Last reviewed:** 2026-04-22

**Pricing source:** All current list prices (platform fee, seat price, run overage, pilot fee) are in [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). The worked examples in this document compute totals from those locked prices — the numbers are derived here for convenience, but the prices themselves live only in that file. The CI guard `scripts/ci/check_pricing_single_source.py` allows price literals in this file.

---

## 1. Parties

| Role | Detail |
|------|--------|
| **Customer** | Legal entity: __________________ |
| | Contact name: __________________ |
| | Email: __________________ |
| | Billing address: __________________ |
| **Vendor** | [ArchLucid vendor legal entity] |

---

## 2. Subscription details

| Field | Value |
|-------|-------|
| **Tier** | ☐ Team  ☐ Professional  ☐ Enterprise (see [PRICING_PHILOSOPHY.md](PRICING_PHILOSOPHY.md)) |
| **Named architect seats** | _______ |
| **Workspaces** | _______ |
| **Included runs / month** | Per tier (see [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) |
| **Subscription term** | ☐ Monthly  ☐ Annual (annual = 2 months free; see §A — Annual prepay below) |
| **Start date** | __________________ |
| **Monthly platform fee** | See [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). Fill in: $_______ / workspace / month |
| **Monthly seat fee** | See [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). Fill in: $_______ × _______ seats = $_______ / month |
| **Run overage rate** | See [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). Fill in: $_______ / run (applicable when monthly runs exceed tier allowance) |
| **Total monthly** | $_______ (platform + seats; excluding run overage — see §3) |
| **Renewal** | Auto-renew unless either party provides **30 days'** written notice before term end |

---

## 3. Run overage

Run overage is charged when the Customer's monthly run count exceeds the included allowance for the subscribed tier. Runs are counted per committed architecture run (a call to `POST /v1/architecture/run/{runId}/commit`). Development and simulator runs that do not reach commit are not counted.

| Field | Value |
|-------|-------|
| **Included runs / month** | Per tier — see [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) |
| **Overage rate** | Per [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md): $10/run (Team) or $8/run (Professional) |
| **Billing cycle** | Monthly in arrears; Vendor invoices overage on the following month's invoice |
| **Overage cap** | ☐ None (default)  ☐ Customer cap at: _______ runs/month (service paused above cap until next billing period) |
| **Estimated monthly overage** | $_______ (if applicable) |

### Run overage worked example — Professional at 150 % of included allowance

Professional includes 100 runs per month (see [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)).

```
Actual runs this month    = 150
Included runs             = 100
Overage runs              = 50
Overage charge            = 50 × $8 = $400
```

Monthly total for this period = regular monthly fee + $400 overage. The overage rate is drawn from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) — confirm the current rate before quoting.

---

## 4. Worked pricing examples

All prices are computed from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). Verify that the locked prices have not been superseded by a re-rate gate decision before submitting an order form.

### Example A — Team tier, 3 seats, 1 workspace, monthly billing

| Component | Calculation | Amount |
|-----------|------------|--------|
| Platform fee | 1 workspace × (from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) | $199 / month |
| Seat fee | 3 seats × (from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) | $237 / month |
| Included runs | 20 / month included | — |
| **Monthly total** | | **$436 / month** |
| **Annual total (monthly billing)** | $436 × 12 | $5,232 / year |
| **Annual total (prepay, 2 months free)** | $436 × 10 | $4,360 / year |

### Example B — Professional tier, 8 seats, 1 workspace, monthly billing

| Component | Calculation | Amount |
|-----------|------------|--------|
| Platform fee | 1 workspace × (from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) | $899 / month |
| Seat fee | 8 seats × (from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) | $1,432 / month |
| Included runs | 100 / month included | — |
| **Monthly total** | | **$2,331 / month** |
| **Annual total (monthly billing)** | $2,331 × 12 | $27,972 / year |
| **Annual total (prepay, 2 months free)** | $2,331 × 10 | $23,310 / year |

### Example C — Enterprise tier, 50 seats, 3 workspaces, custom audit retention, annual contract

Enterprise pricing is a custom annual contract with a floor and range defined in [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). This example illustrates a representative mid-range Enterprise deal; actual pricing requires a commercial proposal.

| Component | Notes | Representative amount |
|-----------|-------|----------------------|
| Annual contract | 50 named seats, 3 workspaces, unlimited runs (fair-use soft cap per [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) | From [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) land range |
| Custom audit retention | 3-year retention with cold-tier export (roadmap; priced separately) | TBD at contract |
| Custom policy packs | Authoring engagement (professional services) | TBD at contract |
| Dedicated CSM | Included in Enterprise tier | Included |
| **Representative annual range** | | $120,000–$180,000 / year |

*Representative range above is illustrative for a 50-seat / 3-workspace deal within the Enterprise land range in [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). Final pricing is determined by commercial proposal.*

### Run overage example at 150% of included allowance (Professional)

See §3 above. At 150 runs in a month vs 100 included: 50 × $8 overage rate (from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) = $400 additional charge that month.

---

## 5. Annual prepay terms

| Field | Value |
|-------|-------|
| **Discount** | 2 months free (equivalent to ~16.7% off monthly rate) |
| **Payment** | Annual invoice due **Net 30** from contract start date |
| **Refund on cancellation** | Pro-rata refund of remaining unused months if Vendor terminates; no refund for Customer-initiated early cancellation unless agreed in writing |
| **Annual total** | $_______ (10 months × monthly rate, per §4 example applicable to Customer's tier) |

---

## 6. Incorporated terms

By signing this order form, Customer agrees to the following (each incorporated by reference):

| Document | Location |
|----------|----------|
| **Master Service Agreement** | [MSA_TEMPLATE.md](MSA_TEMPLATE.md) |
| **Data Processing Agreement** | [DPA_TEMPLATE.md](DPA_TEMPLATE.md) |
| **Service Level Objectives** | [SLA_SUMMARY.md](SLA_SUMMARY.md) |
| **Acceptable Use Policy** | [TBD — URL] |
| **Subprocessors** | [SUBPROCESSORS.md](SUBPROCESSORS.md) |

In the event of conflict, the order of precedence is: this Order Form > DPA > Terms of Service > SLA > AUP.

---

## 7. Payment terms

| Term | Detail |
|------|--------|
| **Invoicing** | Vendor invoices Customer at the start of each billing period (monthly or annually), plus any run overage from the prior period |
| **Payment due** | **Net 30** days from invoice date |
| **Accepted methods** | Bank transfer, credit card (if billing system supports) |
| **Late payment** | Vendor may suspend access after **15 days** past due with **10 days'** written notice |
| **Taxes** | Prices exclude applicable taxes; Customer is responsible for taxes unless tax-exempt documentation is provided |

---

## 8. Termination and data

| Event | Handling |
|-------|---------|
| **Customer termination** | 30 days' written notice; access continues through paid period |
| **Vendor termination** | 30 days' written notice; pro-rata refund of prepaid unused term |
| **Data export** | Customer may export data via product features (DOCX, ZIP, audit CSV, API) before termination |
| **Data deletion** | Per [DPA](DPA_TEMPLATE.md) §9 — deletion within agreed timeline after termination, except where law requires retention |

---

## 9. Chargeback, refund, and dunning

**Drafted 2026-04-22; pending legal sign-off before any commerce un-hold.**

### Chargeback

A **chargeback** is a bank-initiated dispute after the card network’s rules-based window opens for the cardholder. Vendor may submit an **evidence package** (invoices, delivery logs, contract acceptance) through Stripe’s dispute flow. **Liability** follows the card network outcome: if the dispute is upheld, the charge is reversed and network fees may apply; if Vendor wins, funds are released per Stripe settlement timing.

### Refund

**Refunds** follow the tier and term rules in **§5** (annual prepay): Vendor provides a **pro-rata refund of prepaid unused months** when Vendor terminates; there is **no Customer-initiated early-cancellation refund** unless **agreed in writing** in the order form or a signed amendment. Monthly billing refunds (if any) are handled case-by-case under the same written-agreement rule — do not imply automatic refunds in quotes without legal review.

### Dunning

**Dunning** for card failures uses **Stripe smart retries** by default unless Customer is on invoice-only terms. After repeated failure, access may align with **§7** — Vendor may suspend after **15 days past due** following **10 days’** written notice, consistent with the late-payment row in §7.

---

## Addendum A — Annual prepay schedule

*(Complete if Customer selects annual billing in §2)*

| Field | Value |
|-------|-------|
| **Annual amount** | $_______ (from §5 above) |
| **Invoice date** | __________________ |
| **Payment due** | 30 days from invoice date |
| **Auto-renew** | ☐ Yes  ☐ No — if No, confirm renewal intent **60 days** before term end |

---

## Addendum B — Design partner agreement

*(Complete only if Customer qualifies as a Design Partner — confirm slot availability with sales before signing; limited to first 3 customers)*

**Design partner discount:** 50% off Professional list price for 12 months from contract start. Discount applies to platform fee and seat fee. Run overage is charged at standard Professional rate (see [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)).

**Customer deliverables (both required for discount to apply):**

| Deliverable | Description | Due date |
|-------------|-------------|----------|
| **Published case study** | Minimum 500-word written case study with organization name, use case, and quantified outcome. Co-authored with ArchLucid; approved by Customer before publication. | Within 90 days of completing the 6-week pilot |
| **Quarterly reference call** | Up to 1 hour per quarter; Customer speaks with an ArchLucid prospect about their experience. Schedule coordinated by ArchLucid CSM. | Once per calendar quarter for the 12-month term |

**Discount table (all values computed from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) — verify current list before quoting):**

| Tier | Standard monthly | Design partner monthly (50% off) |
|------|-----------------|-----------------------------------|
| Professional, 8 seats, 1 workspace | $2,331 | $1,166 |
| Professional, 15 seats, 2 workspaces | $899 × 2 + $179 × 15 = $4,483 | $2,242 |

*These rows are computed from prices in [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md). If prices have been re-rated, recompute before signing.*

**Forfeiture:** If Customer does not deliver both case study and reference call within the 12-month term, the 50% discount is forfeited for the renewal period. Discount applied to prior months is not clawed back.

**Slot confirmation:** Sales must confirm in writing that a Design Partner slot is available. Maximum 3 simultaneous Design Partner agreements.

| Field | Value |
|-------|-------|
| **Design partner slot confirmed by** | __________________ (ArchLucid sales rep) |
| **Confirmation date** | __________________ |
| **Discount term start** | __________________ |
| **Discount term end** | __________________ (12 months from start) |

---

## 10. Signature

| | Customer | Vendor |
|--|----------|--------|
| **Name** | | |
| **Title** | | |
| **Date** | | |

---

## Related documents

| Doc | Use |
|-----|-----|
| [MSA_TEMPLATE.md](MSA_TEMPLATE.md) | Master Service Agreement (this Order Form is governed by the MSA) |
| [PRICING_PHILOSOPHY.md](PRICING_PHILOSOPHY.md) | **Single source of truth** for all list prices, pilot pricing, re-rate gates, and sensitivity playbook |
| [DPA_TEMPLATE.md](DPA_TEMPLATE.md) | Data processing terms |
| [SLA_SUMMARY.md](SLA_SUMMARY.md) | Service level objectives |
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [ROI_MODEL.md](ROI_MODEL.md) | Value model and payback analysis for the buyer |
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Success criteria for the guided pilot |

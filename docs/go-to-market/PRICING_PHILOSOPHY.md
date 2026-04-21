> **Scope:** ArchLucid — Pricing philosophy and packaging - full detail, tables, and links in the sections below.

# ArchLucid — Pricing philosophy and packaging

**Audience:** Product leadership, sales, and finance — internal alignment before external pricing publication.

**Last reviewed:** 2026-04-17

**Grounding:** Pricing anchors to the ROI model in [ROI_MODEL.md](ROI_MODEL.md) (break-even at ~180 architect-hours/year) and buyer personas in [BUYER_PERSONAS.md](BUYER_PERSONAS.md).

**Single source of truth:** All price figures live **only** in this file, [ORDER_FORM_TEMPLATE.md](ORDER_FORM_TEMPLATE.md), [TRIAL_AND_SIGNUP.md](TRIAL_AND_SIGNUP.md), and [docs/CHANGELOG.md](../CHANGELOG.md). Every other doc must **link here** rather than restate numbers; the CI check `scripts/ci/check_pricing_single_source.py` enforces this on every pull request. **Marketplace tier naming** (Team / Professional / Enterprise) is guarded by `scripts/ci/assert_marketplace_pricing_alignment.py` against [`MARKETPLACE_PUBLICATION.md`](MARKETPLACE_PUBLICATION.md) and [`AZURE_MARKETPLACE_SAAS_OFFER.md`](../AZURE_MARKETPLACE_SAAS_OFFER.md).

---

## 1. Pricing principles

| Principle | Rationale |
|-----------|-----------|
| **Value-based, not cost-plus** | Buyers compare ArchLucid to the cost of manual architecture review (40+ hours per review), not to our LLM token costs. Price against value delivered, not infrastructure consumed. |
| **Predictable for buyer budgeting** | Enterprise procurement needs a number they can put in a PO. Avoid pure consumption pricing that creates forecasting anxiety. |
| **Expansion-friendly** | Revenue should grow as the customer gets more value — more teams, more workspaces, more governance adoption — without requiring a full re-negotiation. |
| **Competitive with manual review cost** | The ROI model shows ~$294K annual savings for a 6-architect team. Pricing should be a small fraction of that value (typically 10–20% of value delivered). |

---

## 2. Pricing model evaluation

| Model | Pros | Cons | Fit for ArchLucid |
|-------|------|------|-------------------|
| **Per-seat (architect)** | Simple, predictable, easy to quote | Caps adoption — customers may limit seats to control cost; penalizes broader team usage | **Good base** — aligns with buyer's architect headcount; simple to explain |
| **Per-run (usage)** | Aligns with value delivered; high-volume users pay more | Unpredictable costs; discourages experimentation; complex metering needed | **Poor as primary** — buyers dislike variable cost; good as an overage mechanism |
| **Platform fee + consumption** | Predictable base with usage upside; expansion-friendly | More complex to explain; requires metering infrastructure | **Best hybrid** — predictable base per workspace/team, with run allowances per tier |

**Recommendation:** **Platform fee per workspace + included run allowance** with per-seat pricing for named architects. This gives buyers predictability (platform fee + seats) while allowing expansion via additional workspaces, seats, and run overages.

---

## 3. Packaging tiers

### Tier overview

| | **Team** | **Professional** | **Enterprise** |
|--|----------|-----------------|----------------|
| **Target buyer** | Small architecture team exploring AI-assisted review | Established architecture practice with governance needs | Large organization with compliance, audit, and multi-team requirements |
| **Target persona** | Persona 3 (CTO/VP Eng) | Persona 1 (Enterprise Architect) | Persona 1 + Persona 2 (Platform Eng Lead) |
| **Platform fee** | $199 / workspace / month | $899 / workspace / month (up to 5 workspaces) | Included in annual contract |
| **Seats included** | Up to 5 architects | Up to 20 architects | Unlimited (named) |
| **Seat price** | $79 / architect / month | $179 / architect / month | Included in annual contract |
| **Workspaces** | 1 | Up to 5 | Unlimited |
| **Runs / month** | 20 included; $10 / run overage | 100 included; $8 / run overage | Unlimited (2,000 run/mo fair-use soft cap) |
| **Annual prepay** | 2 months free | 2 months free | Custom |
| **Finding engines** | All 10 | All 10 | All 10 + custom engine support |
| **Governance** | Basic (pre-commit gate) | Full (approval workflows, policy packs, segregation of duties) | Full + custom policy packs |
| **Comparison / drift** | Included | Included | Included |
| **Audit trail** | 90-day retention | 1-year retention | Custom retention + export |
| **Authentication** | Entra ID | Entra ID | Entra ID + generic OIDC (roadmap) |
| **Support** | Community / email | Business hours email + onboarding call | Dedicated CSM, priority response |
| **SLA** | Shared SLO targets | Shared SLO targets | Custom SLA with credits |

**Example monthly invoice — Team, 3 seats, 1 workspace:**
Platform fee $199 + (3 × $79) = **$436 / month** (within the < $500 discretionary budget guardrail).

**Example monthly invoice — Professional, 8 seats, 1 workspace:**
Platform fee $899 + (8 × $179) = **$2,331 / month** (within the $2K–$5K manager-approval range).

### Feature gates

| Feature | Team | Professional | Enterprise |
|---------|------|--------------|------------|
| Architecture runs | ✓ | ✓ | ✓ |
| Golden manifests | ✓ | ✓ | ✓ |
| Comparison runs | ✓ | ✓ | ✓ |
| Governance approvals | — | ✓ | ✓ |
| Policy packs | — | ✓ | ✓ (custom) |
| Audit export (CSV) | — | ✓ | ✓ |
| DOCX consulting export | — | ✓ | ✓ |
| Webhook / CloudEvents | — | ✓ | ✓ |
| Service Bus integration | — | — | ✓ |
| SCIM provisioning | — | — | ✓ (roadmap) |
| Dedicated support | — | — | ✓ |

### 3.1 Canonical Marketplace tier names

**Partner Center plan display names** and in-repo GTM docs must use **`Team`**, **`Professional`**, and **`Enterprise`** — the same labels as the packaging table above — not shorthand such as **`Pro`**. That keeps [`MARKETPLACE_PUBLICATION.md`](MARKETPLACE_PUBLICATION.md), [`AZURE_MARKETPLACE_SAAS_OFFER.md`](../AZURE_MARKETPLACE_SAAS_OFFER.md), and webhook tier mapping aligned. **CI:** `python scripts/ci/assert_marketplace_pricing_alignment.py`. **Configuration:** Stripe `Billing:Stripe:PriceIdPro` is a historical key name for the **Professional** tier Price ID only; do not use `Pro` as the external tier label in new docs.

---

## 4. Pilot pricing

| Scenario | Pricing | Duration | Conversion path |
|----------|---------|----------|-----------------|
| **Self-serve trial** | Free | 14 days | Auto-upgrade prompt; see [TRIAL_AND_SIGNUP.md](TRIAL_AND_SIGNUP.md). Team-tier features, simulator agents, 10 runs, 3 seats, sample seeded. |
| **Guided pilot** | $15,000 flat, fully credited on conversion to Professional or Enterprise | 6 weeks (per [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md)) | Scorecard review → commercial proposal |
| **Design partner** | 50% off Professional list price for 12 months; **capped at first 3 customers** | Contract term | In exchange for: published case study + quarterly reference call |
| **Enterprise evaluation** | Custom | Negotiated | Champion + executive sponsor path |

**Guided pilot credit:** The $15,000 pilot fee is fully credited against the first annual invoice on conversion, making it zero net cost to the buyer who converts.

**Design partner eligibility:** Must be within the first 3 signed design-partner agreements. Sales must confirm the slot before quoting. Commitment deliverables (case study + reference call) are contractual.

---

## 5. Locked list prices (2026)

> **Effective date:** 2026-04-17.
> **Valid for:** 12 months, or until a re-rate gate below triggers an explicit re-rate decision.
> **Change control:** Any revision to the numbers in this section requires a product leadership decision and an update to this file + CHANGELOG.md before becoming effective.

### 5.1 Derivation (50%-of-fair-value basis)

| Input | Value | Source |
|-------|-------|--------|
| Annual value delivered (6-architect team) | ~$294,000 / year | [ROI_MODEL.md](ROI_MODEL.md) §5 |
| Value per architect per year | ~$49,000 | $294K ÷ 6 |
| Capture target (10–20% of value) | $4,900–$9,800 / architect / year | Industry benchmark for B2B SaaS |
| "Fair value" seat price | $408–$817 / seat / month | Divide by 12 |
| **Current price multiplier** | **~50% of fair value** | See discount stack below |

**Discount stack applied to arrive at locked prices:**

| Discount | Reason | Magnitude |
|----------|--------|-----------|
| Trust discount | SOC 2 Type II not yet attested; no published pen-test report | −25% |
| Reference discount | No named reference customer logo or published case study | −15% |
| Self-serve discount | Trial/billing loop not fully in production at lock date | −10% |
| **Total discount** | | **−50%** |

### 5.2 Locked price table (do not edit without re-rate gate decision)

The fenced JSON block below is the **machine-readable** source for `archlucid-ui/public/pricing.json` (generated in CI via `scripts/ci/generate_pricing_json.py`). Do not remove the **locked-prices** fence (three backticks + the token `locked-prices` on its own line).

```locked-prices
{
  "schemaVersion": 1,
  "effectiveDate": "2026-04-17",
  "currency": "USD",
  "packages": [
    {
      "id": "team",
      "title": "Team",
      "summary": "Small architecture team exploring AI-assisted review",
      "workspaceMonthlyUsd": 199,
      "includedArchitectSeats": 5,
      "seatMonthlyUsd": 79,
      "includedRunsPerMonth": 20,
      "overageRunUsd": 10
    },
    {
      "id": "professional",
      "title": "Professional",
      "summary": "Established practice with governance and audit needs",
      "workspaceMonthlyUsd": 899,
      "maxWorkspaces": 5,
      "includedArchitectSeats": 20,
      "seatMonthlyUsd": 179,
      "includedRunsPerMonth": 100,
      "overageRunUsd": 8
    },
    {
      "id": "enterprise",
      "title": "Enterprise",
      "summary": "Large organization — annual contract",
      "annualFloorUsd": 60000,
      "annualCeilingUsd": 250000
    }
  ]
}
```

| Item | Price |
|------|-------|
| Team platform fee | $199 / workspace / month |
| Team seat | $79 / architect / month |
| Team run overage | $10 / run |
| Professional platform fee | $899 / workspace / month |
| Professional seat | $179 / architect / month |
| Professional run overage | $8 / run |
| Enterprise annual floor | $60,000 / year |
| Enterprise land range | $60,000–$250,000 / year |
| Enterprise run model | Unlimited in fair-use (2,000 / month soft cap) |
| Guided pilot | $15,000 flat (fully credited on conversion) |
| Design partner discount | 50% off Professional list, 12 months, first 3 customers only |

### 5.3 Re-rate plan

Each gate below removes its associated discount from the stack. Trigger a **product leadership pricing review** (not an automatic price change) when any gate clears. Existing customers receive **price-lock for the remainder of their current term plus one renewal** before any increase applies.

| Gate | Discount removed | Expected list price increase |
|------|-----------------|------------------------------|
| SOC 2 Type II report available under NDA | −25% trust discount | Raise list ~25% |
| Two named, referenceable customers (case study or logo + quote) | −15% reference discount | Raise list ~15% |
| Self-serve signup → tenant → billing loop in production | −10% self-serve discount | Raise list ~10% |

**Gate #3 (2026-04-17):** In-repo evidence clears the *engineering* bar: merge-blocking **`ui-e2e-live`** runs [`archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts`](../../archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts) (register → SQL trial → metering → Noop checkout → harness activation → Prometheus counters). **This still triggers a product-leadership pricing review**, not an automatic list-price change — see §5.3 intro and §7.

**All three gates cleared:** fair-value pricing (~$408–$817 / seat / month) becomes defensible. Re-rate to Professional seat ~$299 / seat / month as a full-discount-cleared target.

### 5.4 Discount-stack work-down

> **Why this section.** § 5.3 above describes *what triggers a re-rate*. This section is the **operational tracker** that names *who is driving each gate to clear*, *when* it is expected to clear, *what evidence will close it*, and *which CI / repo signal* will tell finance the gate is ready for a pricing review. The discount magnitudes in § 5.1 and the locked prices in § 5.2 are **unchanged** by this section — it is a project-management overlay, not a price change.

| Discount line | Magnitude | Owner | Target close date | Evidence link | Re-rate trigger |
|---------------|-----------|-------|-------------------|---------------|-----------------|
| Trust discount (SOC 2 Type II + published pen-test) | −25% | TBD (security lead) | TBD (gated on auditor selection) | `docs/security/PEN_TEST_PROGRAM.md` once it lands (file does not yet exist; the link will be made live in the same PR that introduces the program); SOC 2 attestation available under NDA | Auditor opinion letter received **and** filed in the trust portal; pen-test report (or executive summary) approved for prospect distribution |
| Reference discount (named, published reference customer) | −15% | TBD (product marketing) | TBD (gated on first design partner closing) | [`reference-customers/README.md`](reference-customers/README.md) — first row reaching `Status: Published`; CI runs `scripts/ci/check_reference_customer_status.py` twice in `.github/workflows/ci.yml` — warn-only first, then a **strict** re-run **only when** the warn step succeeds (auto-flip; **do not** remove `continue-on-error` by hand on publish day) | At least **one** row in the reference-customers index has `Status: Published` **and** the strict re-run step is active (same commit that introduces the Published row is enough) |

> **TODO (reference discount copy removal):** Do **not** delete the −15% line from §5.1 / §5.2 until product leadership runs the §5.3 **re-rate review**. The **engineering** signal that the gate is ready is the **same** merge to `main` where the first README row hits `Published` (strict CI step auto-flips — no YAML surgery).
| Self-serve discount (trial / billing loop in production) | −10% | TBD (platform PM) | Cleared in-repo as of 2026-04-17; awaits product-leadership pricing review | Merge-blocking `ui-e2e-live` runs [`archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts`](../../archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts) — see § 5.3 Gate #3 | Product leadership signs off on the engineering signal; no further evidence required |

**How this table is maintained.** Every PR that materially advances a gate (e.g., publishes a real reference customer, lands the SOC 2 attestation, completes the pen-test exec summary) updates the matching row's *Owner* and *Target close date* fields and adds a one-line entry to [`docs/CHANGELOG.md`](../CHANGELOG.md). When all three rows reach a "ready for re-rate" state, finance triggers the re-rate gate decision in § 5.3 — this table does **not** authorize price changes on its own.

**Cross-references.** The reference-customers index and case-study placeholder live in [`reference-customers/`](reference-customers/). The CI guard backing the reference row is `scripts/ci/check_reference_customer_status.py`, wired non-blocking in `.github/workflows/ci.yml`. Both are described from the discoverability side in [`README.md`](../../README.md) "Key documentation".

---

## 6. Expansion levers

| Lever | Trigger |
|-------|---------|
| **Add seats** | New architects join the practice or additional teams adopt |
| **Add workspaces** | New business units, product lines, or projects |
| **Tier upgrade** | Need governance, policy packs, audit export, or dedicated support |
| **Run overage** | Sustained usage above tier allowance |
| **Professional services** | Custom finding engines, policy packs, integration consulting |

---

## 7. Sensitivity playbook

Use this when first deals produce signal about price tolerance. Do not change list prices without a product leadership decision — use discounting within deal economics until patterns emerge.

| Signal | Recommended response |
|--------|---------------------|
| Deals stalling at Pro; price is the stated objection | Offer Pro seat at **$129 / seat / month** as a first-year promotional price (document separately; do not change list) |
| First 5 Pro deals close in < 30 days without discount | Raise Pro seat to **$229 / seat / month** at next quarterly re-rate |
| Azure Marketplace is the primary buying motion | Collapse to flat tiers: Team **$499 / month** (up to 5 seats); Pro **$2,499 / month** (up to 15 seats); Enterprise: talk to sales |
| Run overage causes friction (> 3 deals cite it) | Move Pro/Enterprise to **unlimited runs in fair-use**; keep overage only at Team tier |
| Buyers ignore platform fee / only count per-seat | Roll platform fee into higher seat price: Team **$119 / seat**; Pro **$249 / seat** (equivalent monthly at typical seat counts) |

---

## 8. What is NOT included

- **Professional services:** Custom connector development, bespoke policy packs, training workshops — priced separately.
- **Custom infrastructure:** Dedicated compute, customer-managed keys (BYOK), air-gapped deployment — not available in V1 SaaS.
- **Data migration:** Importing architecture data from other tools — roadmap connector (see [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md)).
- **Adds priced separately at Enterprise:** Custom policy pack authoring engagement, SCIM provisioning (when shipped), Azure Service Bus integration setup.

---

## Related documents

| Doc | Use |
|-----|-----|
| [ROI_MODEL.md](ROI_MODEL.md) | Value model, break-even analysis, and payback math |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Who buys and their budget authority |
| [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) | Competitor pricing context |
| [TRIAL_AND_SIGNUP.md](TRIAL_AND_SIGNUP.md) | Self-serve trial design and trial parameters |
| [ORDER_FORM_TEMPLATE.md](ORDER_FORM_TEMPLATE.md) | Subscription order template (prices link back here) |
| [CUSTOMER_ONBOARDING_PLAYBOOK.md](CUSTOMER_ONBOARDING_PLAYBOOK.md) | Post-conversion onboarding (6-week pilot) |
| [POSITIONING.md](POSITIONING.md) | Positioning narrative and proof points |
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Guided pilot success criteria |
| [../CHANGELOG.md](../CHANGELOG.md) | Release history including pricing freeze entry |

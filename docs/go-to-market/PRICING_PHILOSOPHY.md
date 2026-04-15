# ArchLucid — Pricing philosophy and packaging

**Audience:** Product leadership, sales, and finance — internal alignment before external pricing publication.

**Last reviewed:** 2026-04-15

**Grounding:** Pricing anchors to the ROI model in [ROI_MODEL.md](ROI_MODEL.md) (break-even at ~180 architect-hours/year) and buyer personas in [BUYER_PERSONAS.md](BUYER_PERSONAS.md).

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
|-------|------|------|--------------------|
| **Per-seat (architect)** | Simple, predictable, easy to quote | Caps adoption — customers may limit seats to control cost; penalizes broader team usage | **Good base** — aligns with buyer's architect headcount; simple to explain |
| **Per-run (usage)** | Aligns with value delivered; high-volume users pay more | Unpredictable costs; discourages experimentation; complex metering needed | **Poor as primary** — buyers dislike variable cost; good as an overage mechanism |
| **Platform fee + consumption** | Predictable base with usage upside; expansion-friendly | More complex to explain; requires metering infrastructure | **Best hybrid** — predictable base per workspace/team, with run allowances per tier |

**Recommendation:** **Platform fee per workspace + included run allowance** with per-seat pricing for named architects. This gives buyers predictability (platform fee + seats) while allowing expansion via additional workspaces, seats, and run overages.

---

## 3. Packaging tiers

### Tier overview

| | **Team** | **Professional** | **Enterprise** |
|--|----------|-------------------|-----------------|
| **Target buyer** | Small architecture team exploring AI-assisted review | Established architecture practice with governance needs | Large organization with compliance, audit, and multi-team requirements |
| **Target persona** | Persona 3 (CTO/VP Eng) | Persona 1 (Enterprise Architect) | Persona 1 + Persona 2 (Platform Eng Lead) |
| **Seats included** | Up to 5 architects | Up to 20 architects | Unlimited (named) |
| **Workspaces** | 1 | Up to 5 | Unlimited |
| **Runs / month** | 20 | 100 | Custom |
| **Finding engines** | All 10 | All 10 | All 10 + custom engine support |
| **Governance** | Basic (pre-commit gate) | Full (approval workflows, policy packs, segregation of duties) | Full + custom policy packs |
| **Comparison / drift** | Included | Included | Included |
| **Audit trail** | 90-day retention | 1-year retention | Custom retention + export |
| **Authentication** | Entra ID | Entra ID | Entra ID + generic OIDC (roadmap) |
| **Support** | Community / email | Business hours email + onboarding call | Dedicated CSM, priority response |
| **SLA** | Shared SLO targets | Shared SLO targets | Custom SLA with credits |
| **Price range** | $X–$Y / seat / month | $X–$Y / seat / month | Custom quote |

**Note:** Price placeholders ($X–$Y) are intentional. Final pricing requires competitive benchmarking and cost modeling. Suggested approach: set Team tier at a price point accessible for a small team's discretionary budget (< $500/month total), Professional at a level requiring manager approval ($2K–$5K/month), Enterprise at $10K+/month requiring VP/CTO approval.

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

---

## 4. Pilot pricing

| Scenario | Pricing | Duration | Conversion path |
|----------|---------|----------|-----------------|
| **Self-serve trial** | Free | 14 days | Auto-upgrade prompt; see [TRIAL_AND_SIGNUP.md](TRIAL_AND_SIGNUP.md) |
| **Guided pilot** | Free or discounted Professional tier | 6 weeks (per [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md)) | Scorecard review → commercial proposal |
| **Enterprise evaluation** | Custom | Negotiated | Champion + executive sponsor path |

---

## 5. Expansion levers

| Lever | Trigger |
|-------|---------|
| **Add seats** | New architects join the practice or additional teams adopt |
| **Add workspaces** | New business units, product lines, or projects |
| **Tier upgrade** | Need governance, policy packs, audit export, or dedicated support |
| **Run overage** | Sustained usage above tier allowance |
| **Professional services** | Custom finding engines, policy packs, integration consulting |

---

## 6. What is NOT included

- **Professional services:** Custom connector development, bespoke policy packs, training workshops — priced separately.
- **Custom infrastructure:** Dedicated compute, customer-managed keys (BYOK), air-gapped deployment — not available in V1 SaaS.
- **Data migration:** Importing architecture data from other tools — roadmap connector (see [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md)).

---

## Related documents

| Doc | Use |
|-----|-----|
| [ROI_MODEL.md](ROI_MODEL.md) | Value model and break-even analysis |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Who buys and their budget authority |
| [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) | Competitor pricing context |
| [TRIAL_AND_SIGNUP.md](TRIAL_AND_SIGNUP.md) | Self-serve trial design |
| [ORDER_FORM_TEMPLATE.md](ORDER_FORM_TEMPLATE.md) | Subscription order template |

# ArchLucid Marketability Assessment — SaaS-Only Posture (2026-04-15, post-Trust Center)

**Assumption:** ArchLucid is **SaaS-only** — no self-hosted or on-premises deployment path. Buyers evaluate you as a **vendor-operated service**, not software they run in their own cloud or data center.

**Overall Marketability Score (unweighted average): 37 / 100** | Weighted: **37.6%** (4,479 / 11,900)

**Prior SaaS-only assessment (pre-Trust Center):** `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY_PRE_TRUST_CENTER.md` (34/100 headline, 34.8% weighted).

**Companion assessment (mixed / optional self-host framing):** `docs/MARKETABILITY_ASSESSMENT_2026_04_15.md` (58/100 headline, 42.3% weighted under that framing).

**Technical quality (orthogonal):** `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md` (68.5%).

---

## What changed since last assessment

**SaaS Improvement 1 (Trust Center spine)** delivered six documents into `docs/go-to-market/`:

- `TRUST_CENTER.md` — Buyer-facing security index with compliance table, security-at-a-glance, contact.
- `SUBPROCESSORS.md` — Microsoft Azure services, Entra ID, Azure OpenAI; 30-day change notification; data residency statement.
- `DPA_TEMPLATE.md` — GDPR-style Data Processing Agreement template (requires legal review).
- `INCIDENT_COMMUNICATIONS_POLICY.md` — SEV-1–4 classification, customer comms timelines, breach notification addendum.
- `SOC2_ROADMAP.md` — Controls inventory grounded in repo evidence, gap analysis, phased milestones (Q3 2026–Q3 2027+).
- `TENANT_ISOLATION.md` — Three-layer summary (identity, application, database RLS) with Mermaid diagram and honest "not claimed" list.

The `docs/go-to-market/` folder now contains **14 documents** — up from 8 before this improvement.

---

## Why this reframing matters

Under a **mixed** model, gaps in "how to run it yourself" can be partially offset by **flexibility** and **buyer control**. Under **SaaS-only**, those gaps disappear from the narrative — and are replaced by **harder** requirements:

| Theme | SaaS-only implication |
|--------|------------------------|
| **Trust** | SOC 2, DPA, subprocessors, data residency, incident comms — table stakes |
| **Commercial** | Transparent pricing, self-serve signup, billing, contracts — non-negotiable for velocity |
| **Platform** | Your uptime, scale, multi-tenant isolation, and upgrade discipline *are* the product |
| **Procurement** | Security review centers on *your* controls, not "deploy in our VPC" |

**Net:** Several dimensions that were **moderate** under mixed deployment become **critical** when the only path is "trust our tenant." Overall marketability **drops** versus a mixed assessment unless SaaS platform and GTM infrastructure catch up.

---

## Methodology

Same scale as `docs/MARKETABILITY_ASSESSMENT_2026_04_15.md`: twenty dimensions, scores 1–100, weights 1–10. **Weights are rebalanced** for SaaS-only (importance of vendor platform, billing, trust, and land-and-expand). Dimensions ordered by **weighted improvement priority** (weight × gap to 100).

| Range | Meaning |
|-------|---------|
| 90–100 | Market-leading |
| 75–89 | Competitive |
| 60–74 | Adequate |
| 45–59 | Weak — losing deals |
| Below 45 | Critical — blocking |

**Weighted score:** Σ(score × weight) / (Σ weight × 100). Max numerator = 11,900 (weights sum to **119**).

---

## Executive summary

- **12 of 20** dimensions are in **critical** territory (below 45) — down from **14** pre-Trust Center. Two dimensions (**Enterprise readiness** and **Buyer education**) moved out of critical.
- **Enterprise readiness & procurement** saw the **largest single gain** (+15, from 35 to 50) — the trust center spine provides the skeleton procurement pack that SaaS buyers expect (DPA, subprocessors, incident comms, SOC 2 roadmap).
- **Largest remaining weighted gaps:** GTM commercial infrastructure (pricing, signup, billing), SaaS platform maturity (status page, DR evidence, multi-region), and business model clarity — these require **product and business** investment, not documentation.
- **Bright spots:** The trust center gives the sales team a **credible answer** to "where is your security documentation?" for the first time. Combined with existing GTM docs (competitive landscape, positioning, ROI model, pilot scorecard, datasheet), ArchLucid now has a **14-document buyer-facing library** — meaningful for a pre-revenue product.
- **Strategic implication:** Trust documentation is necessary but not sufficient. The next SaaS-only improvements must address **platform observability** (status page, SLOs in buyer contracts), **commercial rails** (pricing, trial, billing), and **customer success infrastructure**.

---

## Dimension scores and SaaS-only weights

Rows ordered by **weighted improvement priority** (weight × (100 − score)), highest first.

| # | Dimension | Pre | Post | Δ | Weight | Weighted priority (× gap) | Change rationale |
|---|-----------|-----|------|---|--------|---------------------------|------------------|
| 1 | **GTM, pricing, signup, billing** | 28 | **30** | +2 | **10** | 700 | Trust center aids sales conversations; no pricing/signup change |
| 2 | **SaaS platform & reliability** | 18 | **25** | +7 | **9** | 675 | Tenant isolation one-pager + incident comms articulate the story; platform itself unchanged |
| 3 | **Business model & scalability** | 25 | **25** | 0 | **8** | 600 | No change |
| 4 | **Customer success & retention** | 30 | **33** | +3 | **8** | 536 | Incident comms policy is a CS artifact; no health scoring, onboarding checklist, or renewal playbook yet |
| 5 | **Time-to-value** | 40 | **40** | 0 | **8** | 480 | No change |
| 6 | **Enterprise readiness & procurement** | 35 | **50** | +15 | **9** | 450 | DPA template, subprocessors, SOC 2 roadmap, incident comms — skeleton procurement pack exists |
| 7 | **Differentiation & positioning** | 48 | **50** | +2 | **9** | 450 | Trust center differentiates from competitors without governance artifacts |
| 8 | **Product–market fit evidence** | 50 | **50** | 0 | **9** | 450 | No change |
| 9 | **Technology ecosystem** | 38 | **38** | 0 | **6** | 372 | No change |
| 10 | **ROI & business case** | 48 | **48** | 0 | **7** | 364 | No change |
| 11 | **Content & thought leadership** | 22 | **28** | +6 | **5** | 360 | Six publishable trust/security documents; still no blog or external presence |
| 12 | **UX & demo experience** | 48 | **48** | 0 | **6** | 312 | No change |
| 13 | **Pilot-to-paid conversion** | 45 | **48** | +3 | **5** | 260 | DPA template enables contracting on vendor paper |
| 14 | **Partner & channel** | 18 | **18** | 0 | **3** | 246 | No change |
| 15 | **Vertical specificity** | 28 | **28** | 0 | **3** | 216 | No change |
| 16 | **Buyer education & docs** | 44 | **52** | +8 | **4** | 192 | Trust center is buyer-facing; 14 GTM docs now |
| 17 | **Community & advocacy** | 12 | **12** | 0 | **2** | 176 | No change |
| 18 | **Internationalization** | 20 | **22** | +2 | **2** | 156 | Data residency statement in subprocessors |
| 19 | **Brand awareness** | 28 | **29** | +1 | **2** | 142 | Trust center improves professional perception marginally |
| 20 | **Market timing** | 58 | **58** | 0 | **2** | 84 | No change |

**Totals:** Unweighted average **36.6** (rounds to **37/100**). Σ(score × weight) = **4,479**; Σ weight = **119**; **weighted = 4,479 / 11,900 ≈ 37.6%**.

**Score math:** (25×9)+(30×10)+(25×8)+(50×9)+(33×8)+(40×8)+(50×9)+(50×9)+(28×5)+(38×6)+(48×7)+(48×6)+(48×5)+(18×3)+(52×4)+(28×3)+(12×2)+(22×2)+(29×2)+(58×2) = 225+300+200+450+264+320+450+450+140+228+336+288+240+54+208+84+24+44+58+116 = **4,479**.

---

## Score trajectory (SaaS-only)

| Milestone | Weighted % | Unweighted avg | Δ weighted | Critical dims |
|-----------|-----------|----------------|------------|---------------|
| Pre-Trust Center | 34.8% | 34.2 | — | 14 of 20 |
| **Post-Trust Center** | **37.6%** | **36.6** | **+2.8** | **12 of 20** |

---

## Gap analysis (SaaS-only, post-Trust Center)

### Critical (< 45) — 12 dimensions

1. **GTM / commercial rails (30)** — Trust docs help the sales conversation, but **pricing, signup, billing, trial** are still absent. This is the highest weighted gap remaining.
2. **SaaS platform (25)** — Tenant isolation and incident comms are now **articulated**, but there is no **status page**, no **tested DR summary for buyers**, no **multi-region**, no **backup/RTO/RPO statement** outside engineering runbooks. Articulation moved score from 18 to 25; platform substance must follow.
3. **Business model (25)** — Unchanged. Seat vs workload vs outcome; expansion levers; metering — all still undefined.
4. **Customer success (33)** — Incident comms is one CS artifact. Still no onboarding checklist, health scoring, or renewal playbook.
5. **Time-to-value (40)** — Unchanged. Docker demo (M4) exists but setup friction is inherent.
6. **Technology ecosystem (38)** — Unchanged. No SDK, no inbound connectors, no CI/CD examples.
7. **Content (28)** — Six trust docs are publishable, but no external blog, whitepaper, or DevRel presence yet.
8. **Vertical (28), partner (18), community (12), i18n (22), brand (29)** — Unchanged or near-unchanged; deprioritized by weight.

### Weak (45–59) — 6 dimensions

- **Enterprise readiness (50)** — Newly promoted from critical. Skeleton procurement pack exists. Not yet competitive because SOC 2 is "in progress," no independent pen test, DPA needs legal review, no published SLA.
- **Differentiation (50), PMF evidence (50), ROI (48), UX (48), pilot-to-paid (48)** — Reinforced by trust artifacts but still need **proof** (pilot data, case studies, completed screenshots).

### Adequate or above (≥ 60) — 2 dimensions

- **Buyer education (52)** — Newly promoted from critical. 14 GTM docs across positioning, competitive, ROI, trust, demo.
- **Market timing (58)** — AI + governance tailwind unchanged.

---

## Six prioritized improvements (SaaS-only)

| # | Improvement | Status | Next step |
|---|-------------|--------|-----------|
| 1 | **Trust center spine** | **Done** | Legal review of DPA; publish to web or customer portal |
| 2 | **SaaS operational posture** | Open | Status page, buyer-language SLOs, backup/DR summary, RTO/RPO commitment |
| 3 | **Commercial motion** | Open | Pricing philosophy, trial/signup, billing integration, MSA/order form |
| 4 | **Customer success MVP** | Open | Onboarding checklist, health signals, renewal playbook |
| 5 | **Integrations as product** | Open | IdP (SCIM), SIEM/export, Jira/ADO — framed as SaaS connectors |
| 6 | **Narrow ICP + proof** | Open | 2–3 reference narratives from pilots |

See `docs/CURSOR_PROMPTS_SAAS_IMPROVEMENTS_2_TO_6.md` for executable Cursor prompts for Improvements 2–6.

---

## Messaging shift: mixed model → SaaS-only (updated)

| Mixed / self-host friendly | SaaS-only replacement | Trust center support |
|----------------------------|------------------------|----------------------|
| "Deploy in your Azure subscription" | "Hosted by ArchLucid; your data isolated per tenant" | `TENANT_ISOLATION.md` |
| "You control the network boundary" | "We use private connectivity and encryption; here is our architecture" | `TRUST_CENTER.md` security overview |
| "Bring your own keys" (if not offered) | Roadmap honesty + current key management story | `TENANT_ISOLATION.md` §5 |
| "Air-gapped option" | Not available — position export, offline artifacts, or partners | — |
| "Install guide" | "Get started in 10 minutes" + trust links | `TRUST_CENTER.md` |
| "Who are your subprocessors?" | "Here is the list and our 30-day notification commitment" | `SUBPROCESSORS.md` |
| "Do you have a DPA?" | "Template available; legal review required" | `DPA_TEMPLATE.md` |
| "Where is your SOC 2?" | "In progress — here is the roadmap and current controls" | `SOC2_ROADMAP.md` |

---

## Conclusion

The Trust Center spine moved **Enterprise readiness** from critical to weak and **Buyer education** from critical to weak — reducing critical dimensions from **14 to 12**. Weighted score rose **2.8 percentage points** (34.8% → 37.6%). The go-to-market library now covers **14 documents**: competitive landscape, personas, positioning, datasheet, screenshots, ROI model, pilot scorecard, demo quickstart, trust center, subprocessors, DPA template, incident comms, SOC 2 roadmap, and tenant isolation.

The largest remaining gaps are **structural** rather than documentary: commercial infrastructure (pricing, billing, signup), platform observability (status page, SLOs in contracts), and customer success tooling. Improvement 2 (SaaS operational posture) is the recommended next execution target.

---

## Related documents

| Doc | Use |
|-----|-----|
| `docs/MARKETABILITY_ASSESSMENT_2026_04_15.md` | Primary assessment with optional self-host framing |
| `docs/go-to-market/TRUST_CENTER.md` | Trust index (Improvement 1 deliverable) |
| `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` | Competitive context |
| `docs/go-to-market/POSITIONING.md` | Positioning |
| `docs/go-to-market/ROI_MODEL.md` | ROI (M3) |
| `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` | Pilot metrics (M3) |
| `docs/go-to-market/DEMO_QUICKSTART.md` | Docker demo (M4) |
| `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md` | Technical quality |
| `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY_PRE_TRUST_CENTER.md` | Prior SaaS-only assessment (34.8%) |
| `docs/CURSOR_PROMPTS_SAAS_IMPROVEMENTS_2_TO_6.md` | Executable Cursor prompts for remaining improvements |

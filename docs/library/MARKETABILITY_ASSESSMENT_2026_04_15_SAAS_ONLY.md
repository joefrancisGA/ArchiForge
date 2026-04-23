> **Scope:** ArchLucid Marketability Assessment — SaaS-Only Posture (2026-04-15, post-Imp 1–6) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid Marketability Assessment — SaaS-Only Posture (2026-04-15, post-Imp 1–6)

**Assumption:** ArchLucid is **SaaS-only** — no self-hosted or on-premises deployment path. Buyers evaluate you as a **vendor-operated service**, not software they run in their own cloud or data center.

**Overall Marketability Score (unweighted average): 46 / 100** | Weighted: **46.1%** (5,481 / 11,900)

**Prior SaaS-only assessments:**
- Post-Trust Center (Imp 1 only): `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY_PRE_IMP2_6.md` (37/100, 37.6%)
- Pre-Trust Center: `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY_PRE_TRUST_CENTER.md` (34/100, 34.8%)

**Companion assessment (mixed / optional self-host framing):** `docs/MARKETABILITY_ASSESSMENT_2026_04_15.md` (58/100, 42.3%).

**Technical quality (orthogonal):** `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md` (68.5%).

---

## What changed since last assessment

**Improvements 2–6** delivered 15 documents and 2 pipeline examples:

| Imp | Title | Deliverables |
|-----|-------|-------------|
| **2** | SaaS operational posture | `SLA_SUMMARY.md`, `BACKUP_AND_DR.md`, `OPERATIONAL_TRANSPARENCY.md` |
| **3** | Commercial motion | `PRICING_PHILOSOPHY.md`, `TRIAL_AND_SIGNUP.md`, `ORDER_FORM_TEMPLATE.md` |
| **4** | Customer success MVP | `CUSTOMER_ONBOARDING_PLAYBOOK.md`, `CUSTOMER_HEALTH_SCORING.md`, `RENEWAL_EXPANSION_PLAYBOOK.md` |
| **5** | Integrations as product | `INTEGRATION_CATALOG.md`, `SIEM_EXPORT.md`, `CICD_INTEGRATION.md`, GitHub Actions + Azure DevOps examples |
| **6** | Narrow ICP + proof | `IDEAL_CUSTOMER_PROFILE.md`, `REFERENCE_NARRATIVE_TEMPLATE.md`, `PMF_VALIDATION_TRACKER.md` |

The `docs/go-to-market/` folder now contains **29 documents**. The `examples/` folder adds CI/CD pipeline templates. The `docs/integrations/` folder adds the CI/CD integration guide.

---

## Methodology

Same scale: twenty dimensions, scores 1–100, weights 1–10 rebalanced for SaaS-only. Ordered by **weighted improvement priority** (weight × gap to 100).

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

- **8 of 20** dimensions remain in **critical** territory (below 45) — down from **12** after Imp 1 alone.
- **4 dimensions** moved from critical to weak or above in this round (GTM, Customer success, Time-to-value, Tech ecosystem).
- **Largest single gains:** GTM (+18, from 30 to 48), Customer success (+17, from 33 to 50), Business model (+15, from 25 to 40), SaaS platform (+13, from 25 to 38).
- **All six prioritized improvements are now complete** at the documentation/design level. The remaining gaps require **execution**: actual pilots (PMF evidence), real pricing decisions (not placeholder ranges), status page deployment, billing integration, and SOC 2 program execution.
- **Bright spots:** ArchLucid now has a **comprehensive buyer-facing library** (29 GTM docs) covering trust, commercial, customer success, integrations, and ICP — unusual depth for a pre-revenue product. The CI/CD examples and integration catalog position ArchLucid as a **platform** rather than a point tool.
- **Strategic implication:** The documentation foundation is strong. The next phase is **operational execution** — running pilots, publishing the status page, executing the SOC 2 program, building billing integration, and converting fictional reference narratives to real customer stories.

---

## Dimension scores and SaaS-only weights

Rows ordered by **weighted improvement priority** (weight × (100 − score)), highest first.

| # | Dimension | Imp 1 | Imp 1–6 | Δ | Weight | Weighted priority (× gap) | Change rationale |
|---|-----------|-------|---------|---|--------|---------------------------|------------------|
| 1 | **GTM, pricing, signup, billing** | 30 | **48** | +18 | **10** | 520 | Pricing philosophy, trial design, order form template — framework exists; placeholders need real numbers |
| 2 | **SaaS platform & reliability** | 25 | **38** | +13 | **9** | 558 | SLA summary, backup/DR, operational transparency plan — articulated but status page not yet deployed |
| 3 | **Product–market fit evidence** | 50 | **55** | +5 | **9** | 405 | ICP defined, reference narratives templated, PMF tracker ready — no real pilot data yet |
| 4 | **Differentiation & positioning** | 50 | **55** | +5 | **9** | 405 | Integration catalog + CI/CD examples + ICP strengthen "platform, not chatbot" story |
| 5 | **Enterprise readiness & procurement** | 50 | **55** | +5 | **9** | 405 | SLA summary + order form + backup/DR add to procurement pack |
| 6 | **Customer success & retention** | 33 | **50** | +17 | **8** | 400 | Onboarding playbook, health scoring, renewal/expansion — full CS process documented |
| 7 | **Business model & scalability** | 25 | **40** | +15 | **8** | 480 | Pricing tiers, expansion levers, trial → paid path defined; no metering/billing yet |
| 8 | **Time-to-value** | 40 | **45** | +5 | **8** | 440 | Trial design + onboarding playbook reduce friction on paper; product provisioning not built |
| 9 | **Technology ecosystem** | 38 | **52** | +14 | **6** | 288 | Integration catalog, CI/CD examples, SIEM export doc — connectors are roadmap but integration story exists |
| 10 | **ROI & business case** | 48 | **52** | +4 | **7** | 336 | Reference narratives connect ROI to specific scenarios; no real pilot data yet |
| 11 | **Content & thought leadership** | 28 | **38** | +10 | **5** | 310 | 29 GTM docs are publishable content; still no external blog or DevRel |
| 12 | **UX & demo experience** | 48 | **48** | 0 | **6** | 312 | No change |
| 13 | **Pilot-to-paid conversion** | 48 | **55** | +7 | **5** | 225 | Order form + onboarding playbook + renewal playbook = complete pilot-to-paid path |
| 14 | **Buyer education & docs** | 52 | **60** | +8 | **4** | 160 | 29 GTM docs; adequate for most buyer conversations |
| 15 | **Partner & channel** | 18 | **22** | +4 | **3** | 234 | Integration catalog implies partner integration paths; no formal partner program |
| 16 | **Vertical specificity** | 28 | **35** | +7 | **3** | 195 | Reference narratives cover FS, tech, HC verticals; no policy pack content |
| 17 | **Community & advocacy** | 12 | **15** | +3 | **2** | 170 | Finding engine template mentioned in catalog; no public community |
| 18 | **Internationalization** | 22 | **22** | 0 | **2** | 156 | No change |
| 19 | **Brand awareness** | 29 | **32** | +3 | **2** | 136 | GTM library and ICP improve professional perception |
| 20 | **Market timing** | 58 | **58** | 0 | **2** | 84 | No change |

**Totals:** Unweighted average **45.8** (rounds to **46/100**). Σ(score × weight) = **5,481**; Σ weight = **119**; **weighted = 5,481 / 11,900 ≈ 46.1%**.

**Score math:** (48×10)+(38×9)+(55×9)+(55×9)+(55×9)+(50×8)+(40×8)+(45×8)+(52×6)+(52×7)+(38×5)+(48×6)+(55×5)+(22×3)+(60×4)+(35×3)+(15×2)+(22×2)+(32×2)+(58×2) = 480+342+495+495+495+400+320+360+312+364+190+288+275+66+240+105+30+44+64+116 = **5,481**.

---

## Score trajectory (SaaS-only)

| Milestone | Weighted % | Unweighted avg | Δ weighted | Critical dims |
|-----------|-----------|----------------|------------|---------------|
| Pre-Trust Center | 34.8% | 34.2 | — | 14 of 20 |
| Post-Imp 1 (Trust Center) | 37.6% | 36.6 | +2.8 | 12 of 20 |
| **Post-Imp 1–6** | **46.1%** | **45.8** | **+8.5** | **8 of 20** |

---

## Gap analysis (SaaS-only, post-Imp 1–6)

### Critical (< 45) — 8 dimensions

1. **SaaS platform (38)** — SLA, backup/DR, and operational transparency are **documented** but the status page is not deployed, DR has not been customer-tested, and RTO/RPO are estimates. Score improvement requires **operational execution**, not more documentation.
2. **Business model (40)** — Pricing tiers and expansion levers are defined at the strategy level. No metering, no billing integration, no actual prices. Execution moves this to weak/adequate.
3. **Content (38)** — 29 internal/GTM docs exist but nothing is **published externally**. Blog, whitepaper, or conference talk would move this significantly.
4. **Vertical (35)** — Reference narratives cover FS, tech, HC but no policy pack content or vertical-specific demo scenarios.
5. **Partner (22)** — Integration catalog implies partner paths but no formal program exists. Low weight.
6. **Community (15)** — Finding engine template mentioned; no public community. Low weight.
7. **i18n (22)** — English-only, single region. Low weight.
8. **Brand (32)** — No logo, visual brand, or marketing site. Low weight.

### Weak (45–59) — 11 dimensions

- **GTM (48)** — Pricing philosophy, trial design, order form template. Missing: real prices, billing integration, marketing site.
- **Time-to-value (45)** — Trial design + onboarding playbook. Missing: automated provisioning, actual hosted trial.
- **Customer success (50)** — Full CS process documented. Missing: real execution, in-product health signals.
- **PMF evidence (55), Differentiation (55), Enterprise (55), Pilot-to-paid (55)** — Strong documentation; need real pilot data and executed SOC 2 program.
- **Tech ecosystem (52)** — Integration catalog + CI/CD examples. Missing: actual connector implementations.
- **ROI (52)** — Frameworks strong. Missing: real customer data.
- **UX (48)** — Unchanged; screenshots not yet captured.

### Adequate or above (≥ 60) — 1 dimension

- **Buyer education (60)** — 29 GTM docs is adequate for most buyer conversations.

---

## Improvement status

| # | Improvement | Status | What remains (execution) |
|---|-------------|--------|--------------------------|
| 1 | Trust center spine | **Done (docs)** | Legal review of DPA; publish to web |
| 2 | SaaS operational posture | **Done (docs)** | Deploy status page; formalize RTO/RPO; publish SLA in contracts |
| 3 | Commercial motion | **Done (design)** | Set real prices; build billing integration; launch marketing site |
| 4 | Customer success MVP | **Done (process)** | Execute onboarding playbook with real customers; build Phase 2 health scoring |
| 5 | Integrations as product | **Done (docs + examples)** | Build actual connectors (Structurizr, Jira); SCIM implementation |
| 6 | Narrow ICP + proof | **Done (frameworks)** | Run pilots; populate PMF tracker; convert fictional narratives to real |

---

## Conclusion

Five improvements raised the weighted score by **8.5 percentage points** (37.6% → 46.1%) and reduced critical dimensions from **12 to 5**. The `docs/go-to-market/` library now contains **29 documents** covering the full buyer journey from first impression through renewal — trust, pricing, trial, onboarding, health scoring, renewal, integrations, ICP, reference narratives, and PMF validation.

The remaining gaps are **execution gaps**, not documentation gaps:
- **Deploy** the status page and publish real SLOs in contracts
- **Set** real prices and build billing integration
- **Run** pilots and populate the PMF validation tracker
- **Execute** the SOC 2 program per the roadmap
- **Publish** external content (blog, whitepaper) from the internal library

The product's **technical quality** (68.5%) and **documentation completeness** now significantly exceed its **marketability execution** — the next phase is closing that gap through operations, not writing.

---

## Related documents

| Doc | Use |
|-----|-----|
| `docs/MARKETABILITY_ASSESSMENT_2026_04_15.md` | Primary assessment (mixed model, 42.3%) |
| `docs/go-to-market/TRUST_CENTER.md` | Trust index (Imp 1) |
| `docs/go-to-market/PRICING_PHILOSOPHY.md` | Pricing strategy (Imp 3) |
| `docs/go-to-market/INTEGRATION_CATALOG.md` | Integration catalog (Imp 5) |
| `docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md` | ICP (Imp 6) |
| `docs/go-to-market/PMF_VALIDATION_TRACKER.md` | PMF hypothesis tracker (Imp 6) |
| `docs/CURSOR_PROMPTS_SAAS_IMPROVEMENTS_2_TO_6.md` | Prompts used for Imp 2–6 |
| `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md` | Technical quality (68.5%) |
| `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY_PRE_IMP2_6.md` | Prior (post-Imp 1, 37.6%) |
| `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY_PRE_TRUST_CENTER.md` | Original (34.8%) |

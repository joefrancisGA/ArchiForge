# ArchLucid Marketability Assessment — SaaS-Only Posture (2026-04-15)

**Assumption:** ArchLucid is **SaaS-only** — no self-hosted or on-premises deployment path. Buyers evaluate you as a **vendor-operated service**, not software they run in their own cloud or data center.

**Overall Marketability Score (unweighted average): 34 / 100** | Weighted: **34.8%** (4,136 / 11,900)

**Companion assessment (mixed / optional self-host framing):** `docs/MARKETABILITY_ASSESSMENT_2026_04_15.md` (58/100 headline, 42.3% weighted under that framing).

**Technical quality (orthogonal):** `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md` (68.5%).

---

## Why this reframing matters

Under a **mixed** model, gaps in “how to run it yourself” can be partially offset by **flexibility** and **buyer control**. Under **SaaS-only**, those gaps disappear from the narrative — and are replaced by **harder** requirements:

| Theme | SaaS-only implication |
|--------|------------------------|
| **Trust** | SOC 2, DPA, subprocessors, data residency, incident comms — table stakes |
| **Commercial** | Transparent pricing, self-serve signup, billing, contracts — non-negotiable for velocity |
| **Platform** | Your uptime, scale, multi-tenant isolation, and upgrade discipline *are* the product |
| **Procurement** | Security review centers on *your* controls, not “deploy in our VPC” |

**Net:** Several dimensions that were **moderate** under mixed deployment become **critical** when the only path is “trust our tenant.” Overall marketability **drops** versus a mixed assessment unless SaaS platform and GTM infrastructure catch up.

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

- **14 of 20** dimensions sit in **critical** territory (below 45) under SaaS-only weights — versus a smaller critical set when self-hosting can absorb some buyer anxiety.
- **Largest weighted gaps:** SaaS platform maturity (multi-tenant isolation, SLOs, DR, roadmap for regions), **GTM plumbing** (pricing page, signup, billing), **enterprise procurement pack** (SOC 2, DPA, SLA), and **customer success** motion for a service they cannot “patch locally.”
- **Bright spots** that still transfer: **differentiation** (agentic, evidence-linked outputs), **PMF narrative** for architecture engagement, **ROI** and **pilot scorecard** (M3), **UX** investment, and **market timing** (AI + governance pressure).
- **Strategic implication:** The product can be technically strong (see quality assessment) yet **under-marketable as pure SaaS** until **platform + trust + commercial rails** match the story.

---

## Dimension scores and SaaS-only weights

Rows ordered by **weighted improvement priority** (weight × (100 − score)), highest first.

| # | Dimension | Score | Weight | Weighted priority (× gap) | SaaS-only rationale for weight |
|---|-----------|-------|--------|---------------------------|--------------------------------|
| 1 | **SaaS platform & reliability** | 18 | **9** | 738 | You *are* the infrastructure |
| 2 | **GTM, pricing, signup, billing** | 28 | **10** | 720 | No self-serve = friction |
| 3 | **Business model & scalability** | 25 | **8** | 600 | Unit economics and expansion |
| 4 | **Enterprise readiness & procurement** | 35 | **9** | 585 | Vendor trust, not buyer-deployed |
| 5 | **Customer success & retention** | 30 | **8** | 560 | Churn risk without on-prem escape |
| 6 | **Time-to-value** | 40 | **8** | 480 | First session must “just work” |
| 7 | **Differentiation & positioning** | 48 | **9** | 468 | Still core |
| 8 | **Product–market fit evidence** | 50 | **9** | 450 | Case studies, logos |
| 9 | **Content & thought leadership** | 22 | **5** | 390 | Lower than platform/GTM |
| 10 | **Technology ecosystem** | 38 | **6** | 372 | Integrations as SaaS connectors |
| 11 | **ROI & business case** | 48 | **7** | 364 | M3 helps |
| 12 | **UX & demo experience** | 48 | **6** | 312 | Trial is the product |
| 13 | **Pilot-to-paid conversion** | 45 | **5** | 275 | Contracting on *your* paper |
| 14 | **Partner & channel** | 18 | **3** | 246 | Important but later |
| 15 | **Buyer education & docs** | 44 | **4** | 224 | Trust docs, not install docs |
| 16 | **Vertical specificity** | 28 | **3** | 216 | Nice-to-have early |
| 17 | **Community & advocacy** | 12 | **2** | 176 | Long cycle |
| 18 | **Internationalization** | 20 | **2** | 160 | Reg + language |
| 19 | **Brand awareness** | 28 | **2** | 144 | Earned over time |
| 20 | **Market timing** | 58 | **2** | 84 | Tailwind remains |

**Totals:** Unweighted average **34.2** (rounds to **34/100**). Σ(score × weight) = **4,136**; Σ weight = **119**; **weighted = 4,136 / 11,900 ≈ 34.8%**.

---

## Gap analysis (SaaS-only)

### Critical (< 45)

1. **SaaS platform (18)** — Buyers will ask: tenant isolation, encryption, backups, RTO/RPO, status page, incident process, data deletion, region strategy. A “strong codebase” does not substitute for **articulated** operational maturity.
2. **GTM / commercial rails (28)** — Without self-host, **self-serve or low-friction sales** and **clear pricing** matter more. Missing pieces read as “not ready to buy.”
3. **Business model clarity (25)** — Seat vs workload vs outcome; expansion path; professional services boundary — all must be crisp for SaaS CFO scrutiny.
4. **Enterprise readiness (35)** — SOC 2 timeline, DPA, subprocessors, SLA, support tiers — weighted **up** vs mixed model.
5. **Customer success (30)** — Playbooks, health metrics, and escalation for a service they cannot operate.
6. **Content (22)** — Trust content (security, architecture) beats generic thought leadership for SaaS buyers.
7. **Partners (18)** — SI and cloud marketplaces matter for enterprise SaaS distribution.
8. **Buyer docs (44)** — Security architecture, data flow, and compliance mapping — not installation guides.
9. **Vertical (28), community (12), i18n (20), brand (28)** — Deprioritized weights but still mostly critical **scores**; fix after platform and GTM.

### Adequate / competitive

- **Market timing (58)** — AI + governance still favorable.
- **Differentiation, PMF narrative, ROI, UX, pilot** — Mid-40s to 50; reinforce with **proof** and **published** trust artifacts.

---

## Six prioritized improvements (SaaS-only)

1. **Ship a “trust center” spine** — Public security overview, subprocessors, DPA template, incident comms policy, roadmap to SOC 2 (even if “in progress” with clear milestones).
2. **Publish SaaS operational posture** — Status page, SLOs in buyer language, backup/DR summary, tenant isolation one-pager, data residency statement (even if single-region today).
3. **Unblock commercial motion** — Pricing philosophy page, trial/signup path, billing integration story, order form / MSA pattern for SMB-midmarket.
4. **Customer success minimum viable** — Onboarding checklist, health signals, renewal playbook; tie to pilot scorecard (M3).
5. **Integrations as product** — IdP (SCIM if claimed), SIEM/export, Jira/ADO — framed as **your** connectors, not “run our agent in your VPC.”
6. **Narrow ICP + proof** — 2–3 reference narratives emphasizing **vendor-managed** value and time-to-first-outcome.

---

## Messaging shift: mixed model → SaaS-only

| Mixed / self-host friendly | SaaS-only replacement |
|----------------------------|------------------------|
| “Deploy in your Azure subscription” | “Hosted by ArchLucid; your data isolated per tenant” |
| “You control the network boundary” | “We use private connectivity and encryption in transit and at rest; here is our architecture” |
| “Bring your own keys” (if not offered) | Roadmap honesty + current key management story |
| “Air-gapped option” | Not available — position **export**, **offline artifacts**, or **partners** if needed |
| “Install guide” | “Get started in 10 minutes” + trust links |

---

## Conclusion

SaaS-only **raises the bar** on **platform, trust, and commercial completeness**. The codebase can score well on technical quality while **marketability as a service vendor** lags until those surfaces match buyer expectations. Use this document for **GTM, security, and product roadmap** alignment; use the mixed-model assessment when explaining **flexibility** that you do **not** plan to offer — to avoid a credibility gap.

---

## Related documents

| Doc | Use |
|-----|-----|
| `docs/MARKETABILITY_ASSESSMENT_2026_04_15.md` | Primary assessment with optional self-host framing |
| `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` | Competitive context |
| `docs/go-to-market/POSITIONING.md` | Positioning |
| `docs/go-to-market/ROI_MODEL.md` | ROI (M3) |
| `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` | Pilot metrics (M3) |
| `docs/go-to-market/DEMO_QUICKSTART.md` | Docker demo (seller-led; not buyer self-host) |
| `docs/go-to-market/TRUST_CENTER.md` | Buyer trust index (Improvement 1 spine: DPA, subprocessors, incidents, SOC 2 roadmap) |
| `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md` | Technical quality |

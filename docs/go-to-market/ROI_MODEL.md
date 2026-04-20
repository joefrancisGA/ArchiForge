> **Scope:** ArchLucid ROI model - full detail, tables, and links in the sections below.

# ArchLucid ROI model

**Audience:** Pilot champions, enterprise architects, and engineering leaders who need to justify an ArchLucid purchase to their CFO or procurement team.

**Last reviewed:** 2026-04-17

**Grounding rule:** Value claims are mapped to shipped V1 capabilities per [V1_SCOPE.md](../V1_SCOPE.md). Estimates are conservative. Adjust all numbers to your organization's actuals.

**Pricing:** Current list prices (seat, platform fee, run overage, pilot) are in [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) — the single source of truth. The value model in this document is the input that justifies those prices; the prices themselves live only in that file.

---

## 1. Objective

Provide a **reusable template** for building a business case. Fill in your organization's numbers in the "Your value" column and present the result to leadership.

---

## 2. Cost of the status quo

Collect these numbers from your architecture practice before starting the pilot.

| Input | Description | Industry benchmark | Your value |
|-------|-------------|-------------------|------------|
| **Reviews per quarter** | Architecture reviews conducted (formal or informal) | 8–20 for a 200-person eng org | _______ |
| **Hours per review** | Total person-hours: architect prep, stakeholder sessions, documentation, follow-up | 30–60 hours | _______ |
| **Architect hourly cost** | Fully loaded (salary + benefits + overhead) | $120–$200/hr | _______ |
| **Compliance gaps found post-deploy (per year)** | Architecture-related findings surfaced during audits or incidents, not during design | 4–12 per year | _______ |
| **Cost per remediation** | Average cost to fix a compliance or architecture gap found in production (eng time + incident + audit response) | $15K–$75K | _______ |
| **Inconsistency incidents (per year)** | Incidents caused by teams making different architecture choices for the same problem (duplicated infra, conflicting patterns, integration failures) | 2–8 per year | _______ |
| **Cost per inconsistency incident** | Average eng time + customer impact + rework | $10,000–$50,000 | _______ |

### Status quo annual cost formula

```
Annual review cost       = Reviews/quarter × 4 × Hours/review × Hourly cost
Annual remediation cost  = Gaps/year × Cost per remediation
Annual inconsistency cost = Incidents/year × Cost per incident
─────────────────────────────────────────────────────────────
Total status quo cost    = Sum of above
```

### Example: 200-person engineering organization

| Input | Value |
|-------|-------|
| Reviews per quarter | 12 |
| Hours per review | 40 |
| Architect hourly cost | $150 |
| Compliance gaps post-deploy | 6 / year |
| Cost per remediation | $30,000 |
| Inconsistency incidents | 4 / year |
| Cost per incident | $25,000 |

```
Annual review cost       = 12 × 4 × 40 × $150 = $288,000
Annual remediation cost  = 6 × $30,000          = $180,000
Annual inconsistency cost = 4 × $25,000          = $100,000
─────────────────────────────────────────────────────────────
Total status quo cost    = $568,000
```

---

## 3. ArchLucid value model

Each value lever maps to a specific product capability. Do not claim value for capabilities you will not use.

### 3.1 Review cycle time reduction

| Lever | How ArchLucid helps | V1 capability | Conservative estimate |
|-------|--------------------|--------------|-----------------------|
| **Automated initial analysis** | AI agents perform topology, cost, compliance, and critique analysis — architect reviews findings rather than conducting full analysis from scratch | `IAuthorityRunOrchestrator`, 4 agent types, 10 finding engines | 50% reduction in architect hours per review |
| **Structured request intake** | Seven-step wizard standardizes what information is captured, eliminating back-and-forth on "what do I need to provide" | First-run wizard, `ArchitectureRequest` schema | 10% reduction in total review cycle time |
| **Automated documentation** | DOCX export produces a stakeholder-ready report automatically — no manual report writing | `IDocxExportService`, artifact bundles | 4–8 hours saved per review (documentation phase) |

**Review cost with ArchLucid:**

```
Reduced hours/review     = Current hours × 0.50 (conservative)
New annual review cost   = Reviews/quarter × 4 × Reduced hours × Hourly cost
Review savings           = Current review cost − New review cost
```

**Example:** `12 × 4 × 20 × $150 = $144,000` → **savings: $144,000/year**

### 3.2 Compliance shift-left

| Lever | How ArchLucid helps | V1 capability | Conservative estimate |
|-------|--------------------|--------------|-----------------------|
| **Pre-commit governance gate** | Findings at or above a configurable severity threshold block manifest commit — compliance gaps caught at design time, not in production | `PreCommitGovernanceGate`, `BlockCommitMinimumSeverity` | 50% reduction in post-deploy compliance gaps |
| **Policy pack enforcement** | Versioned compliance rules applied consistently across every review — no "forgot to check" scenarios | `PolicyPackContentDocument`, `IEffectiveGovernanceResolver` | 30% reduction in inconsistency incidents |
| **Approval workflow** | Segregation of duties enforced — architecture changes require explicit approval | `GovernanceApprovalRequests`, self-approval blocked | Audit evidence generated automatically |

**Compliance savings:**

```
Reduced gaps/year        = Current gaps × 0.50
Compliance savings       = (Current gaps − Reduced gaps) × Cost per remediation
Reduced incidents/year   = Current incidents × 0.30
Inconsistency savings    = (Current − Reduced) × Cost per incident
```

**Example:**
- Compliance: `3 × $30,000 = $90,000 saved`
- Inconsistency: `1.2 × $25,000 = $30,000 saved`
- **Total compliance savings: $120,000/year**

### 3.3 Audit and documentation efficiency

| Lever | How ArchLucid helps | V1 capability | Conservative estimate |
|-------|--------------------|--------------|-----------------------|
| **Durable audit trail** | 78 typed audit events with append-only enforcement — compliance evidence generated as a byproduct of normal operation | `dbo.AuditEvents`, `DENY UPDATE/DELETE`, JSON/CSV export | 2–4 weeks saved per audit cycle |
| **Evidence packages** | ZIP artifact bundles, DOCX reports, and comparison replays provide ready-made audit evidence | Artifact bundles, comparison replay, export endpoints | $20,000–$50,000 saved per audit in evidence-gathering effort |
| **Explainability trace** | Every finding has a structured trace — auditors can verify the basis for each recommendation | `ExplainabilityTrace` (5 fields per finding) | Audit response time reduced by 40% |

**Audit savings (conservative):** **$30,000/year** (assumes 1–2 audits per year with reduced evidence-gathering effort)

---

## 4. Total cost of ArchLucid

| Cost component | Estimate | Notes |
|----------------|----------|-------|
| **Infrastructure** (Azure SQL, Container Apps, blob storage) | $500–$2,000/month | Consumption-based; varies by run volume and data retention |
| **LLM consumption** (Azure OpenAI) | $2–$10 per run | Depends on model, prompt length, and number of agents. Simulator mode is free. |
| **Team time** (setup, configuration, learning) | 40–80 hours one-time | Pilot setup, Terraform deployment, auth configuration |
| **Ongoing operation** | 2–4 hours/month | Health monitoring, policy pack updates, audit export |

**Example annual cost:** `$1,000/mo infra × 12 + $5/run × 48 runs/quarter × 4 + 60 hrs × $150 + 3 hrs/mo × 12 × $150 = $12,000 + $960 + $9,000 + $5,400 = $27,360`

---

## 5. ROI calculation

```
Annual savings           = Review savings + Compliance savings + Audit savings
Annual cost              = Infrastructure + LLM + Team time (amortized) + Operations
Net annual value         = Annual savings − Annual cost
ROI                      = Net annual value / Annual cost × 100%
Payback period           = Annual cost / (Annual savings / 12)
```

### Example calculation

| Line item | Amount |
|-----------|--------|
| Review savings | $144,000 |
| Compliance savings | $120,000 |
| Audit savings | $30,000 |
| **Total annual savings** | **$294,000** |
| Total annual cost | $27,360 |
| **Net annual value** | **$266,640** |
| **ROI** | **975%** |
| **Payback period** | **1.1 months** |

---

## 6. Intangible benefits

These are difficult to quantify but frequently cited by enterprise architecture leaders:

| Benefit | Description |
|---------|-------------|
| **Consistency** | Every architecture review follows the same process, applies the same engines, and produces the same artifact structure — regardless of which architect is involved |
| **Institutional knowledge** | Golden manifests, findings, and decision traces accumulate as an organizational knowledge base — architecture decisions do not leave when people do |
| **Speed of onboarding** | New architects review AI-generated findings against policy packs rather than learning tribal knowledge about what to check |
| **Stakeholder confidence** | DOCX exports and provenance graphs give non-technical stakeholders a tangible, visual understanding of architecture decisions |
| **Regulatory posture** | Demonstrating a governed, auditable architecture review process strengthens the organization's compliance narrative during audits and regulatory examinations |

---

## 7. Sensitivity analysis

The ROI model is most sensitive to these inputs. Adjust these first when customizing for your organization.

| Input | If higher than benchmark | If lower than benchmark |
|-------|--------------------------|-------------------------|
| **Hours per review** | ROI increases — more time to save | ROI decreases — less room for improvement |
| **Cost per remediation** | ROI increases significantly — compliance shift-left becomes dominant | ROI still positive from review time savings |
| **Reviews per quarter** | ROI scales linearly | Below 4 reviews/quarter, ROI may be marginal for small teams |
| **LLM cost per run** | ROI decreases slightly — monitor with `archlucid_llm_*` OTel metrics | ROI increases — simulator mode eliminates this cost entirely for testing |

**Break-even point:** ArchLucid pays for itself if it saves **more than ~180 architect-hours per year** (at $150/hr vs. $27K annual cost). That is approximately **4.5 hours saved per review across 40 reviews** — a conservative threshold.

---

## 8. ArchLucid subscription cost and payback (locked 2026 prices)

> All prices in this section are drawn from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) — the single source of truth. If a re-rate gate has been cleared since 2026-04-17, verify the current list before presenting this section.

### 8.1 Annual subscription cost — Professional tier, 6-architect baseline

The 6-architect baseline used throughout this document maps to Professional tier (up to 20 architects, governance, policy packs, audit export).

| Component | Calculation | Monthly | Annual (monthly billing) | Annual (prepay, 2 months free) |
|-----------|------------|---------|--------------------------|-------------------------------|
| Platform fee | 1 workspace × list (see [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) | $899 | $10,788 | $8,990 |
| Seat fee | 6 seats × list (see [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md)) | $1,074 | $12,888 | $10,740 |
| **Total subscription** | | **$1,973 / month** | **$23,676 / year** | **$19,730 / year** |

*Infrastructure (Azure SQL, Container Apps, OpenAI) is additional — see §4 for estimates.*

### 8.2 All-in first-year cost

| Cost item | Amount | Notes |
|-----------|--------|-------|
| Subscription (annual prepay) | $19,730 | From §8.1; draws from [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) |
| Infrastructure | $12,000 | $1,000/mo Azure estimate from §4 |
| LLM consumption | $960 | $5/run × 48 runs/quarter × 4 quarters |
| Setup + onboarding | $9,000 | 60 hrs × $150/hr one-time |
| Operations | $5,400 | 3 hrs/mo × 12 × $150 |
| **Total year 1** | **$47,090** | |

### 8.3 Payback period — full list price

Using the §5 example: $294,000 annual savings, $47,090 all-in year-1 cost.

```
Monthly savings          = $294,000 / 12 = $24,500
Monthly cost (year 1)    = $47,090 / 12  = $3,924
Payback period           = $47,090 / $24,500/mo ≈ 1.9 months
```

**Payback at full Professional list price: approximately 2 months.**

Year 2+ all-in cost drops to ~$38,090 (subscription + infra + LLM + ops; no setup cost), so steady-state ROI improves further.

### 8.4 Payback period — Design partner discount (50% off Professional list, 12 months)

Design partner terms: 50% off platform fee and seat fee for the first 12 months (see [PRICING_PHILOSOPHY.md §4](PRICING_PHILOSOPHY.md) and [ORDER_FORM_TEMPLATE.md Addendum B](ORDER_FORM_TEMPLATE.md)).

| Cost item | Amount |
|-----------|--------|
| Subscription at 50% off (annual prepay) | $9,865 |
| Infrastructure | $12,000 |
| LLM consumption | $960 |
| Setup + onboarding | $9,000 |
| Operations | $5,400 |
| **Total year 1 (design partner)** | **$37,225** |

```
Payback period (design partner) = $37,225 / $24,500/mo ≈ 1.5 months
```

**Payback at design partner discount: approximately 6 weeks.**

---

## 9. Three-year TCO comparison vs. incumbents

**Basis:** Professional tier, 6-architect team, 3-year horizon. Infrastructure and operations costs are included for ArchLucid. Competitor prices are **publicly observed ranges** from [COMPETITIVE_LANDSCAPE.md §2.1](COMPETITIVE_LANDSCAPE.md) and public pricing pages; actual quotes may differ significantly based on negotiation and feature scope.

### 9.1 Assumptions

| Assumption | Value | Source |
|------------|-------|--------|
| ArchLucid subscription (year 1 prepay) | $19,730 | [PRICING_PHILOSOPHY.md §5](PRICING_PHILOSOPHY.md) via §8.1 above |
| ArchLucid subscription (year 2–3, no setup) | $19,730/yr | Same |
| ArchLucid infrastructure + LLM + ops | ~$18,360/yr | §8.2 without setup |
| LeanIX per-seat range | $100–$300 / seat / month | Publicly observed enterprise range; SAP-backed; negotiated |
| Ardoq per-seat range | $80–$200 / seat / month | Publicly observed range; varies by module selection |
| LeanIX/Ardoq typical contract | Annual with 1–3 year commitments | Standard EAM deal structure |

*Publicly observed ranges are sourced from analyst reports, public case studies, and community price disclosures as of 2026-Q2. They are cited for directional comparison only — treat as rough order of magnitude.*

### 9.2 Three-year total cost of ownership

All figures are for a 6-architect team. ArchLucid includes infrastructure; competitors include subscription only (no infrastructure required for SaaS-only tools).

| | Year 1 | Year 2 | Year 3 | **3-year total** |
|--|--------|--------|--------|-----------------|
| **ArchLucid — full list, monthly billing** | $47,090 | $38,090 | $38,090 | **$123,270** |
| **ArchLucid — full list, annual prepay** | $47,090 | $38,090 | $38,090 | **$123,270** |
| **ArchLucid — design partner (year 1 only)** | $37,225 | $38,090 | $38,090 | **$113,405** |
| **LeanIX (low end: $100/seat/mo, 6 seats)** | $7,200 | $7,200 | $7,200 | **$21,600** |
| **LeanIX (high end: $300/seat/mo, 6 seats)** | $21,600 | $21,600 | $21,600 | **$64,800** |
| **Ardoq (low end: $80/seat/mo, 6 seats)** | $5,760 | $5,760 | $5,760 | **$17,280** |
| **Ardoq (high end: $200/seat/mo, 6 seats)** | $14,400 | $14,400 | $14,400 | **$43,200** |

### 9.3 TCO interpretation

**Why ArchLucid costs more than EAM incumbents:** ArchLucid costs are higher because the product delivers capabilities incumbents do not offer at any price: AI-agent orchestration, structured explainability traces, pre-commit governance gates, typed audit events, and comparison replay. The TCO comparison is not apples-to-apples — it is a decision between tools with fundamentally different capabilities.

**The correct frame is net value, not cost:**

| | ArchLucid (full list, 3-year) | LeanIX (high end, 3-year) |
|--|-------------------------------|--------------------------|
| 3-year cost | ~$123,270 | ~$64,800 |
| 3-year savings (from §5 model) | ~$882,000 ($294K × 3) | Savings not quantified (no AI analysis, no shift-left compliance) |
| **3-year net value** | **~$758,730** | **Not comparable** |

**Buy vs augment decision:** ArchLucid is **not** a replacement for LeanIX or Ardoq for CMDB, application portfolio inventory, or roadmap management. See [COMPETITIVE_LANDSCAPE.md §4.1](COMPETITIVE_LANDSCAPE.md). The correct question is: "What is the cost of continuing manual architecture review vs shifting to AI-governed review?" — not "Is ArchLucid cheaper than LeanIX?"

### 9.4 Sensitivity: what if savings are 50% of benchmark?

Even if the savings model is optimistic by 50% (conservative scenario):

```
Conservative annual savings = $294,000 × 0.50 = $147,000
3-year conservative savings = $441,000
3-year ArchLucid cost       = $123,270
3-year net value            = $317,730
Payback period              = $47,090 / ($147,000/12) ≈ 3.8 months
```

ArchLucid pays for itself in under 4 months even in the conservative scenario.

---

## 10. How to present to leadership

1. **Fill in your numbers** in the "Your value" column of Section 2.
2. **Choose which value levers apply** to your organization (Section 3) — do not claim all of them if you will not use all capabilities.
3. **Run the calculation** (Section 5) with your numbers.
4. **Show the payback** from Section 8 — use the Professional tier numbers as the baseline; adjust if your team size or tier differs.
5. **Show the 3-year TCO** from Section 9 if the conversation is about cost vs. incumbents — always pair TCO with net-value, not cost alone.
6. **Add 1–2 intangible benefits** (Section 7) that resonate with your leadership's priorities.
7. **Present the one-page summary:** current cost, projected savings, net value, ROI percentage, payback period.
8. **Attach the pilot scorecard** ([PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md)) as the measurement plan.

---

## Related documents

| Doc | Use |
|-----|-----|
| [PRICING_PHILOSOPHY.md](PRICING_PHILOSOPHY.md) | **Single source of truth** for all list prices used in §8 and §9 |
| [ORDER_FORM_TEMPLATE.md](ORDER_FORM_TEMPLATE.md) | Order form with Design partner addendum and worked examples |
| [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) | Competitor capability and pricing context used in §9 |
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Measurement framework for the pilot that validates this ROI model |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Who presents this model and to whom |
| [POSITIONING.md](POSITIONING.md) | Value pillars that map to the levers above |
| [../V1_SCOPE.md](../V1_SCOPE.md) | What V1 actually ships (grounding for capability claims) |
| [../PILOT_GUIDE.md](../PILOT_GUIDE.md) | Technical pilot onboarding |

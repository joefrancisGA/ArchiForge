# ArchLucid ROI model

**Audience:** Pilot champions, enterprise architects, and engineering leaders who need to justify an ArchLucid purchase to their CFO or procurement team.

**Last reviewed:** 2026-04-15

**Grounding rule:** Value claims are mapped to shipped V1 capabilities per [V1_SCOPE.md](../V1_SCOPE.md). Estimates are conservative. Adjust all numbers to your organization's actuals.

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
| **Cost per remediation** | Average cost to fix a compliance or architecture gap found in production (eng time + incident + audit response) | $15,000–$75,000 | _______ |
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

## 8. How to present to leadership

1. **Fill in your numbers** in the "Your value" column of Section 2.
2. **Choose which value levers apply** to your organization (Section 3) — do not claim all of them if you will not use all capabilities.
3. **Run the calculation** (Section 5) with your numbers.
4. **Add 1–2 intangible benefits** (Section 6) that resonate with your leadership's priorities.
5. **Present the one-page summary:** current cost, projected savings, net value, ROI percentage, payback period.
6. **Attach the pilot scorecard** ([PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md)) as the measurement plan.

---

## Related documents

| Doc | Use |
|-----|-----|
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Measurement framework for the pilot that validates this ROI model |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Who presents this model and to whom |
| [POSITIONING.md](POSITIONING.md) | Value pillars that map to the levers above |
| [../V1_SCOPE.md](../V1_SCOPE.md) | What V1 actually ships (grounding for capability claims) |
| [../PILOT_GUIDE.md](../PILOT_GUIDE.md) | Technical pilot onboarding |

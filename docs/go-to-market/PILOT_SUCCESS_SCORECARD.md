> **Scope:** ArchLucid pilot success scorecard - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid pilot success scorecard

**Audience:** Pilot champions, architecture team leads, and sales engineers who need to measure whether a pilot succeeded — and present the results to leadership for a purchase decision.

**Last reviewed:** 2026-04-15

**Grounding rule:** Metrics reference shipped V1 capabilities per [V1_SCOPE.md](../library/V1_SCOPE.md) and existing data collection per [PRODUCT_LEARNING.md](../library/PRODUCT_LEARNING.md).

---

## 1. Purpose

This scorecard defines **what to measure, when to measure it, and what "good" looks like** during an ArchLucid pilot. Use it alongside the [ROI_MODEL.md](ROI_MODEL.md) to translate pilot results into a business case.

---

## 2. Quantitative metrics

Measure these before the pilot (baseline) and at pilot end (actual). The delta is the evidence.

### 2.1 Efficiency metrics

| Metric | How to measure | Baseline (before pilot) | Pilot actual | Source |
|--------|---------------|------------------------|--------------|--------|
| **Time to complete an architecture review** | Wall-clock hours from "review requested" to "approved manifest" | _______ hours | _______ hours | Team tracking (Jira, calendar) |
| **Architect hours per review** | Person-hours of architect effort per review (prep + sessions + documentation) | _______ hours | _______ hours | Time tracking |
| **Documentation time per review** | Hours spent writing the architecture review report | _______ hours | _______ hours | Time tracking |
| **Reviews completed per quarter** | Number of formal reviews completed in the pilot period (annualized) | _______ / qtr | _______ / qtr | Team tracking |

### 2.2 Quality metrics

| Metric | How to measure | Baseline | Pilot actual | Source |
|--------|---------------|----------|--------------|--------|
| **Findings per review** | Average number of findings produced per architecture run | N/A (manual: ___) | _______ | `GET /v1.0/runs/{id}/findings` or UI run detail |
| **Findings with complete explainability trace** | Percentage of findings where all 5 `ExplainabilityTrace` fields are populated | N/A | _______% | `archlucid.explanation_trace_completeness_ratio` OTel metric |
| **Unique finding engine types triggered** | Number of distinct finding engine types that produced findings across the pilot | N/A | _______ / 10 | Finding engine type distribution in run results |
| **Agent output quality score** | Average structural completeness + semantic quality score across runs | N/A | _______ | `archlucid.authority.agent_output_quality` OTel metric |

### 2.3 Governance metrics

| Metric | How to measure | Baseline | Pilot actual | Source |
|--------|---------------|----------|--------------|--------|
| **Governance compliance rate** | Percentage of manifests committed that passed the pre-commit governance gate without critical/high findings | N/A | _______% | Governance gate pass/fail count |
| **Time from manifest to approval** | Average hours from manifest commit to governance approval | _______ hours | _______ hours | `GovernanceApprovalRequests` timestamps |
| **Compliance gaps found pre-deploy (during pilot)** | Architecture or compliance findings surfaced by ArchLucid that would have been missed in the manual process | 0 (baseline) | _______ | Finding review with architecture team |

### 2.4 Operational metrics

| Metric | How to measure | Target | Pilot actual | Source |
|--------|---------------|--------|--------------|--------|
| **Run success rate** | Percentage of runs that reach "Committed" status without errors | ≥ 90% | _______% | Run status counts in API or UI |
| **Average run duration** | Wall-clock time from run creation to manifest availability | < 5 min | _______ min | `archlucid.authority.run_duration_ms` OTel histogram |
| **LLM cost per run** | Average Azure OpenAI consumption per run | < $10 | $_______ | `archlucid.llm_*` OTel metrics, Azure billing |

---

## 3. Qualitative metrics

Collect via stakeholder interviews at pilot midpoint and end. Score each on a 1–5 scale.

| Question | Who to ask | Midpoint (1–5) | End (1–5) |
|----------|-----------|----------------|-----------|
| "How confident are you that ArchLucid findings are useful and accurate?" | Architects who reviewed findings | _______ | _______ |
| "Would you trust ArchLucid as the first pass for architecture reviews going forward?" | Lead architect / architecture board | _______ | _______ |
| "How much time did ArchLucid save you compared to a fully manual review?" | Architects who ran reviews during the pilot | _______ | _______ |
| "How easy was it to configure policy packs and governance rules for your organization?" | Architect who set up governance | _______ | _______ |
| "Would you recommend ArchLucid to a peer at another organization?" | All pilot participants | _______ | _______ |
| "How clear and useful is the explainability trace on findings?" | Architects who reviewed findings in detail | _______ | _______ |
| "How useful is the provenance graph for understanding decision lineage?" | Architects and stakeholders who viewed it | _______ | _______ |
| "How satisfied are you with the DOCX export quality for stakeholder communication?" | Architects who shared reports with non-technical stakeholders | _______ | _______ |

### Scoring guide

| Score | Meaning |
|-------|---------|
| 1 | Strongly disagree / not useful at all |
| 2 | Disagree / marginally useful |
| 3 | Neutral / moderately useful |
| 4 | Agree / useful |
| 5 | Strongly agree / very useful |

**Qualitative success threshold:** Average score ≥ 3.5 across all questions at pilot end.

---

## 4. Data collection plan — six-week pilot timeline

| Week | Activity | Data collected |
|------|----------|---------------|
| **0 (pre-pilot)** | Collect baseline metrics: review hours, documentation time, compliance gap history. Deploy ArchLucid to pilot environment. Configure auth, policy packs, and governance rules. | Baseline numbers for Section 2 |
| **1** | Run 2–3 architecture reviews using ArchLucid. Architects review AI-generated findings alongside their normal process. | First run metrics, initial qualitative impressions |
| **2** | Run 2–3 more reviews. Enable pre-commit governance gate (warning-only mode). Compare ArchLucid findings to manual findings for accuracy assessment. | Finding accuracy comparison, governance gate metrics |
| **3 (midpoint)** | Conduct midpoint stakeholder interviews (Section 3). Review and adjust policy packs based on feedback. Switch governance gate to enforcing mode if findings quality is satisfactory. | Midpoint qualitative scores, policy pack refinements |
| **4** | Run 3–4 reviews. Conduct at least one two-run comparison. Export DOCX reports and share with non-technical stakeholders for feedback. | Comparison metrics, export feedback, operational metrics |
| **5** | Run 3–4 reviews. Focus on governance workflow: submit approval requests, test segregation of duties, review approval SLA metrics. | Governance metrics, approval workflow feedback |
| **6 (end)** | Conduct end-of-pilot stakeholder interviews. Compile all metrics. Calculate ROI using [ROI_MODEL.md](ROI_MODEL.md). Prepare leadership presentation. | Final qualitative scores, complete scorecard |

**Total runs during pilot:** 12–18 (enough to establish patterns without overwhelming the team).

---

## 5. Success criteria

### 5.1 Minimum (pilot is viable for continued use)

| Criterion | Threshold |
|-----------|-----------|
| Review cycle time reduction | ≥ 25% reduction vs. baseline |
| Run success rate | ≥ 85% |
| Finding usefulness (qualitative) | Average ≥ 3.0 |
| At least one compliance gap caught pre-deploy | ≥ 1 finding that would have been missed manually |

### 5.2 Target (pilot supports a purchase recommendation)

| Criterion | Threshold |
|-----------|-----------|
| Review cycle time reduction | ≥ 40% reduction vs. baseline |
| Run success rate | ≥ 90% |
| Finding usefulness (qualitative) | Average ≥ 3.5 |
| Compliance gaps caught pre-deploy | ≥ 3 findings |
| Governance compliance rate | ≥ 80% of manifests pass gate |
| Overall qualitative score | Average ≥ 3.5 across all questions |
| ROI projection (annualized) | ≥ 300% using actual pilot numbers in ROI model |

### 5.3 Stretch (pilot demonstrates transformative value)

| Criterion | Threshold |
|-----------|-----------|
| Review cycle time reduction | ≥ 60% reduction vs. baseline |
| Run success rate | ≥ 95% |
| Finding usefulness (qualitative) | Average ≥ 4.0 |
| Compliance gaps caught pre-deploy | ≥ 5 findings |
| Architect willingness to recommend | Average ≥ 4.5 |
| ROI projection (annualized) | ≥ 500% using actual pilot numbers |

---

## 6. Report template for leadership

Use this structure when presenting pilot results to leadership for a purchase decision.

### Executive summary (1 paragraph)

> We conducted a [X]-week pilot of ArchLucid with [N] architecture reviews. Architecture review cycle time decreased by [Y]% from [baseline] hours to [actual] hours. ArchLucid identified [N] compliance or architecture gaps that our manual process would have missed. The projected annual ROI based on pilot results is [Z]%. We recommend [proceeding to purchase / extending the pilot / not proceeding].

### Results summary table

| Metric | Baseline | Pilot actual | Change | Target met? |
|--------|----------|-------------|--------|-------------|
| Review cycle time | ___ hrs | ___ hrs | -__% | Yes/No |
| Architect hours per review | ___ hrs | ___ hrs | -__% | Yes/No |
| Findings per review | N/A | ___ | — | — |
| Explainability trace completeness | N/A | ___% | — | — |
| Governance compliance rate | N/A | ___% | — | Yes/No |
| Compliance gaps caught pre-deploy | 0 | ___ | +___ | Yes/No |
| Run success rate | N/A | ___% | — | Yes/No |
| Overall qualitative score | N/A | ___/5 | — | Yes/No |

### ROI projection

> Using actual pilot numbers in the [ROI Model](ROI_MODEL.md):
>
> - **Annual review savings:** $___
> - **Annual compliance savings:** $___
> - **Annual audit savings:** $___
> - **Total annual savings:** $___
> - **Total annual cost:** $___
> - **Net annual value:** $___
> - **ROI:** ___%
> - **Payback period:** ___ months

### Qualitative highlights

- [2–3 specific quotes or observations from stakeholder interviews]
- [1–2 examples of findings that added value the manual process would have missed]

### Recommendation

> Based on [minimum / target / stretch] success criteria met, we recommend [action].

### Risks and mitigations

| Risk | Mitigation |
|------|-----------|
| [Risk identified during pilot] | [How to address it] |

---

## 7. Where pilot data lives in ArchLucid

| Data | Location | How to access |
|------|----------|---------------|
| Run results and findings | SQL (`dbo.Runs`, `dbo.Findings`) | API: `GET /v1.0/runs`, UI: `/runs` |
| Audit events | SQL (`dbo.AuditEvents`) | API: `GET /v1.0/audit`, UI: `/audit`, export: CSV/JSON |
| Governance approvals | SQL (`dbo.GovernanceApprovalRequests`) | API, UI: `/governance/dashboard` |
| OTel metrics | Grafana dashboards (committed in repo) | Grafana: authority, SLO, LLM usage dashboards |
| Product learning signals | SQL (`dbo.ProductLearningPilotSignals`) | UI: `/product-learning`, export: CSV |
| Comparison records | SQL (`dbo.ComparisonRecords`) | API: `GET /v1.0/comparisons`, UI: `/compare` |

---

## Related documents

| Doc | Use |
|-----|-----|
| [ROI_MODEL.md](ROI_MODEL.md) | Fill in with actual pilot numbers to calculate ROI |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Which persona presents the report (Section 6) and to whom |
| [POSITIONING.md](POSITIONING.md) | Value pillars to reference in the executive summary |
| [../PILOT_GUIDE.md](../library/PILOT_GUIDE.md) | Technical setup for the pilot environment |
| [../PRODUCT_LEARNING.md](../library/PRODUCT_LEARNING.md) | How pilot feedback signals are captured and analyzed |
| [../OBSERVABILITY.md](../library/OBSERVABILITY.md) | OTel metric names referenced in this scorecard |
| [../MARKETABILITY_ASSESSMENT_2026_04_15.md](../library/MARKETABILITY_ASSESSMENT_2026_04_15.md) | Full marketability assessment |

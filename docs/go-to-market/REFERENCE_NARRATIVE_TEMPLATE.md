> **Scope:** ArchLucid — Customer reference narrative template - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Customer reference narrative template

**Audience:** Marketing, sales, and customer success teams creating case studies.

**Last reviewed:** 2026-04-15

**Note:** The three narratives below are **fictional but realistic**, grounded in ICP criteria ([IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md)) and buyer personas ([BUYER_PERSONAS.md](BUYER_PERSONAS.md)). Replace with real customer data as pilots complete.

**Alignment:** Outcomes and quotes should stay plausible against **[EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md)** and **[PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md)**—especially §8 of the brief (what not to over-claim in V1).

---

## Template structure

Every reference narrative follows this pattern:

1. **Customer profile** — Industry, size, architecture team, cloud posture (map to ICP)
2. **Challenge** — What pain drove the evaluation (map to persona pain points)
3. **Solution** — How ArchLucid was deployed and used (SaaS framing: "provisioned a tenant," not "installed")
4. **Results** — Quantitative outcomes mapped to [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md)
5. **Quote** — Champion pull-quote (placeholder)
6. **What's next** — Expansion plans

---

## Narrative A — Financial services: compliance-driven governance

### Customer profile

| Attribute | Value |
|-----------|-------|
| Industry | Financial services (regional bank) |
| Employees | 2,500 |
| Architecture team | 6 enterprise architects, central practice |
| Cloud posture | Azure-primary (Azure landing zone) |
| ICP score | 40 / 45 (strong fit) |

### Challenge

The architecture team conducted ~15 formal design reviews per quarter, each consuming 30–40 architect-hours across preparation, review meetings, and documentation. Internal audit regularly asked "who approved this architecture, and based on what analysis?" — and the team had no structured answer beyond meeting notes. Compliance reviews for regulatory submissions took weeks because architecture decisions were scattered across emails and slide decks.

### Solution

The team provisioned an ArchLucid tenant (SaaS), configured Entra SSO, and ran their next three design reviews through the platform. Governance approval workflows replaced the informal email chain. The audit trail provided the structured answer that internal audit had requested for two years.

### Results

| Metric | Before | After | Source |
|--------|--------|-------|--------|
| Hours per review | 35 | 12 | Time tracking |
| Reviews per quarter | 15 | 22 | Run count |
| Audit trail coverage | 0% | 100% | Governance approval records |
| Compliance review prep time | 3 weeks | 3 days | Team estimate |

### Quote

> *"For the first time, I can show an auditor exactly what was analyzed, what rules were applied, and who approved it — without digging through emails."*
> — [Champion name], Chief Architect

### What's next

Expanding to two additional business units (8 more architects). Evaluating policy packs for PCI-DSS compliance findings. Considering DOCX export integration into their regulatory submission workflow.

---

## Narrative B — Technology company: modernization velocity

### Customer profile

| Attribute | Value |
|-----------|-------|
| Industry | B2B SaaS (developer tools) |
| Employees | 800 |
| Architecture team | 4 platform engineers + 2 principal engineers |
| Cloud posture | Azure + AWS (Azure-primary for platform) |
| ICP score | 34 / 45 (moderate-strong fit) |

### Challenge

Rapid growth drove 3–4 major architecture changes per month. Different architects made different recommendations for similar problems. There was no way to compare a proposed design against the decisions made six months ago. "Architecture drift" was discovered in production, not in review.

### Solution

The platform team ran ArchLucid against new designs and used comparison runs to detect drift from previous decisions. The multi-agent pipeline applied the same finding engines consistently — eliminating "it depends on who reviews it."

### Results

| Metric | Before | After | Source |
|--------|--------|-------|--------|
| Time to first review feedback | 5 days | 4 hours | Calendar tracking |
| Consistency (same input, same findings) | ~40% | 95%+ | Team assessment |
| Drift detected before production | 0 / quarter | 6 / quarter | Comparison runs |
| Architecture decision documentation | Sparse | 100% (manifests) | Golden manifests |

### Quote

> *"We used to spend a week preparing for an architecture review. Now the AI does the analysis in minutes and we spend our time on the hard decisions — where humans add value."*
> — [Champion name], VP Platform Engineering

### What's next

Integrating ArchLucid into the PR workflow via GitHub Actions ([../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md)). Adding a second workspace for the infrastructure team.

---

## Narrative C — Healthcare SaaS: security review mandate

### Customer profile

| Attribute | Value |
|-----------|-------|
| Industry | Healthcare SaaS (EHR integration platform) |
| Employees | 350 |
| Architecture team | CTO + 3 senior engineers (no formal EA role) |
| Cloud posture | Azure (single region, HIPAA-eligible services) |
| ICP score | 30 / 45 (moderate fit) |

### Challenge

Every new feature touching patient data required a security architecture review by the CTO. Reviews were bottlenecked — a 2-week backlog was normal. The security review board demanded structured evidence of design analysis, not "the CTO said it's fine." The team spent more time writing review documentation than doing actual architecture work.

### Solution

The CTO configured ArchLucid to analyze proposed designs with all 10 finding engines, with particular attention to compliance and security findings. Explainability traces provided the structured evidence the security board required. The governance workflow formalized the approval process.

### Results

| Metric | Before | After | Source |
|--------|--------|-------|--------|
| Review backlog | 2 weeks | 2 days | Jira tracking |
| CTO hours on reviews / week | 15 | 4 | Time tracking |
| Security board documentation | Manual (8 hours/review) | Auto-generated (0 hours) | Team estimate |
| Finding coverage | 3 areas (manual checklist) | 10 engine types | Run metadata |

### Quote

> *"I got 11 hours a week back. The explainability traces give the security board exactly what they wanted — structured evidence, not my opinion."*
> — [Champion name], CTO

### What's next

Exploring governance policy packs aligned with HIPAA technical safeguards. Expanding to the product engineering team (6 additional users).

---

## Usage guidance

- **Replace fictional details** with real customer data after permission is obtained.
- **Label as "representative scenario"** until a real customer has approved their name and data.
- **Format for multiple channels:** website case study page, PDF one-pager, sales deck slide, conference talk anecdote.
- **Quantitative results are critical** — vague "improved productivity" claims do not differentiate. Use pilot scorecard data.

---

## Related documents

| Doc | Use |
|-----|-----|
| [IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md) | ICP criteria |
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Metrics to populate results |
| [ROI_MODEL.md](ROI_MODEL.md) | Value calculation for results sections |
| [POSITIONING.md](POSITIONING.md) | Messaging alignment |
| [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) | Differentiation framing |

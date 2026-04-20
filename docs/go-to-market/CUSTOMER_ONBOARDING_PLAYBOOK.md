> **Scope:** ArchLucid — Customer onboarding playbook - full detail, tables, and links in the sections below.

# ArchLucid — Customer onboarding playbook

**Audience:** Customer success, sales engineering, and account management teams onboarding new SaaS customers.

**Last reviewed:** 2026-04-17

This playbook aligns with the 6-week pilot timeline in [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) and the technical quickstart in [../OPERATOR_QUICKSTART.md](../OPERATOR_QUICKSTART.md).

**Pricing:** Current tier pricing, pilot fee, and design-partner terms are in [PRICING_PHILOSOPHY.md §4–§5](PRICING_PHILOSOPHY.md). Do not restate prices in this playbook.

---

## 1. Onboarding phases

### Week 0 — Pre-launch

| Item | Owner | Definition of done |
|------|-------|--------------------|
| Tenant provisioned in ArchLucid SaaS | ArchLucid | Tenant ID confirmed, workspace created |
| SSO configured (Entra / OIDC) | Joint | Admin can sign in with corporate identity |
| Admin account active with Admin role | ArchLucid | Admin sees first-run wizard on login |
| Welcome email sent with getting-started links | ArchLucid | Email delivered, links verified |
| Kickoff call scheduled | ArchLucid CSM | Calendar invite sent to champion + team |
| Success criteria agreed (from scorecard) | Joint | Minimum / target / stretch documented |

**Common blockers:** Entra app registration delays (coordinate with customer IT), firewall rules blocking API access (provide IP ranges or use private connectivity).

### Week 1 — Foundation

| Item | Owner | Definition of done |
|------|-------|--------------------|
| Kickoff call completed | Joint | Team introduced, goals reviewed, questions answered |
| Admin completes first-run wizard | Customer | Sample preset selected, first run visible |
| Team members invited (2–5 operators) | Customer admin | Users can sign in and view runs |
| First sample run executed and reviewed | Joint | Champion can navigate findings, manifest, explainability traces |
| Scorecard metrics baseline captured | Joint | Pre-pilot values recorded per [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) §2 |

**Common blockers:** Team availability, confusion about presets (provide preset selection guidance).

### Weeks 2–3 — Adoption

| Item | Owner | Definition of done |
|------|-------|--------------------|
| First **real** architecture review submitted | Customer | Run with actual system description, not sample |
| Governance workflow configured | Customer (guided) | At least one approval workflow active |
| Team completes 3+ runs | Customer | Visible in run list |
| Findings reviewed and discussed in team meeting | Customer | Architecture decisions informed by findings |
| Mid-pilot check-in call | ArchLucid CSM | Adoption signals reviewed, blockers addressed |

**Common blockers:** "We don't have a review coming up" (suggest running against a recent completed design), governance setup confusion (provide walkthrough).

### Weeks 4–5 — Expansion

| Item | Owner | Definition of done |
|------|-------|--------------------|
| Comparison run executed (two runs compared) | Customer | Drift or evolution visible in comparison view |
| Policy packs explored | Customer | At least one policy pack reviewed or configured |
| Governance approvals used in production | Customer | Real approval request submitted and resolved |
| Export features tested (DOCX, audit CSV) | Customer | Champion has a sample artifact for leadership |

### Week 6 — Review

| Item | Owner | Definition of done |
|------|-------|--------------------|
| Pilot scorecard completed | Joint | All metrics captured per [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) |
| Results presented to leadership | Customer champion | ROI model populated with actual data per [ROI_MODEL.md](ROI_MODEL.md) |
| Renewal / expansion discussion | ArchLucid + champion | Go/no-go decision; commercial terms if proceeding |

---

## 2. Touchpoint schedule

| Timing | Type | Purpose |
|--------|------|---------|
| Week 0 | Kickoff call (60 min) | Introductions, goals, success criteria, technical setup review |
| Week 1 | Check-in (30 min) | First run debrief, team readiness, early blockers |
| Week 3 | Mid-pilot review (45 min) | Adoption metrics, governance setup, course correction |
| Week 6 | Scorecard review (60 min) | Results, ROI calculation, renewal/expansion conversation |
| Ad hoc | Support / Slack / email | As needed for technical issues |

---

## 3. Health signals during onboarding

| Signal | Green | Yellow | Red |
|--------|-------|--------|-----|
| **Run frequency** | Increasing week-over-week | Flat after week 2 | No runs after week 2 |
| **Active operators** | 3+ unique users | 1 user only | Zero logins after week 1 |
| **Governance** | Approval workflow active | Configured but unused | Not configured by week 4 |
| **Support** | No critical tickets | Questions but engaged | Unresolved tickets, disengaged |
| **Champion engagement** | Attends all touchpoints | Misses one touchpoint | No-shows repeatedly |

**Action on Yellow:** Proactive outreach — offer training session, feature walkthrough, or adjusted timeline.

**Action on Red:** Escalate internally; engage executive sponsor on customer side if accessible; assess whether pilot extension or scope change is needed.

---

## 4. Handoff to steady-state

After successful pilot conversion:

- Transition from pilot CSM touchpoints to **steady-state** cadence (quarterly business review).
- Activate health scoring per [CUSTOMER_HEALTH_SCORING.md](CUSTOMER_HEALTH_SCORING.md).
- Enter renewal timeline per [RENEWAL_EXPANSION_PLAYBOOK.md](RENEWAL_EXPANSION_PLAYBOOK.md).

---

## Related documents

| Doc | Use |
|-----|-----|
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | What to measure during pilot |
| [ROI_MODEL.md](ROI_MODEL.md) | Value calculation for leadership presentation |
| [CUSTOMER_HEALTH_SCORING.md](CUSTOMER_HEALTH_SCORING.md) | Post-onboarding health framework |
| [RENEWAL_EXPANSION_PLAYBOOK.md](RENEWAL_EXPANSION_PLAYBOOK.md) | Renewal process |
| [TRUST_CENTER.md](TRUST_CENTER.md) | Security and trust artifacts for onboarding |

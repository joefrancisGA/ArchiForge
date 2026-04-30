> **Scope:** Procurement FAQ for enterprise buyers — honest answers anchored to shipped V1 materials; not legal advice.

# Procurement FAQ (Enterprise)

**Audience:** procurement, InfoSec questionnaires, resilience reviews preparing **SOC 2** / SIG / CAIQ spreadsheets.

**Evidence index:** **[trust-center.md](../trust-center.md)**

---

## Q & A

### 1. Do you have SOC 2 Type II?

**Answer:** Today we publish a **SOC 2 self-assessment** and control mapping—**no** SOC 2 **Type II** CPA **attestation** yet ([SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md)). Type **I** followed by Type **II** is the typical SaaS roadmap once operating evidence exists alongside budget.

---

### 2. Can we see the latest penetration-test report?

**Answer:** Formal third-party engagements follow an SoW (**[2026-Q2-SOW.md](../security/pen-test-summaries/2026-Q2-SOW.md)**). Redacted customer-facing summaries are distributed **under NDA** consistent with **`docs/PENDING_QUESTIONS.md`** posture on pen-test disclosures.

---

### 3. Where is customer **data processed / stored**?

**Answer:** **Vendor-hosted** Azure workloads (region choices depend on contracted Azure regions and private-connectivity setup). Architectural networking guidance: **[CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md)** and infra modules under **`infra/`**.

---

### 4. Can we authenticate with **Okta / Ping / Auth0** instead of Microsoft Entra ID?

**Answer:** Shipping **JWT + Entra-compatible** integrations are documented in **[SECURITY.md](../library/SECURITY.md)**; non-Microsoft IdPs commonly federate inbound to **Entra** or negotiate a **JWT** integration roadmap—capture your IdP OAuth/OIDC specifics in questionnaire follow-ups (**no guaranteed same-day turnkey** for arbitrary IdPs in V1).

---

### 5. What **SLA** do you publish?

**Answer:** Targets are documented (**[SLA_TARGETS.md](../library/SLA_TARGETS.md)**, **[SLA_SUMMARY.md](SLA_SUMMARY.md)**). Contractual SLA language is finalized per **Order Form** (**[ORDER_FORM_TEMPLATE.md](ORDER_FORM_TEMPLATE.md)**)—pre-contract **targets**, not unconditional guarantees until executed.

---

### 6. Can we execute the **Data Processing Agreement**?

**Answer:** Template: **[DPA_TEMPLATE.md](DPA_TEMPLATE.md)** • Subprocessors: **[SUBPROCESSORS.md](SUBPROCESSORS.md)**

---

### 7. What **subprocessors** apply?

**Answer:** Maintain **[SUBPROCESSORS.md](SUBPROCESSORS.md)** quarterly; aligns with contractual notification windows in the **[DPA_TEMPLATE.md](DPA_TEMPLATE.md)**.

---

### 8. What happens if ArchLucid **ceases trading**?

**Answer:** Operational continuity hinges on contractual **termination assistance**, **export rights**, negotiated ** escrow** arrangements, and staged **migration** timelines—**explicit source-code escrow** is negotiable rather than universally bundled in starter paper. Start from **[MSA_TEMPLATE.md](MSA_TEMPLATE.md)** / Order Form playbook.

---

### 9. Do you maintain **cyber insurance**?

**Answer:** Procurement should request current **coverage limits**, **carrier**, renewal date, and **claims history** directly from Vendor during diligence—figures change year to year (**do not cite an unsigned MD as proof**).

---

### 10. Can we speak with **reference customers**?

**Answer:** **Public Published** references are tracked (**[reference-customers/README.md](reference-customers/README.md)**) with **Status** placeholders until **V1.1-program** approvals—coordinate via sales for **permissioned pilots**.

---

## Trust progression timeline (informal)

| Window | Checkpoint |
|--------|-----------|
| Q2–Q3 FY26 | Aeronova **pen-test engagement** (**[Trust Center posture](../trust-center.md)**) |
| Rolling | **Self-assessment** updates (**[SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md)**) |
| Future (budgeted) | **SOC 2 Type I readiness** milestone |
| Subsequent | **SOC 2 Type II** (~6–12 months operating effectiveness evidence) |

**Note:** Dates are illustrative—bind via executed Order Form milestones when procuring regulated workloads.

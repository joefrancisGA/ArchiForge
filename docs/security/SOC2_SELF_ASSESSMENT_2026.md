> **Scope:** SOC 2 Trust Services Criteria — **self-assessment only** (not CPA attestation). **2026-Q2 update:** pen-test SoW awarded; CAIQ/SIG pre-filled; Type I scoping funded as a **readiness** milestone (not yet an opinion).

# SOC 2 — Owner self-assessment (2026)

> **IMPORTANT:** This document is an **internal / buyer-transparency self-assessment**. It is **not** a SOC 2 Type I or Type II **audit opinion** and must not be represented as third-party attestation.

## Scope

**In scope:** Security (CC) and Availability (A) criteria most relevant to the hosted API + SQL data plane. Confidentiality, Processing Integrity, and Privacy are **partially** addressed where they overlap engineering controls (see gap register).

## Control summary (high level)

| TSC theme | ArchLucid evidence (examples) | Maturity |
|-----------|-------------------------------|----------|
| Security — logical access | Entra / JWT roles, API keys, RBAC policies; `AuthSafetyGuard` | Partial |
| Security — data protection | SQL RLS + `SESSION_CONTEXT`; private endpoint posture (Terraform) | Partial |
| Security — secure SDLC | OWASP ZAP, Schemathesis, CodeQL **security-extended**, unit/integration tiers | Strong |
| Availability | `/health/*`, SLO docs, synthetic probes, runbooks | Partial |

## Gap register

| ID | Gap | Owner | Target | Status |
|----|-----|-------|--------|--------|
| G-001 | No CPA SOC 2 report | CFO / Security | Fund external readiness consultant + CPA firm; Type I observation window | **Open** — requires external readiness consultant shortlist and budget line (see Pending Questions) |
| G-002 | Pen-test summary not yet published | Security | Complete 2026-Q2 engagement | **Closed (process)** — SoW awarded to **Aeronova Red Team LLC**; kickoff **2026-05-06**; redacted summary path [`pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md`](pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md); publication audit via `SecurityAssessmentPublished` |
| G-003 | CAIQ / SIG not pre-filled | Security | Publish alongside trust center | **Closed (artifacts)** — [`CAIQ_LITE_2026.md`](CAIQ_LITE_2026.md), [`SIG_CORE_2026.md`](SIG_CORE_2026.md) |

## SOC 2 Type I — funded scoping (Q2–Q3 2026)

**Intent:** Move from self-assessment to **procurement-ready** third-party validation within **one quarter** for *readiness* (control narrative, evidence index, observation-period plan), **not** a guaranteed Type I opinion date.

| Milestone | Target | Notes |
|-----------|--------|------|
| Readiness consultant engaged | 2026-06-15 | Shortlist 3 CPA-aligned boutiques; scope: gap workshop + evidence room |
| Control baseline freeze for observation | 2026-07-31 | Align with pen-test remediation closure for material findings |
| Type I observation period start | 2026-09-01 | Illustrative — confirm with selected CPA |
| Type I report (stretch) | 2026-Q4 | Requires executed attestation agreement |

## Pending questions (G-001)

1. **Budget ceiling** for readiness consultant + CPA Type I (cap ex vs op ex coding).
2. **Observation window** length the business will accept (45 vs 90 days) before marketing “SOC 2 in process”.
3. **Regional scope** of customer data centers that must appear in system description (single-region vs multi-region Azure).

## Related

- [`COMPLIANCE_MATRIX.md`](COMPLIANCE_MATRIX.md)
- [`../go-to-market/SOC2_ROADMAP.md`](../go-to-market/SOC2_ROADMAP.md)
- [`../go-to-market/TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)
- [`pen-test-summaries/2026-Q2-SOW.md`](pen-test-summaries/2026-Q2-SOW.md)

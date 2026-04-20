> **Scope:** ArchLucid — SOC 2 readiness roadmap - full detail, tables, and links in the sections below.

# ArchLucid — SOC 2 readiness roadmap

**Audience:** Customers, prospects, and internal GRC stakeholders.

**Last reviewed:** 2026-04-15

This document describes **controls and evidence** already reflected in the product and repo, **typical gaps** for a SOC 2 Type I / II program, and a **milestone roadmap**. It is **not** an auditor’s report.

---

## 1. Current strengths (engineering and operations)

The following are **observable** in the codebase and documentation (non-exhaustive):

| Area | Evidence (examples) |
|------|---------------------|
| **Access control** | JWT / Entra roles, API keys, policy-based authorization; [../SECURITY.md](../SECURITY.md) |
| **Network & edge** | Front Door / WAF, optional APIM, private endpoints; [../CUSTOMER_TRUST_AND_ACCESS.md](../CUSTOMER_TRUST_AND_ACCESS.md) |
| **Data protection** | SQL RLS with `SESSION_CONTEXT`, parameterized data access; [../security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md), [TENANT_ISOLATION.md](TENANT_ISOLATION.md) |
| **Logging & audit** | Append-only `dbo.AuditEvents`, typed event catalog (CI-tracked count in [../AUDIT_COVERAGE_MATRIX.md](../AUDIT_COVERAGE_MATRIX.md)); correlation IDs |
| **Reliability measurement** | HTTP SLOs (e.g. **99.5%** / 30 days), Prometheus rules, synthetic probes; [../API_SLOS.md](../API_SLOS.md) |
| **Secure SDLC** | OWASP ZAP + Schemathesis in CI; [../SECURITY.md](../SECURITY.md) |
| **Threat modeling** | STRIDE summary; [../security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md) |
| **Operational drills** | Geo-failover drill runbook; [../runbooks/GEO_FAILOVER_DRILL.md](../runbooks/GEO_FAILOVER_DRILL.md) |

---

## 2. Typical gaps for SOC 2 (to close with process and policy)

SOC 2 requires **documented** policies, **operating evidence**, and often **independent** validation. Items commonly **not** fully satisfied by code alone:

| Gap | What “done” looks like |
|-----|-------------------------|
| **Formal ISMS / policies** | Written information security policy, acceptable use, access review cadence, approved exceptions |
| **Vendor / subprocessor risk** | Due diligence on Microsoft and any future vendors; [SUBPROCESSORS.md](SUBPROCESSORS.md) maintained under change control |
| **HR / training** | Security awareness training records, onboarding/offboarding checklists |
| **BCP / DR** | Tested recovery objectives aligned with customer messaging; tie internal drills to RTO/RPO statements |
| **Incident response** | Playbooks, tabletop exercises, evidence of post-incident reviews ([INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md)) |
| **Penetration testing** | Third-party pen test or bug bounty; threat model notes “not a pen test” ([../security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md) §3) |
| **Management oversight** | Risk register, periodic review minutes |
| **Customer commitments** | SLAs and support tiers published where offered |

---

## 3. Milestone roadmap (illustrative quarters)

| Phase | Target | Outcomes |
|-------|--------|----------|
| **Phase 1** | Q3 2026 | Policy pack v1; vendor/subprocessor register; IR tabletop; evidence folders |
| **Phase 2** | Q4 2026 | Auditor shortlist; readiness gap assessment; pen test scoped |
| **Phase 3** | Q1 2027 | SOC 2 Type I **observation period**; control testing |
| **Phase 4** | Q2 2027 | **SOC 2 Type I** report issued (target) |
| **Phase 5** | Q3 2027+ | **Type II** observation window |

Dates are **placeholders** until leadership and an auditor confirm.

---

## 4. What customers can request today

- **Security architecture:** [TRUST_CENTER.md](TRUST_CENTER.md), [TENANT_ISOLATION.md](TENANT_ISOLATION.md), [../security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md)
- **Subprocessors:** [SUBPROCESSORS.md](SUBPROCESSORS.md)
- **DPA:** [DPA_TEMPLATE.md](DPA_TEMPLATE.md) (legal review required)
- **Incident process:** [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md)
- **SLOs:** [../API_SLOS.md](../API_SLOS.md)

**SOC 2 report:** Not available until Phase 4; roadmap above applies.

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) | Incident customer comms |

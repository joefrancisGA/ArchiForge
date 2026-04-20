> **Scope:** SOC 2 Trust Services Criteria — **self-assessment only** (not CPA attestation).

# SOC 2 — Owner self-assessment (2026)

> **IMPORTANT:** This document is an **internal / buyer-transparency self-assessment**. It is **not** a SOC 2 Type I or Type II **audit opinion** and must not be represented as third-party attestation.

## Scope

**In scope:** Security (CC) and Availability (A) criteria most relevant to the hosted API + SQL data plane. Confidentiality, Processing Integrity, and Privacy are **partially** addressed where they overlap engineering controls (see gap register).

## Control summary (high level)

| TSC theme | ArchLucid evidence (examples) | Maturity |
|-----------|-------------------------------|----------|
| Security — logical access | Entra / JWT roles, API keys, RBAC policies; `AuthSafetyGuard` | Partial |
| Security — data protection | SQL RLS + `SESSION_CONTEXT`; private endpoint posture (Terraform) | Partial |
| Security — secure SDLC | OWASP ZAP, Schemathesis, CodeQL, unit/integration tiers | Strong |
| Availability | `/health/*`, SLO docs, synthetic probes, runbooks | Partial |

## Gap register

| ID | Gap | Owner | Target |
|----|-----|-------|--------|
| G-001 | No CPA SOC 2 report | `<<TBD>>` | Fund external audit when revenue allows |
| G-002 | Pen-test summary not yet published | `<<TBD>>` | Complete 2026-Q2 engagement |
| G-003 | CAIQ / SIG not pre-filled | `<<TBD>>` | Publish alongside trust center |

## Related

- [`COMPLIANCE_MATRIX.md`](COMPLIANCE_MATRIX.md)
- [`../go-to-market/SOC2_ROADMAP.md`](../go-to-market/SOC2_ROADMAP.md)

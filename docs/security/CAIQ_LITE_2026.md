> **Scope:** CAIQ Lite-style questionnaire (Cloud Security Alliance **CAIQ v4** alignment). **Not** a completed STAR / CCM submission — pre-filled for procurement drafts.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# CAIQ Lite — ArchLucid (2026 pre-fill)

**Source alignment:** CSA Consensus Assessment Initiative Questionnaire (CAIQ) **Lite** themes. Download the authoritative **CAIQ v4** spreadsheet from [Cloud Security Alliance](https://cloudsecurityalliance.org/) and map row IDs when submitting through a STAR registry.

**Product context:** ArchLucid SaaS (API + SQL authority plane + optional Azure OpenAI). Identity: Microsoft Entra ID. Data: Azure SQL with row-level security.

## Governance (GOV)

| Theme | Response (summary) | Evidence in repo |
|-------|----------------------|------------------|
| Security policies maintained | Yes — engineering [`SECURITY.md`](../library/SECURITY.md); incident comms [`../go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md`](../go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md) | Links |
| Risk assessments | Partial — STRIDE [`SYSTEM_THREAT_MODEL.md`](SYSTEM_THREAT_MODEL.md); external pen test SoW [`pen-test-summaries/2026-Q2-SOW.md`](pen-test-summaries/2026-Q2-SOW.md) | Links |
| Management oversight | Partial — SOC2 roadmap [`../go-to-market/SOC2_ROADMAP.md`](../go-to-market/SOC2_ROADMAP.md) | Link |

## Human resources (HRS)

| Theme | Response | Evidence |
|-------|----------|----------|
| Security awareness | Partial — internal process; not customer-auditable here | TBD HR system |

## Information management (IMC)

| Theme | Response | Evidence |
|-------|----------|----------|
| Data classification | Partial — tenant isolation doc [`../go-to-market/TENANT_ISOLATION.md`](../go-to-market/TENANT_ISOLATION.md) | Link |
| Encryption in transit | Yes — TLS to API; private endpoints optional [`../CUSTOMER_TRUST_AND_ACCESS.md`](../library/CUSTOMER_TRUST_AND_ACCESS.md) | Link |
| Encryption at rest | Yes — Azure SQL / storage platform defaults (see Terraform modules under `infra/`) | IaC |

## Operations (OPS)

| Theme | Response | Evidence |
|-------|----------|----------|
| Logging / monitoring | Yes — audit matrix [`../AUDIT_COVERAGE_MATRIX.md`](../library/AUDIT_COVERAGE_MATRIX.md); SLOs [`../API_SLOS.md`](../library/API_SLOS.md) | Links |
| Vulnerability management | Partial — Dependabot / `dotnet list package --vulnerable` in CI; ZAP + Schemathesis scheduled | CI workflows |

## Application security (APP)

| Theme | Response | Evidence |
|-------|----------|----------|
| Secure SDLC | Yes — CodeQL `security-extended`, ZAP strict, mutation testing (Persistence Stryker matrix) | `.github/workflows/` |
| API authorization | Yes — policy-based `[Authorize(Policy=...)]`; guard tests `ApiControllerMutationPolicyGuardTests` | `ArchLucid.Api.Tests` |

## Related

- [`SIG_CORE_2026.md`](SIG_CORE_2026.md)
- [`../go-to-market/TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)

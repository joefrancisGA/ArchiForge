> **Scope:** ArchLucid Trust Center - full detail, tables, and links in the sections below.

# ArchLucid Trust Center

**Audience:** Security reviewers, procurement, and legal teams evaluating ArchLucid as a **vendor-operated (SaaS)** service.

**Last reviewed:** 2026-04-20

ArchLucid is built so that **security, privacy, and operational transparency** are first-class: identity-backed access, defense-in-depth on the data plane, measurable reliability targets, and documentation you can trace to the product and infrastructure code. This page is the **buyer-facing index** into policies and deep technical references maintained in the repository.

---

## Security overview at a glance

- **Identity:** Microsoft **Entra ID** (OIDC / JWT) with **app roles** (**Admin**, **Operator**, **Reader**, **Auditor**) and optional **API keys** for automation; see [../SECURITY.md](../SECURITY.md) and [../CUSTOMER_TRUST_AND_ACCESS.md](../CUSTOMER_TRUST_AND_ACCESS.md).
- **Network:** Optional **Azure Front Door + WAF**, optional **API Management**, **TLS** to the API, and **private endpoints** for **Azure SQL** and **Blob** when the private stack is enabled; **no public SMB (port 445)** for tenant data paths (see [../CUSTOMER_TRUST_AND_ACCESS.md](../CUSTOMER_TRUST_AND_ACCESS.md)).
- **Data isolation:** **Row-level security** in SQL with per-request **`SESSION_CONTEXT`** scope (tenant / workspace / project) on covered tables — defense-in-depth alongside application authorization; see [TENANT_ISOLATION.md](TENANT_ISOLATION.md) and [../security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md).
- **Secrets:** **Azure Key Vault** references for application configuration in hosted deployments (see [../CONFIGURATION_KEY_VAULT.md](../CONFIGURATION_KEY_VAULT.md)).
- **Auditability:** Durable **append-only** audit trail in SQL (`dbo.AuditEvents`) with a **typed event catalog** and correlation identifiers; see [../AUDIT_COVERAGE_MATRIX.md](../AUDIT_COVERAGE_MATRIX.md) and [../SECURITY.md](../SECURITY.md) (PII / exports).
- **Testing in CI:** **OWASP ZAP** baseline on the API image, **Schemathesis** contract checks, and documented rate limiting / RBAC — see [../SECURITY.md](../SECURITY.md).
- **LLM outbound hygiene:** Optional deny-list **prompt redaction** before Azure OpenAI and aligned redaction for trace persistence (`LlmPromptRedaction`); see [../runbooks/LLM_PROMPT_REDACTION.md](../runbooks/LLM_PROMPT_REDACTION.md).

For a **STRIDE-oriented** view of the whole product boundary, see [../security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md).

---

## Penetration testing (scaffolding)

Formal third-party pen tests are **scheduled per customer / release train**, not implied by CI smoke alone. Procurement-ready templates:

- [Statement of work template](../security/PEN_TEST_SOW_TEMPLATE.md)
- [Redacted customer summary template](../security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md)

---

## Trust documents

| Document | Description |
|----------|-------------|
| [TENANT_ISOLATION.md](TENANT_ISOLATION.md) | Buyer-readable summary of tenant isolation (identity, app layer, SQL RLS). |
| [SUBPROCESSORS.md](SUBPROCESSORS.md) | Subprocessors used to deliver the service (Microsoft Azure, Entra ID, Azure OpenAI, etc.). |
| [DPA_TEMPLATE.md](DPA_TEMPLATE.md) | Data Processing Agreement **template** for customers (requires legal review before use). |
| [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) | How we classify incidents and communicate with customers. |
| [SOC2_ROADMAP.md](SOC2_ROADMAP.md) | SOC 2 readiness: controls in place, gaps, and milestone roadmap. |
| [../SECURITY.md](../SECURITY.md) | Engineering security overview (ZAP, Schemathesis, RBAC, rate limits, PII). |
| [../CUSTOMER_TRUST_AND_ACCESS.md](../CUSTOMER_TRUST_AND_ACCESS.md) | Architecture: edge, identity, private connectivity, correlation IDs. |
| [../API_SLOS.md](../API_SLOS.md) | Customer-visible HTTP SLOs (e.g. availability **99.5%** / 30 days) and measurement. |
| [../security/PII_RETENTION_CONVERSATIONS.md](../security/PII_RETENTION_CONVERSATIONS.md) | PII framing and retention considerations for conversation / Ask data. |
| [SLA_SUMMARY.md](SLA_SUMMARY.md) | Buyer-facing SLO targets (availability 99.5%, latency, maintenance). |
| [BACKUP_AND_DR.md](BACKUP_AND_DR.md) | Backup schedule, disaster recovery, data lifecycle, RTO/RPO estimates. |
| [OPERATIONAL_TRANSPARENCY.md](OPERATIONAL_TRANSPARENCY.md) | Status page plan and operational transparency roadmap. |
| [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md) | Available and planned integrations (API, CLI, webhooks, CI/CD, SIEM). |
| [SIEM_EXPORT.md](SIEM_EXPORT.md) | Audit log export for SIEM ingestion (Splunk, Sentinel, generic). |
| [CUSTOMER_ONBOARDING_PLAYBOOK.md](CUSTOMER_ONBOARDING_PLAYBOOK.md) | Structured onboarding checklist (6-week pilot alignment). |

---

## Compliance and certifications

| Item | Status | Notes |
|------|--------|--------|
| **SOC 2** (Type I / II) | In progress | See [SOC2_ROADMAP.md](SOC2_ROADMAP.md). |
| **GDPR / DPA** | Template available | See [DPA_TEMPLATE.md](DPA_TEMPLATE.md); subprocessors in [SUBPROCESSORS.md](SUBPROCESSORS.md). |
| **ISO 27001** | Not claimed | Roadmap TBD with SOC 2 program. |

---

## Contact

- **Security inquiries:** `security@archlucid.dev` (replace with your production security contact when published).

For support alignment during incidents, clients should include **`X-Correlation-ID`** on API requests where possible ([../CUSTOMER_TRUST_AND_ACCESS.md](../CUSTOMER_TRUST_AND_ACCESS.md) §8).

---

## Related documents

| Doc | Use |
|-----|-----|
| [POSITIONING.md](POSITIONING.md) | Product positioning and messaging |
| [../MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md](../MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md) | SaaS-only marketability assessment |
| [../V1_SCOPE.md](../V1_SCOPE.md) | What V1 ships (grounding for claims) |

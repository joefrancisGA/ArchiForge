> **Scope:** Buyers and security reviewers: repository-linked snapshot of current assurance claims; not legal advice, CPA attestation, or customer-specific commitments.

# ArchLucid — Current Assurance Posture

**Date:** 2026-05-01
**Last reviewed:** 2026-05-01
**Classification:** Buyer-facing (include in procurement pack ZIP)

This document summarizes the security, compliance, and assurance evidence that ArchLucid provides today. Every claim below links to a source artifact in the repository. Status labels follow [`ASSURANCE_STATUS_CANONICAL.md`](ASSURANCE_STATUS_CANONICAL.md) to avoid contradictory buyer wording.

---

## 1. Continuous security testing in CI

ArchLucid runs automated security checks on every pull request and merge to main. These are **merge-blocking** unless noted.

| Check | Tool | What it catches | CI status |
|-------|------|----------------|-----------|
| Secret scanning | [Gitleaks](https://github.com/gitleaks/gitleaks) (`.gitleaks.toml`) | Leaked API keys, connection strings, tokens in committed code | **Merge-blocking** (Tier 0) |
| Static analysis (security-extended) | [CodeQL](https://codeql.github.com/) (`.github/workflows/codeql.yml`) | SQL injection, XSS, insecure deserialization, tainted data flows | **Merge-blocking** |
| DAST baseline | [OWASP ZAP](https://www.zaproxy.org/) (`infra/zap/`) | Common web vulnerabilities (OWASP Top 10) against running API image | **Scheduled** (strict variant: `zap-baseline-strict-scheduled.yml`) |
| API contract fuzz | [Schemathesis](https://schemathesis.readthedocs.io/) (`.github/workflows/schemathesis-scheduled.yml`) | Invalid inputs, unexpected status codes, OpenAPI contract violations | **Scheduled** |
| Container image scan | [Trivy](https://aquasecurity.github.io/trivy/) (in `ci.yml`) | Known CVEs in OS packages and .NET dependencies | **Merge-blocking** |
| IaC misconfiguration scan | [Trivy](https://aquasecurity.github.io/trivy/) (Terraform config check in `ci.yml`) | Public exposure, encryption gaps, IAM misconfigurations in Terraform | **Merge-blocking** |
| Dependency audit | [Dependabot](https://docs.github.com/en/code-security/dependabot) (`.github/dependabot.yml`) | Known vulnerabilities in NuGet and npm dependencies | **Automated PRs** |
| SBOM generation | CycloneDX (in `ci.yml`) | Software Bill of Materials for .NET and npm packages | **Per-build artifact** |

**Evidence:** [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml), [`.github/workflows/codeql.yml`](../../.github/workflows/codeql.yml)

---

## 2. Data isolation model

| Layer | Mechanism | Evidence |
|-------|-----------|---------|
| **Identity** | Microsoft Entra ID (OIDC / JWT) with app roles (Admin, Operator, Reader, Auditor); optional API keys for automation | [`docs/library/SECURITY.md`](../library/SECURITY.md) |
| **Application** | RBAC policies (`ReadAuthority`, `ExecuteAuthority`, `AdminAuthority`); request-scoped tenant/workspace/project context | [`ArchLucid.Api/Auth/`](../../ArchLucid.Api/Auth/) |
| **Database** | SQL Server row-level security (RLS) with `SESSION_CONTEXT` per request; tenant ID enforced on covered tables | [`docs/security/MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md) |
| **Network** | Optional Azure Front Door + WAF; private endpoints for Azure SQL and Blob; no public SMB (port 445) | [`docs/library/CUSTOMER_TRUST_AND_ACCESS.md`](../library/CUSTOMER_TRUST_AND_ACCESS.md) |
| **Secrets** | Azure Key Vault references for application configuration in hosted deployments | [`docs/library/CONFIGURATION_KEY_VAULT.md`](../library/CONFIGURATION_KEY_VAULT.md) |

**Evidence:** [`docs/go-to-market/TENANT_ISOLATION.md`](TENANT_ISOLATION.md)

---

## 3. Audit trail

| Capability | Detail |
|-----------|--------|
| Event catalog | 117 typed audit event constants with CI guard on count |
| Storage | Append-only SQL table (`dbo.AuditEvents`) with `DENY UPDATE` / `DENY DELETE` at database level |
| Search | Paginated API with keyset cursor, filtered by event type, actor, run ID, correlation ID, time window |
| Export | JSON and CSV bulk export (`GET /v1/audit/export`); 90-day window per request; max 10,000 rows per call |
| Retention | Tiered (hot 0-90 days, warm 90-365 days, cold 365+ days via operator-scheduled blob exports) |

**Evidence:** [`docs/library/AUDIT_COVERAGE_MATRIX.md`](../library/AUDIT_COVERAGE_MATRIX.md), [`docs/library/AUDIT_RETENTION_POLICY.md`](../library/AUDIT_RETENTION_POLICY.md)

---

## 4. Threat modeling

| Artifact | Scope | Evidence |
|----------|-------|---------|
| STRIDE system threat model | Full product boundary (API, SQL, LLM, Blob, Service Bus, billing webhooks, trial lifecycle) | [`docs/security/SYSTEM_THREAT_MODEL.md`](../security/SYSTEM_THREAT_MODEL.md) |
| ASK/RAG threat model | Natural-language query surface (prompt injection, data exfiltration, context poisoning) | [`docs/security/ASK_RAG_THREAT_MODEL.md`](../security/ASK_RAG_THREAT_MODEL.md) |
| LLM prompt redaction | Configurable deny-list redaction before Azure OpenAI; aligned trace persistence redaction | [`docs/runbooks/LLM_PROMPT_REDACTION.md`](../runbooks/LLM_PROMPT_REDACTION.md) |

---

## 5. Compliance and privacy

| Artifact | Status | Evidence |
|----------|--------|---------|
| SOC 2 self-assessment (Security + Availability) | **Completed** (internal; not CPA attestation) | [`docs/security/SOC2_SELF_ASSESSMENT_2026.md`](../security/SOC2_SELF_ASSESSMENT_2026.md) |
| SOC 2 Type I scoping | **Deferred (funding-gated)** | [SOC2_STATUS_PROCUREMENT.md](SOC2_STATUS_PROCUREMENT.md), [ASSURANCE_STATUS_CANONICAL.md](ASSURANCE_STATUS_CANONICAL.md) |
| CAIQ Lite pre-fill (CSA STAR) | **Completed** | [`docs/security/CAIQ_LITE_2026.md`](../security/CAIQ_LITE_2026.md) |
| SIG Core pre-fill (Shared Assessments) | **Completed** | [`docs/security/SIG_CORE_2026.md`](../security/SIG_CORE_2026.md) |
| Compliance control matrix | **Completed** | [`docs/security/COMPLIANCE_MATRIX.md`](../security/COMPLIANCE_MATRIX.md) |
| VPAT® 2.5–style WCAG 2.1 A/AA Accessibility Conformance Report | **Completed** (honest, evidence-based; not legal certification) | [`docs/security/VPAT_2_5_WCAG_2_1_AA.md`](../security/VPAT_2_5_WCAG_2_1_AA.md); evidence map: [`docs/security/VPAT_EVIDENCE_MAP.md`](../security/VPAT_EVIDENCE_MAP.md) |
| Data Processing Agreement template | **Completed** (requires legal review) | [`docs/go-to-market/DPA_TEMPLATE.md`](DPA_TEMPLATE.md) |
| GDPR DSAR process | **Completed** | [`docs/security/DSAR_PROCESS.md`](../security/DSAR_PROCESS.md) |
| Subprocessors register | **Completed** | [`docs/go-to-market/SUBPROCESSORS.md`](SUBPROCESSORS.md) |

---

## 6. Penetration testing

| Engagement | Status | Detail |
|-----------|--------|--------|
| Third-party pen test (external vendor) | **Deferred to V2** — no vendor awarded for V1; templates at [`docs/security/pen-test-summaries/2026-Q2-SOW.md`](../security/pen-test-summaries/2026-Q2-SOW.md) | Typical scope: API, operator UI, hosted SaaS data plane — confirm in executed SoW |
| Owner-conducted penetration-style assessment | **Active V1 control** (owner-led) | [`docs/security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md`](../security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md) |
| Owner-conducted security self-assessment | **Completed** (interim posture) | [`docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2.md`](../security/OWNER_SECURITY_ASSESSMENT_2026_Q2.md) |

**Access to pen-test results:** Redacted summaries are available **under NDA only**. Contact `security@archlucid.net`.

---

## 7. Infrastructure as Code

All infrastructure is defined in Terraform across 14 modules:

| Module | Purpose |
|--------|---------|
| `infra/terraform/` | Core Azure resources (resource group, app config) |
| `infra/terraform-sql-failover/` | Azure SQL with auto-failover groups, automatic tuning, consumption budgets |
| `infra/terraform-container-apps/` | Container Apps environment, jobs, secondary region |
| `infra/terraform-edge/` | Azure Front Door, WAF, marketing routes |
| `infra/terraform-monitoring/` | Application Insights, Grafana dashboards, Prometheus SLO rules |
| `infra/terraform-storage/` | Azure Blob Storage |
| `infra/terraform-keyvault/` | Azure Key Vault |
| `infra/terraform-servicebus/` | Azure Service Bus with IAM |
| `infra/terraform-entra/` | Entra ID app registrations, External ID |
| `infra/terraform-openai/` | Azure OpenAI |
| `infra/terraform-private/` | Private endpoints, App Service, network |
| `infra/terraform-otel-collector/` | OpenTelemetry collector |
| `infra/terraform-pilot/` | Pilot-sized deployment |
| `infra/terraform-orchestrator/` | Orchestrator resources |

**Evidence:** [`infra/`](../../infra/), [`docs/library/DEPLOYMENT_TERRAFORM.md`](../library/DEPLOYMENT_TERRAFORM.md)

---

## 8. Contact

For security inquiries, procurement pack requests, or NDA-gated materials: **`security@archlucid.net`**

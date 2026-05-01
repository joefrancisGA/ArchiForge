# Artifact status index

Each row reflects `artifact_status` from the canonical procurement list (`scripts/procurement_pack_canonical.json`).

| Pack file | Status | Description |
| --- | --- | --- |
| `SECURITY.md` | **Evidence** | Security policy and operational commitments |
| `README.md` | **Evidence** | Repository overview |
| `TRUST_CENTER.md` | **Evidence** | Buyer-facing trust index |
| `DATA_SUBPROCESSORS.md` | **Evidence** | Subprocessor register (buyer-facing filename; source is SUBPROCESSORS.md) |
| `DPA_TEMPLATE.md` | **Template** | Data processing agreement template |
| `INTEGRATION_CATALOG.md` | **Evidence** | Third-party integrations touching tenant data |
| `AUDIT_COVERAGE_MATRIX.md` | **Evidence** | Audit event coverage |
| `MULTI_TENANT_RLS.md` | **Evidence** | Tenant isolation (RLS) |
| `ACCESSIBILITY.md` | **Evidence** | Accessibility posture / WCAG alignment |
| `SECURITY.txt` | **Evidence** | security.txt (RFC 9116) contents |
| `OWNER_SECURITY_ASSESSMENT_REDACTED.md` | **Self-assessment** | Owner self-assessment — procurement-safe excerpt (no customer names) |
| `PEN_TEST_SUMMARY.md` | **Template** | Penetration test summary pointer (third-party engagement pending) |
| `SOC2_STATUS.md` | **Deferred** | SOC 2 attestation status (deferred; see TRUST_CENTER) |
| `API_CONTRACTS.md` | **Evidence** | API contract surface |
| `INTEGRATION_EVENTS_AND_WEBHOOKS.md` | **Evidence** | Integration events and webhooks |
| `CUSTOMER_TRUST_AND_ACCESS.md` | **Evidence** | Customer trust and access model |
| `V1_SCOPE.md` | **Evidence** | V1 product scope |
| `BREAKING_CHANGES.md` | **Evidence** | Breaking change policy and log |
| `PROCUREMENT_PACK_COVER.md` | **Template** | Cover letter scaffold (owner completes before sending) |
| `ENTERPRISE_COMPARISON_ONE_PAGE.md` | **Evidence** | Enterprise comparison one-pager (Markdown; PDF via GET /v1/marketing/enterprise-comparison.pdf) |
| `DSAR_PROCESS.md` | **Evidence** | GDPR Data Subject Access Request process — PII map, fulfillment steps, erasure constraints |
| `CURRENT_ASSURANCE_POSTURE.md` | **Self-assessment** | Current assurance posture summary — CI security checks, isolation model, audit, compliance status |

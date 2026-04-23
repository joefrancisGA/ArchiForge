> **Scope:** Shared Assessments **SIG Core**-style control mapping (pre-fill). **Not** a completed SIG submission — use for RFP appendix drafts.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# SIG Core — ArchLucid (2026 pre-fill)

**Source alignment:** Shared Assessments SIG **Core** control families. Obtain the current SIG Core workbook from [Shared Assessments](https://sharedassessments.org/) and copy authoritative control IDs into your vendor profile.

## Control family A — Corporate governance

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Information security program | Partial | [`SOC2_SELF_ASSESSMENT_2026.md`](SOC2_SELF_ASSESSMENT_2026.md), [`COMPLIANCE_MATRIX.md`](COMPLIANCE_MATRIX.md) |

## Control family B — Risk management

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Threat modeling | Partial | [`SYSTEM_THREAT_MODEL.md`](SYSTEM_THREAT_MODEL.md) |
| Third-party pen test | In flight | [`pen-test-summaries/2026-Q2-SOW.md`](pen-test-summaries/2026-Q2-SOW.md) |

## Control family C — Human resources

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Personnel security | Partial | HR artifacts out of repo |

## Control family D — Information security

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Access control | Strong (engineering) | [`../SECURITY.md`](../library/SECURITY.md), [`../CUSTOMER_TRUST_AND_ACCESS.md`](../library/CUSTOMER_TRUST_AND_ACCESS.md) |
| Data protection | Strong (engineering) | [`MULTI_TENANT_RLS.md`](MULTI_TENANT_RLS.md) |

## Control family E — Asset management

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Configuration / CMDB | Partial | Terraform state discipline; `infra/` |

## Control family F — Physical / environmental

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Cloud DC controls | Inherited | Microsoft Azure DPA / trust pages (customer responsibility model) |

## Control family G — Operations

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Logging & monitoring | Partial | [`../AUDIT_COVERAGE_MATRIX.md`](../library/AUDIT_COVERAGE_MATRIX.md) |
| Incident response | Partial | [`../go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md`](../go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md) |

## Control family H — Communications / operations

| Control intent | Status | Evidence |
|----------------|--------|----------|
| Network security | Partial | Edge/WAF/APIM optional; private endpoints documented |

## Related

- [`CAIQ_LITE_2026.md`](CAIQ_LITE_2026.md)
- [`../go-to-market/TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)

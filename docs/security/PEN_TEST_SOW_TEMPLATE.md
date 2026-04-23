> **Scope:** Penetration test — statement of work (template) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Penetration test — statement of work (template)

**Audience:** Vendor + qualified assessor. **Do not** paste production secrets, connection strings, or customer PII into this document.

## 1. Objective

Authorize a **time-boxed** technical assessment of ArchLucid’s exposed attack surface (web API, operator UI, supporting infrastructure in scope).

## 2. Scope

| In scope | Out of scope (unless explicitly added) |
|----------|------------------------------------------|
| HTTPS API surface documented in OpenAPI | Customer-owned IdP misconfiguration |
| Operator UI (`archlucid-ui`) staging tenant | Physical / social engineering |
| Azure OpenAI integration (rate limits, auth) | Third-party SaaS outside subprocessors list |
| SQL Server with **RLS session context** enabled as in staging | Denial-of-service against shared CI |

## 3. Methodology

- OWASP ASVS-aligned testing; **no** destructive data mutation on production.
- Credential rotation after test accounts are used; **break-glass** SQL accounts forbidden unless pre-approved.

## 4. Deliverables

1. Executive summary (risk-ranked).
2. Detailed findings with reproduction, impact, and remediation guidance.
3. Re-test window for critical fixes (negotiated).

## 5. Artifacts for assessor

- Staging **base URL**, non-production **Entra** app registration, **test tenant** id.
- Redacted architecture diagram link: [`docs/CUSTOMER_TRUST_AND_ACCESS.md`](../library/CUSTOMER_TRUST_AND_ACCESS.md).

Use [`PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md`](PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md) when publishing a **customer-shareable** excerpt.

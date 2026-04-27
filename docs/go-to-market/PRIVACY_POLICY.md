> **Scope:** Public-facing privacy policy for archlucid.net visitors and ArchLucid product users. Covers GDPR and CCPA. Not legal advice — owner has reviewed and approved; no external law firm engagement.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

<!-- PRIVACY_POLICY_LAST_REVIEWED_UTC:2026-04-26 -->

# ArchLucid Privacy Policy

**Effective date:** 2026-04-26

**Last reviewed (UTC):** 2026-04-26

This privacy policy describes how ArchLucid ("we", "us", "our") collects, uses, shares, and protects personal information when you visit our website at `archlucid.net` or use the ArchLucid platform (the "Service"). It applies to all visitors, trial users, and paying customers.

For operator-facing processing activity records (GDPR Article 30), see [`docs/security/PRIVACY_NOTE.md`](../security/PRIVACY_NOTE.md).

For data processing agreement terms, see [`DPA_TEMPLATE.md`](DPA_TEMPLATE.md).

---

## 1. Who we are

ArchLucid is a B2B SaaS platform that provides AI-assisted architecture workflow tools. We act as a **data controller** for personal data collected through our website and platform operations, and as a **data processor** for customer workload content submitted through architecture runs (governed by the subscription agreement and DPA).

**Contact:** `privacy@archlucid.net`

---

## 2. What personal information we collect

### 2.1 Information you provide directly

| Category | Examples | When collected |
|----------|----------|----------------|
| **Account and identity** | Name, email address, organization name, job title | Account creation, trial signup, contact forms |
| **Authentication credentials** | Entra ID / OIDC tokens (we do not store passwords for federated authentication) | Sign-in |
| **Architecture content** | System descriptions, architecture briefs, configuration details submitted to runs | Product use (governed by DPA as processor) |
| **Communications** | Email content when you contact us at `privacy@archlucid.net` or `security@archlucid.net` | Support and inquiry correspondence |

### 2.2 Information collected automatically

| Category | Examples | Purpose |
|----------|----------|---------|
| **Usage telemetry** | Pages visited, features used, run counts, trial lifecycle events | Product improvement and onboarding measurement |
| **Technical diagnostics** | Browser type, OS, error messages, stack traces (sanitized via `LogSanitizer`), URL pathnames | Service reliability and bug detection |
| **Server logs** | IP address, request timestamps, HTTP method, response codes, correlation IDs | Security, abuse prevention, debugging |

### 2.3 Information we do not collect

- We do not use advertising trackers, third-party analytics cookies, or social media tracking pixels.
- We do not sell personal information.
- We do not use personal information for automated decision-making or profiling that produces legal effects.

---

## 3. How we use personal information

| Purpose | Legal basis (GDPR) | CCPA category |
|---------|-------------------|---------------|
| **Provide the Service** — authenticate users, run architecture analysis, deliver results | Art. 6(1)(b) — contract performance | Business purpose |
| **Deliver transactional email** — welcome, usage warnings, trial expiry | Art. 6(1)(b) — contract performance | Business purpose |
| **Maintain security and reliability** — detect errors, prevent abuse, enforce rate limits | Art. 6(1)(f) — legitimate interest | Business purpose |
| **Measure onboarding success** — aggregated funnel metrics (per-tenant correlation is opt-in and flag-gated; default is aggregated-only with no personal data) | Art. 6(1)(f) — legitimate interest | Business purpose |
| **Respond to inquiries** — answer questions, provide support | Art. 6(1)(f) — legitimate interest | Business purpose |
| **Comply with legal obligations** — respond to lawful requests, maintain audit records | Art. 6(1)(c) — legal obligation | Business purpose |

We do **not** use personal information for marketing, advertising, or selling to third parties.

---

## 4. How we share personal information

We share personal information only in the following circumstances:

| Recipient | Purpose | Safeguards |
|-----------|---------|------------|
| **Microsoft Azure** (subprocessor) | Infrastructure hosting, identity, AI inference, email delivery | Azure data processing terms; managed identity; encryption at rest and in transit; private endpoints where configured |
| **Your organization** | Tenant administrators can view audit logs and user activity within their tenant scope | RLS tenant isolation; RBAC role enforcement |
| **Legal compliance** | Response to valid legal process (subpoena, court order) | We will notify affected customers unless legally prohibited |

We do **not** sell, rent, or trade personal information to third parties. The full subprocessor list is maintained at [`SUBPROCESSORS.md`](SUBPROCESSORS.md).

---

## 5. Data retention

| Data type | Retention period | Deletion mechanism |
|-----------|-----------------|-------------------|
| **Account data** | Duration of subscription + 90 days post-termination | Tenant deletion cascade |
| **Architecture content** (processor role) | Per subscription agreement; customer controls export and deletion | Product export features (ZIP, DOCX, CSV) + tenant deletion |
| **Audit events** | Per audit retention policy (default: hot 90 days, warm 1 year, cold per contract) | Automated tiered archival; see [`docs/library/AUDIT_RETENTION_POLICY.md`](../library/AUDIT_RETENTION_POLICY.md) |
| **Transactional email records** | Idempotency keys only (no email bodies stored); cascade-deleted with tenant | Tenant deletion |
| **Onboarding funnel telemetry** | Aggregated: Application Insights retention. Per-tenant (flag-gated): 90 days | Automated SQL prune job |
| **Server logs** | Application Insights workspace retention (platform default) | Azure Monitor lifecycle |

---

## 6. Your rights under GDPR

If you are in the European Economic Area (EEA), United Kingdom, or Switzerland, you have the following rights under the General Data Protection Regulation:

| Right | How to exercise |
|-------|----------------|
| **Access** — obtain a copy of your personal data | Email `privacy@archlucid.net` |
| **Rectification** — correct inaccurate personal data | Email `privacy@archlucid.net` or update your profile in the Service |
| **Erasure** — request deletion of your personal data | Email `privacy@archlucid.net`; tenant administrators can also request tenant-level deletion |
| **Restriction** — limit how we process your data | Email `privacy@archlucid.net` |
| **Data portability** — receive your data in a structured, machine-readable format | Use product export features (ZIP, DOCX, CSV, API) or email `privacy@archlucid.net` |
| **Objection** — object to processing based on legitimate interest | Email `privacy@archlucid.net`; for onboarding telemetry, the per-tenant flag defaults to OFF |
| **Withdraw consent** — where processing is based on consent (currently none in V1) | Email `privacy@archlucid.net` |
| **Lodge a complaint** — contact your local supervisory authority | See your national data protection authority's website |

We will respond to rights requests within **30 days** (extendable by 60 days for complex requests, with notice).

---

## 7. Your rights under CCPA

If you are a California resident, the California Consumer Privacy Act (as amended by the CPRA) provides you with the following rights:

### 7.1 Right to know

You have the right to request that we disclose the categories and specific pieces of personal information we have collected about you, the categories of sources, the business purposes for collection, and the categories of third parties with whom we share it.

### 7.2 Right to delete

You have the right to request deletion of personal information we have collected from you, subject to certain exceptions (e.g., legal compliance, completing a transaction, security).

### 7.3 Right to correct

You have the right to request correction of inaccurate personal information.

### 7.4 Right to opt out of sale or sharing

We do **not** sell or share (as defined by CCPA) personal information for cross-context behavioral advertising. There is nothing to opt out of.

### 7.5 Right to non-discrimination

We will not discriminate against you for exercising your CCPA rights. Exercising your rights will not affect the pricing or quality of Service you receive.

### 7.6 How to exercise CCPA rights

Submit requests to `privacy@archlucid.net`. We will verify your identity before fulfilling requests. We will respond within **45 days** (extendable by an additional 45 days with notice).

### 7.7 Categories of personal information collected (CCPA disclosure)

| CCPA category | Collected | Sold | Shared for cross-context behavioral advertising |
|---------------|-----------|------|------------------------------------------------|
| Identifiers (name, email, account ID) | Yes | No | No |
| Internet or network activity (pages visited, interactions) | Yes | No | No |
| Professional or employment information (job title, organization) | Yes | No | No |
| Geolocation data (IP-derived, coarse) | Yes (server logs) | No | No |
| Inferences drawn from personal information | No | No | No |

---

## 8. International data transfers

ArchLucid is hosted on Microsoft Azure. Data may be processed in the Azure region(s) selected at deployment time. Where personal data is transferred outside the EEA, we rely on:

- **Microsoft's data processing terms** and Standard Contractual Clauses (SCCs) incorporated into Azure agreements
- **Adequacy decisions** where applicable
- **Supplementary measures** as documented in our DPA template

The primary Azure region for the ArchLucid SaaS offering is documented in the order form or security pack. See [`SUBPROCESSORS.md`](SUBPROCESSORS.md) for details.

---

## 9. Cookies and tracking

ArchLucid uses **only essential cookies** required for authentication and session management. We do **not** use:

- Third-party analytics cookies (no Google Analytics, Mixpanel, etc.)
- Advertising or retargeting cookies
- Social media tracking pixels
- Cross-site tracking of any kind

If this changes in a future version, this policy will be updated and users will be notified.

---

## 10. Security

We implement technical and organizational measures to protect personal information, including:

- Encryption at rest (Azure SQL TDE, Azure Blob encryption) and in transit (TLS 1.2+)
- Row-level security (RLS) for tenant isolation
- Managed identity for service-to-service authentication
- Private endpoints for data-plane services where configured
- Append-only audit log with database-level `DENY UPDATE/DELETE`
- Prompt redaction before LLM calls
- Secret scanning, container scanning, and OWASP ZAP in CI

For full details, see the [Trust Center](../trust-center.md) and [System Threat Model](../security/SYSTEM_THREAT_MODEL.md).

---

## 11. Children's privacy

ArchLucid is a B2B enterprise product. We do not knowingly collect personal information from children under 16 (or the applicable age in your jurisdiction). If we learn that we have collected personal information from a child, we will delete it promptly.

---

## 12. Changes to this policy

We may update this policy to reflect changes in our practices or legal requirements. When we make material changes:

- We will update the "Effective date" and "Last reviewed" dates at the top of this page.
- We will notify active customers via the Service or email for material changes.
- Prior versions will be available in the repository's git history.

---

## 13. Contact us

For privacy-related questions, requests, or complaints:

- **Email:** `privacy@archlucid.net`
- **Security disclosures:** `security@archlucid.net` (see [`SECURITY.md`](../../SECURITY.md))

---

## Related documents

| Doc | Use |
|-----|-----|
| [PRIVACY_NOTE.md](../security/PRIVACY_NOTE.md) | Operator-facing GDPR Art. 30 processing activity records |
| [DPA_TEMPLATE.md](DPA_TEMPLATE.md) | Data Processing Agreement template |
| [SUBPROCESSORS.md](SUBPROCESSORS.md) | Current subprocessor list |
| [trust-center.md](../trust-center.md) | Trust Center index |
| [PII_EMAIL.md](../security/PII_EMAIL.md) | PII boundary for transactional email |
| [PII_RETENTION_CONVERSATIONS.md](../security/PII_RETENTION_CONVERSATIONS.md) | PII classification for conversation data |
| [AUDIT_RETENTION_POLICY.md](../library/AUDIT_RETENTION_POLICY.md) | Audit data retention tiers |

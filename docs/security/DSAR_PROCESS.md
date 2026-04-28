> **Scope:** GDPR Data Subject Access Request (DSAR) process — identifies PII storage locations and documents the manual fulfillment process.

# GDPR Data Subject Access Request (DSAR) process

**Audience:** DPOs, compliance officers, operators, and procurement teams who need to understand how ArchLucid handles GDPR data subject rights.

**Status:** V1 manual process. This document covers the data map, the fulfillment steps for each right, and known limitations.

**Not legal advice:** This document describes technical capabilities and operational processes. It does not constitute legal advice. Consult qualified counsel for jurisdiction-specific obligations.

---

## Related

- [DPA_TEMPLATE.md](../go-to-market/DPA_TEMPLATE.md) — Data Processing Agreement template
- [TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md) — buyer-facing trust index
- [AUDIT_RETENTION_POLICY.md](../library/AUDIT_RETENTION_POLICY.md) — audit data lifecycle
- [CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md) — data access patterns
- [MULTI_TENANT_RLS.md](MULTI_TENANT_RLS.md) — row-level security and tenant isolation

---

## 1. Personal data map

ArchLucid stores personal data in the following locations. "Personal data" means any information relating to an identified or identifiable natural person, per GDPR Article 4(1).

### 1.1 SQL Server tables containing PII

| Table | PII fields | Purpose | Retention |
|-------|-----------|---------|-----------|
| `dbo.AuditEvents` | `ActorUserId`, `ActorUserName` | Append-only audit trail for governance, compliance, and forensic review | Operator-managed (see [AUDIT_RETENTION_POLICY.md](../library/AUDIT_RETENTION_POLICY.md)); no automatic expiry |
| `dbo.TrialIdentityUsers` | `Email`, `DisplayName`, `UserId` | Local identity for trial/self-service users (when trial auth is configured) | Tenant lifecycle; purged on tenant hard-delete |
| `dbo.TenantRegistrations` | `ContactEmail`, `ContactName`, `OrganizationName` | Tenant onboarding and billing contact | Retained for commercial/legal record; removable on request |
| `dbo.MarketingPricingQuoteRequests` | `Email`, `Name`, `OrganizationName` | Pricing quote requests from the public marketing page | Sales follow-up; removable on request |
| `dbo.SentEmails` | `RecipientEmail` | Email delivery log for trial lifecycle and notifications | Operator-managed retention |
| `dbo.ScimUsers` | `UserName`, `DisplayName`, `ExternalId`, `Emails` (JSON) | SCIM 2.0 inbound provisioning from external IdP | Synced with IdP; removable on SCIM DELETE |
| `dbo.TenantNotificationChannelPreferences` | `Email` (where channel is email) | Per-tenant notification routing preferences | Tenant lifecycle |
| `dbo.TenantExecDigestPreferences` | `Email` | Executive digest delivery preferences | Tenant lifecycle |
| `dbo.GovernanceApprovalRequests` | `RequestedByUserId`, `ApprovedByUserId` | Governance workflow actors | Retained for audit integrity |
| `dbo.BillingWebhookEvents` | Indirect PII via Stripe/Marketplace payloads (customer email in metadata) | Billing event idempotency and reconciliation | Operator-managed |

### 1.2 Other storage locations

| Location | PII fields | Purpose |
|----------|-----------|---------|
| **Azure Blob Storage** (artifact exports) | Free-text PII in architecture briefs or documents uploaded by users | User-provided content within architecture requests |
| **Application logs** (structured ILogger) | `ActorUserId`, `CorrelationId`, IP addresses in HTTP request logs | Operational diagnostics |
| **Azure OpenAI** (outbound LLM calls) | Prompt content may contain user-provided names, identifiers, or architecture descriptions with PII | AI-assisted analysis; prompt redaction available via `LlmPromptRedaction` configuration |

---

## 2. Right of access (Article 15)

**What:** A data subject may request a copy of all personal data held about them.

**Fulfillment process:**

1. Verify the identity of the requester through the tenant administrator or the Controller's established process.
2. Query `dbo.AuditEvents` for rows where `ActorUserId` matches the data subject's user identifier:

```sql
SELECT EventId, OccurredUtc, EventType, ActorUserId, ActorUserName,
       TenantId, RunId, DataJson
FROM dbo.AuditEvents
WHERE ActorUserId = @SubjectUserId
ORDER BY OccurredUtc DESC;
```

3. Query `dbo.TrialIdentityUsers` (if trial auth is used) for the user's registration data.
4. Query `dbo.ScimUsers` (if SCIM provisioning is used) for the user's provisioned profile.
5. Export the results as JSON or CSV using the audit export API (`GET /v1/audit/export` with `Accept: text/csv` or `Accept: application/json`), filtered by `actorUserId` where supported, or via direct SQL query by an authorized operator.
6. Provide the exported data to the data subject in a commonly used, machine-readable format (JSON or CSV).

**Timeline:** Respond within 30 days per GDPR Article 12(3).

---

## 3. Right to rectification (Article 16)

**What:** A data subject may request correction of inaccurate personal data.

**Fulfillment process:**

1. For `dbo.TrialIdentityUsers`: update `Email`, `DisplayName` via the admin API or direct SQL update by an authorized operator.
2. For `dbo.ScimUsers`: trigger a SCIM PUT/PATCH from the external IdP, or update directly via `PUT /scim/v2/Users/{id}`.
3. For `dbo.TenantRegistrations`: update contact fields via the admin API or direct SQL.
4. For `dbo.AuditEvents`: **rectification is constrained.** The audit trail is append-only (`DENY UPDATE` enforced at SQL level). Correcting PII in audit events would require a privileged break-glass SQL operation that bypasses the `ArchLucidApp` role. Document the rectification request and the corrective action in a new audit event rather than modifying historical rows.

**Constraint:** Modifying append-only audit events conflicts with the integrity purpose of the audit trail. The recommended approach is to document the correction as a new audit entry referencing the original event, preserving both the historical record and the correction.

---

## 4. Right to erasure (Article 17)

**What:** A data subject may request deletion of their personal data ("right to be forgotten").

**Fulfillment process:**

| Data location | Erasure approach | Constraints |
|--------------|-----------------|-------------|
| `dbo.TrialIdentityUsers` | Delete the row via admin API or SQL | None; straightforward deletion |
| `dbo.ScimUsers` | SCIM DELETE via IdP or admin endpoint | None; IdP-driven lifecycle |
| `dbo.TenantRegistrations` | Anonymize contact fields (`ContactEmail` → redacted, `ContactName` → redacted) rather than delete, to preserve the tenant commercial record | Commercial record retention may apply |
| `dbo.MarketingPricingQuoteRequests` | Delete the row or anonymize email/name fields | None |
| `dbo.SentEmails` | Delete rows matching `RecipientEmail` | None |
| `dbo.AuditEvents` | **Cannot delete under normal operations.** `DENY DELETE` is enforced for the application principal. Erasure requires a **privileged break-glass operation** by `dbo`/`db_owner` | See below |

### Audit event erasure tension

`dbo.AuditEvents` is append-only by design (migration `051_AuditEvents_DenyUpdateDelete.sql`). This creates a tension between GDPR Article 17 (erasure) and the legitimate interest in maintaining audit integrity for governance, compliance, and legal obligations (Article 17(3)(b) and (e)).

**Recommended approach:**

1. **Pseudonymize rather than delete:** Replace `ActorUserId` and `ActorUserName` in affected audit rows with a pseudonymous identifier (e.g., `REDACTED-<hash>`) using a break-glass SQL operation under `dbo`/`db_owner`. This preserves the audit trail structure while removing direct identifiability.
2. **Document the legal basis:** Record in the DPA or processing records whether audit retention falls under a legal obligation or legitimate interest exemption.
3. **Execute under change control:** Any audit row modification must be logged separately (outside the modified table) and approved by the data controller.

```sql
-- Break-glass pseudonymization (run as dbo, not ArchLucidApp)
UPDATE dbo.AuditEvents
SET ActorUserId = 'REDACTED',
    ActorUserName = 'REDACTED'
WHERE ActorUserId = @SubjectUserId;
```

---

## 5. Right to data portability (Article 20)

**What:** A data subject may request their data in a structured, commonly used, machine-readable format.

**Fulfillment process:**

1. Export audit events for the data subject via `GET /v1/audit/export` with `Accept: application/json` (JSON) or `Accept: text/csv` (CSV).
2. For trial identity data, export from `dbo.TrialIdentityUsers` as JSON.
3. Provide the export to the data subject or transmit to another controller as directed.

**Formats supported:** JSON, CSV.

---

## 6. Processor obligations

Per the DPA template ([DPA_TEMPLATE.md](../go-to-market/DPA_TEMPLATE.md)):

- ArchLucid (Processor) processes personal data only on documented instructions from the Controller.
- ArchLucid assists the Controller in fulfilling DSAR obligations by providing the query and export capabilities described above.
- Subprocessors are listed in the subprocessors register referenced from the Trust Center.
- Data breach notification follows the process in the DPA (72-hour notification to Controller).

---

## 7. Contact

For DSAR inquiries or to initiate a request: **`security@archlucid.net`**

---

## 8. Change control

Update this document when:
- New tables containing PII are added
- Audit retention policy changes
- Erasure automation is implemented (replacing break-glass SQL)
- Subprocessor changes affect PII flows

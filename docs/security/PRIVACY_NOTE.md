> **Scope:** Operators and privacy or compliance reviewers; states controller-side processing activities and legal-basis framing for ArchLucid operational telemetry. Not legal advice, a full subprocessors inventory, tenant workload DPA terms, or a consumer-facing privacy policy.

> **Status:** **APPROVED — 2026-04-25.** All four owner sign-off items resolved. See §6 change history.

---

# ArchLucid privacy notice (operator-facing)

## 1. Objective

Document the personal-data processing activities that ArchLucid carries out as a **data controller** for tenant operator interactions with the SaaS surface. This file is the canonical reference for **GDPR Article 30** records of processing and the **Article 6(1)(f)** legitimate-interest balancing tests cited in shipping code.

## 2. Assumptions

- ArchLucid's primary commercial relationship is **B2B SaaS**: the data subject is the operator employee at a tenant organisation, not a consumer.
- Tenant-supplied **architecture artefacts** (manifests, findings, run narratives) are **customer workload content** and are governed by the master subscription agreement, not this notice.
- Tenant **identity contact data** (operator email, display name, organisation) is governed by [`docs/security/PII_EMAIL.md`](PII_EMAIL.md) for the transactional-email flow and by the relevant subprocessor agreements (Entra ID, Azure SQL).
- This notice covers **operational telemetry** that ArchLucid collects to operate, secure, and improve the SaaS product. It does not cover marketing tracking, advertising, or sales-CRM enrichment (none of which exist in V1).

## 3. Named processing activities

Each subsection follows the GDPR Art. 30 record shape: purpose, legal basis, categories of data subjects, categories of personal data, recipients, retention, safeguards.

### 3.A — First-tenant onboarding funnel (Improvement 12)

**Purpose.** Measure the time-to-first-finding for newly provisioned tenants so the marketing claim "first finding inside 30 minutes" can be substantiated with first-party evidence rather than anecdote. See [`docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](../QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md) §3 Improvement 12 for the assessment finding that motivated the work.

**Legal basis.** **Article 6(1)(f)** — legitimate interest of the controller (ArchLucid) in measuring product onboarding success without distorting the result by asking each tenant for opt-in consent at the moment of signup (which would itself depress the funnel and bias the measurement).

**Balancing test.** ArchLucid has concluded that its legitimate interest in measuring onboarding success is not overridden by the interests or fundamental rights of the data subjects, given that aggregated mode collects no personal data and per-tenant mode is scoped to organisation-level identifiers only, with no ability to single out an individual operator.

The activity is **default aggregated-only** (no per-tenant correlation in the funnel store). When the optional **per-tenant** flag (`Telemetry:FirstTenantFunnel:PerTenantEmission`) is **off** (the V1 default), the funnel cannot identify any individual operator or tenant — the rows are counts, nothing more. When the flag is **on** (owner-only decision; see [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 40), the funnel store records `tenantId` only — never `userId`, never IP address, never user-agent. The per-tenant mode is therefore a **tenant-organisation-level** record, not a personal-data record on any specific operator employee.

**Categories of data subjects.**
- **Aggregated mode (default):** none (counters only).
- **Per-tenant mode (flag-gated):** operator employees at tenant organisations, identified only by the **tenant they belong to** (not individually).

**Categories of personal data.**
- **Aggregated mode (default):** none.
- **Per-tenant mode (flag-gated):** `tenantId` (UUID), event name (one of six low-cardinality enum values), event timestamp (UTC, second-precision). **Explicitly excluded:** `userId`, IP address, user-agent, browser fingerprint, geo-location, organisation name, operator email.

**Recipients.**
- **Aggregated mode (default):** Application Insights (the same Azure Monitor workspace that already hosts ArchLucid operational telemetry — see [`docs/library/OBSERVABILITY.md`](../library/OBSERVABILITY.md)).
- **Per-tenant mode (flag-gated):** Application Insights (same as aggregated) **plus** the new SQL table `dbo.FirstTenantFunnelEvents` in the ArchLucid production database (RLS-scoped per tenant on read; see [`docs/security/MULTI_TENANT_RLS.md`](MULTI_TENANT_RLS.md)).

**Retention.**
- **Aggregated mode (default):** governed by the Application Insights workspace retention (currently the platform default; the audit-retention policy at [`docs/library/AUDIT_RETENTION_POLICY.md`](../library/AUDIT_RETENTION_POLICY.md) does not apply because no audit-event row is written).
- **Per-tenant mode (flag-gated):** **90 days.** Rows older than the retention horizon are pruned by a scheduled Azure SQL job; the deletion runbook lives in the cohort-ops folder.

**Safeguards.**
- The per-tenant flag defaults to **`FALSE`** in `appsettings.json` and in the production environment configuration; flipping it to `TRUE` requires an owner-only configuration change.
- The application-layer emitter (`ArchLucid.Application.Telemetry.FirstTenantFunnelEmitter`) reads the flag at every call so config changes take effect without restart.
- Unit tests assert that `tenantId` is **absent** from emitted payloads when the flag is `FALSE` (`ArchLucid.Application.Tests/Telemetry/FirstTenantFunnelEmitterTests.cs`).
- An integration test asserts that no row is written to `dbo.FirstTenantFunnelEvents` when the flag is `FALSE`.
- The Workbook dashboard (`infra/modules/first-tenant-funnel-dashboard/`) reads from the **aggregated** counters by default; per-tenant queries require an explicit author-mode edit by a cohort-ops role-holder and are not on the default landing page.

**Controller contact.** `privacy@archlucid.net`.

---

### 3.B — Trial lifecycle transactional email

**Purpose.** Deliver transactional email notifications to operator employees throughout the trial lifecycle: welcome on provisioning, approaching-run-limit warning, and trial expiry notice. These emails are a contractual obligation of the trial relationship and are required to operate the SaaS service safely (operators must be informed when limits are approaching).

**Legal basis.** **Article 6(1)(b)** — processing necessary for the performance of a contract to which the data subject's employer (the tenant) is party. The trial agreement requires ArchLucid to notify the operator contact of material trial events.

**Categories of data subjects.** Operator employees designated as the trial contact for their tenant organisation.

**Categories of personal data.**
- **Email address** (To field, resolved from the `TrialProvisioned` / `TenantSelfRegistered` audit event actor).
- **Tenant display name** (often a company name; used in email salutation and subject line).
- **Usage counts and dates** (run counts, trial expiry date, tier label). These are product-metadata fields; they do not include the contents of architecture runs, findings, or manifests.
- **Explicitly excluded from email bodies:** architecture artefacts, manifest JSON, finding text, run narratives, operator passwords, API keys.

**Recipients.**
- **Azure Communication Services (ACS)** — preferred production transport with private networking and managed identity. SMTP is available for development environments only and must not be used in production.
- `dbo.SentEmails` in the ArchLucid production database stores **idempotency keys only** — email bodies are not persisted, which limits the blast radius of a database breach.

**Retention.**
- Email addresses in the audit store are governed by the audit-retention policy at [`docs/library/AUDIT_RETENTION_POLICY.md`](../library/AUDIT_RETENTION_POLICY.md).
- `dbo.SentEmails` idempotency rows are pruned on the same schedule as the owning tenant record (cascade delete on tenant removal).
- Email bodies are not stored; no separate body-level retention applies.

**Safeguards.**
- All email templates default to `MetadataOnly` content mode (counts, dates, tier labels). A future `TenantEmailContentMode` column would be required to expand content, and only after further legal review — see [`docs/security/PII_EMAIL.md`](PII_EMAIL.md).
- Links in email bodies are opaque HTTPS paths; no JWTs or session tokens are embedded in URLs.
- Full email bodies are never logged at `Information` level in production; only template IDs and byte sizes are logged.
- The email lookup interface (`ITenantTrialEmailContactLookup`) is the sole read path for the email address; it is not passed to any analytics, CRM, or marketing system.

**Controller contact.** `privacy@archlucid.net`.

---

### 3.C — Operator-shell client-error telemetry

**Purpose.** Detect and diagnose JavaScript errors occurring in the operator shell browser application so that ArchLucid can maintain service reliability and identify regressions before they affect a broad operator population. No user-generated content is captured; only technical diagnostic signals are collected.

**Legal basis.** **Article 6(1)(f)** — legitimate interest of the controller (ArchLucid) in maintaining the quality, stability, and security of the SaaS product delivered to paying tenants.

**Balancing test.** The data collected is technical in nature (error messages, stack traces, URL pathnames, user-agent strings) and is processed solely for service reliability purposes. It is not used for profiling, advertising, or any purpose beyond diagnosing and resolving errors. The `LogSanitizer` is applied to every field before logging, preventing inadvertent capture of free-text user input. The interest in reliable service delivery is not overridden by the data subjects' interests.

**Categories of data subjects.** Operator employees using the operator shell browser application at the time a client-side JavaScript error occurs.

**Categories of personal data.**
- **User-agent string** (truncated to a platform-defined maximum length; identifies browser and OS family, not the individual).
- **URL pathname** (the operator shell route at the time of the error; truncated; does not include query parameters or fragments that might carry session tokens).
- **Error message and stack trace** (application-layer JavaScript error text; truncated; sanitized via `LogSanitizer` before logging).
- **Client-reported timestamp** (UTC string; truncated to 64 characters).
- **Explicitly excluded:** `userId`, operator email, IP address, architecture artefact content, session tokens, request bodies.

**Recipients.** Application Insights (Azure Monitor workspace) via `ILogger<ClientErrorTelemetryController>` structured logging at `Warning` level. No SQL row is written; no separate store is created for client-error events.

**Retention.** Governed by the Application Insights workspace retention policy (currently the platform default). The audit-retention policy at [`docs/library/AUDIT_RETENTION_POLICY.md`](../library/AUDIT_RETENTION_POLICY.md) does not apply because no audit-event row is written.

**Safeguards.**
- `LogSanitizer.Sanitize(…)` is applied to every logged field before the `LogWarning` call (`ClientErrorTelemetryController.cs` line 154–158).
- All inbound fields are truncated to platform-defined maximum lengths (`ClientErrorTelemetryIngestLimits`) before logging.
- The endpoint requires a valid operator session (authenticated request); unauthenticated callers cannot POST to the endpoint.
- Context key/value pairs are bounded to a maximum count and character length, preventing log-injection via large context maps.
- Logging is conditional on `_logger.IsEnabled(LogLevel.Warning)`, respecting any environment-level log-level configuration.

**Controller contact.** `privacy@archlucid.net`.

---

## 4. Subject rights

Operators (or their data-controller employer) may request:

- **Access** — for per-tenant funnel mode rows (§3.A), the tenant administrator may export the relevant `dbo.FirstTenantFunnelEvents` rows via the same RLS-scoped read path used by the operator shell (forthcoming admin endpoint; not in V1). For transactional email (§3.B), the audit store holds the email address; access requests are fulfilled via the standard audit-data export path. For client-error telemetry (§3.C), Application Insights workspace access is restricted to ArchLucid operations staff; data subjects may request confirmation of what was logged about a specific session.
- **Erasure** — the per-tenant funnel 90-day retention satisfies erasure on automatic schedule; out-of-cycle erasure for a single tenant is supported via the existing tenant-deletion path. For transactional email, tenant deletion cascades to `dbo.SentEmails`. For client-error telemetry, Application Insights data is purged on workspace retention schedule; individual-record purge is available via the Azure Monitor purge API.
- **Objection** — for §3.A per-tenant mode, the tenant administrator may object by leaving the feature flag at `FALSE` (the V1 default). For §3.B and §3.C, the processing is necessary for contract performance and legitimate service operation respectively; objection would require ceasing use of the service.

## 5. Subprocessor map (this notice's scope only)

| Subprocessor | Purpose | Data flowing |
|---|---|---|
| **Microsoft Azure** (Application Insights) | Aggregated funnel counters; client-error telemetry | §3.A counter increments (no `tenantId` in aggregated mode); §3.C error messages, pathnames, user-agent strings (sanitized, truncated). |
| **Microsoft Azure** (SQL Database) | Per-tenant funnel rows (flag-gated); email idempotency keys | §3.A: `tenantId`, event name, timestamp (RLS-scoped). §3.B: idempotency keys only, no email bodies. |
| **Microsoft Azure** (Azure Communication Services) | Trial lifecycle transactional email delivery | §3.B: email address (To), email body (metadata-only; not persisted by ACS after delivery). |

The full ArchLucid subprocessor list lives in the Trust Center (`docs/trust-center.md`) and is not duplicated here.

## 6. Change history

| Date | Change | Owner sign-off |
|---|---|---|
| 2026-04-24 | DRAFT — file created with §3.A funnel activity. §3.B + §3.C placeholders left for owner direction. | **PENDING** |
| 2026-04-25 | Decision 1: controller contact confirmed as `privacy@archlucid.net`. Decision 2: balancing-test conclusion for §3.A approved. Decision 3: 90-day retention for `dbo.FirstTenantFunnelEvents` confirmed. Decision 4: §3.B and §3.C drafted in full and approved for inclusion. Status promoted from DRAFT to APPROVED. | **APPROVED** |

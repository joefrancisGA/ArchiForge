> **Status:** **DRAFT — OWNER REVIEW PENDING.** This file was created on 2026-04-24 as part of Improvement 12 (first-tenant onboarding telemetry funnel) because the assistant needed to record the funnel as a named processing activity and no prior privacy notice existed. Every section below is owner-reviewable; the **First-tenant onboarding funnel** activity is the only one currently relied on by shipping code (and only under the **aggregated-only** default — per-tenant emission stays off until the owner explicitly flips it).
>
> **What needs owner sign-off before this file stops being marked DRAFT:**
>
> 1. The data-controller name + contact (placeholders below).
> 2. The legitimate-interest balancing test wording for §3.A (the funnel activity).
> 3. The retention period for `dbo.FirstTenantFunnelEvents` (placeholder: 90 days).
> 4. Whether to add §3.B and §3.C activities for the **trial lifecycle email** flow already shipped (`docs/security/PII_EMAIL.md`) and the **client-error telemetry** flow already shipped (`ArchLucid.Api/Controllers/Admin/ClientErrorTelemetryController.cs`). The assistant intentionally did **not** add those without owner direction — see "Stop-and-ask boundary log" at the bottom.

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

**Balancing test (DRAFT — owner review pending).** The activity is **default aggregated-only** (no per-tenant correlation in the funnel store). When the optional **per-tenant** flag (`Telemetry:FirstTenantFunnel:PerTenantEmission`) is **off** (the V1 default), the funnel cannot identify any individual operator or tenant — the rows are counts, nothing more. When the flag is **on** (owner-only decision; see [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 40), the funnel store records `tenantId` only — never `userId`, never IP address, never user-agent. The per-tenant mode is therefore a **tenant-organisation-level** record, not a personal-data record on any specific operator employee. The owner's pending question 40 sub-decision is whether tenant-level correlation rises to a privacy-notice line-item in customer comms; this draft assumes the answer is "yes — it must be in the public privacy notice before the flag flips on for any production tenant".

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
- **Per-tenant mode (flag-gated):** **90 days** (DRAFT — owner review pending). Rows older than the retention horizon are pruned by a scheduled Azure SQL job; the deletion runbook lives in the cohort-ops folder.

**Safeguards.**
- The per-tenant flag defaults to **`FALSE`** in `appsettings.json` and in the production environment configuration; flipping it to `TRUE` requires an owner-only configuration change.
- The application-layer emitter (`ArchLucid.Application.Telemetry.FirstTenantFunnelEmitter`) reads the flag at every call so config changes take effect without restart.
- Unit tests assert that `tenantId` is **absent** from emitted payloads when the flag is `FALSE` (`ArchLucid.Application.Tests/Telemetry/FirstTenantFunnelEmitterTests.cs`).
- An integration test asserts that no row is written to `dbo.FirstTenantFunnelEvents` when the flag is `FALSE`.
- The Workbook dashboard (`infra/modules/first-tenant-funnel-dashboard/`) reads from the **aggregated** counters by default; per-tenant queries require an explicit author-mode edit by a cohort-ops role-holder and are not on the default landing page.

**Controller contact.** *(placeholder — owner provides before publication)* `privacy@archlucid.com`.

### 3.B — Trial lifecycle transactional email *(placeholder — see [`docs/security/PII_EMAIL.md`](PII_EMAIL.md))*

> **DRAFT — pending owner sign-off.** The trial transactional email flow is already shipped and already has a PII boundary doc; it should also have a privacy-notice line-item under this notice. Assistant did **not** draft the activity record here without owner direction. See "Stop-and-ask boundary log" at the bottom.

### 3.C — Operator-shell client-error telemetry *(placeholder — see `ArchLucid.Api/Controllers/Admin/ClientErrorTelemetryController.cs`)*

> **DRAFT — pending owner sign-off.** The client-error telemetry endpoint is already shipped and writes structured warnings. It should have a privacy-notice line-item. Assistant did **not** draft the activity record here without owner direction. See "Stop-and-ask boundary log" at the bottom.

## 4. Subject rights

Operators (or their data-controller employer) may request:

- **Access** — for per-tenant mode rows, the tenant administrator may export the relevant `dbo.FirstTenantFunnelEvents` rows via the same RLS-scoped read path used by the operator shell (forthcoming admin endpoint; not in V1).
- **Erasure** — the per-tenant retention default of 90 days satisfies erasure on automatic schedule. Out-of-cycle erasure for a single tenant is supported via the existing tenant-deletion path (cascades to the new table by `tenantId`).
- **Objection** — the tenant administrator may object to per-tenant mode by leaving the feature flag at `FALSE` (the V1 default). For aggregated mode, no individual data subject can be identified, so the right of objection does not attach to any personal data.

## 5. Subprocessor map (this notice's scope only)

| Subprocessor | Purpose | Data flowing |
|---|---|---|
| **Microsoft Azure** (Application Insights) | Aggregated funnel counters | Counter increments; no `tenantId` in aggregated mode. |
| **Microsoft Azure** (SQL Database) | Per-tenant funnel rows (flag-gated) | `tenantId`, event name, event timestamp. RLS-scoped per tenant. |

The full ArchLucid subprocessor list lives in the Trust Center (`docs/trust-center.md`) and is not duplicated here.

## 6. Change history

| Date | Change | Owner sign-off |
|---|---|---|
| 2026-04-24 | DRAFT — file created with §3.A funnel activity. §3.B + §3.C placeholders left for owner direction. | **PENDING** |

## Stop-and-ask boundary log

This file was created in response to a Cursor stop-and-ask boundary in **Prompt 12** (`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md` lines 583–643): step 7 said *"Stop-and-ask if the existing privacy notice text doesn't already cover the activity shape."* The repository did not contain `docs/security/PRIVACY_NOTE.md` at all. The owner explicitly chose **option 2 — create a draft** in the same session: ship the funnel activity record now, mark the file `DRAFT — OWNER REVIEW PENDING`, and surface §3.B / §3.C as owner follow-ups rather than drafting them speculatively.

The assistant treats the following as **out of scope** for this draft and explicitly declined to write them without owner direction:

1. Privacy-notice line-items for already-shipped flows the assistant did not author (transactional email, client-error telemetry, RAG retrieval, agent execution traces). Each of those has its own existing security doc; promoting them to the privacy notice is an owner decision because the wording has external-facing legal weight.
2. The legitimate-interest balancing-test conclusion text in §3.A. The assistant drafted the structure and the relevant guardrails (default off, no `userId`, no IP, RLS scope, 90-day retention placeholder); the actual balancing-test conclusion is a controller decision.
3. The retention period for per-tenant mode. 90 days is a placeholder — owner picks the actual value at sign-off.
4. Any reference to **specific named subprocessors** beyond Microsoft Azure (which is unavoidable because the platform is Azure-native).

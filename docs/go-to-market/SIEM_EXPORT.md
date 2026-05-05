> **Scope:** ArchLucid — Audit log export for SIEM integration (buyer summary); full payload examples and KQL live in the library SIEM guide linked below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Audit log export for SIEM integration

**Audience:** Security engineers and SOC teams evaluating ArchLucid's audit data for SIEM ingestion.

**Last reviewed:** 2026-05-05

**Technical detail:** Copy-paste **Splunk HEC** and **Microsoft Sentinel / Log Analytics** JSON mappings for **audit rows** (`AuditEvent`) are in **[`../library/SIEM_EXPORT.md`](../library/SIEM_EXPORT.md)** (§4).

---

## 1. What is exported

ArchLucid maintains a **durable, append-only audit trail** in SQL (`dbo.AuditEvents`) with a typed event catalog. The catalog currently contains **81 event types** (CI-tracked; see [../library/AUDIT_COVERAGE_MATRIX.md](../library/AUDIT_COVERAGE_MATRIX.md)).

Each audit event includes:

| Field | Description |
|-------|-------------|
| `eventType` | Typed string from `AuditEventTypes` catalog (e.g., `RunStarted`, `GovernanceApprovalSubmitted`) |
| `occurredUtc` | UTC timestamp of the event |
| `tenantId` | Tenant scope |
| `workspaceId` | Workspace scope |
| `projectId` | Project scope (where applicable) |
| `correlationId` | Request correlation ID (aligns with `X-Correlation-ID` header) |
| `actorUserId` / `actorUserName` | Identity of the user or service principal that triggered the event |
| `dataJson` | JSON **string** with event-specific details (parse in SIEM or forwarder — see library guide) |

---

## 2. Export methods available today

| Method | Delivery | Format | Access required | Latency |
|--------|----------|--------|-----------------|---------|
| **JSON / CSV / CEF export** | Pull via `GET /v1/audit/export` | JSON array, CSV, or CEF lines | **Auditor** or **Admin** role; Enterprise tier | On-demand |
| **Integration events** | Push (Azure Service Bus topic) | Canonical `com.archlucid.*` JSON (subset of lifecycle; not full audit mirror) | Service Bus configuration | Near real-time |

---

## 3. SIEM integration patterns

### Splunk

1. Pull or forward **audit export** rows into your pipeline; map each row to Splunk **HTTP Event Collector (HEC)** — see [library §4.1](../library/SIEM_EXPORT.md#41-splunk-http-event-collector-hec).
2. Create a Splunk **sourcetype** for ArchLucid audit JSON.
3. Build dashboards and alerts on `eventType`, actor fields, and `correlationId`.

### Microsoft Sentinel

1. Ingest normalized rows into a **Log Analytics custom table** (DCR / ingestion API) — see [library §4.2](../library/SIEM_EXPORT.md#42-microsoft-sentinel--log-analytics-custom-log-row).
2. Create **analytics rules** on ArchLucid event types (e.g., `GovernanceApprovalRejected`).

### Generic SIEM (scheduled pull)

1. Schedule a **cron job** or **Azure Logic App** to call `GET /v1/audit/export` periodically (respecting the documented window limits).
2. Ingest the CSV or JSON into the SIEM's file-based or HTTP import pipeline.
3. Map fields to the SIEM schema.

---

## 4. Retention

- **In ArchLucid:** Audit events are retained until archived or deleted by operator workflows. Default posture is **keep indefinitely** — see [../library/SECURITY.md](../library/SECURITY.md) (PII and retention).
- **In your SIEM:** Apply your organization's log retention policy. ArchLucid events may contain tenant-scoped operational data; see [DPA_TEMPLATE.md](DPA_TEMPLATE.md) for data handling terms.

---

## 5. Roadmap

| Capability | Status |
|------------|--------|
| **CEF** export (`format=cef` on `GET /v1/audit/export`) | Available |
| Native **syslog** output (RFC 5424) | [Planned] |
| Dedicated SIEM connector (Splunk, Sentinel) | [Planned] |
| Streaming export API (WebSocket / SSE) | Under consideration |

---

## Related documents

| Doc | Use |
|-----|-----|
| [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md) | Full integration catalog |
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [../library/SIEM_EXPORT.md](../library/SIEM_EXPORT.md) | **Splunk HEC + Sentinel JSON examples**, `dataJson` / API field names |
| [../library/AUDIT_COVERAGE_MATRIX.md](../library/AUDIT_COVERAGE_MATRIX.md) | 81 typed events, coverage detail |
| [../library/SECURITY.md](../library/SECURITY.md) | Audit, PII, retention |

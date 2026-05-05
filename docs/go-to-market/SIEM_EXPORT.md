> **Scope:** ArchLucid — Audit log export for SIEM integration - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Audit log export for SIEM integration

**Audience:** Security engineers and SOC teams evaluating ArchLucid's audit data for SIEM ingestion.

**Last reviewed:** 2026-04-15

---

## 1. What is exported

ArchLucid maintains a **durable, append-only audit trail** in SQL (`dbo.AuditEvents`) with a typed event catalog. The catalog currently contains **81 event types** (CI-tracked; see [../AUDIT_COVERAGE_MATRIX.md](../library/AUDIT_COVERAGE_MATRIX.md)).

Each audit event includes:

| Field | Description |
|-------|-------------|
| `eventType` | Typed string from `AuditEventTypes` catalog (e.g., `RunStarted`, `GovernanceApprovalSubmitted`) |
| `occurredUtc` | UTC timestamp of the event |
| `tenantId` | Tenant scope |
| `workspaceId` | Workspace scope |
| `projectId` | Project scope (where applicable) |
| `correlationId` | Request correlation ID (aligns with `X-Correlation-ID` header) |
| `actor` | Identity of the user or service principal that triggered the event |
| `payload` | JSON object with event-specific details |

---

## 2. Export methods available today

| Method | Delivery | Format | Access required | Latency |
|--------|----------|--------|-----------------|---------|
| **CSV export** | Pull via `GET /v1/audit/export` | CSV | **Auditor** or **Admin** role | On-demand |
| **CloudEvents webhook** | Push (HTTP POST to configured endpoint) | CloudEvents JSON envelope | Webhook configuration (Admin) | Near real-time |
| **Service Bus** | Push (Azure Service Bus topic) | JSON message | Service Bus configuration (Admin) | Near real-time |

---

## 3. SIEM integration patterns

### Splunk

1. Configure ArchLucid **CloudEvents webhook** to POST to Splunk's **HTTP Event Collector (HEC)** endpoint.
2. Create a Splunk **source type** for ArchLucid events (JSON, key fields mapped).
3. Build Splunk dashboards and alerts on `eventType`, `actor`, and `correlationId`.

**Example HEC event (raw CloudEvents JSON embedded in `event` as a string — common when forwarding the exact ArchLucid POST body):**

```json
{
  "time": 1746120000,
  "host": "archlucid-audit-forwarder",
  "source": "archlucid:audit",
  "sourcetype": "archlucid:cloudevents",
  "event": "{\"specversion\":\"1.0\",\"type\":\"com.archlucid.governance.approval.submitted\",\"source\":\"/archlucid/tenant/11111111-1111-1111-1111-111111111111\",\"id\":\"a0d3c4d2-5c2b-4c2b-9c2b-000000000001\",\"time\":\"2026-05-01T12:00:00Z\",\"datacontenttype\":\"application/json\",\"data\":{\"schemaVersion\":1,\"approvalRequestId\":\"AR-1001\",\"runId\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}}"
}
```

### Microsoft Sentinel

1. Configure ArchLucid **Service Bus** integration to publish to a dedicated topic.
2. Deploy an **Azure Function** (Service Bus trigger → Log Analytics workspace via Data Collector API).
3. Create Sentinel **analytics rules** on ArchLucid event types (e.g., alert on `GovernanceApprovalRejected`).

**Example custom-table row (JSON Lines) after your forwarder unwraps the Service Bus message body:**

```json
{
  "TimeGenerated": "2026-05-01T12:00:00Z",
  "ArchLucidEventType": "com.archlucid.authority.run.completed",
  "TenantId": "11111111-1111-1111-1111-111111111111",
  "WorkspaceId": "22222222-2222-2222-2222-222222222222",
  "ProjectId": "33333333-3333-3333-3333-333333333333",
  "RunId": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "Payload": { "schemaVersion": 1, "manifestId": "bbbbbbbb-bbbb-4bbb-bbbb-bbbbbbbbbbbb" }
}
```

### Generic SIEM (scheduled pull)

1. Schedule a **cron job** or **Azure Logic App** to call `GET /v1/audit/export` periodically (e.g., every 15 minutes).
2. Ingest the CSV into the SIEM's file-based import pipeline.
3. Map CSV columns to SIEM schema.

---

## 4. Retention

- **In ArchLucid:** Audit events are retained until archived or deleted by operator workflows. Default posture is **keep indefinitely** — see [../SECURITY.md](../library/SECURITY.md) (PII and retention).
- **In your SIEM:** Apply your organization's log retention policy. ArchLucid events may contain tenant-scoped operational data; see [DPA_TEMPLATE.md](DPA_TEMPLATE.md) for data handling terms.

---

## 5. Roadmap

| Capability | Status |
|------------|--------|
| Native **CEF** (Common Event Format) output | [Planned] |
| Native **syslog** output (RFC 5424) | [Planned] |
| Dedicated SIEM connector (Splunk, Sentinel) | [Planned] |
| Streaming export API (WebSocket / SSE) | Under consideration |

---

## Related documents

| Doc | Use |
|-----|-----|
| [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md) | Full integration catalog |
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [../AUDIT_COVERAGE_MATRIX.md](../library/AUDIT_COVERAGE_MATRIX.md) | 81 typed events, coverage detail |
| [../SECURITY.md](../library/SECURITY.md) | Audit, PII, retention |

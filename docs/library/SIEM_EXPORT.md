> **Scope:** ArchLucid — Audit log export for SIEM integration - full detail, payload examples for Splunk HEC and Microsoft Sentinel, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

> **Buyer-facing index:** [../go-to-market/SIEM_EXPORT.md](../go-to-market/SIEM_EXPORT.md) (short summary and trust links).


# ArchLucid — Audit log export for SIEM integration

**Audience:** Security engineers and SOC teams evaluating ArchLucid's audit data for SIEM ingestion.

**Last reviewed:** 2026-05-05

---

## 1. What is exported

ArchLucid maintains a **durable, append-only audit trail** in SQL (`dbo.AuditEvents`) with a typed event catalog. The catalog currently contains **81 event types** (CI-tracked; see [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md)).

Each audit event includes:

| Field | Description |
|-------|-------------|
| `eventType` | Typed string from `AuditEventTypes` catalog (e.g., `RunStarted`, `GovernanceApprovalSubmitted`) |
| `occurredUtc` | UTC timestamp of the event |
| `tenantId` | Tenant scope |
| `workspaceId` | Workspace scope |
| `projectId` | Project scope (where applicable) |
| `correlationId` | Request correlation ID (aligns with `X-Correlation-ID` header when present) |
| `actorUserId` / `actorUserName` | Identity of the user or service principal that triggered the event |
| `dataJson` | JSON **string** with event-specific details (parse in the SIEM or forwarder) |
| `runId`, `manifestId`, `artifactId` | Optional scope references |

JSON returned from the HTTP API uses **camelCase** property names (`eventId`, `occurredUtc`, `eventType`, …) per default ASP.NET Core serialization.

---

## 2. Export methods available today

| Method | Delivery | Format | Access required | Latency |
|--------|----------|--------|-----------------|---------|
| **JSON / CSV / CEF export** | Pull via `GET /v1/audit/export` | Array of `AuditEvent` (JSON), CSV, or CEF lines | **Auditor** or **Admin** role; Enterprise tier | On-demand |
| **Integration events (Service Bus)** | Push (Azure Service Bus topic) | Canonical `com.archlucid.*` payloads — **not** the full audit row shape | Service Bus configuration | Near real-time (subset of lifecycle events) |

Near-real-time **HTTP push of every audit row** is not a separate first-class product channel today; operators typically **poll export**, use **Azure Monitor / diagnostic pipelines** on their side, or forward **Service Bus** integration events where those overlap with audit-worthy actions. The JSON examples below model the **audit row** (`AuditEvent`) as it appears in export and API listings, wrapped for each SIEM collector.

---

## 3. SIEM integration patterns (summary)

### Splunk

1. Schedule a forwarder (Logic App, Azure Function, Universal Forwarder wrapper, or enterprise integration) to pull `GET /v1/audit/export` or stream from your own queue that receives normalized audit rows.
2. Map each row to Splunk **HTTP Event Collector (HEC)** `event` objects (see §4.1).
3. Build dashboards on `eventType`, actor fields, and `correlationId`.

### Microsoft Sentinel

1. Ingest JSON Lines or batch JSON into a **custom table** (Log Analytics workspace) via **Data Collection Rule** / **Azure Monitor Agent**, or the **Logs ingestion API**, after normalizing export rows (see §4.2).
2. Optionally parse `DataJson` / `dataJson` in KQL (`parse_json`) for analytics.

### Generic SIEM (scheduled pull)

1. Poll `GET /v1/audit/export` on a cadence respecting the 90-day max window per request.
2. Map columns or JSON properties to the SIEM common schema.

---

## 4. Copy-paste payload examples (audit row → collector)

These examples use one representative **durable audit** event: `GovernanceApprovalSubmitted`. Replace UUIDs and payload text with values from your tenant.

The inner **`data`** object (CloudEvents) matches the **`AuditEvent`** shape returned by `GET /v1/audit/export?format=json` and `GET /v1/audit` (camelCase). The CloudEvents envelope **`type`** value `com.archlucid.audit.event` is a **recommended forwarder convention** for routing; it is not emitted verbatim by ArchLucid today unless your integration sets it.

### 4.1 Splunk HTTP Event Collector (HEC)

HEC JSON can use an **`event`** object (preferred for structured extraction). Map **`time`** from `occurredUtc` (epoch seconds). The following is a single event suitable for `POST /services/collector/event` (token in `Authorization: Splunk <token>`):

```json
{
  "time": 1746455400,
  "host": "archlucid-audit-forwarder",
  "source": "archlucid:audit",
  "sourcetype": "archlucid:audit:cloudevents",
  "event": {
    "specversion": "1.0",
    "type": "com.archlucid.audit.event",
    "source": "/archlucid/tenant/11111111-1111-1111-1111-111111111111/audit",
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "time": "2026-05-05T15:30:00Z",
    "datacontenttype": "application/json",
    "data": {
      "eventId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "occurredUtc": "2026-05-05T15:30:00.0000000Z",
      "eventType": "GovernanceApprovalSubmitted",
      "actorUserId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "actorUserName": "jdoe@contoso.com",
      "tenantId": "11111111-1111-1111-1111-111111111111",
      "workspaceId": "22222222-2222-2222-2222-222222222222",
      "projectId": "33333333-3333-3333-3333-333333333333",
      "runId": "44444444-4444-4444-4444-444444444444",
      "manifestId": "55555555-5555-5555-5555-555555555555",
      "artifactId": null,
      "dataJson": "{\"approvalRequestId\":\"AR-1001\",\"sourceEnvironment\":\"dev\",\"targetEnvironment\":\"prod\",\"requestedBy\":\"jdoe@contoso.com\"}",
      "correlationId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
    }
  }
}
```

If your collector only accepts **`event` as a string** (legacy pattern), stringify the same `event` object in one line and assign it to `"event": "<escaped-json-string>"`.

---

### 4.2 Microsoft Sentinel / Log Analytics custom log row

Custom ingestion typically expects **flattened** columns plus a **`TimeGenerated`** field (often forced or matched to column type `datetime`). Align **`TimeGenerated`** with **`occurredUtc`**. Preserve **`dataJson`** as a string for fidelity, or add a second column with parsed JSON in your forwarder.

Example **single record** (one element of a batch array your pipeline sends to the **Logs ingestion API** or writes as one JSON Lines row):

```json
{
  "TimeGenerated": "2026-05-05T15:30:00Z",
  "ArchLucidEventId_g": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "ArchLucidEventType_s": "GovernanceApprovalSubmitted",
  "TenantId_g": "11111111-1111-1111-1111-111111111111",
  "WorkspaceId_g": "22222222-2222-2222-2222-222222222222",
  "ProjectId_g": "33333333-3333-3333-3333-333333333333",
  "ActorUserId_s": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "ActorUserName_s": "jdoe@contoso.com",
  "RunId_g": "44444444-4444-4444-4444-444444444444",
  "ManifestId_g": "55555555-5555-5555-5555-555555555555",
  "CorrelationId_s": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "DataJson_s": "{\"approvalRequestId\":\"AR-1001\",\"sourceEnvironment\":\"dev\",\"targetEnvironment\":\"prod\",\"requestedBy\":\"jdoe@contoso.com\"}"
}
```

**Column suffix note:** Log Analytics often appends type suffixes (`_s`, `_g`, `_d`, `_t`) when creating a custom table from JSON; your Data Collection Rule transform may rename them to cleaner schema names. KQL example after ingestion:

```kusto
ArchLucidAudit_CL
| extend Payload = parse_json(DataJson_s)
| project TimeGenerated, ArchLucidEventType_s, TenantId_g, Payload.approvalRequestId
```

---

## 5. Retention

- **In ArchLucid:** Audit events are retained until archived or deleted by operator workflows. Default posture is **keep indefinitely** — see [SECURITY.md](SECURITY.md) (PII and retention).
- **In your SIEM:** Apply your organization's log retention policy. ArchLucid events may contain tenant-scoped operational data; see [../go-to-market/DPA_TEMPLATE.md](../go-to-market/DPA_TEMPLATE.md) for data handling terms.

---

## 6. Roadmap

| Capability | Status |
|------------|--------|
| Native **CEF** (Common Event Format) output (export) | Supported via `format=cef` on `GET /v1/audit/export` for ArcSight `.cef` lines |
| Native **syslog** output (RFC 5424) | [Planned] (see go-to-market summary) |
| Dedicated SIEM connector (Splunk, Sentinel) | [Planned] |
| Streaming export API (WebSocket / SSE) | Under consideration |

---

## Related documents

| Doc | Use |
|-----|-----|
| [../go-to-market/INTEGRATION_CATALOG.md](../go-to-market/INTEGRATION_CATALOG.md) | Full integration catalog |
| [../go-to-market/TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md) | Trust index |
| [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) | Typed audit events, coverage detail |
| [SECURITY.md](SECURITY.md) | Audit, PII, retention |
| [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md) | CloudEvents on digests/alerts + **integration** event SIEM patterns |

# ArchLucid → ServiceNow: Webhook Bridge Recipe (V1)

> **Customer-operated bridge.** A **first-party ServiceNow connector** is **in scope for V1 GA** per [V1_SCOPE.md](../../../docs/library/V1_SCOPE.md) §2.13 (store listing may trail shipping). This file is **not** that connector — it is a **customer-operated** webhook/Table API pattern you may use before or instead of the hosted connector.

> **CMDB note:** The first-party connector will set incident **`cmdb_ci`** via **`cmdb_ci_appl`** lookup on architecture **`SystemName`** (see [INTEGRATION_CATALOG.md](../../../docs/go-to-market/INTEGRATION_CATALOG.md) **Sequencing and CMDB**). **This recipe does not** populate **`cmdb_ci`**.

**Contracts:** [catalog.json](../../../schemas/integration-events/catalog.json) · [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md)  
**Event catalog (code):** [ArchLucid.Core/Integration/IntegrationEventTypes.cs](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs)

**V1 event note:** There is **no** Finding-family integration type (`finding.*` under the `com.archlucid` namespace) in the catalog. For **findings → incident**, use **`com.archlucid.authority.run.completed`** and then **GET** run detail from ArchLucid; for **alerts → incident**, use **`com.archlucid.alert.fired`** in one step (same as the historical short template below).

---

## 1. Objective

Deliver **ServiceNow `incident`** rows from ArchLucid using:

- **Inbound:** CloudEvents-wrapped HTTP webhooks (or Service Bus → your worker with the same JSON `data` shape), and  
- **Outbound:** ServiceNow [Table API](https://www.servicenow.com/docs/bundle/utah-api-reference/page/integrate/inbound-rest/concept/c_TableAPI.html) `POST /api/now/table/incident`.

---

## 2. Assumptions

- ServiceNow instance URL, integration user (Basic) or OAuth client, and **incident** table create rights.  
- Bridge endpoint is **HTTPS**; secrets in Key Vault or equivalent.  
- ArchLucid HMAC secret matches `WebhookDelivery:HmacSha256SharedSecret` when the platform signs the POST body.  
- For the **findings** path, you can authenticate to ArchLucid **GET** `/v1/authority/runs/{runId}` (see OpenAPI).

---

## 3. Constraints

- **One-way** create in this recipe (no bidirectional sync). Do **not** assume **`cmdb_ci`** is set here; follow the first-party **`cmdb_ci_appl`** lookup contract in [INTEGRATION_CATALOG.md](../../../docs/go-to-market/INTEGRATION_CATALOG.md) if you add CMDB yourself.  
- **Do not** widen ArchLucid rate limits — **throttle** outbound ServiceNow calls.  
- **PII / scope IDs:** put only what policy allows in `description` / `work_notes`.

---

## 4. Architecture Overview

1. Receive **POST** (raw bytes) → verify **`X-ArchLucid-Webhook-Signature`** = `sha256=` + HMAC-SHA256(secret, body).  
2. Parse CloudEvents JSON; branch on `type` (`com.archlucid.authority.run.completed` | `com.archlucid.alert.fired`).  
3. For **run.completed**: optionally **GET** ArchLucid run → map findings (or severities) → one or more **incident** POSTs.  
4. For **alert.fired**: map `data` → single **incident** POST.  
5. Use **correlation** (`correlation_id` or `u_archlucid_dedupe`) for deduplication.

---

## 5. Component Breakdown

| Component | Role |
|-----------|------|
| **ArchLucid** | Publishes signed webhooks or Service Bus events. |
| **Bridge** | HMAC verify, transform, idempotency, ServiceNow Table API. |
| **ServiceNow** | System of record for incidents. |

---

## 6. Data Flow

```text
ArchLucid --(POST HMAC CloudEvents)--> Bridge --(GET, JWT/API key)--> ArchLucid API  (findings path)
   |
   +--------------------(POST /api/now/table/incident)-------------------> ServiceNow
```

---

## 7. Security Model

| Topic | Practice |
|-------|----------|
| **Webhook** | HMAC; constant-time compare; reject large bodies at edge. |
| **ServiceNow** | Least-privilege integration user; rotate passwords; use OAuth where possible. |
| **ArchLucid API** | API key or Entra JWT; Reader if only GET run. |

---

## 8. Operational Considerations

- **Table API 429/5xx:** backoff with jitter.  
- **Batching:** N findings → M incidents: cap M per run.  
- **DLQ:** poison webhooks to dead letter after N tries.  
- **Audit:** log `ce-id`, `runId`, ServiceNow `sys_id` (not full customer narrative if restricted).

---

## Expose HTTPS

Same as Jira: public TLS endpoint, secret vault for `ARCHLUCID_HMAC_SECRET` (ArchLucid `WebhookDelivery:HmacSha256SharedSecret` or your bridge secret).

---

## Validate ArchLucid HMAC

Read the raw POST body as bytes; read header **`X-ArchLucid-Webhook-Signature`**; expect **`sha256=`** + lowercase hex HMAC-SHA256(secret, raw_body). Reject on mismatch (constant-time compare).

---

## CloudEvents — `com.archlucid.authority.run.completed`

Schema: [authority-run-completed.v1.schema.json](../../../schemas/integration-events/authority-run-completed.v1.schema.json).

```json
{
  "specversion": "1.0",
  "type": "com.archlucid.authority.run.completed",
  "source": "/archlucid/webhook/digest",
  "id": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
  "time": "2026-04-24T12:00:00.000Z",
  "datacontenttype": "application/json",
  "data": {
    "schemaVersion": 1,
    "runId": "123e4567-e89b-12d3-a456-426614174000",
    "manifestId": "223e4567-e89b-12d3-a456-426614174001",
    "tenantId": "323e4567-e89b-12d3-a456-426614174002",
    "workspaceId": "423e4567-e89b-12d3-a456-426614174003",
    "projectId": "523e4567-e89b-12d3-a456-426614174004"
  }
}
```

Then: `GET https://{archlucid}/v1/authority/runs/{runId}` → map each finding in the run detail body to an incident (see field mapping A).

---

## CloudEvents — `com.archlucid.alert.fired`

Schema: [alert-fired.v1.schema.json](../../../schemas/integration-events/alert-fired.v1.schema.json) — same worked example as below.

---

## Map `data` → Table API `POST /api/now/table/incident`

`POST https://<instance>.service-now.com/api/now/table/incident` with `Authorization: Basic` (integration user) or OAuth. **Do not** widen ArchLucid rate limits—batch and backoff on your side.

### Field mapping A — run completed + API findings (per finding row)

| ArchLucid / API field | Incident column (example) |
|------------------------|---------------------------|
| Finding **title** | `short_description` |
| Finding **severity** | `severity` (map to your SN **integer** or **reference** as required) or `u_priority` if custom |
| Finding **narrative** + run/manifest id | `description` (HTML allowed per policy) |
| `runId` (URL to ArchLucid) | `description` or `u_archlucid_run` (custom) |

### Field mapping B — `com.archlucid.alert.fired` (direct)

| ArchLucid `data` | Incident column (example) |
|------------------|-----------------------------|
| `title` | `short_description` |
| `severity`, `category`, `alertId`, `deduplicationKey`, scope ids | `description` (plain text or HTML) |
| `deduplicationKey` | `correlation_id` (if within length; else truncate + hash) |

**Severity map (example):** `critical` → 1, `high` → 2, `medium` → 3, `low` → 4 (align to your `incident.severity` dictionary).

---

## Worked example — `com.archlucid.alert.fired`

Inbound CloudEvents:

```json
{
  "specversion": "1.0",
  "type": "com.archlucid.alert.fired",
  "source": "/customer/bridge",
  "id": "11111111-1111-1111-1111-111111111111",
  "datacontenttype": "application/json",
  "data": {
    "schemaVersion": 1,
    "tenantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "workspaceId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "projectId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "alertId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
    "ruleId": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
    "category": "policy",
    "severity": "high",
    "title": "Example alert title",
    "deduplicationKey": "tenant:…:rule:…"
  }
}
```

ServiceNow JSON body (template):

```json
{
  "short_description": "[ArchLucid] Example alert title",
  "description": "severity=high category=policy alertId=dddddddd-dddd-dddd-dddd-dddddddddddd tenant=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "correlation_id": "tenant:…:rule:…"
}
```

---

## Error handling

- HMAC failure → **401** / drop.  
- ServiceNow **4xx** on create → log, alert, optional DLQ.  
- ArchLucid **GET** **404** → no incident (stale `runId`).

---

## Sample: Azure Logic App (outline)

1. **When a HTTP request is received** (raw) — *prefer* **Function** in front for HMAC.  
2. **Condition** on `type` from `json(body('HTTP'))?['type']`.  
3. For **run.completed** → **HTTP** GET `https://@{parameters('archlucidHost')}/v1/authority/runs/@{...}` with **Microsoft Entra ID** app registration (OAuth) or **API key** header.  
4. **Parse JSON** of run; **for each** finding: **HTTP POST** to `.../api/now/table/incident` with **Basic** or **SN OAuth**.  
5. For **alert.fired** → single **HTTP POST** incident (mapping table B).

**Managed connectors:** ServiceNow connector can replace raw HTTP if you map fields; HMAC must still be validated in an Azure Function or APIM policy **before** Logic App.

---

## Pinned samples (template only)

**Azure Functions:** Python 3.12 + `azure-functions>=1.20.0,<2`, HTTP trigger; outbound `httpx` or `urllib.request` to ServiceNow with secrets from Key Vault. Pin the Microsoft Learn revision you tested.

**AWS Lambda:** Python 3.12, API Gateway HTTP API; Snow secret in Secrets Manager; use `httpx` / `urllib` for Table API.

Shared HMAC contract matches [../jira/jira-webhook-receiver.md](../jira/jira-webhook-receiver.md) (`X-ArchLucid-Webhook-Signature`).

**Jira equivalent depth:** [../jira/jira-webhook-bridge-recipe.md](../jira/jira-webhook-bridge-recipe.md).

---

*Last reviewed: 2026-04-24 — event types from [IntegrationEventTypes.cs](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs) and [catalog.json](../../../schemas/integration-events/catalog.json).*

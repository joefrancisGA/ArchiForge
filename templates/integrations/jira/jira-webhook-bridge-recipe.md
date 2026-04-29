# ArchLucid → Jira: Webhook Bridge Recipe (V1)

> **This bridge is a V1 workaround.** A **first-party Jira connector** is planned for **V1.1**; see [docs/go-to-market/INTEGRATION_CATALOG.md](../../../docs/go-to-market/INTEGRATION_CATALOG.md) and [V1_DEFERRED.md](../../../docs/library/V1_DEFERRED.md) §6.  
> This document is a **customer-operated** integration pattern: **no** first-party Jira code ships in the ArchLucid core for V1.

**Contracts:** [catalog.json](../../../schemas/integration-events/catalog.json) · [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) · [jira-webhook-receiver.md](jira-webhook-receiver.md) (short form, `alert.fired` only)

**Runnable bridge:** [`scripts/integrations/jira/jira-webhook-bridge.mjs`](../../../scripts/integrations/jira/jira-webhook-bridge.mjs) / [`jira-webhook-bridge.ps1`](../../../scripts/integrations/jira/jira-webhook-bridge.ps1) — narrative **[`JIRA_WEBHOOK_BRIDGE.md`](../../../docs/integrations/JIRA_WEBHOOK_BRIDGE.md)**.


---

## 1. Objective

Provide a **repeatable recipe** to open **Jira issues** from ArchLucid using:

1. **Inbound:** ArchLucid **HTTP webhooks** with [CloudEvents 1.0](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md) JSON when `WebhookDelivery:UseCloudEventsEnvelope` is **true** (see [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md)), and  
2. **Outbound:** [Jira Cloud REST API v3](https://developer.atlassian.com/cloud/jira/platform/rest/v3/intro/) `POST /rest/api/3/issue`.

**V1 reality — no Finding-family CloudEvent in catalog:** The product’s canonical integration event strings live in `IntegrationEventTypes` and [catalog.json](../../../schemas/integration-events/catalog.json). There is **no** `finding.*` suffix under the `com.archlucid` namespace (for example no `…finding.created`) in V1. For **findings → Jira**, this recipe uses:

- **`com.archlucid.authority.run.completed`** to signal a committed run, then **ArchLucid REST** to load run + findings; **or**
- **`com.archlucid.alert.fired`** for a **one-step** ticket from an alert (title, severity, optional `runId`).

---

## 2. Assumptions

- You operate a **public HTTPS** endpoint (Azure Function, API Management + Logic App, etc.) that can validate **HMAC** and call Jira.
- Jira Cloud (or Data Center with compatible REST) and credentials (**API token** + user email, or OAuth) are available in a secret store.
- ArchLucid is configured to **POST** webhooks to your URL; shared secret matches `WebhookDelivery:HmacSha256SharedSecret` (or your bridge re-signs consistently).
- For the **findings** path, a service principal or automation user can call ArchLucid **GET** `/v1/authority/runs/{runId}` (or equivalent) with a **JWT** or **API key** per [SECURITY.md](../../../docs/library/SECURITY.md).

---

## 3. Constraints

- **One-way in this recipe:** ArchLucid → Jira create (no status sync). Two-way is **V1.1+** product scope.
- **Do not** increase ArchLucid outbound rate: **batch, deduplicate, backoff** in your bridge.
- **Do not** log raw secrets or full JWTs; redact PII in Jira `description` if your DLP requires it.
- **Entra ID / API auth** to ArchLucid must follow least privilege (Reader role if read-only is enough for GET run).

---

## 4. Architecture Overview

1. **Trigger:** Webhook receiver accepts **POST** with CloudEvents body; verifies **`X-ArchLucid-Webhook-Signature`** = `sha256=` + HMAC-SHA256(shared secret, **raw** UTF-8 body).  
2. **Route by `type`:**  
   - `com.archlucid.authority.run.completed` → optional **idempotency** on `id` + `data.runId` → **GET** run from ArchLucid → map each finding (or high-severity subset) → **POST** Jira issue.  
   - `com.archlucid.alert.fired` → map `data` directly → **POST** one Jira issue.  
3. **Jira:** Use REST v3 with `Content-Type: application/json`; `fields.description` may be **Atlassian Document Format** (ADF) or plain string where supported by your Jira project.

---

## 5. Component Breakdown

| Component | Responsibility |
|-----------|------------------|
| **ArchLucid** | Emits webhooks (digest/alert/lifecycle per product config); signs body with HMAC. |
| **Your HTTPS endpoint** | Verifies signature, parses CloudEvents, enforces idempotency, calls Jira. |
| **ArchLucid API client (optional)** | Second step: fetch `GET /v1/authority/runs/{runId}` to read embedded findings in run detail (see OpenAPI). |
| **Jira** | Creates issues; issue keys returned to your logs or optional storage for deduplication. |

---

## 6. Data Flow

```text
ArchLucid --(POST, HMAC, CloudEvents)--> Bridge --(GET + Bearer/API key)--> ArchLucid API (findings path only)
   |                                                                          |
   +------------------------------(POST /rest/api/3/issue)------------------> Jira
```

**Idempotency:** Store `(cloudevent id)` or `(runId, findingId)` in your store before creating a Jira issue to avoid duplicates on webhook retries.

---

## 7. Security Model

| Item | Practice |
|------|----------|
| **Webhook authenticity** | HMAC over **exact** request bytes; constant-time compare for signature. |
| **Transport** | TLS 1.2+ to your endpoint; restrict source IPs if you use a fixed egress (optional). |
| **ArchLucid API** | Short-lived tokens; scope to minimum project/workspace; no shared passwords in Jira description. |
| **Jira credentials** | Key Vault / managed identity; rotate API tokens per Atlassian policy. |

---

## 8. Operational Considerations

- **429 / 5xx from Jira:** Exponential backoff; queue failed creates for replay.  
- **Large finding sets:** Cap issues per run (e.g. top N by severity) to avoid Jira rate limits.  
- **Observability:** Log `ce-type`, `ce-id`, `runId`, Jira `key` (not full PII).  
- **Testing:** Use Jira **project** dedicated to non-prod; dry-run with `com.archlucid.alert.fired` sample first.

---

## Webhook subscription (operator)

Configure ArchLucid to deliver to your URL. Operator steps are product-specific; align with [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) and your deployment’s Webhook / digest settings. Ensure **CloudEvents** envelope is enabled if you rely on `type` / `data` as below.

---

## CloudEvents payload — `com.archlucid.authority.run.completed`

Canonical schema: [authority-run-completed.v1.schema.json](../../../schemas/integration-events/authority-run-completed.v1.schema.json). Example envelope:

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

**Findings step:** `GET /v1/authority/runs/{runId}` (Guid) with auth — response includes run detail; use the **findings** collection in the payload (field names per OpenAPI `RunDetailDto`) to build Jira `summary` / `description` and a **link** to the run in your ArchLucid UI or API.

---

## CloudEvents payload — `com.archlucid.alert.fired`

Schema: [alert-fired.v1.schema.json](../../../schemas/integration-events/alert-fired.v1.schema.json). Example:

```json
{
  "specversion": "1.0",
  "type": "com.archlucid.alert.fired",
  "source": "/archlucid/webhook/digest",
  "id": "b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a12",
  "time": "2026-04-24T12:00:01.000Z",
  "datacontenttype": "application/json",
  "data": {
    "schemaVersion": 1,
    "tenantId": "323e4567-e89b-12d3-a456-426614174002",
    "workspaceId": "423e4567-e89b-12d3-a456-426614174003",
    "projectId": "523e4567-e89b-12d3-a456-426614174004",
    "alertId": "623e4567-e89b-12d3-a456-426614174005",
    "ruleId": "723e4567-e89b-12d3-a456-426614174006",
    "category": "policy",
    "severity": "high",
    "title": "Policy breach detected in committed manifest",
    "deduplicationKey": "tenant:323e...:rule:723e...",
    "runId": "123e4567-e89b-12d3-a456-426614174000"
  }
}
```

---

## Field mapping

### A) Run completed → Jira (after API fetch of findings)

| ArchLucid source | Jira field (REST v3) | Notes |
|------------------|----------------------|--------|
| Finding **title** / **name** (from run detail) | `fields.summary` | Prefix `[ArchLucid]` if required. |
| Finding **severity** | `fields.priority` | Map to project’s priority **names** (e.g. Critical → *Highest*). |
| Finding **description** + **runId** + deep link | `fields.description` | ADF `doc` or string; include `GET /v1/authority/runs/{runId}` or UI URL. |
| `manifestId` | `description` / custom field | Correlation only. |
| `tenantId` / `workspaceId` / `projectId` | `description` | Non-PII or hashed per policy. |

### B) Alert fired → Jira (direct, no extra GET)

| ArchLucid `data` | Jira field | Notes |
|------------------|------------|--------|
| `title` | `fields.summary` | Required. |
| `severity` | `fields.priority` | Map string → Jira priority. |
| `category`, `alertId`, `ruleId`, `deduplicationKey`, `runId?` | `fields.description` | Include link to run if `runId` present. |
| `deduplicationKey` | custom field or `description` | Use for de-dupe before create. |

**Severity → priority (example):** `critical` → Highest, `high` → High, `medium` → Medium, `low` → Low, `info` → Lowest (adjust to your Jira scheme).

---

## Jira REST — `POST /rest/api/3/issue`

Minimal JSON (project key and issue type must exist):

```json
{
  "fields": {
    "project": { "key": "ARCH" },
    "issuetype": { "name": "Task" },
    "summary": "[ArchLucid] Policy breach detected in committed manifest",
    "description": {
      "type": "doc",
      "version": 1,
      "content": [
        {
          "type": "paragraph",
          "content": [
            { "type": "text", "text": "severity=high runId=123e4567-e89b-12d3-a456-426614174000 deduplicationKey=…" }
          ]
        }
      ]
    }
  }
}
```

**Auth:** `Authorization: Basic` base64(`email:api_token`) or OAuth per Atlassian.

---

## Error handling

- **HMAC fail:** return **401**; do not parse `data`.  
- **Unknown `type`:** return **202** and log (or 400 if you want hard fail).  
- **Jira 4xx:** log body; do not infinite-retry business errors.  
- **ArchLucid GET 404:** run not in scope; exit without Jira call.

---

## Sample: Azure Logic App (outline)

1. **Trigger:** HTTP Request (receive raw body) — *or* **Service Bus** if you already fan-out integration events to a queue.  
2. **Compose:** `base64String()` HMAC not native — use **Azure Function** (HTTP trigger) for HMAC verify, or **inline** in Function only.  
3. **Condition:** `json(...)['type']` equals `com.archlucid.authority.run.completed` vs `com.archlucid.alert.fired`.  
4. **HTTP (GET):** `https://{archlucid-host}/v1/authority/runs/@{body('ParseJson')?['data']?['runId']}` with **Authorization: Bearer** from Key Vault.  
5. **For each** finding: **HTTP (POST)** Jira `.../rest/api/3/issue` with mapped JSON.

*Recommended:* Implement **HMAC in an Azure Function** (Python or C#) as the only public step; Logic App then receives **pre-validated** JSON (internal queue or function output).

---

## Sample: Azure Function (sketch, Python 3.12)

Validate HMAC per [jira-webhook-receiver.md](jira-webhook-receiver.md); on `com.archlucid.alert.fired`, `requests.post` to Jira. On `com.archlucid.authority.run.completed`, `requests.get` to ArchLucid with `os.environ["ARCHLUCID_BASE"]` and `ARCHLUCID_API_KEY` or token from MI.

---

*Last reviewed: 2026-04-24 — aligns with [IntegrationEventTypes.cs](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs) and [catalog.json](../../../schemas/integration-events/catalog.json).*

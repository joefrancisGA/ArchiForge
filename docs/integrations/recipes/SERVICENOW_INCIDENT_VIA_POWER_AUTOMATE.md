> **Scope:** Step-by-step Power Automate recipe to create ServiceNow incidents from ArchLucid CloudEvents webhooks — no custom code required.

# ServiceNow incident via Power Automate (no-code recipe)

**Audience:** V1 customers who need ServiceNow incidents from ArchLucid findings or alerts but do not want to write an Azure Function or custom webhook receiver.

**Optional customer-owned bridge.** **First-party ServiceNow** is **in scope for V1 GA** ([`V1_SCOPE.md`](../../library/V1_SCOPE.md) §2.13, [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md)). Use this recipe when you prefer **Power Automate** or need automation **before** managed connector enablement.

> **Customer-owned:** This flow runs in **your** Microsoft 365 / Power Platform tenant and calls **your** ServiceNow Table API. It is **not** a ServiceNow Store or ArchLucid-certified integration pack. ArchLucid only delivers signed CloudEvents webhooks per [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).

**Contracts:** [catalog.json](../../../schemas/integration-events/catalog.json) · [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) · [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md)  
**Event catalog (code):** [`IntegrationEventTypes.cs`](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs)

---

## 1. Prerequisites

| Requirement | Detail |
|-------------|--------|
| **ArchLucid webhook** | Configured to POST to a URL you control; `WebhookDelivery:UseCloudEventsEnvelope` = **true**; HMAC shared secret recorded. See [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md). |
| **Power Automate license** | **Premium** plan (the HTTP trigger and HTTP actions require it). |
| **ServiceNow instance** | Integration user with `incident` table create rights. Obtain instance URL (e.g. `https://myorg.service-now.com`). |
| **ServiceNow credentials** | Basic auth (username + password) for the integration user, or OAuth client credentials. Store securely. |
| **ArchLucid API credentials** | API key or Entra ID service principal with **Reader** authority for `GET /v1/authority/runs/{runId}` (only needed for the findings path). |

---

## 2. Event types to subscribe to

| Path | CloudEvents `type` (from `IntegrationEventTypes`) | When to use |
|------|----------------------------------------------------|-------------|
| **A — Findings from a completed run** | `com.archlucid.authority.run.completed` | Creates one ServiceNow incident per finding (or top-N by severity) after an authority run commits. Requires a second call to the ArchLucid API to fetch run detail. |
| **B — Alert → single incident** | `com.archlucid.alert.fired` | Creates one ServiceNow incident directly from the alert payload. No extra API call needed. |

Configure your ArchLucid webhook subscription to deliver **one or both** of these event types to the Power Automate HTTP trigger URL.

---

## 3. Flow overview

```text
Power Automate HTTP trigger
  │
  ├─ Parse CloudEvents JSON
  │
  ├─ Condition: type == "com.archlucid.authority.run.completed"?
  │    ├─ Yes → HTTP GET ArchLucid /v1/authority/runs/{runId}
  │    │         → For each finding → HTTP POST ServiceNow /api/now/table/incident
  │    │
  │    └─ No → Condition: type == "com.archlucid.alert.fired"?
  │              ├─ Yes → HTTP POST ServiceNow /api/now/table/incident (single)
  │              └─ No  → Respond 202 (unhandled type, log & skip)
  │
  └─ Respond 200 OK
```

---

## 4. Step-by-step flow configuration

### Step 1 — Create flow and add HTTP trigger

1. Open **Power Automate** → **My flows** → **New flow** → **Instant cloud flow** → select **When an HTTP request is received**.
2. In the trigger configuration, set **Method** to `POST`.
3. Paste the following JSON Schema so Power Automate parses the CloudEvents envelope automatically:

```json
{
  "type": "object",
  "properties": {
    "specversion": { "type": "string" },
    "type": { "type": "string" },
    "source": { "type": "string" },
    "id": { "type": "string" },
    "time": { "type": "string" },
    "datacontenttype": { "type": "string" },
    "data": {
      "type": "object",
      "properties": {
        "schemaVersion": { "type": "integer" },
        "runId": { "type": "string" },
        "manifestId": { "type": "string" },
        "tenantId": { "type": "string" },
        "workspaceId": { "type": "string" },
        "projectId": { "type": "string" },
        "alertId": { "type": "string" },
        "ruleId": { "type": "string" },
        "category": { "type": "string" },
        "severity": { "type": "string" },
        "title": { "type": "string" },
        "deduplicationKey": { "type": "string" }
      }
    }
  }
}
```

4. **Save** the flow to generate the HTTP POST URL. Copy this URL — you will configure it as the ArchLucid webhook destination.

### Step 2 — Initialize variables

Add two **Initialize variable** actions:

| Variable name | Type | Initial value |
|---------------|------|---------------|
| `eventType` | String | `@{triggerBody()?['type']}` |
| `incidentsCreated` | Integer | `0` |

### Step 3 — Validate HMAC signature (recommended)

Power Automate does not have a native HMAC action. Two options:

**Option A (recommended):** Place an **Azure API Management** policy or a lightweight **Azure Function** (HTTP trigger, ~10 lines) in front of the Power Automate URL. The Function validates `X-ArchLucid-Webhook-Signature` = `sha256=` + HMAC-SHA256(shared secret, raw body) and forwards valid payloads only.

**Option B (accept risk):** Skip HMAC validation at the flow level and rely on the obscurity of the Power Automate URL plus IP restrictions. **Not recommended for production.**

### Step 4 — Condition: route by event type

Add a **Condition** action:

- **Left:** `eventType`
- **Operator:** is equal to
- **Right:** `com.archlucid.authority.run.completed`

### Step 5 — Yes branch: fetch run detail from ArchLucid API

Inside the **If yes** branch, add an **HTTP** action:

| Setting | Value |
|---------|-------|
| Method | `GET` |
| URI | `https://{your-archlucid-host}/v1/authority/runs/@{triggerBody()?['data']?['runId']}` |
| Headers | `Authorization`: `Bearer {your-api-key-or-token}` |

Add a **Parse JSON** action on the response body using the ArchLucid OpenAPI `RunDetailDto` schema (or a minimal subset with `findings` array).

### Step 6 — Yes branch: loop over findings and create ServiceNow incidents

Add an **Apply to each** action on `body('Parse_JSON')?['findings']`.

Inside the loop, add an **HTTP** action:

| Setting | Value |
|---------|-------|
| Method | `POST` |
| URI | `https://{your-instance}.service-now.com/api/now/table/incident` |
| Headers | `Authorization`: `Basic {base64(username:password)}`, `Content-Type`: `application/json`, `Accept`: `application/json` |
| Body | See JSON transformation below |

**Request body (expression):**

```json
{
  "short_description": "[ArchLucid] @{items('Apply_to_each')?['title']}",
  "severity": "@{if(equals(items('Apply_to_each')?['severity'],'critical'),'1',if(equals(items('Apply_to_each')?['severity'],'high'),'2',if(equals(items('Apply_to_each')?['severity'],'medium'),'3','4')))}",
  "description": "ArchLucid finding from run @{triggerBody()?['data']?['runId']}\nSeverity: @{items('Apply_to_each')?['severity']}\nManifest: @{triggerBody()?['data']?['manifestId']}\nProject: @{triggerBody()?['data']?['projectId']}",
  "correlation_id": "@{triggerBody()?['data']?['runId']}-@{items('Apply_to_each')?['title']}"
}
```

After the HTTP POST, add an **Increment variable** action on `incidentsCreated`.

**Cap the loop:** To avoid flooding ServiceNow, add a **Condition** inside the loop that checks `incidentsCreated` < 25 (or your preferred cap) and wraps the HTTP POST. Skip remaining iterations when the cap is reached.

### Step 7 — No branch: check for alert.fired

Inside the **If no** branch, add a second **Condition**:

- **Left:** `eventType`
- **Operator:** is equal to
- **Right:** `com.archlucid.alert.fired`

### Step 8 — Alert branch: create single ServiceNow incident

Inside the **If yes** sub-branch, add an **HTTP** action:

| Setting | Value |
|---------|-------|
| Method | `POST` |
| URI | `https://{your-instance}.service-now.com/api/now/table/incident` |
| Headers | `Authorization`: `Basic {base64(username:password)}`, `Content-Type`: `application/json`, `Accept`: `application/json` |
| Body | See JSON transformation below |

**Request body:**

```json
{
  "short_description": "[ArchLucid Alert] @{triggerBody()?['data']?['title']}",
  "severity": "@{if(equals(triggerBody()?['data']?['severity'],'critical'),'1',if(equals(triggerBody()?['data']?['severity'],'high'),'2',if(equals(triggerBody()?['data']?['severity'],'medium'),'3','4')))}",
  "description": "Alert ID: @{triggerBody()?['data']?['alertId']}\nSeverity: @{triggerBody()?['data']?['severity']}\nCategory: @{triggerBody()?['data']?['category']}\nRule ID: @{triggerBody()?['data']?['ruleId']}\nDeduplication key: @{triggerBody()?['data']?['deduplicationKey']}\nRun: @{coalesce(triggerBody()?['data']?['runId'],'N/A')}\nTenant: @{triggerBody()?['data']?['tenantId']}\nWorkspace: @{triggerBody()?['data']?['workspaceId']}\nProject: @{triggerBody()?['data']?['projectId']}",
  "correlation_id": "@{triggerBody()?['data']?['deduplicationKey']}"
}
```

### Step 9 — Final response

After the outermost condition, add a **Response** action:

| Setting | Value |
|---------|-------|
| Status Code | `200` |
| Body | `{"status":"processed","eventType":"@{eventType}"}` |

---

## 5. JSON transformation reference

### A) `com.archlucid.authority.run.completed` → ServiceNow incident (per finding)

| CloudEvents / API source | ServiceNow Table API field | Notes |
|--------------------------|---------------------------|-------|
| Finding `title` (from `GET /v1/authority/runs/{runId}`) | `short_description` | Prefix `[ArchLucid]`. |
| Finding `severity` | `severity` | Map: `critical` → 1, `high` → 2, `medium` → 3, `low`/`info` → 4. Align to your `incident.severity` dictionary. |
| Finding `description` + `runId` + `manifestId` | `description` | Include a deep link to the run in ArchLucid UI if available. |
| `runId` + finding title | `correlation_id` | For deduplication on retries. |
| `data.tenantId`, `data.workspaceId`, `data.projectId` | `description` or `work_notes` | Correlation context; omit PII per your policy. |

### B) `com.archlucid.alert.fired` → ServiceNow incident (direct)

| CloudEvents `data` field | ServiceNow Table API field | Notes |
|--------------------------|---------------------------|-------|
| `title` | `short_description` | Prefix `[ArchLucid Alert]`. |
| `severity` | `severity` | Same integer mapping as above. |
| `alertId`, `ruleId`, `category`, `deduplicationKey` | `description` | Machine-readable context for triage. |
| `deduplicationKey` | `correlation_id` | For deduplication; truncate + hash if longer than ServiceNow column limit. |
| `runId` (optional) | `description` | Include only when present. |

---

## 6. Error handling guidance

### Retry policy

Configure the HTTP actions (both ArchLucid GET and ServiceNow POST) with **retry policy**:

| Setting | Recommended value |
|---------|-------------------|
| Type | **Exponential** |
| Count | 3 |
| Interval | `PT10S` (10 seconds) |
| Maximum interval | `PT5M` (5 minutes) |

In Power Automate, set this under each HTTP action → **Settings** → **Retry Policy**.

### Dead-letter / failure handling

1. Add a **Scope** action around the entire incident-create block.
2. Add a **parallel branch** on the Scope's **Configure run after** → select **has failed** and **has timed out**.
3. In the failure branch, add an **action** to persist the failed payload — options include:
   - **Send an email** (Office 365 connector) with the CloudEvents `id`, `type`, and error message.
   - **Create a row** in a SharePoint list or Excel table for manual replay.
   - **POST** to a dead-letter HTTP endpoint or Azure Storage Queue.

### Input validation

- After parsing the CloudEvents body, add a **Condition** that checks `triggerBody()?['data']?['runId']` is not null (for `run.completed`) or `triggerBody()?['data']?['alertId']` is not null (for `alert.fired`). Respond `400` if critical fields are missing.
- Check `triggerBody()?['specversion']` equals `1.0` to reject non-CloudEvents payloads.

### Deduplication

Power Automate does not have built-in deduplication. To prevent duplicate ServiceNow incidents on webhook retries:

1. Before the ServiceNow POST, query the incident table for existing incidents:
   - `GET /api/now/table/incident?sysparm_query=correlation_id={deduplicationKey}` (for alerts), or
   - `GET /api/now/table/incident?sysparm_query=correlation_id={runId}-{findingTitle}` (for findings).
2. Wrap the ServiceNow POST in a **Condition** that only fires when the query returns zero results.

---

## 7. Limitations and when to adopt first-party delivery

| Limitation | First-party V1 connector ([`V1_SCOPE.md`](../../library/V1_SCOPE.md) §2.13) |
|------------|--------------------------------------------------------------------------|
| **One-way only** — this recipe creates incidents but does not sync status back to ArchLucid. | **Two-way** SNOW→ArchLucid status sync is **not** in committed V1 scope **unless** an owner decision adds it — do not assume parity with Jira bi-directional. |
| **No native HMAC in Power Automate** — HMAC validation requires an Azure Function or API Management in front. | Managed connector handles authentication natively; no external HMAC layer for ArchLucid→ServiceNow traffic. |
| **Manual severity mapping** — you maintain the severity-to-integer map in flow expressions. | Managed connector ships a configurable mapping with sensible defaults. |
| **No deduplication** — you must build correlation_id-based dedup logic yourself. | Managed connector uses `deduplicationKey` / `runId` + `findingId` natively for idempotent incident creation. |
| **Premium license required** — HTTP actions in Power Automate need a Premium plan. | Managed connector runs server-side in ArchLucid; no Power Automate license needed for the base flow. |
| **No finding-level event** — `com.archlucid.authority.run.completed` signals run completion, not individual findings. You must call the API to get findings. | Managed connector has direct access to the finding projection; no extra API call. |
| **No CMDB CI mapping** — this recipe does not populate `cmdb_ci`. | Same **open planning** topic as [`INTEGRATION_CATALOG.md`](../../go-to-market/INTEGRATION_CATALOG.md) and [`V1_SCOPE.md`](../../library/V1_SCOPE.md) §2.13 (`cmdb_ci` same release vs fast-follow). |

When you enable the first-party connector, migrate by:
1. Disabling the Power Automate flow (or leaving it as a redundant safety path only with clear owner approval).
2. Configuring the managed ServiceNow connector in ArchLucid (see release notes when available).
3. Verifying incident creation with a test run.
4. Deleting or archiving the Power Automate flow if no longer needed.

---

## 8. Test steps (smoke and acceptance)

Use these steps in a **non-production** ArchLucid tenant and ServiceNow subproduction instance first.

1. **Configure webhook** — In ArchLucid, point digest/alert outbound delivery at your Power Automate HTTP trigger URL (or at an APIM/Function front door that validates HMAC per [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) before forwarding). Enable CloudEvents envelope if this recipe’s schema applies.
2. **Probe signing** — Run `archlucid webhooks test --url <your-entrypoint-url> [--secret …]` (or equivalent) so the trigger receives a realistic POST; confirm the flow parses `specversion`, `type`, and `data`.
3. **`alert.fired` path** — Emit or simulate an alert that produces `com.archlucid.alert.fired`; verify one `incident` row with `correlation_id` matching `deduplicationKey` (or your chosen key) and expected severity mapping.
4. **`authority.run.completed` path** — Complete a run that emits `com.archlucid.authority.run.completed`; confirm the flow’s `GET /v1/authority/runs/{runId}` succeeds and creates up to your capped number of incidents with stable `correlation_id` values.
5. **Retry** — Intentionally fail a ServiceNow POST (wrong ACL), confirm Power Automate retries then dead-letters per your Scope branch; replay from the dead-letter store once fixed.
6. **Idempotency** — Re-send the same CloudEvents `id` (or replay body); confirm deduplication query prevents duplicate incidents if you implemented section 6 dedup.

---

## Related documents

| Doc | Use |
|-----|-----|
| [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) | Event delivery, CloudEvents envelope, HMAC signing |
| [INTEGRATION_EVENT_CATALOG.md](../../library/INTEGRATION_EVENT_CATALOG.md) | Full event type catalog |
| [V1_SCOPE.md](../../library/V1_SCOPE.md) | V1 first-party ServiceNow connector commitment (§2.13) |
| [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md) | Connector roadmap and status |
| [servicenow-incident-recipe.md](../../../templates/integrations/servicenow/servicenow-incident-recipe.md) | Developer-oriented bridge (custom code, Azure Function) |
| [JIRA_ISSUE_VIA_POWER_AUTOMATE.md](JIRA_ISSUE_VIA_POWER_AUTOMATE.md) | Companion recipe — Jira via Power Automate |
| [CONFLUENCE_PAGE_VIA_LOGIC_APPS.md](CONFLUENCE_PAGE_VIA_LOGIC_APPS.md) | Companion recipe — Confluence via Logic Apps |
| [Recipes README](README.md) | Index of all no-code recipes |

---

*Last reviewed: 2026-05-01 — customer-owned disclaimer, test steps, catalog cross-links.*

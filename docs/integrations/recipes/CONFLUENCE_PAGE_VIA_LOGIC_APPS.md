> **Scope:** Step-by-step Azure Logic Apps recipe to publish Confluence pages from ArchLucid CloudEvents webhooks — no custom code required.

# Confluence page via Logic Apps (no-code recipe)

**Audience:** V1 customers who want to push architecture run summaries or advisory scan results to a Confluence space without writing a custom webhook consumer.

**V1 interim bridge.** A first-party Confluence connector is planned for **V1.1** — see [V1_DEFERRED.md §6](../../library/V1_DEFERRED.md) and [INTEGRATION_CATALOG.md §2](../../go-to-market/INTEGRATION_CATALOG.md). V1.1 minimum viable shape: one-way publish to a single fixed `Confluence:DefaultSpaceKey`. This recipe bridges the gap using **Azure Logic Apps (Standard)**.

**Contracts:** [catalog.json](../../../schemas/integration-events/catalog.json) · [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md)
**Event catalog (code):** [`IntegrationEventTypes.cs`](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs)

---

## 1. Prerequisites

| Requirement | Detail |
|-------------|--------|
| **ArchLucid webhook** | Configured to POST to a URL you control; `WebhookDelivery:UseCloudEventsEnvelope` = **true**; HMAC shared secret recorded. See [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md). |
| **Azure subscription** | Logic Apps Standard plan (Workflow Standard or WS1). |
| **Confluence Cloud** | API token for an Atlassian user with **Add Pages** permission in the target space. Obtain an [Atlassian API token](https://id.atlassian.com/manage-profile/security/api-tokens). |
| **Confluence space key** | e.g. `ARCHREVIEWS`. The space must already exist. |
| **ArchLucid API credentials** | API key or Entra ID service principal with **Reader** authority for `GET /v1/authority/runs/{runId}` (needed to fetch run detail and findings). |
| **Azure Key Vault** | Store the ArchLucid API key, Confluence API token, Confluence user email, and HMAC shared secret as Key Vault secrets. Reference them from Logic Apps via managed identity. |

---

## 2. Event types to subscribe to

| Path | CloudEvents `type` (from `IntegrationEventTypes`) | When to use |
|------|----------------------------------------------------|-------------|
| **A — Run summary page** | `com.archlucid.authority.run.completed` | Creates or updates a Confluence page summarizing a committed authority run — findings, severity counts, and a link back to ArchLucid. |
| **B — Advisory scan summary page** | `com.archlucid.advisory.scan.completed` | Creates a Confluence page summarizing an advisory scan execution — schedule context, whether runs were produced, and a link to the scan detail. |

Configure your ArchLucid webhook subscription to deliver **one or both** of these event types to the Logic App HTTP trigger URL.

---

## 3. Flow overview

```text
Logic App HTTP trigger (raw body)
  │
  ├─ Azure Function: validate HMAC signature
  │
  ├─ Parse CloudEvents JSON
  │
  ├─ Switch on "type":
  │    │
  │    ├─ "com.archlucid.authority.run.completed"
  │    │    → HTTP GET ArchLucid /v1/authority/runs/{runId}
  │    │    → Compose Confluence page body (Storage Format HTML)
  │    │    → HTTP POST Confluence /wiki/api/v2/pages
  │    │
  │    ├─ "com.archlucid.advisory.scan.completed"
  │    │    → Compose Confluence page body from event data
  │    │    → HTTP POST Confluence /wiki/api/v2/pages
  │    │
  │    └─ Default → Respond 202 (unhandled type)
  │
  └─ Respond 200 OK
```

---

## 4. Step-by-step flow configuration

### Step 1 — Create a Logic App Standard workflow

1. In the Azure Portal, create a **Logic App (Standard)** resource in your subscription.
2. Under **Workflows**, create a new **Stateful** workflow named `confluence-page-from-archlucid`.
3. Add a **When a HTTP request is received** trigger. Set the method to `POST`.
4. Use the same CloudEvents JSON Schema as the Power Automate Jira recipe (see [JIRA_ISSUE_VIA_POWER_AUTOMATE.md §4 Step 1](JIRA_ISSUE_VIA_POWER_AUTOMATE.md)) — paste the schema so the designer parses CloudEvents fields.
5. **Save** to generate the trigger URL. Copy this URL for the ArchLucid webhook configuration.

> **Screenshot description:** The Logic Apps designer showing a "When a HTTP request is received" trigger card with Method = POST and the CloudEvents JSON Schema expanded. The callback URL appears at the top of the card.

### Step 2 — Validate HMAC signature

Add an **Azure Functions** action that calls your HMAC validation function (same function as the Power Automate recipe — a lightweight HTTP-triggered function that verifies `X-ArchLucid-Webhook-Signature`).

**Alternative (Logic Apps only):** Use an **API Management** inbound policy with `<validate-hmac>` or a custom policy expression in front of the Logic App endpoint.

Configure the action:

| Setting | Value |
|---------|-------|
| Function App | Your HMAC validation Function App |
| Function | `validate-hmac` |
| Request Body | `@{triggerBody()}` |
| Headers | Forward `X-ArchLucid-Webhook-Signature` from the trigger |

Add a **Condition** after the Function action: if the function returns status code != 200, **Respond** with 401 and **Terminate**.

> **Screenshot description:** An "Azure Functions" action card pointing to the HMAC validator, followed by a Condition card that checks the function's status code. The "If no" branch contains a Response (401) and Terminate action.

### Step 3 — Parse CloudEvents body

Add a **Parse JSON** action:

| Setting | Value |
|---------|-------|
| Content | `@{triggerBody()}` |
| Schema | Same CloudEvents schema from Step 1 |

> **Screenshot description:** A "Parse JSON" card with the Content expression and the CloudEvents schema. Output tokens like `type`, `data.runId`, `data.tenantId` appear in the dynamic content picker.

### Step 4 — Switch on event type

Add a **Switch** action on `body('Parse_JSON')?['type']`:

| Case | Value |
|------|-------|
| Case 1 | `com.archlucid.authority.run.completed` |
| Case 2 | `com.archlucid.advisory.scan.completed` |
| Default | (respond 202, log, skip) |

> **Screenshot description:** A Switch card with two cases and a Default branch. Each case shows the literal event type string.

### Step 5 — Case 1: Fetch run detail and create Confluence page

**5a. HTTP GET run detail:**

| Setting | Value |
|---------|-------|
| Method | `GET` |
| URI | `https://{your-archlucid-host}/v1/authority/runs/@{body('Parse_JSON')?['data']?['runId']}` |
| Headers | `Authorization`: `Bearer @{parameters('archlucidApiKey')}` |

**5b. Parse JSON** on the run detail response to extract findings (use the `RunDetailDto` schema subset).

**5c. Compose** the Confluence page body. Add a **Compose** action with the following value (Confluence Storage Format — a subset of XHTML):

```json
{
  "spaceId": "@{parameters('confluenceSpaceId')}",
  "status": "current",
  "title": "ArchLucid Run Summary — @{body('Parse_JSON')?['data']?['runId']} — @{utcNow('yyyy-MM-dd')}",
  "body": {
    "representation": "storage",
    "value": "<h2>Run Summary</h2><table><tr><th>Field</th><th>Value</th></tr><tr><td>Run ID</td><td>@{body('Parse_JSON')?['data']?['runId']}</td></tr><tr><td>Manifest ID</td><td>@{body('Parse_JSON')?['data']?['manifestId']}</td></tr><tr><td>Tenant</td><td>@{body('Parse_JSON')?['data']?['tenantId']}</td></tr><tr><td>Workspace</td><td>@{body('Parse_JSON')?['data']?['workspaceId']}</td></tr><tr><td>Project</td><td>@{body('Parse_JSON')?['data']?['projectId']}</td></tr><tr><td>Completed</td><td>@{body('Parse_JSON')?['time']}</td></tr></table><h2>Findings</h2><p>@{length(body('Parse_Run_Detail')?['findings'])} finding(s) in this run.</p><table><tr><th>Title</th><th>Severity</th></tr>@{join(body('Select_Findings'),'')}</table><hr /><p><em>Auto-generated by ArchLucid webhook bridge. <a href=\"https://{your-archlucid-host}/runs/@{body('Parse_JSON')?['data']?['runId']}\">View in ArchLucid</a></em></p>"
  }
}
```

Before the Compose, add a **Select** action (`Select_Findings`) that transforms each finding into an HTML table row:

| Setting | Value |
|---------|-------|
| From | `body('Parse_Run_Detail')?['findings']` |
| Map | `<tr><td>@{item()?['title']}</td><td>@{item()?['severity']}</td></tr>` |

**5d. HTTP POST to Confluence REST API v2:**

| Setting | Value |
|---------|-------|
| Method | `POST` |
| URI | `https://{your-confluence-instance}.atlassian.net/wiki/api/v2/pages` |
| Headers | `Authorization`: `Basic @{base64(concat(parameters('confluenceEmail'),':',parameters('confluenceApiToken')))}`, `Content-Type`: `application/json` |
| Body | `@{outputs('Compose_Confluence_Page')}` |

> **Screenshot description:** Four action cards stacked vertically: HTTP GET (ArchLucid), Parse JSON, Select (findings → HTML rows), Compose (page body), and HTTP POST (Confluence). The POST card shows the Confluence URI, Basic auth header, and the composed JSON body.

### Step 6 — Case 2: Advisory scan summary page

**6a. Compose** the page body directly from the event `data` (no extra API call needed for basic scan metadata):

```json
{
  "spaceId": "@{parameters('confluenceSpaceId')}",
  "status": "current",
  "title": "ArchLucid Advisory Scan — @{body('Parse_JSON')?['data']?['executionId']} — @{utcNow('yyyy-MM-dd')}",
  "body": {
    "representation": "storage",
    "value": "<h2>Advisory Scan Completed</h2><table><tr><th>Field</th><th>Value</th></tr><tr><td>Execution ID</td><td>@{body('Parse_JSON')?['data']?['executionId']}</td></tr><tr><td>Schedule ID</td><td>@{body('Parse_JSON')?['data']?['scheduleId']}</td></tr><tr><td>Has Runs</td><td>@{body('Parse_JSON')?['data']?['hasRuns']}</td></tr><tr><td>Completed</td><td>@{body('Parse_JSON')?['data']?['completedUtc']}</td></tr><tr><td>Tenant</td><td>@{body('Parse_JSON')?['data']?['tenantId']}</td></tr><tr><td>Workspace</td><td>@{body('Parse_JSON')?['data']?['workspaceId']}</td></tr><tr><td>Project</td><td>@{body('Parse_JSON')?['data']?['projectId']}</td></tr></table><hr /><p><em>Auto-generated by ArchLucid webhook bridge.</em></p>"
  }
}
```

**6b. HTTP POST** to Confluence (same URI, headers, and auth as Step 5d).

> **Screenshot description:** Two action cards in the Case 2 branch: a Compose card with the advisory scan page JSON, and an HTTP POST card pointing to the Confluence API.

### Step 7 — Default branch and final response

In the **Default** branch of the Switch, add a **Response** action returning `202 Accepted` with body `{"status":"skipped","reason":"unhandled event type"}`.

After the Switch, add a final **Response** action:

| Setting | Value |
|---------|-------|
| Status Code | `200` |
| Body | `{"status":"processed","eventType":"@{body('Parse_JSON')?['type']}"}` |

> **Screenshot description:** The Default branch shows a Response 202 card. Below the entire Switch, a Response 200 card closes the flow.

---

## 5. JSON transformation reference

### A) `com.archlucid.authority.run.completed` → Confluence page

| CloudEvents / API source | Confluence REST field | Notes |
|--------------------------|----------------------|-------|
| `data.runId` + timestamp | `title` | Format: `ArchLucid Run Summary — {runId} — {date}`. |
| Run detail findings (from API) | `body.value` (Storage Format HTML) | Table of finding title + severity. |
| `data.manifestId`, `data.tenantId`, `data.workspaceId`, `data.projectId` | `body.value` (metadata table) | Correlation context. |
| Parameter `confluenceSpaceId` | `spaceId` | Target Confluence space (numeric ID from Confluence API `GET /wiki/api/v2/spaces`). |

### B) `com.archlucid.advisory.scan.completed` → Confluence page

| CloudEvents `data` field | Confluence REST field | Notes |
|--------------------------|----------------------|-------|
| `executionId` + timestamp | `title` | Format: `ArchLucid Advisory Scan — {executionId} — {date}`. |
| `scheduleId`, `hasRuns`, `completedUtc` | `body.value` (metadata table) | Scan execution context. |
| `tenantId`, `workspaceId`, `projectId` | `body.value` | Scope identifiers. |

### Confluence space ID lookup

The v2 API requires the **numeric space ID**, not the space key. To find it:

```
GET https://{instance}.atlassian.net/wiki/api/v2/spaces?keys=ARCHREVIEWS
```

Use the `id` field from the response. Store it as a Logic App parameter (`confluenceSpaceId`).

---

## 6. Error handling guidance

### Retry policy

Configure each HTTP action with the Logic Apps retry policy:

| Setting | Recommended value |
|---------|-------------------|
| Type | **Exponential** |
| Count | 4 |
| Interval | `PT15S` (15 seconds) |
| Maximum interval | `PT10M` (10 minutes) |

Set this under each HTTP action → **Settings** → **Retry policy** in the Logic Apps designer.

### Dead-letter / failure handling

1. Wrap the Confluence POST (and the ArchLucid GET) inside a **Scope** action.
2. Add a **parallel branch** on the Scope's **Configure run after** → select **has failed** and **has timed out**.
3. In the failure branch:
   - **Send a message** to a Service Bus dead-letter queue (if you already use the ArchLucid Service Bus namespace), or
   - **Send an email** via the Office 365 connector with the CloudEvents `id`, `type`, and the error from `result('Scope')?['error']`.
   - **Insert a row** into an Azure Table Storage `deadletters` table for manual replay.

### Input validation

After parsing the CloudEvents body:

- **Condition:** `body('Parse_JSON')?['specversion']` equals `1.0`. Respond `400` otherwise.
- **Condition (Case 1):** `body('Parse_JSON')?['data']?['runId']` is not null or empty. Respond `400` otherwise.
- **Condition (Case 2):** `body('Parse_JSON')?['data']?['executionId']` is not null or empty. Respond `400` otherwise.

### Duplicate page prevention

To avoid creating duplicate pages on webhook retries:

1. Before the Confluence POST, search for existing pages:
   ```
   GET https://{instance}.atlassian.net/wiki/api/v2/pages?spaceId={spaceId}&title=ArchLucid Run Summary — {runId}
   ```
2. If the search returns results, **update** the existing page (`PUT /wiki/api/v2/pages/{pageId}`) with an incremented `version.number` instead of creating a new one.
3. If no results, proceed with the `POST`.

---

## 7. Limitations and when to upgrade to V1.1

| Limitation | V1.1 first-party connector resolution |
|------------|----------------------------------------|
| **One-way only** — this recipe publishes pages but does not sync edits back to ArchLucid. | V1.1 connector provides server-side publish; no sync back is in the V1.1 minimum scope either, but the ArchLucid-side finding record remains authoritative. |
| **HMAC validation requires an Azure Function** — Logic Apps has no native HMAC action. | V1.1 connector handles authentication natively; no external HMAC layer needed. |
| **Manual HTML composition** — you maintain the Storage Format HTML template in the Compose action. | V1.1 connector ships a built-in page template with configurable sections. |
| **Single space only** — this recipe targets one `confluenceSpaceId` parameter. | V1.1 minimum viable shape also targets a single fixed `Confluence:DefaultSpaceKey`; multi-space is a follow-on. |
| **No finding-level event** — `com.archlucid.authority.run.completed` requires a second API call to load findings. | V1.1 connector has direct access to the finding projection without an extra HTTP hop. |
| **Logic Apps Standard cost** — the workflow runs on a Logic Apps Standard plan. | V1.1 connector runs server-side in ArchLucid; no Logic Apps plan required. |
| **No OAuth 2.0** — this recipe uses Basic auth (email + API token). | V1.1 connector may add OAuth 2.0 within the V1.1 release window if a buyer requests it (see [PENDING_QUESTIONS.md](../../PENDING_QUESTIONS.md) Improvement 3, sub-decisions 3a/3b). |

When V1.1 ships, migrate by:
1. Disabling the Logic App workflow.
2. Configuring the first-party Confluence connector in ArchLucid with the target space key.
3. Running a test authority run and verifying the page appears in Confluence.
4. Deleting or disabling the Logic App workflow.

---

## Related documents

| Doc | Use |
|-----|-----|
| [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) | Event delivery, CloudEvents envelope, HMAC signing |
| [INTEGRATION_EVENT_CATALOG.md](../../library/INTEGRATION_EVENT_CATALOG.md) | Full event type catalog |
| [V1_DEFERRED.md §6](../../library/V1_DEFERRED.md) | V1.1 Confluence connector commitment |
| [INTEGRATION_CATALOG.md §2](../../go-to-market/INTEGRATION_CATALOG.md) | Connector roadmap and status |
| [JIRA_ISSUE_VIA_POWER_AUTOMATE.md](JIRA_ISSUE_VIA_POWER_AUTOMATE.md) | Companion recipe — Jira via Power Automate |
| [Recipes README](README.md) | Index of all no-code recipes |

---

*Last reviewed: 2026-04-26 — event types from [IntegrationEventTypes.cs](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs) and [catalog.json](../../../schemas/integration-events/catalog.json).*

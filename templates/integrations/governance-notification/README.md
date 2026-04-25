# Governance Notification Pipeline

**Pattern:** A **CloudEvents** JSON payload for `com.archlucid.governance.approval.submitted` (see [catalog.json](../../../schemas/integration-events/catalog.json) and [governance-approval-submitted.v1.schema.json](../../../schemas/integration-events/governance-approval-submitted.v1.schema.json)) is delivered to your edge (HTTP webhook, Azure Logic App, or an Azure Function). You **validate** the `X-ArchLucid-Webhook-Signature` **HMAC** the same way as the Jira bridge recipe, then **fan out** to **Microsoft Teams** (Incoming Webhook or Graph), **email** (SMTP / Microsoft 365), and **Slack** (URL webhook) with the same field mapping.

**HMAC (inbound) — use the same contract as [jira-webhook-bridge-recipe.md](../jira/jira-webhook-bridge-recipe.md):**

- Header: `X-ArchLucid-Webhook-Signature: sha256=<lowercase-hex>`  
- Body: the **exact** raw UTF-8 bytes ArchLucid posted (if `WebhookDelivery:UseCloudEventsEnvelope` is **true**, the signed bytes are the **envelope** JSON, not only `data`).  
- Local verification: use [`validate-inbound-hmac.sh`](./validate-inbound-hmac.sh) with the same `WEBHOOK_SHARED_SECRET` you configured as `WebhookDelivery:HmacSha256SharedSecret` in ArchLucid.

```bash
# After saving the raw POST body to /tmp/body.json and copying the request header:
export WEBHOOK_SHARED_SECRET="paste-from-your-vault"   # must match server configuration
export EXPECTED_HEADER="sha256=abcdef..."            # from X-ArchLucid-Webhook-Signature
export RAW_BODY_FILE=/tmp/body.json
bash validate-inbound-hmac.sh && echo "HMAC ok"
```

**Constant-time compare** in production (do not use plain `==` for secrets in C#/Node; the shell sample is for ad hoc validation only).

On the Service Bus path (no HTTP edge), the JSON body in the message matches the `data` object in the same schema; HMAC is not applied by Service Bus — you filter by `event_type` in subscription rules instead (see [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md)).

**Deep link in notifications:** set `actionUrl` / `{{ACTION_URL}}` to the operator base URL and approval path, for example:

- `https://<your-ui-host>/governance/approval-requests/<approvalRequestIdFromPayload>/lineage`  
- Replace `<your-ui-host>` with the hostname of your **deployed** `archlucid-ui` (or your public reverse proxy in front of it), and use the `approvalRequestId` string from the CloudEvents `data` object.

**Shipped in-repo reference:** the Terraform **Service Bus** subscription and operational notes for the **governance** flow are in [`infra/terraform-logicapps/workflows/governance-approval-routing/README.md`](../../../infra/terraform-logicapps/workflows/governance-approval-routing/README.md) (outbound to Logic App host).

---

## Azure Logic App (Standard) template

**File:** [`logic-app-standard-approval-routing.json`](./logic-app-standard-approval-routing.json) — import into a **Logic App (Standard)** project, or merge the `definition` into a workflow in the designer.

**Parameters (Standard):** define a parameter value for **`teamsIncomingWebhookUri`**. The URI is a **Microsoft Teams Incoming Webhook** (Office 365 connector) URL, which is **HTTPS** and includes a long path and token. Create a connector in Teams, paste the full URL from the “Incoming Webhook” card into the parameter, and set it in **local.settings.json** / app settings (Logic App) **per environment**.

The workflow posts `{"text": "…"}` to Teams, which is the **simplest** V1 path. For a richer experience, use the next section’s Adaptive Card and replace the `Http` action’s **body** with a **POST** to an **Actionable Message** or **Graph** sender.

---

## Teams (Adaptive Card)

**File:** [`teams-approval-adaptive-card.json`](./teams-approval-adaptive-card.json) — a **skeleton** Microsoft Teams / Bot Framework **attachment** payload. Replace the `${…}` and `${actionUrl}` placeholders in your build step with values from the CloudEvents `data` object:

- `approvalRequestId`, `runId`, `manifestVersion`, `sourceEnvironment`, `targetEnvironment`, `requestedBy` (see schema)  
- `actionUrl` — deep link to the product as above (must be **https**).

**Example filled body** (illustration only; swap IDs to match your test tenant):

```json
{
  "line1": "Post this object as the HTTP `body` to a Teams flow that accepts Adaptive Cards (Incoming Webhook accepts simple `text` only; use a Bot, Power Automate, or a Logic App with the Teams connector for Adaptive Cards in production)."
}
```

---

## Email (HTML)

**File:** [`approval-email.html`](./approval-email.html) — replace the `{{APPROVAL_REQUEST_ID}}`, `{{RUN_ID}}`, `{{MANIFEST_VERSION}}`, `{{SOURCE_ENVIRONMENT}}`, `{{TARGET_ENVIRONMENT}}`, `{{REQUESTED_BY}}`, and `{{ACTION_URL}}` tokens with the same `data` fields, then pass the HTML to your **SMTP** or **Microsoft 365** sender (Graph `sendMail`, or Logic App **Office 365 Outlook** connector).

Set **subject** in your rule to: `ArchLucid: approval <approvalRequestId> submitted`

---

## Slack (optional outbound)

Post a second HTTP action with JSON:

```json
{ "text": "Governance approval *123* — run 456e789 — open: https://<your-ui>/governance/…" }
```

To your Slack **Incoming Webhook** URL. Use a **separate** secret in your vault; Slack does not use the ArchLucid HMAC (only your edge validates ArchLucid; Slack trusts **your** server).

---

## Configuration (operator)

| Name | Use |
|------|-----|
| `ARCHLUCID_API_URL` | **Not** required to *receive* webhooks; set if the same job calls back **GET** to ArchLucid for more context |
| `ARCHLUCID_API_KEY` | Only for **outbound** ArchLucid REST in the same automation job; store in a vault |
| `WEBHOOK_SHARED_SECRET` / Key Vault | Must **match** ArchLucid `HmacSha256SharedSecret` when validating HMAC on **inbound** |
| `TEAMS_INCOMING_WEBHOOK` / `teamsIncomingWebhookUri` | Per-environment Teams channel connector URL |

**Governance read APIs (optional enrichment after event):** see OpenAPI; examples include `GET` `/v1/governance/approval-requests/{id}/rationale` and `GET` `/v1/governance/approval-requests/{id}/lineage` (read policies apply).

---

## Verify it works

1. **HMAC (local file):** save a test POST body, capture `X-ArchLucid-Webhook-Signature` from a real (or `curl` replay) delivery, and run `validate-inbound-hmac.sh` (see [jira-webhook-bridge-recipe.md](../jira/jira-webhook-bridge-recipe.md) §7 for failure modes).  
2. **V1 read + API key (same env vars as other recipes):**  
   `curl -sS -H "X-Api-Key: $ARCHLUCID_API_KEY" "$ARCHLUCID_API_URL/v1/governance/dashboard"`  
   A **200** with JSON means your key is valid for at least one `GET` under the governance policy (exact shape depends on tenant).  
3. **Health:**  
   `curl -sS -o /dev/null -w "%{http_code}\n" "$ARCHLUCID_API_URL/health"`  
   should return **200**.

**Sample CloudEvents (HTTP)** — for manual tests, send **POST** to your **Logic App** callback URL (no HMAC) only on a test endpoint; in production, always require HMAC:

```json
{
  "specversion": "1.0",
  "type": "com.archlucid.governance.approval.submitted",
  "source": "/archlucid/test",
  "id": "11111111-1111-1111-1111-111111111111",
  "time": "2026-01-15T10:00:00Z",
  "datacontenttype": "application/json",
  "data": {
    "schemaVersion": 1,
    "tenantId": "00000000-0000-0000-0000-000000000001",
    "workspaceId": "00000000-0000-0000-0000-000000000002",
    "projectId": "00000000-0000-0000-0000-000000000003",
    "approvalRequestId": "apr-test-1",
    "runId": "a0000000000000000000000000000000",
    "manifestVersion": "1.0.0",
    "sourceEnvironment": "stg",
    "targetEnvironment": "prod",
    "requestedBy": "operator@contoso.com"
  }
}
```

(Real deliveries also carry the HMAC you must verify.)

---

## Troubleshooting

| Symptom | What to check |
|--------|----------------|
| **HMAC always fails** | You must hash the **exact** bytes of the string ArchLucid sent (envelope, not re-serialized JSON). `validate-inbound-hmac.sh` uses **OpenSSL** `dgst` on a **file** — ensure line endings and encoding. |
| **200 from Logic App but nothing in Teams** | Incoming Webhook URL expired or was rotated; Teams returns **4xx** — add **retry/alert** in Logic App. |
| **403** on `GET /v1/governance/...` | Policy / scope: key or JWT not authorized for the tenant. |

**Correlation:** your receiver logs the CloudEvents `id` and `data.approvalRequestId` — align with `GET /v1/audit?…` in the product if your operator has audit access, or with Application Insights on the **ArchLucid** API and **Logic App** run history.

---

## Security and reliability

- Treat **`WEBHOOK_SHARED_SECRET`** like a credential; rotate on the same schedule as the ArchLucid `HmacSha256SharedSecret` — **both** sides must update.  
- Use **idempotent** downstream creates (e.g. dedupe on `id` for Teams/email if you replay the same `POST`).  
- Do **not** add new surface area on ArchLucid; this is **outbound** HTTP + your infrastructure only.

See also: [jira-webhook-receiver.md](../jira/jira-webhook-receiver.md) for a minimal `alert.fired` **receiver** sketch; adapt `type` routing to `com.archlucid.governance.approval.submitted` for the same HMAC and HTTP shape.

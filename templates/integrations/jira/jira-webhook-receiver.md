# Jira issue create — customer-operated webhook recipe

> **Long-form V1 bridge (objectives, run + findings, Security, Operations):** [jira-webhook-bridge-recipe.md](jira-webhook-bridge-recipe.md)

**Disclaimer:** This is a **recipe template**, not a first-party ArchLucid Jira connector. First-party Jira is **V1.1**; see [V1_DEFERRED.md](../../../docs/library/V1_DEFERRED.md) §6.

**Contracts:** [catalog.json](../../../schemas/integration-events/catalog.json) · [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md)

## 1. Expose HTTPS

Deploy a stateless HTTPS endpoint (Azure Function, AWS Lambda, API Gateway + worker, etc.). Use a secret manager for `ARCHLUCID_HMAC_SECRET` (must match ArchLucid `WebhookDelivery:HmacSha256SharedSecret` when ArchLucid signs the POST, or your bridge’s secret if you re-sign).

## 2. Validate ArchLucid HMAC on the raw body

ArchLucid signs the **exact UTF-8 bytes** of the JSON body (the CloudEvents envelope when `WebhookDelivery:UseCloudEventsEnvelope` is true). Header: **`X-ArchLucid-Webhook-Signature`**, value format **`sha256=`** + **lowercase hex** (HMAC-SHA256 over the raw body). Compare with `hmac.compare_digest` (or equivalent) after stripping the prefix.

If you ingest **Service Bus** integration events instead of ArchLucid HTTP, there is no ArchLucid HMAC on the broker message—apply your own gateway signing policy, or validate only after you normalize to the same JSON shape below.

## 3. Parse CloudEvents and read `data`

Expect CloudEvents 1.0 JSON (`specversion`, `type`, `source`, `id`, `data`, …). This recipe’s worked example uses envelope `type` **`com.archlucid.alert.fired`** and `data` matching [alert-fired.v1.schema.json](../../../schemas/integration-events/alert-fired.v1.schema.json) (see catalog).

## 4. Map `data` → Jira REST `POST /rest/api/3/issue`

| ArchLucid `data` field | Jira field (example) |
|------------------------|----------------------|
| `title` | `fields.summary` (prefix with `[ArchLucid]` if desired) |
| `severity`, `category`, `alertId`, `deduplicationKey` | `fields.description` (Atlassian Document Format or plain text adapter) |
| `tenantId`, `workspaceId`, `projectId`, `runId` | same description block for correlation |

Use `POST https://<site>.atlassian.net/rest/api/3/issue` with `Authorization: Basic` (email + API token) or OAuth as required by your org.

## Worked example — `com.archlucid.alert.fired` → Jira body (minimal)

CloudEvents inbound (abbreviated):

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

Jira `fields` payload (template—set `project.key`):

```json
{
  "fields": {
    "project": { "key": "ARCH" },
    "issuetype": { "name": "Task" },
    "summary": "[ArchLucid] Example alert title",
    "description": {
      "type": "doc",
      "version": 1,
      "content": [
        {
          "type": "paragraph",
          "content": [
            { "type": "text", "text": "severity=high category=policy deduplicationKey=tenant:…:rule:…" }
          ]
        }
      ]
    }
  }
}
```

## Pinned samples (template only — pin runtime in your repo)

**Azure Functions (Python v2 programming model, Linux consumption):** use `azure-functions>=1.20.0,<2` on **Python 3.11 or 3.12**; HTTP trigger returns `400` on bad signature. See [Azure Functions Python developer guide](https://learn.microsoft.com/azure/azure-functions/functions-reference-python).

**AWS Lambda:** Python 3.12 runtime, `awslambdaric` / `mangum` optional; API Gateway HTTP API behind TLS; same HMAC gate as above. Pin `boto3` only if reading secrets from Secrets Manager.

Shared verification core (illustrative):

```python
import hmac, hashlib

def archlucid_hmac_valid(secret: str, raw: bytes, header: str | None) -> bool:
    if header is None or not header.startswith("sha256="): return False
    want, got = header[7:], hmac.new(secret.encode("utf-8"), raw, hashlib.sha256).hexdigest()
    return hmac.compare_digest(want, got)
```

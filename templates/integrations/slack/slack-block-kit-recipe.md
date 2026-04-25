# Slack Block Kit via Incoming Webhook — customer-operated recipe

> **First-party Slack support is V2.** Microsoft Teams is the supported first-party chat-ops surface for V1 / V1.1. This file is a **self-managed bridge** only. See [V1_DEFERRED.md](../../../docs/library/V1_DEFERRED.md) §6a.

**Disclaimer:** Not a first-party ArchLucid Slack connector.

**Contracts:** [catalog.json](../../../schemas/integration-events/catalog.json) · [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md)

## 1. Inbound from ArchLucid (or your Service Bus bridge)

Terminate TLS on your HTTPS function. Validate **`X-ArchLucid-Webhook-Signature`** = **`sha256=`** + lowercase hex HMAC-SHA256 over the **raw** JSON body (see [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../../docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) — CloudEvents envelope when enabled).

## 2. Parse CloudEvents

Worked example uses `type` **`com.archlucid.alert.fired`** and `data` fields from [alert-fired.v1.schema.json](../../../schemas/integration-events/alert-fired.v1.schema.json).

## 3. Outbound to Slack Incoming Webhook

Slack’s Incoming Webhook URL embeds its own secret path segment—**that is not ArchLucid HMAC**. Your function:

1. Validates ArchLucid (or bridge) on **inbound**.
2. POSTs a **new** JSON body to `https://hooks.slack.com/services/...` with Slack’s expected shape (`text` and/or `blocks`).

## Worked example — `com.archlucid.alert.fired` → Block Kit

Assume `data.title`, `data.severity`, `data.deduplicationKey` populated as in the catalog.

```json
{
  "blocks": [
    {
      "type": "header",
      "text": { "type": "plain_text", "text": "ArchLucid alert" }
    },
    {
      "type": "section",
      "fields": [
        { "type": "mrkdwn", "text": "*Severity*\nhigh" },
        { "type": "mrkdwn", "text": "*Title*\nExample alert title" }
      ]
    },
    {
      "type": "context",
      "elements": [
        { "type": "mrkdwn", "text": "dedupe: `tenant:…:rule:…`" }
      ]
    }
  ]
}
```

Keep payloads under Slack posted limits; truncate `title` defensively.

## Pinned samples (template only)

**Azure Functions:** Python 3.12, `azure-functions>=1.20.0,<2`; second HTTP client call to Slack with webhook URL from app settings / Key Vault. Pin [Block Kit reference](https://api.slack.com/block-kit) snapshot you tested.

**AWS Lambda:** Python 3.12 + API Gateway; `urllib.request.urlopen` POST to Slack webhook URL from environment/Secrets Manager.

Reuse the same HMAC helper pattern as [jira-webhook-receiver.md](../jira/jira-webhook-receiver.md) for **inbound** verification only.

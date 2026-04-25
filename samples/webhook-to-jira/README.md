# Sample: ArchLucid webhooks → Jira issues (bridge)

**Not a supported product feature.** Minimal **Node.js** receiver that:

1. Accepts `POST /webhook` with the same JSON body ArchLucid signs for outbound webhooks.
2. Validates `X-ArchLucid-Webhook-Signature: sha256=<hex>` using HMAC-SHA256 over the raw body (`WebhookSignature` in `ArchLucid.Host.Core`).
3. Parses a CloudEvents-style JSON envelope when present (`data` holds the inner payload).
4. Prints a **placeholder** Jira REST payload (set `JIRA_BASE_URL`, `JIRA_EMAIL`, `JIRA_API_TOKEN`, `JIRA_PROJECT_KEY` to call Jira for real).

**V1 posture:** first-party Jira connector is **V1.1** — see `docs/library/V1_DEFERRED.md`. Until then, customers can run this sample (or Logic Apps) in front of Jira.

## Run locally

```bash
cd samples/webhook-to-jira
set WEBHOOK_SECRET=your-shared-secret   # Windows; export on bash
node server.mjs
```

Send a signed test POST (PowerShell):

```powershell
$secret = "test-secret"
$body = '{"type":"com.archlucid.alert.fired","specversion":"1.0","id":"evt-1","source":"archlucid","data":{"title":"Demo"}}'
$hmac = [System.BitConverter]::ToString(
  [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($secret)).ComputeHash([Text.Encoding]::UTF8.GetBytes($body))
).Replace("-","").ToLowerInvariant()
Invoke-WebRequest -Uri http://127.0.0.1:8787/webhook -Method POST -Body $body -ContentType "application/json" -Headers @{ "X-ArchLucid-Webhook-Signature" = "sha256=$hmac" }
```

## Deploy

Package as an **Azure Function** HTTP trigger or **Container Apps** single revision — keep `WEBHOOK_SECRET` in **Key Vault** and reference by name in app settings only.

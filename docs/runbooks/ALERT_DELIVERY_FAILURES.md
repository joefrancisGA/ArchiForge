# Alert delivery failures

**Audience:** Operators debugging outbound alert notifications (webhooks, email, or other channels) and subscription health.

## Symptoms

- Operators see alerts **evaluated** in the API but no message at the destination.
- HTTP webhook returns non-2xx or times out; metrics `alert_delivery_failed` (if enabled) increase.
- API listing of **AlertDeliveryAttempt** rows shows **Failed** status with `ErrorMessage` populated.

## System boundaries (for diagrams)

- **Nodes:** `AlertDeliveryDispatcher` → `IAlertDeliveryChannel` implementations → external endpoints; persistence: `AlertDeliveryAttempts`, `AlertRoutingSubscriptions`, alert rows.
- **Edges:** Subscription match on severity/channel → attempt row **Started** → channel send → attempt **Succeeded** / **Failed**.
- **Flows:** One attempt row per subscription per dispatch; updates are status + error message + retry count (policy may evolve).

## Triage checklist

1. **Subscription scope**  
   Confirm `TenantId` / `WorkspaceId` / `ProjectId` on the subscription matches the alert. Disabled subscriptions are skipped.

2. **Channel configuration**  
   Verify destination URLs, headers, and secrets (see `WebhookDelivery:*` for HTTP client mode). In **Production**, `WebhookDelivery:HmacSha256SharedSecret` must be configured and meet minimum length when `UseHttpClient` is true (see startup validation).

3. **Recent attempts**  
   Use the routing API (or SQL on `dbo.AlertDeliveryAttempts`) filtered by `AlertId` or `RoutingSubscriptionId`, ordered by `AttemptedUtc` descending. Compare **Started** vs **Succeeded** vs **Failed**.

4. **Network**  
   From the API’s network context, confirm reachability to the webhook (egress, DNS, TLS, mutual TLS). Prefer private connectivity for internal receivers; avoid exposing SMB or internal file shares to the public internet.

5. **Idempotency / duplicates**  
   Multiple attempts are expected under retries or duplicate evaluations; correlate with alert id and attempt id.

## Security

- Do not log full webhook bodies if they contain PII; shared secrets belong in Key Vault / secret stores, not in tickets.

## Reliability & cost

- Channel timeouts should be bounded; failing fast preserves worker threads and surfaces degradation via health and metrics.  
- Scale: high fan-out increases outbound HTTP; consider batching or queue-backed delivery if volume grows.

## Related docs

- `docs/ALERTS.md` — route map and concepts.  
- `ADVISORY_SCAN_FAILURES.md` — advisory pipeline (often related operationally).  
- `SECRET_AND_CERT_ROTATION.md` — webhook HMAC and TLS.

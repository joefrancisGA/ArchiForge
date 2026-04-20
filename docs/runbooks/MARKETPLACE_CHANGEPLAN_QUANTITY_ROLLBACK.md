> **Scope:** Runbook — Roll back Marketplace ChangePlan / ChangeQuantity to AcknowledgedNoOp - full detail, tables, and links in the sections below.

# Runbook — Roll back Marketplace `ChangePlan` / `ChangeQuantity` to `AcknowledgedNoOp`

**Audience:** SRE / on-call billing engineer.

**When to use:** A `ChangePlan` or `ChangeQuantity` webhook from Azure Marketplace has misbehaved (mis-mapped tier, wrong seat count, unexpected mutation), and you need to **stop further mutations** while you investigate. The system was migrated to **`Billing:AzureMarketplace:GaEnabled=true`** as the shipped default on **2026-04-20** (Quality Assessment Improvement 4 Marketplace flip — see [`docs/CHANGELOG.md`](../CHANGELOG.md)). The `false` branch is **deliberately preserved** as the supported rollback path and is not dead code.

**Related:** [`docs/BILLING.md`](../BILLING.md) (operational considerations table), [`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`](../AZURE_MARKETPLACE_SAAS_OFFER.md) (webhook actions table), [`docs/adr/0016-billing-provider-abstraction.md`](../adr/0016-billing-provider-abstraction.md), migration **086** (`086_Billing_MarketplaceChangePlanQuantity.sql`).

---

## First 5 minutes (copy-paste)

1. **Confirm the symptom.** Check Application Insights / Grafana for the canary signals:

   ```kusto
   // App Insights — recent ChangePlan / ChangeQuantity webhooks and their result statuses
   customEvents
   | where timestamp >= ago(30m)
   | where name in ("Marketplace.Webhook.ChangePlan.Applied", "Marketplace.Webhook.ChangeQuantity.Applied",
                    "Marketplace.Webhook.ChangePlan.Deferred", "Marketplace.Webhook.ChangeQuantity.Deferred")
   | summarize count() by name, bin(timestamp, 1m)
   | render timechart
   ```

   ```promql
   # Grafana / Prometheus — same signal via metrics surface
   sum by (action, outcome) (rate(archlucid_billing_marketplace_webhook_total{action=~"ChangePlan|ChangeQuantity"}[5m]))
   ```

2. **Flip `Billing:AzureMarketplace:GaEnabled` to `false`** (no redeploy required) at the App Configuration / appsettings overlay layer. The provider reloads via `IOptionsMonitor<BillingOptions>`. Pick the path appropriate for your environment:

   - **Azure App Configuration (preferred):** set the key `ArchLucid:Billing:AzureMarketplace:GaEnabled` to `false` and let the API container revalidate (within `Sentinel`/cache-expiry, typically < 60 s).
   - **Container Apps env override:** add `Billing__AzureMarketplace__GaEnabled=false` to the API revision's environment variables. Container Apps will create a new revision and shift traffic; an explicit redeploy of the image is **not** required.
   - **Local / non-prod:** set the same key in `appsettings.Development.json` or via `--Billing:AzureMarketplace:GaEnabled=false` on the CLI.

3. **Verify the flip took effect** with a synthetic webhook (Microsoft-issued JWT not required for the smoke check — use the existing `BillingMarketplaceWebhookDeferredApiFactory` test factory pattern, or the curl in [`AZURE_MARKETPLACE_SAAS_OFFER.md`](../AZURE_MARKETPLACE_SAAS_OFFER.md) "Example webhook"). Expect **HTTP 202** with `AcknowledgedNoOp` instead of HTTP 200.

4. **Page the on-call billing engineer.** This is a rollback — they own the post-incident analysis, the data-fix decision, and the re-enable timing.

---

## Architecture overview

**Nodes:** Azure Marketplace → `POST /v1/billing/webhooks/marketplace` → `AzureMarketplaceBillingProvider.HandleWebhookAsync` → `MarketplaceChange{Plan,Quantity}WebhookMutationHandler.HandleAsync` → `IBillingLedger.{ChangePlanAsync, ChangeQuantityAsync}` → SQL `dbo.sp_Billing_ChangePlan` / `dbo.sp_Billing_ChangeQuantity` → `dbo.BillingSubscriptions`.

**Edges:**

- **GA path (`GaEnabled=true`, default):** webhook → handler → ledger → stored procedure → row mutated → `Marketplace.Webhook.ChangePlan.Applied` audit / metric / Service Bus event.
- **Rollback path (`GaEnabled=false`):** webhook → handler returns `MarketplaceWebhookMutationOutcome.DeferredGaDisabled` → provider records `AcknowledgedNoOp` on `dbo.BillingWebhookEvents` → returns HTTP 202 → **no** row mutation, **no** Service Bus event for that webhook.

The two handlers (`MarketplaceChangePlanWebhookMutationHandler`, `MarketplaceChangeQuantityWebhookMutationHandler`) both inspect `IOptionsMonitor<BillingOptions>.CurrentValue.AzureMarketplace.GaEnabled` on every call, which is why the flag flip propagates without a process restart.

## Component breakdown

| Component | Role |
|-----------|------|
| `Billing:AzureMarketplace:GaEnabled` (`BillingOptions.AzureMarketplace.GaEnabled`) | The single switch. `true` = mutate; `false` = `AcknowledgedNoOp`. |
| `MarketplaceChangePlanWebhookMutationHandler` | Maps `planId` → tier code; calls `IBillingLedger.ChangePlanAsync` only when GA is on. |
| `MarketplaceChangeQuantityWebhookMutationHandler` | Reads `quantity`; calls `IBillingLedger.ChangeQuantityAsync` only when GA is on. |
| `AzureMarketplaceBillingProvider.DispatchMarketplaceActionAsync` | Owns the `AcknowledgedNoOp` vs `Processed` mark on `dbo.BillingWebhookEvents`. |
| `dbo.BillingWebhookEvents` (PK `EventId`) | Idempotency log; status column tells you what each webhook resolved to. |
| `dbo.sp_Billing_ChangePlan` / `dbo.sp_Billing_ChangeQuantity` | The only paths that mutate `dbo.BillingSubscriptions`; **EXECUTE AS OWNER** because `ArchLucidApp` is `DENY INSERT/UPDATE/DELETE`. |

## Data flow during rollback

1. Operator flips `GaEnabled=false` (App Config, env, or appsettings override).
2. Within ~60 s, `IOptionsMonitor<BillingOptions>` reflects the new value.
3. The next `ChangePlan` / `ChangeQuantity` webhook is acknowledged with HTTP 202 and recorded as `AcknowledgedNoOp`.
4. The `Subscribe`, `Suspend`, `Reinstate`, `Unsubscribe` actions are **unaffected** by this flag — they continue to mutate normally.
5. Operator decides whether to (a) re-process specific events from `dbo.BillingWebhookEvents` after the mis-mapping is fixed, (b) reconcile `Tier` / `SeatsPurchased` directly via `sp_Billing_*`, or (c) re-enable GA and let the next legitimate webhook overwrite.

---

## Re-process a webhook from `dbo.BillingWebhookEvents`

A webhook is identified by the dedupe key `{subscriptionId}|{action}|{rawBodyHash}` (column `DedupeKey`, also surfaced as `EventId` for the PK). To re-process:

```sql
SELECT TOP 50
    EventId,
    DedupeKey,
    Provider,
    Action,
    ResultStatus,
    ReceivedAt,
    ProcessedAt
FROM dbo.BillingWebhookEvents
WHERE Provider = 'AzureMarketplace'
  AND Action IN ('ChangePlan', 'ChangeQuantity')
  AND ReceivedAt >= DATEADD(hour, -2, SYSUTCDATETIME())
ORDER BY ReceivedAt DESC;
```

Identify the offending row(s). To **re-drive** a specific event after a fix:

1. Pull the raw body from `dbo.BillingWebhookEvents.RawBody` for the target `EventId`.
2. POST it back to `/v1/billing/webhooks/marketplace` with the original Marketplace JWT (operationally easier: replay through Partner Center "Resend webhook" if the event is still in their retention window, since that re-issues a valid bearer).
3. The handler will see the duplicate `EventId` and return `BillingWebhookHandleResult.Duplicate()` if `ResultStatus=Processed` — that is the correct no-op behavior. To **force** re-processing, the operator must first update `ResultStatus` to a non-`Processed` value (e.g., `'Replaying'`) before resending; this is a deliberately gated mutation requiring DB owner credentials.

> **Do not** delete rows from `dbo.BillingWebhookEvents`. The audit trail is part of SOC 2 evidence; mutations to `ResultStatus` should be timestamped via `ProcessedAt` so the post-incident review can reconstruct the timeline.

---

## Reconcile `Tier` / `SeatsPurchased` after a `ChangePlan` mis-map

If a `ChangePlan` webhook mapped `planId="contoso-enterprise"` to `Enterprise` when the customer actually purchased the `Pro` plan (or vice versa), call the same stored procedure that the GA path would have called, with the corrected tier code:

```sql
EXEC dbo.sp_Billing_ChangePlan
    @TenantId = @TenantId,
    @TierCode = N'Standard',                 -- or 'Enterprise', or whatever the real plan maps to
    @RawBody  = N'{"manualReconciliation":true,"originalEventId":"<EventId>"}';
```

For a `ChangeQuantity` mis-map (wrong `SeatsPurchased`):

```sql
EXEC dbo.sp_Billing_ChangeQuantity
    @TenantId      = @TenantId,
    @SeatsPurchased = 12,                    -- the corrected number
    @RawBody       = N'{"manualReconciliation":true,"originalEventId":"<EventId>"}';
```

Both procedures are **EXECUTE AS OWNER** so they bypass the `DENY INSERT/UPDATE/DELETE` posture on `dbo.BillingSubscriptions` for `ArchLucidApp`. The `RawBody` parameter is captured in the audit trail so the post-incident timeline shows that the change came from a manual reconciliation, not from a Marketplace webhook.

---

## Confirm the rollback held

Five minutes after the flip, re-run the canary queries from § "First 5 minutes" and assert:

```kusto
// Expect Deferred to dominate, Applied to be ~0 for ChangePlan / ChangeQuantity
customEvents
| where timestamp >= ago(5m)
| where name in ("Marketplace.Webhook.ChangePlan.Applied", "Marketplace.Webhook.ChangePlan.Deferred",
                 "Marketplace.Webhook.ChangeQuantity.Applied", "Marketplace.Webhook.ChangeQuantity.Deferred")
| summarize Hits=count() by name
| order by Hits desc
```

```promql
# Grafana — Deferred outcome should be > 0; Applied for the two actions should be ~0 in the same window.
sum by (outcome) (
  rate(archlucid_billing_marketplace_webhook_total{action=~"ChangePlan|ChangeQuantity"}[5m])
)
```

If `Applied` is still non-zero after 5 minutes, the flag has **not** been picked up by the running revision — re-check that the env override or App Configuration value reached the API container. `kubectl get pods` / `az containerapp revision list` will show whether a new revision rolled.

---

## When to re-enable GA

Once the root cause is fixed (mis-mapped `planId` substring, mis-counted `quantity` payload, etc.), re-enable in the **reverse** order of the flip: appsettings/CLI → env override → App Configuration. The reverse order minimizes the window where two paths disagree.

After re-enable, the next `ChangePlan` / `ChangeQuantity` webhook should mutate normally. Spot-check a synthetic webhook before stepping away.

## Operational considerations

- **No schema changes are required to roll back.** This is purely a configuration flip.
- **No data loss.** `dbo.BillingWebhookEvents` retains every webhook regardless of `ResultStatus`; `dbo.BillingSubscriptions` is unchanged by `AcknowledgedNoOp` rows.
- **Other Marketplace actions are unaffected.** `Subscribe` / `Purchase` / `Suspend` / `Reinstate` / `Unsubscribe` continue to mutate. If those are also misbehaving, this is the wrong runbook — escalate to the on-call billing engineer for a broader provider quarantine.
- **Stripe is unaffected.** This flag only gates the Marketplace mutation handlers.
- **Tests cover both branches.** `MarketplaceChangePlanWebhookMutationHandlerTests` and `MarketplaceChangeQuantityWebhookMutationHandlerTests` exercise `GaEnabled=false` (returns `DeferredGaDisabled`) and `GaEnabled=true` (calls ledger mutation) — keep both passing whenever the handlers are touched.

## First-5-minutes summary (for the on-call dashboard)

| Action | Where | Expected outcome |
|--------|-------|------------------|
| Set `Billing:AzureMarketplace:GaEnabled=false` | App Configuration / Container Apps env / appsettings overlay | New revision (~60s); next webhook returns HTTP 202 |
| Smoke test webhook | curl from [`AZURE_MARKETPLACE_SAAS_OFFER.md`](../AZURE_MARKETPLACE_SAAS_OFFER.md) | HTTP 202, `AcknowledgedNoOp` recorded |
| Watch `Marketplace.Webhook.ChangePlan.Deferred` rate | App Insights / Grafana | Rises; `Applied` rate falls to 0 |
| Page on-call billing engineer | Pager / chat | Triage and decide on data-fix path |

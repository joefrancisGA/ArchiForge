> **Scope:** Azure Marketplace — SaaS offer (fulfillment v2) checklist - full detail, tables, and links in the sections below.

# Azure Marketplace — SaaS offer (fulfillment v2) checklist

## Objective

Stand up a **transactable** SaaS offer that lands buyers in ArchLucid while using **managed identity** to call **`https://marketplaceapi.microsoft.com/.default`** for subscription activation.

## Step-by-step (operator)

1. **Partner Center** → Commercial Marketplace → New offer → **Software as a Service**.
2. **Plan IDs** align with ArchLucid commercial tiers (`Team`, `Pro`, `Enterprise`) or map in your landing page.
3. **Technical configuration**
   - **Landing page URL:** the URL returned from `Billing:AzureMarketplace:LandingPageUrl` (must accept `tenantId`, `workspaceId`, `projectId`, `tier`, `session` query parameters from ArchLucid checkout).
   - **Webhook URL:** `https://<api-host>/v1/billing/webhooks/marketplace`
   - **Microsoft Entra ID tenant ID** for the webhook app registration (Microsoft validates JWTs against OIDC metadata).
4. **ArchLucid configuration**
   - `Billing:Provider=AzureMarketplace`
   - `Billing:AzureMarketplace:OpenIdMetadataAddress` (typically `https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration` or a tenant-specific metadata URL)
   - `Billing:AzureMarketplace:ValidAudiences` includes `https://marketplaceapi.microsoft.com`
   - `Billing:AzureMarketplace:FulfillmentApiEnabled=true` in production (set `false` only in isolated tests without network).
   - `Billing:AzureMarketplace:GaEnabled` — **default `true` since 2026-04-20** (Quality Assessment Improvement 4 Marketplace flip). `ChangePlan` / `ChangeQuantity` webhooks call `sp_Billing_ChangePlan` / `sp_Billing_ChangeQuantity` and return **HTTP 200** with the row mutated. Set to `false` only as a documented rollback (no redeploy needed via App Configuration override) — see [`runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`](runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md). When `false`, the same two webhooks return **HTTP 202** with `AcknowledgedNoOp` and do **not** mutate `dbo.BillingSubscriptions`. The `false` branch is preserved precisely so operators can roll back without code changes.
5. **Managed identity**
   - Grant the API’s user-assigned or system MI permission to call Marketplace fulfillment APIs per Microsoft guidance.

## Webhook actions (implemented subset)

| Marketplace `action` | ArchLucid behavior |
|----------------------|--------------------|
| `Subscribe` / `Purchase` | Optional HTTP **activate** (when enabled) + `TenantTrialConverted` path |
| `Suspend` | `sp_Billing_Suspend` |
| `Reinstate` | `sp_Billing_Reinstate` |
| `Unsubscribe` | `sp_Billing_Cancel` |
| `ChangePlan` | **Default (`GaEnabled=true`):** `sp_Billing_ChangePlan` updates `Tier` from `planId` (substring map: `enterprise` → `Enterprise`, else `Standard`); returns **HTTP 200**. **Rollback (`GaEnabled=false`):** **HTTP 202** + `AcknowledgedNoOp`, no mutation. |
| `ChangeQuantity` | **Default (`GaEnabled=true`):** `sp_Billing_ChangeQuantity` sets `SeatsPurchased` from numeric `quantity`; returns **HTTP 200**. **Rollback (`GaEnabled=false`):** **HTTP 202** + `AcknowledgedNoOp`, no mutation. |

### Example webhook (curl)

Replace `<api-host>` and use a real Microsoft-issued bearer JWT from Partner Center validation flow:

```bash
curl -sS -X POST "https://<api-host>/v1/billing/webhooks/marketplace" \
  -H "Authorization: Bearer <marketplace_jwt>" \
  -H "Content-Type: application/json" \
  -d '{"action":"ChangePlan","subscriptionId":"<saas_subscription_id>","planId":"contoso-enterprise","purchaser":{"tenantId":"<archlucid_tenant_guid>"}}'
```

Expect **HTTP 200** under the new default (`GaEnabled=true`) when the subscription row exists. Expect **HTTP 202** only after an explicit rollback flip — see [`runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`](runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md).

## Related

- [`docs/BILLING.md`](BILLING.md)
- [`docs/adr/0016-billing-provider-abstraction.md`](adr/0016-billing-provider-abstraction.md)
- [`docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`](runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md) — rollback procedure for the `GaEnabled=true` default.

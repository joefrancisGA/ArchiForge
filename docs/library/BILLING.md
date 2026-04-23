> **Scope:** Billing — provider abstraction (Stripe + Azure Marketplace) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Billing — provider abstraction (Stripe + Azure Marketplace)

## Objective

Provide a **single** `IBillingProvider` surface for trial conversion checkout and provider webhooks so HTTP controllers stay stable when payment channels change.

## Assumptions

- Operators may start with **Stripe** (broad SaaS default) and later prefer **Azure Marketplace** for Azure-native procurement.
- Webhooks are **unauthenticated HTTP** endpoints; trust is established only via **cryptographic verification** (Stripe signature or Microsoft-issued JWT).

## Constraints

- **No run content** or architecture payloads are sent to payment providers; only commercial metadata (tier, seat counts, scope ids).
- **Migration 078** (`078_BillingSubscriptions.sql`) is the forward migration; **074** is already used for trial seat occupants — do not renumber historical migrations.
- **Migration 086** (`086_Billing_MarketplaceChangePlanQuantity.sql`) adds `dbo.sp_Billing_ChangePlan` and `dbo.sp_Billing_ChangeQuantity` for Azure Marketplace plan/seat updates when `Billing:AzureMarketplace:GaEnabled=true`.
- `dbo.BillingSubscriptions` is **RLS-scoped**; `ArchLucidApp` is **DENY INSERT/UPDATE/DELETE** with mutations only via `dbo.sp_Billing_*` (**EXECUTE AS OWNER**).

## Architecture overview

**Nodes:** `ArchLucid.Api` → `IBillingProviderRegistry` → `StripeBillingProvider` | `AzureMarketplaceBillingProvider` | `NoopBillingProvider` → SQL `dbo.BillingSubscriptions` / `dbo.BillingWebhookEvents`.

**Edges:** Checkout (admin JWT) → provider session URL; provider → HTTPS webhook → provider implementation → SQL + `ITenantRepository.MarkTrialConvertedAsync` + `TenantTrialConverted` audit. For **Azure Marketplace**, after a successful non-duplicate webhook, the API may publish **`com.archlucid.billing.marketplace.webhook.received.v1`** to Service Bus for downstream orchestration (ADR **0019**). Optional IaC: **`enable_logic_app_marketplace_fulfillment_subscription`** in **`infra/terraform-servicebus/`** creates a topic subscription filtered to that **`event_type`** for Logic App (Standard) triggers; optional dedicated host **`enable_marketplace_fulfillment_logic_app`** in **`infra/terraform-logicapps/`** — see **`infra/terraform-logicapps/workflows/marketplace-fulfillment-handoff/README.md`**.

## Component breakdown

| Component | Role |
|-----------|------|
| `IBillingProvider` | `ProviderName`, `CreateCheckoutSessionAsync`, `HandleWebhookAsync` |
| `IBillingProviderRegistry` | Resolves provider from `Billing:Provider` |
| `IBillingLedger` | Subscription rows + webhook idempotency persistence |
| `BillingWebhookTrialActivator` | Shared “activate + convert trial + audit” side effects |
| `IBillingTrialConversionGate` | Blocks `POST /v1/tenant/convert` until an **Active** paid row exists when a paid provider is configured |

## Data flow

1. **Checkout:** Admin calls `POST /v1/tenant/billing/checkout` → ledger `Pending` row → provider returns hosted URL.
2. **Webhook:** Provider posts event → insert `BillingWebhookEvents.EventId` (PK) → process → mark `Processed`, `Failed`, or `AcknowledgedNoOp`. The `AcknowledgedNoOp` terminal status is only reached when an operator has rolled back to `Billing:AzureMarketplace:GaEnabled=false` for `ChangePlan` / `ChangeQuantity` — see [`runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`](../runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md). The default since 2026-04-20 is `GaEnabled=true`, in which case both webhooks reach `Processed`.
3. **Manual convert:** `POST /v1/tenant/convert` runs gate → `MarkTrialConvertedAsync` (optional tier update).

## Security model

- **Stripe:** `Stripe-Signature` + `Billing:Stripe:WebhookSigningSecret`.
- **Marketplace:** `Authorization: Bearer` JWT validated via OIDC metadata (`Billing:AzureMarketplace:OpenIdMetadataAddress`, `ValidAudiences`).
- **Production safety:** `Billing:Provider=Stripe` requires `Billing:Stripe:SecretKey` (see `ProductionSafetyRules.CollectBillingStripeSecret`). **`BillingProductionSafetyRules`** (same startup gate) additionally: **`sk_live_` requires** `Billing:Stripe:WebhookSigningSecret`; **`Billing:Provider=AzureMarketplace`** requires a non-loopback `Billing:AzureMarketplace:LandingPageUrl`; **`Billing:AzureMarketplace:GaEnabled=true`** requires **`Billing:AzureMarketplace:MarketplaceOfferId`** (Partner Center offer id).

## Operational considerations

- Register webhook URLs in Stripe Dashboard / Partner Center:
  - `POST /v1/billing/webhooks/stripe`
  - `POST /v1/billing/webhooks/marketplace`
- Key Vault secret names (illustrative): `billing-stripe-secret`, `billing-stripe-webhook-signing-secret`.
- **Idempotency:** duplicate provider event id → **HTTP 200** without re-processing once `ResultStatus=Processed`.
- **Marketplace GA flag:** `Billing:AzureMarketplace:GaEnabled` — **default `true` since 2026-04-20** (Quality Assessment Improvement 4 Marketplace flip; previously `false`). When `true`, `ChangePlan` / `ChangeQuantity` return **HTTP 200**, persist `Processed`, and call `sp_Billing_ChangePlan` / `sp_Billing_ChangeQuantity` to mutate `dbo.BillingSubscriptions`. The `false` branch is **preserved** as the supported rollback path: it returns **HTTP 202 Accepted**, persists `AcknowledgedNoOp`, and does **not** mutate any subscription row. Operators can flip the flag at the App Configuration / appsettings layer without redeploying — see [`runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`](../runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md).

## Provider matrix

| Buyer context | Provider | Checkout UX | Notes |
|---------------|----------|-------------|-------|
| Generic SaaS | `Stripe` | Stripe Checkout URL | Needs price ids per tier |
| Azure procurement | `AzureMarketplace` | Landing page URL + Marketplace fulfillment | MI to `marketplaceapi.microsoft.com` |
| CI / local | `Noop` | Fake URL | No webhooks |

See also: **`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`**, **`docs/security/PII_EMAIL.md`** (no run bodies in email; same spirit for billing metadata).

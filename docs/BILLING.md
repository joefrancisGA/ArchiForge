# Billing — provider abstraction (Stripe + Azure Marketplace)

## Objective

Provide a **single** `IBillingProvider` surface for trial conversion checkout and provider webhooks so HTTP controllers stay stable when payment channels change.

## Assumptions

- Operators may start with **Stripe** (broad SaaS default) and later prefer **Azure Marketplace** for Azure-native procurement.
- Webhooks are **unauthenticated HTTP** endpoints; trust is established only via **cryptographic verification** (Stripe signature or Microsoft-issued JWT).

## Constraints

- **No run content** or architecture payloads are sent to payment providers; only commercial metadata (tier, seat counts, scope ids).
- **Migration 078** (`078_BillingSubscriptions.sql`) is the forward migration; **074** is already used for trial seat occupants — do not renumber historical migrations.
- `dbo.BillingSubscriptions` is **RLS-scoped**; `ArchLucidApp` is **DENY INSERT/UPDATE/DELETE** with mutations only via `dbo.sp_Billing_*` (**EXECUTE AS OWNER**).

## Architecture overview

**Nodes:** `ArchLucid.Api` → `IBillingProviderRegistry` → `StripeBillingProvider` | `AzureMarketplaceBillingProvider` | `NoopBillingProvider` → SQL `dbo.BillingSubscriptions` / `dbo.BillingWebhookEvents`.

**Edges:** Checkout (admin JWT) → provider session URL; provider → HTTPS webhook → provider implementation → SQL + `ITenantRepository.MarkTrialConvertedAsync` + `TenantTrialConverted` audit. For **Azure Marketplace**, after a successful non-duplicate webhook, the API may publish **`com.archlucid.billing.marketplace.webhook.received.v1`** to Service Bus for downstream orchestration (ADR **0019**).

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
2. **Webhook:** Provider posts event → insert `BillingWebhookEvents.EventId` (PK) → process → mark `Processed` / `Failed`.
3. **Manual convert:** `POST /v1/tenant/convert` runs gate → `MarkTrialConvertedAsync` (optional tier update).

## Security model

- **Stripe:** `Stripe-Signature` + `Billing:Stripe:WebhookSigningSecret`.
- **Marketplace:** `Authorization: Bearer` JWT validated via OIDC metadata (`Billing:AzureMarketplace:OpenIdMetadataAddress`, `ValidAudiences`).
- **Production safety:** `Billing:Provider=Stripe` requires `Billing:Stripe:SecretKey` (see `ProductionSafetyRules.CollectBillingStripeSecret`).

## Operational considerations

- Register webhook URLs in Stripe Dashboard / Partner Center:
  - `POST /v1/billing/webhooks/stripe`
  - `POST /v1/billing/webhooks/marketplace`
- Key Vault secret names (illustrative): `billing-stripe-secret`, `billing-stripe-webhook-signing-secret`.
- **Idempotency:** duplicate provider event id → **HTTP 200** without re-processing once `ResultStatus=Processed`.

## Provider matrix

| Buyer context | Provider | Checkout UX | Notes |
|---------------|----------|-------------|-------|
| Generic SaaS | `Stripe` | Stripe Checkout URL | Needs price ids per tier |
| Azure procurement | `AzureMarketplace` | Landing page URL + Marketplace fulfillment | MI to `marketplaceapi.microsoft.com` |
| CI / local | `Noop` | Fake URL | No webhooks |

See also: **`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`**, **`docs/security/PII_EMAIL.md`** (no run bodies in email; same spirit for billing metadata).

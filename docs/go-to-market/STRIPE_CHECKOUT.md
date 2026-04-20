> **Scope:** Stripe Checkout for Team tier — engineering hand-off.

# Stripe Checkout — Team tier (hosted)

## Goal

Provide a **low-friction conversion path** from self-serve trial to paid Team tier using **Stripe Checkout**, in parallel with Azure Marketplace SaaS.

## Configuration

1. Populate Stripe secrets per `ArchLucid.Api` billing configuration (`Billing:Stripe:*` in Key Vault / environment).
2. Set `teamStripeCheckoutUrl` in `archlucid-ui/public/pricing.json` to the Stripe **Payment Link** or **Checkout Session** URL once issued.
3. Optional: continue using **`POST /v1/tenant/billing/checkout`** (`BillingCheckoutController`) for API-driven checkout when `Billing:Provider` selects Stripe.

## Webhooks

`BillingStripeWebhookController` receives Stripe events — configure the **public HTTPS** endpoint and signing secret per environment.

## Manual provisioning (until Marketplace GA settles)

If webhooks only flip entitlement bits asynchronously, document the **manual runbook** for support to confirm `dbo.Tenants.Tier` after payment (link internal ops doc when available).

## Related

- [`PRICING_PHILOSOPHY.md`](PRICING_PHILOSOPHY.md)
- [`TRIAL_AND_SIGNUP.md`](TRIAL_AND_SIGNUP.md)

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

- **Route:** `POST /v1/billing/webhooks/stripe` on the API host (see [`BILLING.md`](../BILLING.md)).
- **Verification:** `Stripe-Signature` header + `Billing:Stripe:WebhookSigningSecret` (`whsec_…` from the Dashboard).

## Staging end-to-end — Stripe **TEST** mode (`staging.archlucid.com/signup`)

Use this path **before** live keys exist: Stripe Dashboard in **Test mode**, ArchLucid API configured with **`sk_test_…`** and a **test** webhook signing secret, and marketing signup pointing at the staging API + UI.

### 1. Configure the staging API

| Setting | Value |
|---------|--------|
| `Billing:Provider` | `Stripe` |
| `Billing:Stripe:SecretKey` | `sk_test_…` (Dashboard → Developers → API keys, **Test mode**) |
| `Billing:Stripe:WebhookSigningSecret` | Test endpoint signing secret (`whsec_…` from a **test** webhook endpoint) |
| `ASPNETCORE_ENVIRONMENT` | `Staging` (or `Development` for disposable environments — **not** `Production` while on test keys) |

Production safety rules intentionally **do not** treat `sk_test_` like `sk_live_`; only **live** keys require the webhook secret pairing (`BillingProductionSafetyRules`).

### 2. Register the Stripe **test** webhook

1. Stripe Dashboard → **Developers** → **Webhooks** → **Add endpoint** (ensure **Test mode** toggle is on).
2. URL: `https://<staging-api-host>/v1/billing/webhooks/stripe` (replace with your real staging API hostname, e.g. the Container App or App Service URL behind Front Door).
3. Select events your implementation handles (at minimum those emitted by your Checkout / subscription flow — align with `StripeBillingProvider` in repo).
4. Copy the **Signing secret** into Key Vault / GitHub Environment secret for `Billing:Stripe:WebhookSigningSecret`.

### 3. Buyer journey on staging UI

1. Open `https://staging.archlucid.com/signup` (or the current staging marketing hostname).
2. Complete trial signup; trigger **Team** conversion using the Stripe **test** checkout / payment link surfaced from `pricing.json` or `POST /v1/tenant/billing/checkout`.
3. Confirm in SQL (`dbo.BillingWebhookEvents`, `dbo.BillingSubscriptions`) and tenant trial-conversion audits per [`BILLING.md`](../BILLING.md).

### 4. curl — synthetic **test** webhook (signature **will not** verify)

Stripe signs payloads with the endpoint secret; you cannot fabricate a valid `Stripe-Signature` without Stripe CLI or Dashboard “Send test webhook”. The snippets below show **transport only** — expect **400/401** from the API until a real signature is supplied.

**Send a real test event (recommended):** install [Stripe CLI](https://stripe.com/docs/stripe-cli), then:

```bash
stripe listen --forward-to https://<staging-api-host>/v1/billing/webhooks/stripe
```

Use the CLI-printed `whsec_…` as `Billing:Stripe:WebhookSigningSecret` on staging, then in another terminal:

```bash
stripe trigger checkout.session.completed
```

**Raw curl (expects verification failure):**

```bash
curl -sS -o /dev/null -w "%{http_code}\n" -X POST "https://<staging-api-host>/v1/billing/webhooks/stripe" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: t=0,v1=invalid" \
  -d '{"id":"evt_test_placeholder","type":"checkout.session.completed","data":{"object":{}}}'
```

Replace `<staging-api-host>` with your hostname. A healthy deployment returns a **non-2xx** response for invalid signatures (fail-closed). With **Stripe CLI forwarding**, the same route returns **2xx** when Stripe signs the payload.

### 5. Funnel smoke

From a developer machine (no Docker/SQL required for HTTP-only proof):

```bash
archlucid trial smoke --org "StripeStagingSmoke" --email "you+smoke@example.invalid" --api-base-url "https://<staging-api-host>"
```

See also [`docs/runbooks/MARKETING_STRIPE_GA.md`](../runbooks/MARKETING_STRIPE_GA.md) and [`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`](../runbooks/TRIAL_FUNNEL_END_TO_END.md).

## Manual provisioning (until Marketplace GA settles)

If webhooks only flip entitlement bits asynchronously, document the **manual runbook** for support to confirm `dbo.Tenants.Tier` after payment (link internal ops doc when available).

## Related

- [`PRICING_PHILOSOPHY.md`](PRICING_PHILOSOPHY.md)
- [`TRIAL_AND_SIGNUP.md`](TRIAL_AND_SIGNUP.md)
- [`BILLING.md`](../BILLING.md)

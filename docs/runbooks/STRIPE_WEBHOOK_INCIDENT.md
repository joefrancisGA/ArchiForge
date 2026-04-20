> **Scope:** Runbook — Stripe webhook incident - full detail, tables, and links in the sections below.

# Runbook — Stripe webhook incident

> **Scope:** Triage and recovery for failures of the Stripe webhook endpoint (planned: `POST /v1/billing/webhooks/stripe`). Sibling to `docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`.
>
> **Status:** Draft (2026-04-20). Endpoint not yet implemented; this runbook is staged so that when the second billing provider lands, the operational story is already in place.

## Symptom map

| Symptom | First check |
|---|---|
| Stripe dashboard shows webhook deliveries failing with HTTP 401 | Stripe signing-secret rotation; ArchLucid's `Billing:Stripe:WebhookSigningSecret` may be stale. |
| Stripe shows HTTP 200 but no row mutation in `dbo.BillingSubscriptions` | `Billing:Stripe:GaEnabled` is false (intentional rollback); confirm before changing anything. |
| ArchLucid logs show `StripeWebhookSignatureInvalid` | Signing-secret mismatch, replay window exceeded, or proxy mutated the body. |
| ArchLucid logs show `StripeWebhookIdempotencyConflict` | Stripe redelivered an event; idempotency key already processed — usually safe to ignore. |
| Charge succeeded in Stripe but tenant tier did not change | Webhook delivered to a stale ArchLucid environment, OR the subscription metadata is missing the `archlucid_tenant_id` key. |

## Triage steps (15 minutes)

1. **Confirm scope.** In the Stripe dashboard, filter webhook attempts to ArchLucid in the last hour. If failures are <1% of attempts, treat as transient and watch.
2. **Check ArchLucid logs.**
   - In Application Insights / Log Analytics:
     - `traces | where customDimensions.SourceContext startswith "ArchLucid.Application.Billing.Stripe"`
     - Look for `StripeWebhookSignatureInvalid`, `StripeWebhookIdempotencyConflict`, `StripeWebhookSubscriptionLookupMissing`.
3. **Check the GA flag.** `GET /v1/admin/configuration/billing` — confirm `stripe.gaEnabled`. If `false`, this is the documented rollback path; do not flip without product approval.
4. **Check the signing-secret age.** If `Billing:Stripe:WebhookSigningSecretRotatedUtc` is older than 90 days, rotate per § Rotation below.

## Rotation (signing secret)

1. In Stripe dashboard, **Roll** the endpoint signing secret. Stripe accepts both old and new for 24 hours.
2. Update `Billing:Stripe:WebhookSigningSecret` in Key Vault (do **not** commit).
3. Trigger a Container Apps revision redeploy or wait for the in-process secret refresh interval (default 5 minutes).
4. In Stripe dashboard, **Resend** any failed events from the rotation window.
5. Update `Billing:Stripe:WebhookSigningSecretRotatedUtc` in App Configuration (or the equivalent tracked store).

## Manual replay (after a fix)

The endpoint is **idempotent** by Stripe's `event.id`. Resending a previously-failed event is safe:

```bash
# In Stripe dashboard: Developers → Events → <event id> → Resend
```

If Stripe is unavailable, replay from the saved request body using `archlucid billing replay-stripe --event-file ./<eventId>.json` (CLI command planned with the endpoint).

## When to engage product

- Three or more `StripeWebhookSubscriptionLookupMissing` in 24 hours: the subscription metadata wiring on the **Stripe** side is wrong; only product can decide which subscriptions to backfill.
- Any `StripeWebhookSignatureInvalid` after a confirmed-clean rotation: possible attempted replay attack — page security per `docs/security/SYSTEM_THREAT_MODEL.md`.

## Related

- `docs/BILLING.md`
- `docs/adr/0016-billing-provider-abstraction.md`
- `docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md` (sibling provider rollback pattern)

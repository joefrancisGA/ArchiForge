> **Scope:** Runbook — Public marketing site + Stripe billing GA - full detail, tables, and links in the sections below.

# Runbook — Public marketing site + Stripe billing GA

This runbook tracks **Marketability Improvement 2** (public marketing go-live and Stripe self-serve paid conversion). It assumes Azure-first deployment and private storage boundaries (no SMB 445 exposure).

## Objective

Ship **`archlucid-ui`** marketing routes (`(marketing)/welcome`, `(marketing)/signup`, …) behind **Azure Front Door** with a custom domain, and operate **Stripe Checkout** + webhooks in **live** mode with idempotent SQL persistence (`docs/BILLING.md`, migration **078**).

## Assumptions

- Terraform modules under `infra/terraform-edge/` (Front Door + WAF) and application hosting (Container Apps or Static Web Apps) are already provisioned for non-prod.
- Stripe **live** keys and webhook signing secrets live in **Key Vault**, not in repo configuration.
- CI uses **non-credential-shaped** placeholders for Stripe (see `.github/copilot-instructions.md` gitleaks guidance).

## Constraints

- Webhooks are trust-on-crypto only: `POST /v1/billing/webhooks/stripe` verifies `Stripe-Signature` before any tenant mutation.
- Production requires `Billing:Provider=Stripe` plus validated secrets (`ProductionSafetyRules.CollectBillingStripeSecret`).

## Architecture overview

**Nodes:** Public DNS → Front Door → Static Web Apps or Container Apps (UI) | `ArchLucid.Api` (checkout + webhooks) → `dbo.BillingSubscriptions` / `dbo.BillingWebhookEvents`.

**Edges:** Browser → marketing pages → signup → tenant bootstrap → operator converts via Checkout URL → Stripe → webhook → `sp_Billing_*` → trial conversion.

## Component breakdown

| Area | Responsibility |
|------|----------------|
| `archlucid-ui` `(marketing)/*` | Signup + verification flows |
| `infra/terraform-edge/frontdoor.tf` | TLS, WAF, routing to origin |
| `ArchLucid.Api` `BillingCheckoutController` / `BillingStripeWebhookController` | Checkout + webhook surface |
| `ArchLucid.Persistence.Billing.Stripe` | Signature verification + ledger writes |

## Data flow

1. Register DNS + Front Door endpoint; attach custom domain and managed certificate.
2. Deploy UI artifact to the origin (SWA `az staticwebapp` or ACA revision) with environment-specific API base URL.
3. In Stripe Dashboard: enable **live** products/prices aligned with `pricing.json`; register webhook URL `https://{api-host}/v1/billing/webhooks/stripe`; copy **signing secret** to Key Vault `billing-stripe-webhook-signing-secret`.
4. Rotate any `sk_test_…` literals out of automation; use CI-safe placeholders in tests only.

## Security model

- Front Door WAF enabled; rate-limit anonymous marketing routes at the edge where possible.
- Stripe secrets never logged; webhook payloads are not echoed to application logs.
- Least privilege: runtime identity reads Key Vault secrets via managed identity.

## Operational considerations

- Smoke test: `POST /v1/tenant/billing/checkout` in staging with Stripe **test mode** before flipping DNS to production.
- Monitor `dbo.BillingWebhookEvents` for `Failed` rows and Stripe dashboard delivery retries after deploys.
- Search Console / sitemap: publish `sitemap.xml` from the marketing app once the domain is live.

## Related documentation

- `docs/BILLING.md` — provider abstraction and idempotency.
- `docs/runbooks/TRIAL_END_TO_END.md` — trial funnel validation.
- `infra/terraform-edge/frontdoor.tf` — Front Door resources.

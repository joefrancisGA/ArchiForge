> **Scope:** Short entry for operators: webhook URLs and where the full billing design lives — not a substitute for the canonical architecture doc in `docs/library/`.

# Billing documentation (entry)

**Canonical reference:** [Billing — provider abstraction (Stripe + Azure Marketplace)](library/BILLING.md) (architecture, data flow, security, provider matrix).

## Webhook routes (API)

Register these on the public API host in Stripe Dashboard and Partner Center:

- `POST /v1/billing/webhooks/stripe`
- `POST /v1/billing/webhooks/marketplace`

Implementation: `ArchLucid.Api` billing webhook controllers under route prefix `v{version}/billing/webhooks`.

# Azure Marketplace — publication checklist (operator)

## Objective

Track **Partner Center** and repository steps so a transactable SaaS offer can go live without ad-hoc gaps. Technical webhook behavior is documented in [AZURE_MARKETPLACE_SAAS_OFFER.md](../AZURE_MARKETPLACE_SAAS_OFFER.md) and [BILLING.md](../BILLING.md).

## Preconditions (owner)

1. **Microsoft Partner Center** account in **Commercial Marketplace** program.
2. **Landing page URL** aligned with `Billing:AzureMarketplace:LandingPageUrl` (accepts query parameters documented in [AZURE_MARKETPLACE_SAAS_OFFER.md](../AZURE_MARKETPLACE_SAAS_OFFER.md)).
3. **Webhook URL** reachable from Microsoft: `https://<api-host>/v1/billing/webhooks/marketplace` with Entra validation as configured.
4. **Managed identity** (or secret) authorized for Marketplace fulfillment API audience `https://marketplaceapi.microsoft.com` when activation calls are enabled.

## Publication steps

1. Create **Software as a Service** offer; map plans to ArchLucid tiers (`Team` / `Professional` / `Enterprise`) per [PRICING_PHILOSOPHY.md](PRICING_PHILOSOPHY.md) (single source of truth for list prices).
2. Paste **listing copy**; include reference-customer row from [reference-customers/README.md](reference-customers/README.md) when a **Published** row exists.
3. Complete **technical configuration** (landing page, webhook, tenant ID for JWT validation).
4. Run **certification** / validation in Partner Center; fix findings.
5. **Go live** — record date in [CHANGELOG.md](../CHANGELOG.md).

## Default Azure region

Production primary region is **Central US** for new Terraform stacks unless compliance requires otherwise — see [REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md).

## Blockers requiring human owner

- Partner Center seller verification, tax profile, payout account.
- **Azure subscription id** for production (dedicated) — see [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md).

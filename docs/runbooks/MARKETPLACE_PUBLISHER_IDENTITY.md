> **Scope:** Runbook — Microsoft Partner Center / Azure Marketplace publisher identity placeholders for commerce go-live (owner-provided IDs).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Runbook — Marketplace publisher identity

**Status:** Scaffold (2026-04-22). **No live Partner Center keys** in this repository. Decisions are recorded in [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) *Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)* (items **8** / **9d**).

## Publisher display name

**`ArchLucid`** — the customer-facing **publisher display name** on the commercial marketplace listing (owner decision 2026-04-22).

## Microsoft Partner Network (MPN) ID

<!-- TODO(owner) -->

**MPN ID:** `<<MPN_ID>>` — replace after the owner records the real Microsoft Partner Network ID from Partner Center.

## Marketplace Offer ID

<!-- TODO(owner) -->

**Offer / product ID:** `<<OFFER_ID>>` — maps to application configuration key **`Billing:AzureMarketplace:MarketplaceOfferId`** (see [`ArchLucid.Api/appsettings.json`](../../ArchLucid.Api/appsettings.json) and [`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`](../AZURE_MARKETPLACE_SAAS_OFFER.md)).

## CI alignment

Tier naming for publication docs is guarded by **`python scripts/ci/assert_marketplace_pricing_alignment.py`** against [`docs/go-to-market/PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md). That script does **not** validate Partner Center identity fields — this runbook is the placeholder surface until the owner fills `<<MPN_ID>>` and `<<OFFER_ID>>`.

## Footnote (legal entity vs display name)

If a separate legal entity (for example **`ArchLucid Inc.`**) is incorporated later, **Partner Center tax + payout profiles** will require the **legal entity name** on tax and banking records while the **publisher display name** on the listing card may remain **`ArchLucid`** per branding decision.

## Related

- [`docs/go-to-market/MARKETPLACE_PUBLICATION.md`](../go-to-market/MARKETPLACE_PUBLICATION.md)
- [`docs/runbooks/STRIPE_WEBHOOK_INCIDENT.md`](STRIPE_WEBHOOK_INCIDENT.md)

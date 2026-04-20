> **Scope:** ADR 0016 — Billing provider abstraction (Stripe + Azure Marketplace) - full detail, tables, and links in the sections below.

# ADR 0016 — Billing provider abstraction (Stripe + Azure Marketplace)

## Status

Accepted (2026-04-17)

## Context

Trial conversion requires hosted checkout and asynchronous payment confirmation. Stripe and Azure Marketplace use different client credentials, webhook authentication models, and fulfillment APIs.

## Decision

- Introduce **`IBillingProvider`** + **`IBillingProviderRegistry`** (resolved from `Billing:Provider`).
- Persist subscription state in **`dbo.BillingSubscriptions`** with **RLS** and **stored-procedure-only** mutations for the least-privilege SQL role.
- Record webhook attempts in **`dbo.BillingWebhookEvents`** with **primary key idempotency** on provider event identifiers.
- Keep **HTTP controllers** thin: Stripe and Marketplace webhook routes delegate to the respective provider implementation.

## Consequences

- **Positive:** Controllers remain stable when adding providers (e.g., SendGrid-style additions stay out of MVC).
- **Positive:** Production can enforce Stripe secret presence independently of Marketplace OIDC settings.
- **Trade-off:** Contracts live in **`ArchLucid.Core.Billing`** (not `ArchLucid.Application`) to avoid a **Persistence ↔ Application** circular dependency while still mirroring the “application boundary” intent.

## Compliance / security notes

- Webhooks are **anonymous** endpoints; all trust is in **signature/JWT** verification and **SQL idempotency**.
- See **`docs/security/SYSTEM_THREAT_MODEL.md`** (billing webhook row).

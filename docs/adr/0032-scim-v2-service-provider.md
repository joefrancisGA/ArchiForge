> **Scope:** ADR 0032 — SCIM 2.0 inbound service provider — full detail in the sections below.
>
> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).

# ADR 0032: SCIM 2.0 inbound service provider (RFC 7644)

- **Status:** Accepted (implementation shipped in repo)
- **Date:** 2026-04-24
- **Supersedes:** *(none)*
- **Superseded by:** *(none)*

## Context

Enterprise customers expect **automated user lifecycle** from their IdP (Microsoft Entra ID, Okta, OneLogin). SCIM 2.0 is the standard contract for **inbound** provisioning into SaaS tenants. Without it, procurement and IT security reviews stall.

## Decision

ArchLucid implements a **SCIM 2.0 Service Provider** surface under dedicated routes (`/scim/v2/...`), authenticated only via a custom **`ScimBearer`** scheme backed by **per-tenant bearer tokens** (hashed at rest with **Argon2id**, salt derived from `tenantId`). **JWT and API-key sessions never satisfy SCIM routes** — IdPs configure a long-lived bearer secret independent of interactive operator auth.

**Tenant context** for mutating SCIM operations is taken exclusively from **`IScopeContextProvider`** after token validation maps the bearer credential to a tenant; clients cannot assert an arbitrary tenant id in the SCIM payload path.

**Group → role** mapping defaults to well-known `archlucid:*` keys with optional **`Scim:GroupRoleMappingOverrides`** (`Dictionary<string,string>` in configuration).

**Enterprise seat accounting** uses `dbo.Tenants.EnterpriseSeatsLimit` / `EnterpriseSeatsUsed`; only **`Active = true`** SCIM users consume a seat; deprovisioning frees capacity.

**Out of scope (explicit):** SCIM Bulk, outbound provisioning, hosted Entra gallery listing (owner-only), complex PATCH selectors (`members[value eq …]`).

## Consequences

- **Security:** Tokens are high-entropy, stored hashed, compared in constant time after Argon2id verification; SCIM controllers are never `[AllowAnonymous]`.
- **Scalability:** Filter translation to SQL keeps list endpoints bounded (`count` clamped); in-memory storage mode uses the same parser with an in-memory evaluator (not SQL-backed at scale).
- **Reliability:** Token rotation is **non-destructive** until explicit revoke; prior tokens remain valid (documented operator trade-off vs forced rotation).
- **Cost:** Argon2id parameters are tuned for **interactive admin issuance** volumes, not password-login scale; abuse is mitigated by **admin-gated** token mint and rate limits on the shared API host.

## Alternatives considered

- **Shared JWT for SCIM:** Rejected — conflates human operator sessions with headless IdP automation and complicates least-privilege reviews.
- **Separate SCIM microservice:** Deferred — same process reduces operational surface for v1; can split later with identical route prefix behind a gateway.

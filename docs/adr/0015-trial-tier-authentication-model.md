> **Scope:** ADR 0015 — Trial-tier authentication model (External ID + local identity) - full detail, tables, and links in the sections below.

# ADR 0015 — Trial-tier authentication model (External ID + local identity)

**Status:** Accepted  
**Date:** 2026-04-17

## Context

Self-service trials must work when the customer has **not** completed workforce Entra federation. We still want **production-grade** controls: clear trust boundaries, JwtBearer alignment, and safe defaults.

## Decision

Introduce **`Auth:Trial:Modes`** with two optional lanes:

1. **`MsaExternalId`** — Microsoft Entra **External ID (CIAM)** for consumer IdPs (MSA, Google, hosted local accounts in CIAM). **Production** configuration validation **fails** if this mode is enabled without **`Auth:Trial:ExternalIdTenantId`**.
2. **`LocalIdentity`** — ArchLucid-hosted **email/password** in SQL (**`dbo.IdentityUsers`**, migration **077**) using **PBKDF2**, **lockout**, **NIST-style length policy**, optional **HIBP k-anonymity** checks, and **mandatory email verification** before **`TrialProvisioned`**.

Minted local trial JWTs reuse the existing **JwtBearer** public-key path so **`ArchLucidPolicies`** and role transformations stay consistent.

## Alternatives considered

- **B2C-only naming / legacy stacks** — Rejected for greenfield docs; standardize on **External ID** terminology.
- **Third-party auth vendor** — Rejected for default Azure alignment and buyer expectation on MSA.
- **Long-lived API keys for trials** — Rejected: weak revocation story vs short-lived JWTs.

## Consequences

- **Positive:** Single RBAC/policy surface; trials do not fork authorization rules.
- **Negative:** Operators must maintain **two** auth surfaces when both modes are enabled; threat model and monitoring must include both.
- **Operational:** Terraform **`infra/terraform-entra/external_id.tf`** documents External Id wiring; full user-flow automation may require Graph/`azapi` follow-up.

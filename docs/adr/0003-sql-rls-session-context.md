# ADR 0003: SQL RLS and SESSION_CONTEXT

- **Status:** Accepted
- **Date:** 2026-04-04

## Context

Multi-tenant data in SQL Server should be isolated even if application bugs omit scope predicates.

## Decision

Deploy RLS policies with **`SqlServer:RowLevelSecurity:ApplySessionContext=true`** in **Production** when `ArchLucid:StorageProvider=Sql`. The applicator sets `SESSION_CONTEXT` keys for tenant/workspace/project per connection.

## Consequences

- **Positive:** Defense in depth aligned with enterprise expectations.
- **Negative:** Connection setup overhead; misconfiguration fails startup validation by design.

## Links

- `docs/security/MULTI_TENANT_RLS.md` (if present) or migration `036_RlsArchiforgeTenantScope.sql`

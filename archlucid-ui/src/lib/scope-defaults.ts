/**
 * Development default tenant id — mirrors `ArchLucid.Core.Scoping.ScopeIds.DefaultTenant` on the API.
 * Used only as a last-resort UI fallback when `/me` lacks a `tenant_id` claim (never for production isolation).
 */
export const DEFAULT_DEV_TENANT_ID = "11111111-1111-1111-1111-111111111111";

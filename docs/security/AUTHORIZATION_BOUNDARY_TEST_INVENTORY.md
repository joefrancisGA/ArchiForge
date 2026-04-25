> **Scope:** Developers finding Core integration tests for RBAC, API keys, and tenant RLS — not a complete threat model or compliance attestation.

# Authorization boundary test inventory

Integration tests that lock in **RBAC** (Reader / Operator / Admin policy surfaces), **API key** behavior, and **tenant isolation** (SQL row-level security with `SESSION_CONTEXT`) are listed below. They are part of the **Core** integration suite.

**How to run**

```bash
dotnet test ArchLucid.Api.Tests/ArchLucid.Api.Tests.csproj --filter "Suite=Core&Category=Integration"
```

**Test files**

| File | Purpose |
|------|---------|
| [ArchLucid.Api.Tests/Security/AuthorizationBoundaryTests.cs](../../ArchLucid.Api.Tests/Security/AuthorizationBoundaryTests.cs) | API key: Reader vs policies, anonymous 401, health |
| [ArchLucid.Api.Tests/Security/TenantIsolationSmokeTests.cs](../../ArchLucid.Api.Tests/Security/TenantIsolationSmokeTests.cs) | SQL + RLS: two tenants, run visibility |
| [ArchLucid.Api.Tests/Security/ApiKeyReaderAndAdminArchLucidApiFactory.cs](../../ArchLucid.Api.Tests/Security/ApiKeyReaderAndAdminArchLucidApiFactory.cs) | `WebApplicationFactory` with `ArchLucidAuth:Mode=ApiKey` and read + admin keys |
| [ArchLucid.Api.Tests/Security/SqlRlsTenantIsolationApiFactory.cs](../../ArchLucid.Api.Tests/Security/SqlRlsTenantIsolationApiFactory.cs) | `GreenfieldSqlApiFactory` + `SqlServer:RowLevelSecurity:ApplySessionContext=true` |

**Related (not duplicated here)**

- [SupportBundleEndpointTests.cs](../../ArchLucid.Api.Tests/SupportBundleEndpointTests.cs) — `ExecuteAuthority` on `POST /v1/admin/support-bundle` (Reader 403, unauthenticated 401, DevelopmentBypass 200).
- [HealthEndpointSecurityIntegrationTests.cs](../../ArchLucid.Api.Tests/HealthEndpointSecurityIntegrationTests.cs) — anonymous vs API key on `/health` and `/health/ready` (detailed `/health` requires an authenticated principal with read access in ApiKey mode).

## Authorization boundary tests (`AuthorizationBoundaryTests`)

| # | Test (method) | HTTP | Expected | Notes |
|---|---------------|------|----------|--------|
| 1 | `Reader_key_cannot_POST_architecture_request_returns_403` | `POST /v1/architecture/request` | **403** | `ExecuteAuthority`; Reader key only |
| 2 | `Reader_key_cannot_POST_run_commit_returns_403` | `POST /v1/architecture/run/{runId}/commit` | **403** | Commit requires execute |
| 3 | `Reader_key_cannot_POST_demo_seed_returns_403` | `POST /v1/demo/seed` | **403** | Same; Development host may also return 404 if demo disabled (403 first if policy runs) |
| 4 | `Reader_key_GET_run_returns_200_or_404_not_403` | `GET /v1/architecture/run/{runId}` | **200** or **404**, not **403** | `ReadAuthority`; missing run is 404 |
| 5 | `Reader_key_cannot_GET_admin_config_summary_returns_403` | `GET /v1/admin/config-summary` | **403** | `AdminAuthority` only |
| 6 | `No_api_key_on_protected_list_runs_returns_401` | `GET /v1/architecture/runs?limit=1` | **401** | Unauthenticated in ApiKey mode |
| 7 | `Valid_reader_api_key_on_health_ready_returns_200` | `GET /health/ready` | **200** | Liveness is public; key still allowed |
| 8 | `Valid_reader_api_key_on_detailed_health_returns_200` | `GET /health` | **200** | Reader has `ReadAuthority` for full health (see [HealthEndpointSecurityIntegrationTests](../../ArchLucid.Api.Tests/HealthEndpointSecurityIntegrationTests.cs)) |

## Tenant isolation (`TenantIsolationSmokeTests`)

These run only when **either** `ARCHLUCID_API_TEST_SQL` **or** `ARCHLUCID_SQL_TEST` is set to a **reachable** SQL Server (same as other explicit SQL work in [docs/engineering/BUILD.md](../engineering/BUILD.md)) **and** a **4s connect probe** to `master` succeeds. RLS is verified only in that configuration (localhost-only Windows is intentionally **not** used here so a stopped LocalDB does not block the test host for minutes). If the check fails, the test is **skipped** (via **Xunit.SkippableFact** — there is no custom `[SkipIfNoSql]` attribute; the skip reason matches the “no SQL/RLS for this run” intent).

| # | Test (method) | HTTP / action | Expected |
|---|---------------|---------------|----------|
| 1 | `Tenant_b_cannot_see_tenant_a_run_sql_rls` | Tenant **A** `POST /v1/architecture/request` → `runId`; tenant **B** `GET`/`GET` list with different `x-tenant-id` (and matching workspace / project) | B: `GET /v1/architecture/run/{runId}` → **404**; list for B does not contain the run; A: `GET` same run → **2xx** |

**Factory note:** [SqlRlsTenantIsolationApiFactory.cs](../../ArchLucid.Api.Tests/Security/SqlRlsTenantIsolationApiFactory.cs) layers `SqlServer:RowLevelSecurity:ApplySessionContext` on the greenfield SQL `WebApplicationFactory` so the API’s `RlsSessionContextApplicator` applies `al_tenant_id` (and related keys) on each connection.

**Operational note:** RLS is described in [MULTI_TENANT_RLS.md](MULTI_TENANT_RLS.md). Break-glass configuration does not replace per-tenant `SESSION_CONTEXT` for normal app traffic; greenfield test hosts may still set break-glass for bootstrap—see in-repo comments on `SqlRowLevelSecurityBypassAmbient` and [GreenfieldSqlApiFactory.cs](../../ArchLucid.Api.Tests/GreenfieldSqlApiFactory.cs).

## Policy reference (read-only; attributes are not changed in tests)

Policies are defined in `ArchLucid.Core.Authorization.ArchLucidPolicies`: `ReadAuthority` (Reader+), `ExecuteAuthority` (Operator+), `AdminAuthority` (Admin). API key mapping to roles is in `ArchLucid.Api/Authentication/ApiKeyAuthenticationHandler.cs` (read-only key → Reader; admin key → Admin).

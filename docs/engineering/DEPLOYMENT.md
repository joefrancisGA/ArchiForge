> **Scope:** Deployment and rollback umbrella for **ArchLucid internal operators and release managers** - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

> **Audience banner — read first.** ArchLucid is a **SaaS** product. **Customers never deploy ArchLucid; ArchLucid hosts it for them at `archlucid.com`.** This document is for **internal ArchLucid operators and release managers** running our hosted production / staging environments. Customer entry points: **[`START_HERE.md`](../START_HERE.md)** "Audience split" and `archlucid.com`.

# Deployment and rollback (umbrella — internal operators)

This document ties together how **ArchLucid** (product; repository and assemblies still use `ArchLucid.*` until rename Phase 5–6) is released, how database changes roll forward, and where to find deeper procedures. It is aimed at **internal operators and release managers**, not at local `docker compose`-only workflows (see **`docs/engineering/BUILD.md`** and **`docs/engineering/CONTAINERIZATION.md`**).

**New to the repo?** Phased checklist from laptop to Azure: **`docs/onboarding/day-one-sre.md`** (and **`docs/START_HERE.md`** hub).

## Objectives

- Apply application + infrastructure changes in a **predictable order** (schema before behavior that depends on new columns, or feature flags when order cannot be guaranteed).
- Preserve a **credible rollback story**: either revert application version, restore data, or disable features — not all three are always possible; the runbooks below spell out which applies.

## Assumptions

- Production uses **`ArchLucid:StorageProvider=Sql`** (see [ADR 0011 — InMemory vs Sql](../adr/0011-inmemory-vs-sql-storage-provider.md)).
- SQL is reachable only from **private network paths** (private endpoint / VNet integration), not from the public internet.
- Optional components (Redis, Azure AI Search, etc.) follow the same “config-gated” pattern as in **`infra/`** Terraform roots.

## Application deployment

1. **Build and publish** the API image (or package) from **`ArchLucid.Api`** using your pipeline; tag with an immutable version. The same Docker image also carries **`ArchLucid.Worker.dll`** for Azure Container Apps worker revisions (see **`docs/engineering/CONTAINERIZATION.md`**).
2. **Run database migrations** with **DbUp** (`ArchLucid.Persistence.Data.Infrastructure.DatabaseMigrator`) against the target database **before** or **in lockstep** with rolling out the API version that requires new schema. See **`docs/runbooks/MIGRATION_ROLLBACK.md`** for failure handling.
3. **Roll out** the new API revision (App Service slot swap, Container Apps `az containerapp update`, AKS rolling update, etc.). Prefer **health-checked** deployments so readiness fails if SQL or required config is wrong. For GitHub Actions–driven Azure Container Apps, use **[DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md)** (build → push to ACR → update API, optional worker and UI apps → smoke).
4. **Smoke** critical paths: architecture run create → execute → commit, comparison replay (if enabled), governance endpoints if used.

## Rollback

- **Application-only rollback:** deploy the previous image/package. Safe when the new version did **not** apply irreversible migrations.
- **After forward-only migrations:** rolling back code without reverting schema may still work if new columns are unused; if not, plan **forward fixes** instead of schema downgrade (preferred posture — see migration runbook).
- **Disaster / data loss:** restore from **point-in-time** or geo-replicated copy; see **`docs/runbooks/DATABASE_FAILOVER.md`**.

## Related documentation

| Topic | Document |
|--------|-----------|
| Migrations, DbUp, rollback posture | [runbooks/MIGRATION_ROLLBACK.md](../runbooks/MIGRATION_ROLLBACK.md) |
| RTO/RPO targets by tier (dev / staging / production) | [RTO_RPO_TARGETS.md](../library/RTO_RPO_TARGETS.md) |
| Azure SQL HA, failover, RPO/RTO | [runbooks/DATABASE_FAILOVER.md](../runbooks/DATABASE_FAILOVER.md) |
| Terraform roots and environments | [infra/README.md](../../infra/README.md) |
| GitHub Actions CD (ACR, Container Apps, optional Terraform) | [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md) |
| Failed deploy / manual rollback (operators) | [DEPLOYMENT_RUNBOOK.md](../library/DEPLOYMENT_RUNBOOK.md) |
| Containers and compose profiles | [CONTAINERIZATION.md](CONTAINERIZATION.md) |
| Build and test | [BUILD.md](BUILD.md) |
| Storage provider semantics | [adr/0011-inmemory-vs-sql-storage-provider.md](../adr/0011-inmemory-vs-sql-storage-provider.md) |

## CORS (browser → API)

The API registers policy **`ArchLucid`** (`UseCors("ArchLucid")` in the pipeline).

| Configuration | Behavior |
|---------------|----------|
| **`Cors:AllowedOrigins`** (array) | If **empty or missing**, **no** browser origin is allowed (`SetIsOriginAllowed(_ => false)`). If non-empty, only those exact origins receive `Access-Control-Allow-Origin`. |
| **`Cors:AllowedMethods`** (array, optional) | Defaults: **`GET`**, **`POST`**, **`PUT`**, **`DELETE`**, **`OPTIONS`**. **`OPTIONS`** is required for preflight. If you set this array, it **replaces** the default list (include every method you need). |
| **`Cors:AllowedHeaders`** (array, optional) | Defaults: **`Content-Type`**, **`Authorization`**, **`X-Api-Key`**, **`X-Correlation-ID`**, **`Idempotency-Key`**, **`Accept`**. Aligns with the operator UI proxy and idempotent run creation. If you set this array, it **replaces** the default list—add any extra request headers your SPA sends. |

Production validation (`ArchLucidConfigurationRules.CollectErrors` → `ProductionSafetyRules` + `BillingProductionSafetyRules`) still requires a non-empty **`Cors:AllowedOrigins`** without wildcard `*`.

## Security note

Do not expose SMB (port 445) or SQL endpoints publicly. Align with private endpoints and controlled boundaries described in infrastructure Terraform and org network standards.

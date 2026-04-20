> **Scope:** RTO / RPO targets by environment tier - full detail, tables, and links in the sections below.

# RTO / RPO targets by environment tier

**Last reviewed:** 2026-04-04

## Objective

Document **Recovery Time Objective (RTO)** and **Recovery Point Objective (RPO)** expectations for ArchLucid so landing zones, SRE, and procurement can align Azure SKUs (SQL HA, geo-replication, backups) with business requirements. This file is **policy guidance** for the product; your organization may tighten or relax numbers in internal runbooks.

## Assumptions

- **Production** uses Azure SQL (not single-instance Docker) and deploys API/worker via Container Apps or equivalent with health probes.
- **Development** may use local SQL Server, compose, or shared non-HA Azure resources.
- RPO/RTO for **relational data** are driven primarily by **Azure SQL** configuration; blob, queue, and Redis have separate continuity characteristics.

## Constraints

- Achieving **RPO under five minutes** for SQL typically requires **geo-redundant** patterns (e.g. auto-failover group with async secondary, or equivalent) and application connection strings that follow the **failover group read/write listener** — see `docs/runbooks/DATABASE_FAILOVER.md`.
- Targets below are **not** contractual SLAs unless your organization adopts them formally. They are **defaults** for planning and Terraform/SKU conversations.

## Architecture overview (continuity)

**Nodes:** API, Worker, Azure SQL (primary / optional secondary), Storage (blob/queue), edge (optional).

**Edges:** Clients → compute → SQL/storage; SQL primary → geo-secondary (when enabled).

**Flows:** Writes commit on primary; replication lag defines **RPO**; cutover + app recovery time defines **RTO**.

## Tier targets

| Tier | RTO (time to restore service) | RPO (max acceptable data loss) | SQL posture (example) | Non-SQL (blob / queue / UI) |
|------|------------------------------|----------------------------------|-------------------------|-----------------------------|
| **Development** | **Best-effort** (hours acceptable; no on-call) | **Best-effort** (rebuild or restore from last backup if any) | Single database, local or low-cost Azure SQL; no geo-DR required | Local Azurite or disposable storage; no DR requirement |
| **Staging / pre-production** | **Under 4 hours** (business hours remediation) | **Under 1 hour** (acceptable to replay test data or restore from automated backup) | Azure SQL with **automated backups**; optional **zone-redundant** compute; geo-DR optional | Match production topology at smaller SKU where feasible; document if intentionally non-HA |
| **Production** | **Under 1 hour** for regional SQL failure (target; tune with drills) | **Under 5 minutes** for committed relational data (target via **geo-replication** / **auto-failover group** and low replication lag) | **Auto-failover group** or **active geo-replication** with **read/write listener**; zone-redundant where available | Geo-redundant storage for artifacts and job queues where durability is required; redeploy Container Apps revisions from ACR |

### Production — SQL RPO under 5 minutes (how it maps to Azure)

- **Intent:** Committed transactions on the **current primary** should appear on the **secondary** within a few minutes under normal load, so a regional failover loses at most roughly **five minutes** of writes (organization-specific; monitor **replication lag** in Azure Monitor).
- **Mechanism:** **Azure SQL auto-failover group** (preferred) or **geo-replication** with a defined failover procedure.
- **Application:** `ConnectionStrings:ArchLucid` (and read-replica settings if used) must use the **failover group listener** hostname so the app follows the primary after failover — see `docs/runbooks/DATABASE_FAILOVER.md`.

### Production — RTO &lt; 1 hour

- **Intent:** From “declare incident” to “API and worker pass `/health/ready` and accept scoped traffic” within **one hour**, assuming runbooks and credentials are ready.
- **Components:** DNS / Front Door / APIM updates (if any), Container Apps revision health, **Key Vault** / connection string updates if not using group listener, validation smoke tests.

## Component breakdown

| Component | RPO driver | RTO driver |
|-----------|------------|------------|
| **Azure SQL (authority + outbox)** | Replication lag, backup frequency | Failover automation, listener, app restart |
| **Blob (artifacts, large payloads)** | Storage redundancy / GRS | Redeploy + restore from secondary region if required |
| **Queue (durable jobs)** | Message retention / replication | Drain backlog after restore; worker scale |
| **Redis (cache)** | *Not a durability tier* — RPO N/A for cache | Rebuild cache; RTO subordinate to SQL |
| **Entra / secrets** | Microsoft-managed | Rotation runbooks |

## Data flow (failover)

1. Primary SQL stops accepting writes (outage or drill).
2. Azure or operator initiates **failover** to secondary region.
3. Apps using the **group listener** reconnect to new primary (or secrets updated if using fixed hostnames).
4. Workers drain **outbox** and queues; expect transient errors during cutover — clients should **retry with backoff**.

## Security model

- Failover must **not** widen network exposure (private endpoints, firewall rules stay least-privilege).
- RLS **`SESSION_CONTEXT`** applies on each connection after failover the same as before — no change to tenant isolation semantics.

## Operational considerations

- **Drills:** Run at least annual **geo-failover** exercises for production; record actual RTO/RPO achieved vs table above.
- **Monitoring:** Alert on **geo-replication lag** and failed failovers; tie to SLO dashboards in `infra/grafana/dashboard-archlucid-slo.json` and `docs/runbooks/SLO_PROMETHEUS_GRAFANA.md`.
- **Review:** Revisit this document after major topology changes (single region → multi-region, SQL tier change).

## Related documentation

- `docs/runbooks/DATABASE_FAILOVER.md` — operational steps, listener, connection strings.
- `docs/DEPLOYMENT_TERRAFORM.md` — Terraform roots and ordering.
- `docs/ONBOARDING_HAPPY_PATH.md` — end-to-end flow for post-failover smoke tests.

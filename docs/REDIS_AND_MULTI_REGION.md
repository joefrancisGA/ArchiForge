> **Scope:** Redis and multi-region patterns - full detail, tables, and links in the sections below.

# Redis and multi-region patterns

## Redis (shared cache / hot path)

**Invalidation:** Run `GetById`, golden manifest `GetById`, and policy pack `GetById` are cached when `HotPathCache:Enabled` is true. Writes through the corresponding repositories evict affected keys; **data archival** evicts every run row archived in a batch so archived runs do not linger in cache until TTL. Artifact lists, snapshots, and alert/planning reads are **not** behind this hot-path decorator today.

- **Local / compose**: `docker-compose.yml` runs a single Redis node for development. This is not highly available.
- **Production**: Use **Azure Cache for Redis** (Standard or Premium with replication) in the same region as your Container Apps, or in a paired region with private connectivity. Point `HotPathCache:RedisConnectionString` (and LLM cache when using Redis) at the Azure endpoint.
- **Terraform**: There is no dedicated `terraform-redis` stack in-repo yet; add a module that provisions `azurerm_redis_cache` with `minimum_tls_version = "1.2"`, private endpoint (via `terraform-private`), and outputs the connection string for Key Vault / Container App secrets.

## Multi-region application tier

- **Front Door**: `infra/terraform-edge` supports an optional **secondary origin** (`secondary_backend_hostname`, `secondary_origin_host_header`). Set the primary to your main region API/APIM and the secondary to a standby region. Traffic fails over when the primary fails health probes.
- **SQL**: Use `infra/terraform-sql-failover` for geo-redundant databases; align connection strings and RLS session context with your DR runbooks (`docs/runbooks/DATABASE_FAILOVER.md`).
- **Container Apps**: A full active-active multi-region app tier requires a second Container Apps environment, replicated secrets, and coordinated deployments. The CD workflow (`.github/workflows/cd.yml`) pushes images once; promote the same tag to each region or use region-specific pipelines.

## Cost / ops tradeoffs

- Redis HA and second regions increase monthly baseline cost. Enable when RTO/RPO targets (`docs/RTO_RPO_TARGETS.md`) require them, not by default in dev.

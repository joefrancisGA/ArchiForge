# Infrastructure (Terraform)

ArchLucid splits Azure infrastructure into **optional Terraform roots** so local and laptop workflows stay default-off. Each root has its own `README.md`, `terraform.tfvars.example`, and `.terraform.lock.hcl`.

**First-time path:** **`docs/GOLDEN_PATH.md`** (Phase 3 Azure + **advanced appendix** for optional roots like edge, failover group, APIM).

**Ordered validate / apply (no merged state):** **`scripts/provision-landing-zone.ps1`** / **`scripts/provision-landing-zone.sh`** and **`docs/LANDING_ZONE_PROVISIONING.md`**. Example tfvars sketches: **`infra/environments/`**.

| Root | Purpose | Default |
|------|---------|---------|
| [`terraform/`](terraform/) | Core Azure resources (optional **API Management Consumption**, managed identity, outputs for API URL). | Feature flags **`false`** |
| [`terraform-edge/`](terraform-edge/) | **Azure Front Door Standard + WAF** in front of APIM or the API hostname. | **`enable_front_door_waf = false`** |
| [`terraform-private/`](terraform-private/) | VNet, private DNS, **private endpoints** for **SQL** and **Blob**. | **`enable_private_data_plane = false`** |
| [`terraform-entra/`](terraform-entra/) | **Microsoft Entra ID** app registration with **Admin / Operator / Reader** app roles aligned with the API. | **`enable_entra_api_app = false`** |
| [`terraform-container-apps/`](terraform-container-apps/) | **Azure Container Apps**: Log Analytics, environment, **API + background Worker + Operator UI**, **Hosting__Role** split (**Api** vs **Worker**), **min/max replicas**, HTTP scale rules for API/UI, and **ArtifactLargePayload** env + **blob RBAC** on API and Worker when enabled. | **`enable_container_apps = false`** |
| [`terraform-storage/`](terraform-storage/) | **Storage account** + **private blob containers** for **large manifest/bundle offload** (`ArtifactLargePayload`, SQL pointer columns). | **`enable_storage_account = false`** |
| [`terraform-monitoring/`](terraform-monitoring/) | **Azure Monitor** action group + optional **Container App CPU metric alerts**; optional **Azure Managed Grafana**. Dashboard JSON templates under [`grafana/dashboards/`](grafana/dashboards/). | **`enable_monitoring_stack = false`** |
| [`terraform-sql-failover/`](terraform-sql-failover/) | **Azure SQL failover group** (`azurerm_mssql_failover_group`): **IaC-backed read/write listener** FQDN for geo HA (bring-your-own primary/secondary servers + database IDs). | **`enable_sql_failover_group = false`** |
| [`terraform-orchestrator/`](terraform-orchestrator/) | **Validate-only root** (no resources): CI `terraform validate` anchor; real stacks stay separate. | Always safe to `init -backend=false` |

## Suggested order

1. If using **Container Apps** with large-payload offload, apply **`terraform-storage/`** first (or have a storage account + containers), then **`terraform-container-apps/`** (it requires **`artifact_blob_service_uri`** / **`artifact_storage_account_id`** when **`enable_container_apps = true`**). Otherwise deploy compute and data (your landing zone, **`terraform-container-apps/`**, or App Service — your choice).
2. Optionally apply **`terraform-private/`** before cutting over connection strings and disabling public SQL/storage access. If you use a **failover group listener**, optionally apply **`terraform-sql-failover/`** when servers and geo databases exist, then point **`ConnectionStrings:ArchLucid`** at **`read_write_listener_fqdn`** (see **`docs/runbooks/DATABASE_FAILOVER.md`**).
3. Optionally apply **`terraform-entra/`**, then configure the API with **`ArchLucidAuth:Mode = JwtBearer`** (see **`ArchLucid.Api/appsettings.Entra.sample.json`**).
4. Optionally apply **`terraform-edge/`** and route customer traffic to the Front Door hostname.

Customer-facing narrative: **`docs/CUSTOMER_TRUST_AND_ACCESS.md`**.

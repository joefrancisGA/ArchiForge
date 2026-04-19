# Terraform: SQL failover group (optional)

Optional root that manages **`azurerm_mssql_failover_group`** so the **read/write listener** (`{failover-group-name}.database.windows.net`) is **declared in IaC**, not only in the Azure portal.

## Objective

- Encode **auto-failover group** settings (partner server, database membership, read/write policy) in Terraform.
- Emit outputs suitable for **Key Vault / app settings** (`ConnectionStrings:ArchLucid`) so apps follow the **current primary** after failover.

## Assumptions

- **Primary and secondary logical servers** already exist (this root does **not** create `azurerm_mssql_server` or databases).
- Databases are **eligible for geo-replication** and you will add them to the group using **primary** database resource IDs.
- You align **RPO/RTO** expectations with **`docs/RTO_RPO_TARGETS.md`** and operational steps with **`docs/runbooks/DATABASE_FAILOVER.md`**.

## Constraints

- Failover group APIs expect a **single partner** in the common two-region pattern (`partner_server` block).
- **Automatic** read/write policy requires **`grace_minutes` ≥ 60** (enforced by the provider’s `CustomizeDiff`).
- **Manual** mode must **omit** `grace_minutes` in the API; this stack sets `grace_minutes = null` when mode is `Manual`.
- Same **subscription** limitations apply as in Azure SQL failover group documentation (organizational / landing-zone constraints).

## Architecture overview (diagram-ready)

| Node | Role |
|------|------|
| **Primary SQL server** | Hosts `azurerm_mssql_failover_group`; holds read/write endpoint until failover. |
| **Partner SQL server** | Geo-secondary; replication target. |
| **Failover group** | Logical listener + membership of replicated databases. |
| **ArchLucid API** | Consumes **listener FQDN** in the connection string (edge from app to listener). |

**Flow:** App → TLS to **listener FQDN** → current primary server; Azure moves the read/write endpoint on failover.

## Usage

1. Copy **`terraform.tfvars.example`** → **`terraform.tfvars`** (keep untracked).
2. Set **`enable_sql_failover_group = true`** and real **server** + **primary database** resource IDs.
3. `terraform init` → `terraform plan` → `terraform apply` (configure remote state the same way as other `infra/terraform-*` roots).

**Suggested order:** provision servers + geo replication outside or alongside this repo, optionally **`terraform-private/`** for private endpoints, then apply this stack **before** cutting app traffic to the listener if you are migrating from a single-server hostname.

## Optional automatic tuning (maximum Azure-managed optimization)

Set **`enable_sql_automatic_tuning = true`** to apply **server-level** automatic tuning on each **eligible** logical server via **`azapi_update_resource`** (Terraform **`azurerm`** does not yet expose this on `azurerm_mssql_server` / `azurerm_mssql_database`).

| Option | Default when tuning is enabled | Role |
|--------|-------------------------------|------|
| **`forceLastGoodPlan`** | **`On`** | Reverts parameter-sensitive plan regressions using Query Store last known good plan. |
| **`createIndex`** | **`On`** | Creates **nonclustered** indexes Azure deems beneficial from workload telemetry. |
| **`dropIndex`** | **`On`** | Drops indexes Azure deems **unused**; can remove rarely exercised indexes you ship in **`ArchLucid.sql`**—treat Query Store + portal “Automatic tuning recommendations” as a promotion queue into **`ArchLucid.Persistence/Migrations/`** when you want DDL parity (see **`docs/SQL_DDL_DISCIPLINE.md`**). |

**Eligibility:** primary is tuned when **`primary_sql_server_resource_id`** is not the default placeholder; partner is tuned when **`partner_sql_server_resource_id`** is not its placeholder—so **single-server** environments can enable tuning without a geo partner.

**Independence:** tuning does **not** require **`enable_sql_failover_group`**; you can manage only tuning in this root if you pass real server IDs.

Override any option with **`sql_automatic_tuning_*`** variables (`On` / `Off` / `Default`).

## Optional consumption budget

Set **`enable_sql_consumption_budget = true`** to create **`azurerm_consumption_budget_resource_group`** scoped to **`Microsoft.Sql/servers`** and **`Microsoft.Sql/servers/databases`** in the target resource group. Supply **`sql_consumption_budget_resource_group_id`**, or omit it and derive the group from **`primary_sql_server_resource_id`** (must not be the default placeholder when the budget is enabled). Notifications fire at **80% actual** and **100% forecasted** spend.

## Outputs

| Output | Use |
|--------|-----|
| **`read_write_listener_fqdn`** | Preferred SQL host in connection strings. |
| **`failover_group_id`** | Auditing, RBAC, or downstream automation. |
| **`sql_consumption_budget_id`** | Present when the optional SQL consumption budget is enabled. |
| **`sql_automatic_tuning_primary_applied`** | `true` when primary server automatic tuning was applied. |
| **`sql_automatic_tuning_partner_applied`** | `true` when partner server automatic tuning was applied. |

## Security

- No passwords in this root; only **resource IDs** and policies.
- Prefer **private connectivity** to SQL (see **`terraform-private/`**) and **Key Vault** for secrets (**`docs/runbooks/SECRET_AND_CERT_ROTATION.md`**).
- Do **not** expose SMB (port 445) for SQL backups in place of Azure’s native paths; keep storage on **private endpoints** per org policy.

## Operational considerations

- **Drills:** exercise failover/failback per **`DATABASE_FAILOVER.md`**; confirm the app uses the **listener**, not a fixed server FQDN.
- **Cost:** geo-redundant SQL and egress increase spend vs single-region; justify against tiered RPO/RTO.
- **Provider versions:** pinned via **`versions.tf`** (`azurerm` `>= 3.100.0, < 5.0.0`, **`azapi`** `>= 2.0.0, < 3.0.0`); upgrade in a controlled change after `terraform validate` / plan in a sandbox subscription.
- **Automatic tuning:** after apply, confirm in Azure portal **SQL server → Intelligent performance → Automatic tuning** (or Query Store “regressed queries”) that options match Terraform; rotate **`azapi`** credentials the same as **`azurerm`** (OIDC / service principal).

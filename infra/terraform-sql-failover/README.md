# Terraform: SQL failover group (optional)

Optional root that manages **`azurerm_mssql_failover_group`** so the **read/write listener** (`{failover-group-name}.database.windows.net`) is **declared in IaC**, not only in the Azure portal.

## Objective

- Encode **auto-failover group** settings (partner server, database membership, read/write policy) in Terraform.
- Emit outputs suitable for **Key Vault / app settings** (`ConnectionStrings:ArchiForge`) so apps follow the **current primary** after failover.

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
| **ArchiForge API** | Consumes **listener FQDN** in the connection string (edge from app to listener). |

**Flow:** App → TLS to **listener FQDN** → current primary server; Azure moves the read/write endpoint on failover.

## Usage

1. Copy **`terraform.tfvars.example`** → **`terraform.tfvars`** (keep untracked).
2. Set **`enable_sql_failover_group = true`** and real **server** + **primary database** resource IDs.
3. `terraform init` → `terraform plan` → `terraform apply` (configure remote state the same way as other `infra/terraform-*` roots).

**Suggested order:** provision servers + geo replication outside or alongside this repo, optionally **`terraform-private/`** for private endpoints, then apply this stack **before** cutting app traffic to the listener if you are migrating from a single-server hostname.

## Outputs

| Output | Use |
|--------|-----|
| **`read_write_listener_fqdn`** | Preferred SQL host in connection strings. |
| **`failover_group_id`** | Auditing, RBAC, or downstream automation. |

## Security

- No passwords in this root; only **resource IDs** and policies.
- Prefer **private connectivity** to SQL (see **`terraform-private/`**) and **Key Vault** for secrets (**`docs/runbooks/SECRET_AND_CERT_ROTATION.md`**).
- Do **not** expose SMB (port 445) for SQL backups in place of Azure’s native paths; keep storage on **private endpoints** per org policy.

## Operational considerations

- **Drills:** exercise failover/failback per **`DATABASE_FAILOVER.md`**; confirm the app uses the **listener**, not a fixed server FQDN.
- **Cost:** geo-redundant SQL and egress increase spend vs single-region; justify against tiered RPO/RTO.
- **Provider versions:** pinned via **`versions.tf`** (`azurerm` `>= 3.100.0, < 5.0.0`); upgrade in a controlled change after `terraform validate` / plan in a sandbox subscription.

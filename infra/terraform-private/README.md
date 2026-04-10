# Private endpoints (SQL + Blob + optional Azure AI Search)

Optional Terraform root for **private data-plane** connectivity: VNet, **private DNS** zones, and **private endpoints** for **Azure SQL** and **Blob storage**, plus an **optional** endpoint for **Azure AI Search** when `search_service_id` is set. Optionally wires **regional VNet integration** for a **Linux Web App** (`linux_web_app_id` + `web_app_vnet_integration_subnet_id`) so the API resolves private DNS inside the VNet. Defaults **`enable_private_data_plane = false`**.

## Why customers care

- Traffic to SQL and blob storage stays on the **Microsoft backbone** instead of the public internet.
- Aligns with **deny-by-default** and **private endpoint** expectations in regulated environments.

## What you must do after apply

1. **Disable public network access** on the SQL server and storage account (or restrict with firewall rules) so data is not still reachable publicly — Terraform here does not flip those flags by design (avoid locking you out mid-migration).
2. Update **`ConnectionStrings:ArchLucid`** to use the **same server FQDN**; with private DNS linked to the VNet, `*.database.windows.net` resolves to the private IP inside the VNet.
3. Integrate **compute** (App Service, Container Apps, AKS) with this VNet (**VNet integration** or subnet injection) so the API resolves private DNS.

## Variables

See `variables.tf` and `terraform.tfvars.example`.

## SMB / port 445

This module does **not** expose SMB. Blob access from the API should use **HTTPS** to `*.blob.core.windows.net`, which resolves privately when the private DNS zone is linked.

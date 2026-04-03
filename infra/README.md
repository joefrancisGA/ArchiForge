# Infrastructure (Terraform)

ArchiForge splits Azure infrastructure into **optional Terraform roots** so local and laptop workflows stay default-off. Each root has its own `README.md`, `terraform.tfvars.example`, and `.terraform.lock.hcl`.

| Root | Purpose | Default |
|------|---------|---------|
| [`terraform/`](terraform/) | Core Azure resources (optional **API Management Consumption**, managed identity, outputs for API URL). | Feature flags **`false`** |
| [`terraform-edge/`](terraform-edge/) | **Azure Front Door Standard + WAF** in front of APIM or the API hostname. | **`enable_front_door_waf = false`** |
| [`terraform-private/`](terraform-private/) | VNet, private DNS, **private endpoints** for **SQL** and **Blob**. | **`enable_private_data_plane = false`** |
| [`terraform-entra/`](terraform-entra/) | **Microsoft Entra ID** app registration with **Admin / Operator / Reader** app roles aligned with the API. | **`enable_entra_api_app = false`** |
| [`terraform-container-apps/`](terraform-container-apps/) | **Azure Container Apps**: Log Analytics, environment, **API + Operator UI** with **min/max replicas** and HTTP concurrency scale rules. | **`enable_container_apps = false`** |

## Suggested order

1. Deploy compute and data (your landing zone, **`terraform-container-apps/`**, or App Service — your choice).
2. Optionally apply **`terraform-private/`** before cutting over connection strings and disabling public SQL/storage access.
3. Optionally apply **`terraform-entra/`**, then configure the API with **`ArchiForgeAuth:Mode = JwtBearer`** (see **`ArchiForge.Api/appsettings.Entra.sample.json`**).
4. Optionally apply **`terraform-edge/`** and route customer traffic to the Front Door hostname.

Customer-facing narrative: **`docs/CUSTOMER_TRUST_AND_ACCESS.md`**.

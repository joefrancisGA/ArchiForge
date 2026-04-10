# ArchLucid — Azure Terraform (API Management, Consumption)

For the full map of Terraform roots (Container Apps, storage, private networking, edge, monitoring, Entra), see **`docs/DEPLOYMENT_TERRAFORM.md`**.

This directory holds **optional** infrastructure for **Azure** deployments. It does **not** run on your laptop as part of normal app development: keep `enable_api_management = false` (or do not apply this root module) until you provision Azure resources.

## What gets created

When `enable_api_management = true`:

| Resource | Notes |
|----------|--------|
| `azurerm_api_management` | **Consumption** tier only (`sku_name = Consumption_0`). Serverless pricing model suitable for dev/small traffic. |
| `azurerm_api_management_api` | Single API with path suffix `apim_api_path_suffix` (default `v1`), backend `archlucid_api_backend_url`. |
| Optional `azurerm_resource_group` | Only if `create_resource_group = true`. |

**Not included here (follow-up):** Azure Front Door / Application Gateway **WAF**, private endpoints, and APIM-to-private-backend routing. Consumption APIM reaches backends over **public HTTPS** unless you add a different topology (e.g. Premium + VNet).

## Prerequisites

- Terraform `>= 1.5`
- Azure CLI logged in (`az login`) and subscription set, or equivalent service principal env vars for CI
- A **globally unique** `apim_name` (try including org + environment + random suffix)

## Quick start (Azure)

```bash
cd infra/terraform
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars — set enable_api_management, apim_name, URLs, region

terraform init
terraform plan
terraform apply
```

Outputs include `api_management_gateway_url`. Callers use:

`https://<apim-name>.azure-api.net/<apim_api_path_suffix>/...`

aligned with your imported OpenAPI paths.

## OpenAPI import

- **Bootstrap (default):** If `apim_openapi_spec_url` is empty, apply uses `openapi/apim-bootstrap.yaml` (only `/health/live`) so the API resource exists immediately.
- **Full surface:** Set `apim_openapi_spec_url` to your running API’s Swagger URL, for example  
  `https://<app-service>.azurewebsites.net/swagger/v1/swagger.json`  
  then `terraform apply` again so APIM imports all operations.

Terraform must reach that URL at apply time (CI agent with network access to the API host).

## Laptop / no Azure

- Do **not** set `enable_api_management = true` without an Azure target.
- Local development continues to use **direct** API URLs (e.g. `http://localhost:5128`) or the **Next.js** `/api/proxy` path; no APIM is required.

## WAF alignment

- **RE:05 / OE:07:** Clients should send **`X-Correlation-ID`**; ArchLucid.Api echoes it. APIM forwards headers by default for passthrough APIs; add policies later if you strip or inject headers at the edge.
- **Edge WAF:** Add Front Door / App Gateway (WAF SKU) in a separate Terraform root or module in front of APIM when your landing zone requires it.

## Variables reference

See `variables.tf` and `terraform.tfvars.example`.

## Private endpoints (217)

Consumption APIM does **not** support the same VNet-injection model as Premium. If the API must be private-only, plan a **tier or topology change** (e.g. Premium APIM + private backend, or public backend locked to APIM egress IPs via App Service **access restrictions**).

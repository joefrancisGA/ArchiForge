# Terraform: Azure Container Apps (ArchiForge API + Operator UI)

Optional root that deploys:

- **Log Analytics** workspace (required by Container Apps Environment)
- **Container Apps Environment** (consumption; optional **VNet integration** + internal load balancer)
- **`azurerm_container_app`** for **ArchiForge.Api** (port **8080**, liveness `/health/live`, readiness `/health/ready`, `ASPNETCORE_URLS`)
- **`azurerm_container_app`** for **archiforge-ui** (port **3000**, probes on `/`)

HTTP **KEDA-style** scale rules scale each app between **min/max replicas** using **concurrent request** targets.

## When to use this stack

Use this root when you want **per-app replica scaling** and a **container-native** Azure host instead of App Service. It complements:

- **`terraform-private/`** — private endpoints and optional VNet subnet for the Container Apps environment
- **`terraform/`** — APIM in front of the API FQDN
- **`terraform-edge/`** — Front Door + WAF in front of public hostnames

## Defaults

- **`enable_container_apps = false`** — no resources; safe for `terraform validate` in CI.
- When **`true`**, you must set **`api_container_image`** and **`ui_container_image`** (full ACR or registry references).

## Operator UI → API

The UI calls the backend via same-origin **`/api/proxy`** in dev. In Container Apps, set the server-side base URL for the UI container (e.g. **`ARCHIFORGE_API_BASE_URL`** or your Next.js env naming) to the **API HTTPS URL** from Terraform output **`api_https_url`**, or place **APIM** / **Front Door** in front and point at that hostname instead.

## Background services and replicas

The API runs **hosted background jobs** (advisory scans, archival, retrieval outbox, etc.). With **multiple API replicas**, each instance runs those jobs unless you add **leader election** or a **dedicated worker** revision. For most pilots, keep **`api_min_replicas = 1`** until that design is in place.

## Private images (ACR)

If images are in **Azure Container Registry**, attach a **managed identity** to each `azurerm_container_app` and grant **AcrPull**, then add a **`registry`** block — not included in this minimal root; extend `main.tf` or use a registry module.

## Commands

```bash
cd infra/terraform-container-apps
terraform init
cp terraform.tfvars.example terraform.tfvars   # edit
terraform plan
terraform apply
```

## CI

This directory is included in **`.github/workflows/ci.yml`** (`terraform init -backend=false`, `validate`, `fmt -check`).

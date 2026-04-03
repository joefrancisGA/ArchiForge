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
- When **`true`**, you must set **`artifact_blob_service_uri`** and **`artifact_storage_account_id`** (from **`infra/terraform-storage`** outputs) so the API enables **`ArtifactLargePayload`** with **Azure Blob** and receives **Storage Blob Data Contributor** on that account via the API’s **system-assigned managed identity**.

## Large artifact blob offload (staging / production)

The API **`ArchiForge.Api`** uses **`DefaultAzureCredential`** against the blob service URI. Container Apps wiring sets:

- **`ArtifactLargePayload__Enabled`** = `true`
- **`ArtifactLargePayload__BlobProvider`** = `AzureBlob`
- **`ArtifactLargePayload__AzureBlobServiceUri`** = your storage account blob endpoint

**`appsettings.Production.json`** / **`appsettings.Staging.json`** default the same shape with an empty URI so **environment variables** (or Key Vault references) must supply the real endpoint in Azure. **`terraform-container-apps`** injects those env vars from **`artifact_blob_service_uri`**.

Ensure **`infra/terraform-storage`** has created containers **`golden-manifests`**, **`artifact-bundles`**, and **`artifact-contents`** (private). Do not expose **SMB (port 445)** publicly; use private endpoints per workspace policy when hardening networking.

## Operator UI → API

The UI calls the backend via same-origin **`/api/proxy`** in dev. In Container Apps, set the server-side base URL for the UI container (e.g. **`ARCHIFORGE_API_BASE_URL`** or your Next.js env naming) to the **API HTTPS URL** from Terraform output **`api_https_url`**, or place **APIM** / **Front Door** in front and point at that hostname instead.

## Background services and replicas

The API runs **hosted background jobs** (advisory scans, archival, retrieval outbox, etc.). With **multiple API replicas**, each instance runs those jobs unless you add **leader election** or a **dedicated worker** revision.

**Default `api_min_replicas` is 2** for **staging and production** availability (Container Apps keeps at least two API instances). For **local or pilot** stacks where duplicate background work is unacceptable until leader election exists, set **`api_min_replicas = 1`** in your `terraform.tfvars`.

## Hot-path cache (SQL mode)

When **`ArchiForge:StorageProvider`** is **Sql**, the API can cache hot reads (manifests, runs, policy packs) via **`HotPathCache`** in `appsettings` / environment variables.

- With **default `api_min_replicas = 2`**, you typically run **multiple API instances**. Set **`HotPathCache__ExpectedApiReplicaCount`** to **2** (or your **`max_replicas`**) **and** **`HotPathCache__RedisConnectionString`** (e.g. Azure Cache for Redis) in **non-Development** environments when **`HotPathCache__Provider`** is **`Auto`**. Startup validation **requires** Redis when `ExpectedApiReplicaCount` &gt; 1 outside Development; without Redis, either keep **`ExpectedApiReplicaCount` at 1** (per-replica memory cache only, possible cross-replica staleness) or set **`HotPathCache__Provider`** to **`Memory`** explicitly.
- **Development** profile disables hot-path cache in `appsettings.Development.json` by default.

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

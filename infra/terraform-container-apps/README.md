# Terraform: Azure Container Apps (ArchLucid API + Worker + Operator UI)

Optional root that deploys:

- **Log Analytics** workspace (required by Container Apps Environment)
- **Container Apps Environment** (consumption; optional **VNet integration** + internal load balancer)
- **`azurerm_container_app`** for **ArchLucid.Api** (port **8080**, **`Hosting__Role=Api`**, liveness `/health/live`, readiness `/health/ready`, `ASPNETCORE_URLS`)
- **`azurerm_container_app`** for **ArchLucid.Worker** (same image by default, **`command` = `dotnet ArchLucid.Worker.dll`**, **`Hosting__Role=Worker`**, configurable **min/max replicas**, health probes on **8080**; optional **azure-queue** scale rule when **`worker_enable_queue_depth_scaling`** and a **queue connection string** secret are set)
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
- When **`true`**, you must set **`artifact_blob_service_uri`** and **`artifact_storage_account_id`** (from **`infra/terraform-storage`** outputs) so the **API** and **Worker** enable **`ArtifactLargePayload`** with **Azure Blob**; each app’s **system-assigned managed identity** receives **Storage Blob Data Contributor** on that account.

## Large artifact blob offload (staging / production)

The API **`ArchLucid.Api`** uses **`DefaultAzureCredential`** against the blob service URI. Container Apps wiring sets:

- **`ArtifactLargePayload__Enabled`** = `true`
- **`ArtifactLargePayload__BlobProvider`** = `AzureBlob`
- **`ArtifactLargePayload__AzureBlobServiceUri`** = your storage account blob endpoint

**`appsettings.Production.json`** / **`appsettings.Staging.json`** default the same shape with an empty URI so **environment variables** (or Key Vault references) must supply the real endpoint in Azure. **`terraform-container-apps`** injects those env vars from **`artifact_blob_service_uri`**.

Ensure **`infra/terraform-storage`** has created containers **`golden-manifests`**, **`artifact-bundles`**, and **`artifact-contents`** (private). Do not expose **SMB (port 445)** publicly; use private endpoints per workspace policy when hardening networking.

## Operator UI → API

The UI calls the backend via same-origin **`/api/proxy`** in dev. In Container Apps, set the server-side base URL for the UI container (e.g. **`ARCHIFORGE_API_BASE_URL`** or your Next.js env naming) to the **API HTTPS URL** from Terraform output **`api_https_url`**, or place **APIM** / **Front Door** in front and point at that hostname instead.

## Background services and replicas

**Terraform** provisions a dedicated **`archiforge-worker`** container app (**`worker_min_replicas` / `worker_max_replicas`**, default **1 / 20**) that runs **advisory scan polling**, **data archival**, **retrieval indexing outbox** processing, and (when durable) **background export jobs** from Azure Storage Queue. The **API** app uses **`Hosting__Role=Api`**, so it does **not** run those loops.

**Export async jobs** (`IBackgroundJobQueue`): default **`background_jobs_mode = "InMemory"`** keeps the **in-process** queue on the API (or Combined host). Set **`background_jobs_mode = "Durable"`** to use **SQL** (`dbo.BackgroundJobs`), **Azure Storage Queue** (Terraform creates **`azurerm_storage_queue`** when the blob URI parses a storage account name), and **worker-side processing** (`BackgroundJobQueueProcessorHostedService`). The module then sets **`BackgroundJobs__Mode`**, **`BackgroundJobs__QueueName`**, **`BackgroundJobs__ResultsContainerName`**, grants the API **Storage Queue Data Message Sender** and the worker **Storage Queue Data Message Processor** (blob contributor was already required). **Durable** requires **`ArchiForge:StorageProvider=Sql`**, **Azure Blob** artifacts, and matching app configuration (validated at startup).

**Default `api_min_replicas` is 2** for **staging and production** API availability. For **local or pilot** stacks, set **`api_min_replicas = 1`** in `terraform.tfvars` if you prefer a single API instance.

**Durable export jobs** use **SQL row locks** (`UPDLOCK` + transactional claim) and **batch dequeue** (`BackgroundJobs:ProcessorReceiveBatchSize`, default **16**) so multiple workers do not execute the same job twice; duplicate queue notifications while a job is **Running** are deleted idempotently.

**Advisory scan, archival, and retrieval outbox** loops still run on every worker replica. If duplicate side effects are unacceptable for those features, keep **`worker_max_replicas = 1`** or introduce **leader election** / split workloads in a later iteration.

**Queue-depth scaling (KEDA in Container Apps):** set **`worker_enable_queue_depth_scaling = true`**, **`worker_queue_scale_connection_string`** (sensitive; same storage account as the jobs queue), and optionally **`worker_queue_depth_target_messages_per_revision`**. Terraform adds a **`custom_scale_rule`** of type **`azure-queue`** on the worker. **Managed identity** is used for runtime queue access; the connection string is **only** for the scaler secret as required by the platform.

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

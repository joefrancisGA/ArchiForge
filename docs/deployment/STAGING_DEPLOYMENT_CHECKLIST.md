> **Scope:** Operators verifying hosted SaaS staging (Terraform apply order, trial funnel, health probes) using repo-defined stacks — not designing net-new infrastructure.

# Staging deployment checklist (`staging.archlucid.net`)

**Purpose:** Prerequisite and verification list for bringing the **hosted SaaS trial funnel** online on **staging** using **existing** Terraform and CI — **no new resources** in this document; operators apply or configure what is already defined in the repo. Covers signup, tenant provisioning, first-value experience, and health probes. Aligned with [TRIAL_AND_SIGNUP.md](../go-to-market/TRIAL_AND_SIGNUP.md), [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md), and [BUYER_FIRST_30_MINUTES.md](../BUYER_FIRST_30_MINUTES.md).

**Last updated:** 2026-04-25

---

## 1. Default apply order (Terraform)

Apply nested stacks in the order documented in [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md) (multi-root table). Staging-relevant roots for workloads and edge:

| Order (multi-root) | Root | Role for staging |
|-------------------|------|------------------|
| 1–5 | `infra/terraform-private` … `infra/terraform-storage` | Network, Key Vault, SQL, storage — **foundation** |
| 5–6 | `infra/terraform-servicebus`, `infra/terraform-logicapps` | Optional messaging / automation for jobs and trial email |
| 8 | `infra/terraform-entra` | App registrations and consent (API + UI) |
| 9 | `infra/terraform-container-apps` | **API + Worker + UI** Container Apps, identities, ACR pull |
| 10 | `infra/terraform-edge` | **Azure Front Door** / WAF / routes to Container App origins |
| 12+ | `infra/terraform-monitoring` | Observability (as needed) |

**Convenience:** `infra/terraform-pilot` collapses guidance and outputs (including `nested_infrastructure_roots`); it does not create resources — see the pilot README. Optional script: `infra/apply-saas.ps1` (default single-root).

**Verify (read-only):** From repo root, list container app resources defined in the pilot output or open `infra/terraform-container-apps/main.tf` — resources `azurerm_container_app.api`, `.worker`, and `.ui` define the three workloads (`api` / `worker` / `ui` container images via variables `api_container_image`, `worker_container_image` or default to API image, and `ui_container_image`).

---

## 2. GitHub: merge-to-staging CD (optional automation)

The workflow [`.github/workflows/cd-staging-on-merge.yml`](../../.github/workflows/cd-staging-on-merge.yml) runs **only** when all of the following hold:

- Repository variable **`AUTO_DEPLOY_STAGING_MERGE`** is set to **`true`**
- The **CI** workflow completed with **success** on a **push** to **`main`** or **`master`** (not a fork)
- The **GitHub `staging` environment** is configured with the same class of secrets as manual CD (Azure OIDC, ACR, Container App names, etc.) — see [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md)

| Prerequisite | Verification |
|-------------|--------------|
| Auto-deploy enabled | In GitHub: **Settings → Secrets and variables → Actions → Variables** — confirm `AUTO_DEPLOY_STAGING_MERGE` = `true`. If `false` or unset, the workflow does not run. |
| Staging environment secrets | **Settings → Environments → `staging`**: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (see [AZURE_SUBSCRIPTIONS.md](../library/AZURE_SUBSCRIPTIONS.md)). |
| ACR build/push | If **`ACR_LOGIN_SERVER`** (environment secret) is **unset**, the workflow **skips** `docker build` / push and logs that fact — confirm the secret is set. **Verify after deploy:** `az acr repository show-tags --name <registry-name> --repository archlucid-api` (and `archlucid-ui`) — expect tags (e.g. commit SHA or `IMAGE_TAG`). |
| Image tag | If repository variable **`IMAGE_TAG`** is empty, the job uses the **`workflow_run.head_sha`**. **Verify:** GitHub run log, **Set image tag** step. |
| Container app update | Requires **`AZURE_RESOURCE_GROUP`**, **`CONTAINER_APP_API_NAME`**, and **`ACR_LOGIN_SERVER`**. Worker uses the **same** image as API (`archlucid-api:<tag>`), UI uses `archlucid-ui:<tag>`. **Verify:** `az containerapp show -g <rg> -n <api-name> --query "properties.template.containers[0].image" -o tsv` |
| Post-deploy smoke | If **`SMOKE_TEST_BASE_URL`** is unset, post-deploy validation is **skipped**. For a real gate, set it to the public API base URL used by [scripts/ci/cd-post-deploy-verify.sh](../../scripts/ci/cd-post-deploy-verify.sh). |

**Manual alternative:** [`.github/workflows/cd.yml`](../../.github/workflows/cd.yml) with target `staging` (documented in [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md)).

---

## 3. Container Apps images (Terraform vs CD)

| Source | What to verify |
|--------|----------------|
| `infra/terraform-container-apps` | **Terraform root:** `azurerm_container_app` **api** (`var.api_container_image`), **worker** (`local.worker_effective_image` — same as API image unless `worker_container_image` set), **ui** (`var.ui_container_image`). Registry: optional ACR pull via user-assigned identity and `azurerm_container_app` `registry` block. **Do not** change `.tf` here — confirm applied state in Azure matches the images you expect. **CLI:** `az containerapp show -g <rg> -n <app> --query "properties.template.containers[0].image" -o tsv` for each of API, worker, UI. |
| `cd-staging-on-merge` / `cd.yml` | CD updates revisions with `az containerapp update --image` to `<ACR_LOGIN_SERVER>/archlucid-api:<tag>` and `archlucid-ui:<tag>`. If Terraform later reapplies with **pinned** image variables, reconcile process (operator policy) so CD and Terraform do not fight. This checklist does not prescribe merge strategy — it flags the dependency. |

**Dockerfile references (for local parity only):** `ArchLucid.Api/Dockerfile` (API + worker DLLs in one image), `archlucid-ui/Dockerfile` (UI). Full-stack local: `docker compose` profile `full-stack` in [`docker-compose.yml`](../../docker-compose.yml).

---

## 4. Azure Front Door and `staging.archlucid.net`

| Prerequisite | Terraform / doc reference | Verification |
|--------------|---------------------------|--------------|
| Front Door profile, origin, route | `infra/terraform-edge` — e.g. [`frontdoor.tf`](../../infra/terraform-edge/frontdoor.tf) (`azurerm_cdn_frontdoor_*`). Primary origin **`host_name`** = `var.backend_hostname` (Container App or internal hostname). | `az afd profile list` / `az afd endpoint list` (Resource Manager **cdn** commands) in the correct subscription, or Azure Portal: Front Door + endpoint. |
| Custom domain for staging | Custom domains may be set via **variables** (e.g. marketing / multi-route mode in `infra/terraform-edge` — see [`frontdoor-marketing-routes.tf`](../../infra/terraform-edge/frontdoor-marketing-routes.tf), [`variables.tf`](../../infra/terraform-edge/variables.tf)). [`README`](../../infra/terraform-edge/README.md): binding + DNS is environment-specific. | **DNS:** At your DNS host, CNAME (or A/ALIAS per Azure docs) for **`staging.archlucid.net`** to the Front Door endpoint hostname. **Verify:** `nslookup staging.archlucid.net` and compare to `azurerm_cdn_frontdoor_endpoint` default hostname (see Terraform outputs in `infra/terraform-edge/outputs.tf` or Azure Portal). |
| HTTPS + certificate | Front Door **managed certificate** (operator completes domain validation in Azure). | Browser or: `curl -sI https://staging.archlucid.net/health/live` (expect **200** after deploy). |
| API routes on same host (marketing mode) | When `marketing_backend_hostname` is set, `api_route_patterns_when_marketing_enabled` includes `/health/*` — see [`variables.tf`](../../infra/terraform-edge/variables.tf) defaults (`/v1/*`, `/health/*`, …). | `curl -fsS https://staging.archlucid.net/health/live` and `/health/ready` — must match API. |

---

## 5. SQL, Key Vault, Entra, Service Bus

| Prerequisite | Terraform / secret | Verification |
|--------------|---------------------|--------------|
| Azure SQL + connection string for API | `infra/terraform-sql-failover` (and private/DNS as deployed). API reads **`ConnectionStrings:ArchLucid`** (see [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) — missing connection string with `StorageProvider=Sql` fails at DB use). | Key Vault or Container App **secret** reference in portal; do not print secrets in CI logs. **App:** `/health/ready` should not report an unhealthy database dependency. |
| **StorageProvider = Sql** | `ArchLucid:StorageProvider` = `Sql` in production/staging config (env or app settings) — [ADR 0011](../adr/0011-inmemory-vs-sql-storage-provider.md). | **Readiness:** `GET /health/ready` JSON. **Config snapshot:** `StartupConfigurationDiagnostics` log line on API startup (see [TRUSTED_BASELINE.md](../library/TRUSTED_BASELINE.md)) — or inspect Application Settings in Container App (redact secrets). |
| Key Vault | `infra/terraform-keyvault` in stack order; app settings reference Key Vault URI. | `az keyvault show` / secret names in deployment docs — **no secret values in repo**. |
| Entra app registration (API + UI) | `infra/terraform-entra` (order 8). Redirect URIs for `https://staging.archlucid.net` (or your UI path). | Entra admin center: app registration, redirect URIs and API scopes — match UI env for staging. |
| Service Bus (optional) | `infra/terraform-servicebus` if background jobs use Service Bus; durable queue may use **storage** queues (see `terraform-container-apps` **background** jobs and `BackgroundJobs:Mode`). | If enabled: `az servicebus namespace show` in the expected resource group. If not used, confirm `BackgroundJobs:Mode` **does not** require a missing bus. **Artifact** storage: API env uses `ArtifactLargePayload__AzureBlobServiceUri` from Terraform variables in `main.tf` — verify blob connectivity via readiness or app logs. |

---

## 6. Demo seed and trial sample data (important)

| Topic | Fact |
|-------|------|
| **Docker / full-stack** | [`docker-compose.yml`](../../docker-compose.yml) profile `full-stack` sets `ASPNETCORE_ENVIRONMENT=Development`, `ArchLucid__StorageProvider=Sql`, `Demo__Enabled`, `Demo__SeedOnStartup` — for **local** parity. |
| **Hosted staging** | In **`ArchLucidPersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed`**, the **Contoso `Demo:SeedOnStartup` path runs only when `app.Environment.IsDevelopment()`** is true. Staging/Production should **not** use `Development` for a public host — so **global startup demo seed is not the mechanism** for buyer-facing staging. **Source:** `ArchLucid.Host.Core/Startup/ArchLucidPersistenceStartup.cs`. |
| **Trial funnel** | **Sample run / first value** for self-serve is implemented through **product flows** (signup, coordinator, metrics) — see [TRIAL_AND_SIGNUP.md](../go-to-market/TRIAL_AND_SIGNUP.md) and [TRIAL_FUNNEL.md](../runbooks/TRIAL_FUNNEL.md), E2E [`archlucid-ui/e2e/live-api-trial-signup.spec.ts`](../../archlucid-ui/e2e/live-api-trial-signup.spec.ts). **Verify** staging by executing that spec against the staging base URL (with test credentials as your policy allows) or a manual signup smoke test. |

**Action:** If product requirements need **per-tenant** sample data in staging, confirm behavior via **register/trial** APIs and worker jobs — not by relying on `Demo:SeedOnStartup` on a non-Development host.

---

## 7. Repository variable: `ARCHLUCID_STAGING_BASE_URL` (hosted probes)

The scheduled workflow [`.github/workflows/hosted-saas-probe.yml`](../../.github/workflows/hosted-saas-probe.yml) uses repository variable **`ARCHLUCID_STAGING_BASE_URL`**.

| Step | Command / action |
|------|------------------|
| Set variable | **Settings → Secrets and variables → Actions → Variables**: `ARCHLUCID_STAGING_BASE_URL` = public HTTPS **API** origin, e.g. `https://staging.archlucid.net` (no trailing slash), as long as `/health/live` and `/health/ready` are routed to **ArchLucid.Api** (see Front Door `api_route_patterns` above). If API is on a different hostname, use that base URL. |
| Verify (same as workflow) | `curl -fsS --max-time 30 "${ARCHLUCID_STAGING_BASE_URL}/health/live"` and `curl -fsS --max-time 45 "${ARCHLUCID_STAGING_BASE_URL}/health/ready"`. If variable is **unset**, the workflow **exits 0** and skips — so probes do not fail, but you get **no** production signal. |
| Post-deploy (richer) | [scripts/ci/cd-post-deploy-verify.sh](../../scripts/ci/cd-post-deploy-verify.sh) — `bash scripts/ci/cd-post-deploy-verify.sh "https://staging.archlucid.net" /version` (requires `jq` locally; OpenAPI check may 404 in Production-like hosts — see [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md)). |

**Cross-reference:** [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md) (GitHub Actions table).

---

## 8. One-page operator smoke (after deploy)

1. `curl -fsS https://staging.archlucid.net/health/live` → **200**  
2. `curl -fsS https://staging.archlucid.net/health/ready` → **200** and top-level **Healthy** in JSON (see script logic in `cd-post-deploy-verify.sh`).  
3. Open UI path for signup (e.g. `/signup` per your Front Door + UI routing) — **200**, Entra or configured auth.  
4. **Metrics (optional):** if Prometheus is enabled, `https://…/metrics` (may be restricted; align with WAF and auth policy).

---

## 9. Related documentation

- [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md) — secrets, smoke behavior, rollback flags  
- [AZURE_SUBSCRIPTIONS.md](../library/AZURE_SUBSCRIPTIONS.md) — staging subscription and GitHub `staging` environment  
- [INFRA] Apply order: [DEPLOYMENT_TERRAFORM.md](../library/DEPLOYMENT_TERRAFORM.md) (full root map)  
- Trial observability: [TRIAL_FUNNEL.md](../runbooks/TRIAL_FUNNEL.md)

---

## 10. Constraints (this change set)

- **No** edits to `*.tf`, workflow `*.yml` application code (`*.cs`, `*.ts`, `*.tsx`) were required to produce this checklist.  
- **No** new Azure resources are **created** by this document; it references **existing** IaC and settings.

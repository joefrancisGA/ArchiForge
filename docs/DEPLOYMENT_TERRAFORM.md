# ArchLucid deployment — Terraform map (Azure)

## Objective

Give operators a single map of **Terraform roots** under `infra/`, how they compose, and what they intentionally **do not** replace (pipelines, org policy). ArchLucid uses **Terraform only** for Azure IaC in this repository (no Bicep roots here). Greenfield IaC uses **`archlucid`** naming throughout `infra/`; first subscription deploy: [`FIRST_AZURE_DEPLOYMENT.md`](FIRST_AZURE_DEPLOYMENT.md).

## Assumptions

- Azure subscription and resource naming are owned by your landing zone.
- Container images are built in CI (see `.github/workflows/ci.yml`) and stored in **ACR** (or another registry your Terraform references).
- SQL schema is applied by the application host (`SqlSchemaBootstrapper` / DbUp), not by Terraform `azurerm_mssql_database` inline scripts.

## Constraints

- **SMB / port 445** must not be exposed publicly; file shares use private connectivity and network boundaries aligned with `terraform-private`.
- **Least privilege**: managed identities, Key Vault references, and private endpoints are preferred over connection strings in plain app settings where the platform supports it.
- **Consumption APIM** (`infra/terraform`) cannot replace Premium VNet-injected APIM for all private-only topologies — see that root’s README.

## Architecture overview

**Nodes:** operators, CI, ACR, Azure Container Apps (or App Service from legacy modules), Azure SQL, storage accounts, Front Door / edge, monitoring workspace, Entra ID app registrations.

**Edges:** Terraform plans apply dependency order; apps pull config from env + Key Vault; observability flows to Log Analytics / Prometheus / Grafana as configured in `terraform-monitoring`.

**Flows:**

1. **Build** → image artifact (digest-addressable).
2. **Provision** → Terraform creates/changes Azure resources.
3. **Release** → compute pulls image, runs migrations on startup, serves `/v1/...` behind edge or APIM.

```mermaid
flowchart LR
  CI[CI build] --> ACR[ACR]
  TF[Terraform apply] --> CA[Container Apps / App Service]
  TF --> SQL[(Azure SQL)]
  TF --> ST[Storage / queues]
  ACR --> CA
  CA --> SQL
  CA --> ST
```

## Component breakdown

| Root | Role |
|------|------|
| `infra/terraform-container-apps` | Primary **workload** pattern: Container Apps, identity, env wiring (parameters vary by fork/branch). |
| `infra/terraform-storage` | Storage accounts, blobs, queues used by artifacts and durable jobs. |
| `infra/terraform-private` | Networking baseline: VNet segments, private endpoints, alignment with SQL/storage. |
| `infra/terraform-edge` | Front Door (or edge) entry, TLS termination, routing to backends. |
| `infra/terraform-monitoring` | Log Analytics, diagnostics, Grafana/Prometheus hooks as defined in that root. |
| `infra/terraform-entra` | Entra app registrations / service principals **as code**, where used. |
| `infra/terraform-sql-failover` | SQL geo **failover group** (listener FQDN); optional **server-level automatic tuning** (`enable_sql_automatic_tuning`, defaults **On** for force-last-good-plan / create-index / drop-index via **Azure/azapi**); optional **resource-group consumption budget** (`enable_sql_consumption_budget`). |
| `infra/terraform-openai` | Optional **resource-group consumption budget** for **Azure OpenAI** / Cognitive Services accounts (`enable_openai_consumption_budget`). Does not create the OpenAI resource. |
| `infra/terraform` | Optional **API Management (Consumption)** in front of a public HTTPS backend — see `infra/terraform/README.md`. |
| `infra/terraform-logicapps` | Optional **Logic App (Standard)** host for Service Bus–driven edge orchestration (Teams / ITSM fan-out); off by default — see `infra/terraform-logicapps/README.md` and ADR **0019**. |

## Data flow

- **North/south:** Clients → edge/APIM → API → SQL/storage.
- **Observability:** API exposes Prometheus when `Observability:Prometheus:Enabled` is true; dashboards in `infra/grafana/` target metric and trace names produced by OpenTelemetry (verify exact series names on `/metrics` when wiring panels).

## Security model

- **Identity:** Prefer **managed identity** from compute to SQL, Key Vault, and storage APIs; fall back to Key Vault–backed secrets only where required.
- **Network:** Private endpoints for data planes; no public SQL; align with workspace SMB rule for any file share access.
- **RLS:** Production hosts with `ArchLucid:StorageProvider=Sql` must set `SqlServer:RowLevelSecurity:ApplySessionContext=true` (validated at startup — see `ArchLucidConfigurationRules`).

## Operational considerations

- **RTO / RPO by tier:** Default recovery targets (development best-effort; production e.g. relational RPO under five minutes via SQL geo-replication) are documented in **`docs/RTO_RPO_TARGETS.md`**. Implement with auto-failover groups, listeners, and drills per **`docs/runbooks/DATABASE_FAILOVER.md`**.
- **FinOps tags:** In `infra/terraform-container-apps`, set optional **`finops_environment`** and **`finops_cost_center`**; they merge with **`tags`** and a fixed **`Application = ArchLucid`** label on created resources for Azure Cost Management filters.
- **Consumption budgets:** Enable **`enable_container_apps_consumption_budget`** in `infra/terraform-container-apps`, **`enable_sql_consumption_budget`** in `infra/terraform-sql-failover`, and/or **`enable_openai_consumption_budget`** in `infra/terraform-openai` to emit **`azurerm_consumption_budget_resource_group`** resources with Cost Management notifications (amounts and `*_time_period_start` are variables per root).
- **Plan/apply:** Run `terraform init` / `plan` / `apply` per root; compose order is usually **network → data → compute → edge → monitoring**.
- **Drift:** Reconcile manual portal changes back into Terraform or expect the next apply to revert them.
- **Contracts:** HTTP surface is versioned under `/v1/...`; OpenAPI snapshot tests live in `ArchLucid.Api.Tests`; optional AsyncAPI for outbound webhooks is under `docs/contracts/`.
- **Image scanning:** CI runs **Trivy** on container images and Terraform directories — extend with registry gates and Defender for Containers per org policy.
- **CD:** After the stack exists, routine image rollouts are described in **`docs/DEPLOYMENT_CD_PIPELINE.md`** (ACR push + Container App revision updates). Reconcile tfvars image pins when you run **`terraform apply`** so Terraform does not overwrite CLI-pushed tags unintentionally.

## Related docs

- **`docs/REFERENCE_SAAS_STACK_ORDER.md`** — recommended **Terraform apply order** (private networking → data → compute → edge → monitoring).
- `docs/RTO_RPO_TARGETS.md` — RTO/RPO targets by environment tier (SQL HA, drills).
- `docs/CONTAINERIZATION.md` — Docker images and local compose.
- `infra/terraform/README.md` — APIM-specific variables and OpenAPI import.
- `docs/API_CONTRACTS.md` — versioning, deprecation, and contract artifacts.

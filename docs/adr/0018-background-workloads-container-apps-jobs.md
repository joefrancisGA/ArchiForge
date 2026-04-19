# ADR 0018 — Background workloads: Azure Container Apps Jobs (not Functions)

**Status:** Accepted (2026-04-19)

## Context

ArchLucid runs long-lived background loops in `ArchLucid.Worker` via `IHostedService` implementations (advisory polling, archival, trial lifecycle, Cosmos change feed, Service Bus consumer, orphan probes, outbox drains). Operators want to:

- Scale or schedule **batch-shaped** work independently from the HTTP API and the always-on worker.
- Keep **private connectivity** to Azure SQL, Cosmos DB, Service Bus, and Key Vault (workspace default: private endpoints, no public SMB).

**Azure Functions** with VNet integration requires **Premium**-class plans in typical enterprise networking postures, which is materially more expensive than reusing the existing **Container Apps** environment and the **same container image** built for API/Worker.

**Azure Container Apps Jobs** provide cron and event-driven (KEDA) execution on the **Consumption** environment with VNet integration and a **shared monthly free grant** of vCPU/memory seconds with the existing Container App.

## Decision

1. Introduce **`ArchLucid.Jobs.Cli`** — a one-shot host entry (`--job <slug>`) that reuses `AddArchLucidApplicationServices(..., ArchLucidHostingRole.Worker)` without calling `app.Run()`, so in-process hosted services do not start; only the selected `IArchLucidJob` runs.
2. Publish **`ArchLucid.Jobs.Cli.dll`** into the **same** API/Worker container image (`ArchLucid.Api/Dockerfile` publishes Api + Worker + Jobs CLI to `/app`).
3. Extend **`infra/terraform-container-apps/`** with `azurerm_container_app_job` resources driven by `var.container_jobs` (**Schedule** trigger only in this revision; Event/Manual deferred to follow-up).
4. Add configuration **`Jobs:OffloadedToContainerJobs`** (string array) to **suppress** matching in-process hosted services when a job is provisioned, avoiding double execution.
5. Add **`Jobs:DeployedContainerJobNames`** (comma-separated manifest) and **`ContainerJobsOffloadRules`** so **Production Worker** fails fast if an offloaded slug is not listed in the manifest (prevents “disabled in-process, no job exists” outages).
6. Implement the first concrete job: **`advisory-scan`** → `AdvisoryScanArchLucidJob` delegating to `AdvisoryDueScheduleProcessor` (SQL-backed schedules).

## Non-decisions / deferrals

- **`audit-retry-drain`**: **not** offloaded in this ADR. `IAuditRetryQueue` is implemented by **`InMemoryAuditRetryQueue`** in-process; a separate container cannot see the same memory. Offload requires a **durable queue** (SQL table, Redis, or Service Bus) first. The slug is reserved in `ArchLucidJobNames.AuditRetryDrain` with XML documentation pointing here.
- **Event-triggered jobs** (KEDA Service Bus, Cosmos change feed) are **not** wired in Terraform in this revision; variable checks enforce **Schedule-only** until KEDA metadata is codified in Terraform.
- **Azure Functions** remains a non-choice for this path until Functions Consumption supports equivalent private networking at lower TCO than Container Apps Jobs.

## Consequences

- **Positive — cost:** Reuses Container Apps Consumption capacity and one image build; no second Functions Premium plan.
- **Positive — ops:** Terraform owns job definitions; identity per job is **system-assigned** with **Storage Blob Data Contributor** aligned to the worker pattern.
- **Positive — safety:** Production manifest validation prevents silent loss of background work.
- **Negative — image size:** Slightly larger `/app` publish artifact (one more assembly).
- **Negative — config sprawl:** Operators must keep `Jobs:OffloadedToContainerJobs`, `Jobs:DeployedContainerJobNames`, and Terraform `container_jobs` keys in sync until a single source generator or pipeline step exists.

## References

- `docs/runbooks/CONTAINER_APPS_JOBS.md`
- `docs/CURSOR_PROMPTS_BACKGROUND_TO_CONTAINER_JOBS.md`
- `ArchLucid.Host.Core/Jobs/`
- `ArchLucid.Jobs.Cli/Program.cs`
- `infra/terraform-container-apps/jobs.tf`

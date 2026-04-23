> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) and the current assessment pair under ``docs/``. Kept for audit trail.

> **Scope:** Cursor prompts — move background services to Azure Container Apps Jobs - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Cursor prompts — move background services to Azure Container Apps Jobs

**Status:** **Executed (wave 2, 2026-04-19):** additional `IArchLucidJob` implementations (`orphan-probe`, `data-archival`, `trial-lifecycle`, `trial-email-scan`, `audit-change-feed`, `servicebus-integration-events`), shared Service Bus dispatch, Cosmos single-batch change feed processor, Terraform **Event** trigger support in `jobs.tf`, OTel job counters/histogram, `scripts/ci/check_jobs_offload_manifest.py`. **Deferred:** `audit-retry-drain` (durable queue), dedicated Grafana SLO dashboard slice for jobs-only SLOs (use Log Analytics + OTel queries until wired).
**Companion ADR:** [ADR 0018](adr/0018-background-workloads-container-apps-jobs.md).
**Last reviewed:** 2026-04-19

## Why Container Apps Jobs (not Functions)

| Constraint | Container Apps Jobs | Azure Functions |
|---|---|---|
| Private endpoints to SQL / Cosmos / Service Bus / Key Vault (workspace **port-445 / private-endpoint default** rule) | Supported on **Consumption** environment | **Premium plan only (~$168/mo/region/plan)** or Flex Consumption (newer; verify GA) |
| Reuses existing `ArchLucid.Worker` image, identity, OpenTelemetry, Key Vault, leader election | Yes — same container | New runtime, new deploy unit |
| .NET 10 support | Whatever the container runs | Lags Microsoft.NET LTS by quarters |
| Existing Terraform module to extend | **`infra/terraform-container-apps/`** | New module |
| Free monthly grant | **180k vCPU-seconds + 360k GiB-seconds** shared with Container Apps | None |
| Cron + event-driven (KEDA) triggers | Native | Native |

**Decision: every candidate below moves to a Container Apps Job.** No mixed runtime.

## What moves vs what stays

### Moves to Container Apps Jobs

| Hosted service | Job kind | Trigger | Rationale |
|---|---|---|---|
| `AdvisoryScanHostedService` | Cron | Every 5 min | Pure batch, idempotent, leader-elected today (no longer needed) |
| `DataConsistencyOrphanProbeHostedService` | Cron | Hourly | Pure probe, idempotent |
| `DataArchivalHostedService` | Cron | Hourly–weekly per `DataArchivalOptions` | Slow, off-hours, leader-elected today |
| `TrialLifecycleSchedulerHostedService` | Cron | Per `TrialLifecycleSchedulerOptions:IntervalMinutes` | Lifecycle ticks, idempotent |
| `TrialLifecycleEmailScanHostedService` | Cron | Per options | Email scan, separate failure domain from API |
| `AuditRetryDrainHostedService` | Cron | Periodic | Retry drain, low priority, easy first move |
| `AuditEventChangeFeedHostedService` | Event-driven | KEDA Cosmos DB change feed scaler | Native KEDA support; bin packing is automatic |
| `AzureServiceBusIntegrationEventConsumer` | Event-driven | KEDA Service Bus scaler | Native KEDA support; replaces in-process consumer |

### Explicitly stays in `ArchLucid.Worker` / `ArchLucid.Api`

| Hosted service | Why it stays |
|---|---|
| `IntegrationEventOutboxHostedService` | Drains outbox in same SQL transaction as `dbo.Runs` writes (ADR 0004). Latency budget is tight. Splitting adds drift and connection-pool pressure. |
| `RetrievalIndexingOutboxHostedService` | Same outbox reasoning (ADR 0004). |
| `AuthorityPipelineWorkHostedService` | Critical write path; uses in-memory `IBackgroundJobQueue` by design. |
| `BackgroundJobQueueProcessorHostedService` | Same — in-memory queue is the contract. |
| `OutboxOperationalMetricsHostedService` | Co-located with the outbox it observes. |
| `GracefulShutdownNotificationHostedService` | Host-internal; never moves. |

---

## Prompt `jobs-cli-runner-bootstrap` *(do this first)*

Create the shared CLI entry point that every Container Apps Job invokes. Reuses **`ArchLucid.Worker`** composition; **does not** duplicate DI graphs.

1. Add a new console project **`ArchLucid.Jobs.Cli/ArchLucid.Jobs.Cli.csproj`** targeting **`net10.0`**, executable, with project references to **`ArchLucid.Host.Composition`**, **`ArchLucid.Host.Core`**, and **`ArchLucid.Persistence`**.
2. **`ArchLucid.Jobs.Cli/Program.cs`** parses **`--job <name>`** (`System.CommandLine` or manual `args` parse — match the style of **`ArchLucid.Cli/Program.cs`** and **`ArchLucid.Backfill.Cli/Program.cs`**).
3. Build the host with the **same** registration calls used by `ArchLucid.Worker.Program`:
   - `ArchLucidSerilogConfiguration.Configure(builder, "ArchLucid.Jobs.Cli")`
   - `AddArchLucidOpenTelemetry(... telemetryServiceName: "ArchLucid.Jobs.Cli")`
   - `AddArchLucidApplicationServices(builder.Configuration, ArchLucidHostingRole.Worker)`
   - `ArchLucidLegacyConfigurationWarnings.LogIfLegacyKeysPresent`
   - `ArchLucidConfigurationRules.CollectErrors` — fail-fast on errors.
4. Introduce **`IArchLucidJob`** in **`ArchLucid.Host.Core/Jobs/IArchLucidJob.cs`**:
   ```csharp
   public interface IArchLucidJob
   {
       string Name { get; }
       Task<int> RunOnceAsync(CancellationToken cancellationToken);
   }
   ```
5. Add a **`JobRegistry`** that maps `--job` names to `IArchLucidJob` implementations resolved from DI. Unknown names exit with a non-zero code and a clear message.
6. **Process exit codes:** `0` success, `1` job failure (logged), `2` configuration error, `3` unknown job. CI / KEDA failure handlers depend on this contract.
7. **Leader election:** Container Apps Jobs run a **single replica per execution** by default. Job implementations **must not** wrap work in `HostLeaderElectionCoordinator` — that's only for in-process polling.
8. **Dockerfile:** add `ArchLucid.Jobs.Cli/Dockerfile` based on the existing Worker Dockerfile; **same base image** so vulnerability scans cover both.
9. Tests: **`ArchLucid.Jobs.Cli.Tests`** — argument parsing, unknown-job exit code, configuration-error exit code, successful no-op job exit code.

Build: `dotnet build ArchLucid.sln -c Release --nologo` — must remain green.

---

## Prompt `jobs-infra-terraform-module` *(do this second)*

Extend the Container Apps Terraform module to define jobs alongside the existing app(s). Same VNet, same Log Analytics workspace, same managed identity story.

1. In **`infra/terraform-container-apps/`** add **`jobs.tf`** with a reusable map driven by `var.jobs`:
   ```hcl
   variable "jobs" {
     description = "Container Apps Jobs to provision"
     type = map(object({
       trigger_type    = string                 # "Schedule" | "Event" | "Manual"
       cron_expression = optional(string)
       cpu             = optional(number, 0.25)
       memory          = optional(string, "0.5Gi")
       command         = optional(list(string), [])
       args            = list(string)           # e.g., ["--job", "advisory-scan"]
       replica_timeout_seconds = optional(number, 1800)
       parallelism     = optional(number, 1)
       replica_retry_limit     = optional(number, 1)
       env             = optional(map(string), {})
       secret_env      = optional(map(string), {}) # name -> KV reference
       keda_rules      = optional(list(any), [])    # for event-driven jobs
     }))
     default = {}
   }
   ```
2. Define **`azurerm_container_app_job` for_each = var.jobs`** binding to the **same Container Apps Environment** as the Worker, the **same image** (`ArchLucid.Jobs.Cli`), and the **same user-assigned managed identity**.
3. **No public ingress.** Jobs have no inbound endpoint. VNet-only.
4. Identity / RBAC: assign the existing Worker UAMI; do not mint per-job identities (least-privilege is already enforced by the SQL role and Key Vault access policy).
5. **Observability:** route stdout/stderr to the existing Log Analytics workspace; emit OTel via the same exporter env vars the Worker uses.
6. **Cost guardrails:** add a per-job `replica_timeout_seconds` (default 1800) and per-environment monthly **`azurerm_consumption_budget_resource_group`** alert at the same threshold style as **`infra/terraform-container-apps/consumption_budget.tf`**.
7. Outputs: `job_principal_ids`, `job_names` for use in role assignments and dashboards.
8. Run **`terraform fmt`**, **`terraform validate`**, and add a **`checks`** block that asserts `each.value.trigger_type ∈ {"Schedule","Event","Manual"}`.

---

## Prompt `jobs-worker-disable-when-offloaded` *(do this third)*

Avoid double-execution while migrating one job at a time. Add a config switch to **disable** in-process hosted services that have been moved to Container Apps Jobs.

1. Add **`Jobs:OffloadedToContainerJobs`** (string array) to **`ArchLucid.Host.Core/Configuration/`** options.
2. In the host registration where each candidate hosted service is added (search **`AddHostedService<AdvisoryScanHostedService>`**, etc.), wrap with `if (!offloaded.Contains("advisory-scan")) services.AddHostedService<...>();`.
3. **Production safety rule:** in **`ArchLucid.Host.Core/Startup/Validation/Rules/`**, add a rule that fails fast in `Production` if a job name appears in `OffloadedToContainerJobs` **and** the corresponding Container Apps Job is **not** declared in the host configuration manifest (passed via env var by Terraform). This prevents "moved to a job that doesn't exist" outages.
4. Default in `appsettings.Development.json`: empty array (no behavior change).
5. Tests in **`ArchLucid.Host.Core.Tests`**: registration parity — when the array contains `"advisory-scan"`, the hosted service is **not** registered; otherwise it is.

---

## Prompt `job-advisory-scan` *(cron)*

Move **`AdvisoryScanHostedService`** to a Container Apps Job.

1. Extract the per-iteration body of **`ArchLucid.Host.Core/Hosted/AdvisoryScanHostedService.cs`** (`processor.ProcessDueAsync(...)`) into a new **`AdvisoryScanJob : IArchLucidJob`** in **`ArchLucid.Host.Core/Jobs/`** with `Name = "advisory-scan"`. The job **does not** loop or use leader election; it processes once and exits with `0`.
2. Register `AdvisoryScanJob` in DI inside `ArchLucidJobsRegistration` (created by the bootstrap prompt).
3. Wire the job into Terraform via the **`var.jobs`** map:
   - `trigger_type = "Schedule"`, `cron_expression = "*/5 * * * *"`
   - `args = ["--job", "advisory-scan"]`
   - `replica_timeout_seconds = 600`
4. Add **`"advisory-scan"`** to **`Jobs:OffloadedToContainerJobs`** in `appsettings.Production.json` once the job is verified live.
5. Tests:
   - **`AdvisoryScanJobTests`** — successful single iteration returns `0`; processor exception returns `1`.
   - Integration test under **`ArchLucid.Host.Core.Tests`** invoking the CLI in-process via `Process.Start` is **not** required; unit test the job and rely on Container Apps replica logs in staging.
6. Documentation: add a row to **`docs/runbooks/`** (new file **`CONTAINER_APPS_JOBS.md`** if it doesn't exist) describing schedule, expected runtime, and rollback (set `Jobs:OffloadedToContainerJobs` to `[]` and redeploy).

---

## Prompt `job-data-consistency-orphan-probe` *(cron)*

Move **`DataConsistencyOrphanProbeHostedService`** to a Container Apps Job.

1. Extract the iteration body into **`OrphanProbeJob : IArchLucidJob`** with `Name = "orphan-probe"`. Reuse the existing probe processor (do not duplicate logic).
2. Terraform: `cron_expression = "0 * * * *"` (hourly), `replica_timeout_seconds = 900`, `args = ["--job", "orphan-probe"]`.
3. Add **`"orphan-probe"`** to `Jobs:OffloadedToContainerJobs` in `appsettings.Production.json` after verification.
4. Tests for `OrphanProbeJob` mirror the pattern in `AdvisoryScanJobTests`.

---

## Prompt `job-data-archival` *(cron, options-driven schedule)*

Move **`DataArchivalHostedService`** to a Container Apps Job.

1. Extract the iteration body (`DataArchivalHostIteration.RunOnceAsync(...)`) into **`DataArchivalJob : IArchLucidJob`** with `Name = "data-archival"`.
2. Schedule: read **`DataArchivalOptions:IntervalHours`** **at deploy time** and translate to a cron expression in Terraform via a `var` (do not try to dynamically reschedule from the job — Container Apps cron is static per deploy). Default to `"0 2 * * *"` (daily 02:00 UTC) if not set.
3. `replica_timeout_seconds` = max expected archival duration **× 2** (safe default 7200).
4. Tests: existing `DataArchivalHostIteration` tests cover the body; add a thin **`DataArchivalJobTests`** verifying exit codes.
5. Update **`docs/runbooks/DATA_ARCHIVAL_HEALTH.md`** with the Container Apps Jobs replica log query (Log Analytics) and rollback steps.

---

## Prompt `job-trial-lifecycle-scheduler` *(cron)*

Move **`TrialLifecycleSchedulerHostedService`** to a Container Apps Job.

1. Extract the iteration body (lists trial automation tenants, calls `TrialLifecycleTransitionEngine.TryAdvanceTenantAsync` per tenant) into **`TrialLifecycleSchedulerJob : IArchLucidJob`** with `Name = "trial-lifecycle"`.
2. Schedule: cron derived from `TrialLifecycleSchedulerOptions:IntervalMinutes`. Default `"*/15 * * * *"` (every 15 minutes).
3. `replica_timeout_seconds = 900`.
4. Tests verify per-tenant failure does not stop the job (matches existing in-loop behavior).

---

## Prompt `job-trial-lifecycle-email-scan` *(cron)*

Move **`TrialLifecycleEmailScanHostedService`** to a Container Apps Job.

1. Extract iteration body into **`TrialLifecycleEmailScanJob : IArchLucidJob`** with `Name = "trial-email-scan"`.
2. Schedule: cron derived from the corresponding options. Default `"*/30 * * * *"`.
3. `replica_timeout_seconds = 600`.
4. Update **`docs/security/TRIAL_LIMITS.md`** runbook reference.

---

## Prompt `job-audit-retry-drain` *(cron)*

Move **`AuditRetryDrainHostedService`** to a Container Apps Job. Lowest blast radius; ideal first migration.

1. Extract iteration body into **`AuditRetryDrainJob : IArchLucidJob`** with `Name = "audit-retry-drain"`.
2. Schedule: `"*/10 * * * *"` (every 10 minutes); `replica_timeout_seconds = 600`.
3. Tests verify drainer exits cleanly when there is nothing to drain (returns `0`, not `1`).

---

## Prompt `job-audit-event-change-feed` *(event-driven, KEDA Cosmos)*

Move **`AuditEventChangeFeedHostedService`** to a Container Apps Job with a Cosmos DB change-feed KEDA scaler.

1. Extract per-batch processing into **`AuditEventChangeFeedJob : IArchLucidJob`** with `Name = "audit-change-feed"`. The job processes one or more lease batches then exits; KEDA brings up replicas as backlog grows.
2. **Lease ownership:** Cosmos change-feed processor must use the Cosmos SDK's lease container (do **not** invent a new lease store). The job uses `ChangeFeedProcessor` configured to checkpoint after each batch and stop when the current high-water mark is consumed.
3. Terraform job entry:
   - `trigger_type = "Event"`
   - `keda_rules` = single rule of type **`azure-cosmosdb`** keyed off the lease container backlog (verify exact KEDA scaler name at adoption — `azure-cosmosdb` is the documented KEDA Cosmos scaler; confirm against the current KEDA docs).
   - `parallelism` matches the configured **lease range count** (typically 4–16).
   - `replica_timeout_seconds = 1800`.
4. Identity: the Worker UAMI must already have **Cosmos DB Built-in Data Contributor** scoped to the lease container. Verify before deploy.
5. Tests: unit test the batch processor; integration test against the Cosmos emulator if available, otherwise rely on staging.
6. Documentation: add a section to **`CONTAINER_APPS_JOBS.md`** explaining KEDA scaler tuning, expected replica count, and how to drain before redeploy.

---

## Prompt `job-service-bus-integration-event-consumer` *(event-driven, KEDA Service Bus)*

Move **`AzureServiceBusIntegrationEventConsumer`** to a Container Apps Job with a Service Bus KEDA scaler.

1. Convert the consumer into **`ServiceBusIntegrationEventJob : IArchLucidJob`** with `Name = "servicebus-integration-events"`. The job uses **`ServiceBusProcessor`** (or `ServiceBusReceiver`) with **peek-lock**, processes a configurable batch (default 50 messages or 30 seconds, whichever first), then exits `0`. KEDA scales replicas based on queue length.
2. **Idempotency:** existing consumer must already be idempotent (verify against `ArchLucid.Host.Core/Integration/AzureServiceBusIntegrationEventConsumer.cs`). If not, **block migration** and fix idempotency first.
3. Terraform job entry:
   - `trigger_type = "Event"`
   - `keda_rules` = single rule of type **`azure-servicebus`** with `queueName` and `messageCount = 5` (start conservative).
   - Identity-based auth (managed identity); **no connection strings**.
   - `parallelism` capped at 10 initially.
   - `replica_timeout_seconds = 600`.
4. RBAC: confirm Worker UAMI has **`Azure Service Bus Data Receiver`** on the queue (check **`infra/terraform-servicebus/iam.tf`**).
5. **Dead-letter handling:** unchanged — the message handler still moves poison messages to DLQ via `DeadLetterMessageAsync`. Add an alert on DLQ depth in **`infra/terraform-monitoring/`**.
6. Tests: existing consumer tests apply to the new processor class; add a job-shell test for batch-size and timeout exit conditions.
7. Documentation: explicit note that **the in-process consumer must be removed from `ArchLucid.Worker`** in the same change set as the job goes live (use the `Jobs:OffloadedToContainerJobs` switch from the third bootstrap prompt).

---

## Prompt `jobs-observability-and-slos`

Tie the new jobs into existing monitoring so they're not invisible.

1. **Application Insights:** every `IArchLucidJob` must emit a custom event `JobStarted` and `JobCompleted` with `JobName`, `DurationMs`, `ExitCode`. Add a small `JobInstrumentation` helper in **`ArchLucid.Host.Core/Jobs/`** so all jobs use the same shape.
2. **Prometheus / Grafana** (per **`infra/terraform-monitoring/`**): add a recording rule per job for `job_duration_seconds_bucket` and a Grafana panel in the existing dashboards.
3. **SLOs:** add per-job freshness SLOs (e.g., "advisory-scan completes within 10 minutes of schedule" — burn-rate alert tuned conservatively).
4. **DLQ / failure alerts** for the Service Bus and Cosmos jobs.
5. **Runbook:** finalize **`docs/runbooks/CONTAINER_APPS_JOBS.md`** with: how to inspect replica logs, how to manually trigger a job (`az containerapp job start`), how to rollback (`Jobs:OffloadedToContainerJobs = []`).

---

## Prompt `jobs-verify-bundle`

Final session-level verification before declaring the migration complete.

1. **Build:** `dotnet build ArchLucid.sln -c Release --nologo`.
2. **Tests:** `dotnet test ArchLucid.sln -c Release --no-build` — all green; new job tests included.
3. **Worker registration parity:** registration tests in `ArchLucid.Host.Core.Tests` assert that for every name in `Jobs:OffloadedToContainerJobs`, the corresponding hosted service is **not** registered.
4. **Terraform:** `terraform fmt -check`, `terraform validate`, `terraform plan` against staging. Plan must show **no changes** for the existing Worker app and **add** for each job.
5. **CI:** add a docs-link check that **`docs/runbooks/CONTAINER_APPS_JOBS.md`**, **ADR 0018** (when authored), and the per-job entries in `var.jobs` cross-reference correctly.
6. **Cost projection:** record observed vCPU-seconds and memory-seconds for each job over a 7-day window; confirm staying within the **180k vCPU-s + 360k GiB-s** monthly free grant for v1. If not, document in ADR 0018 and adjust schedule cadence.
7. **Rollback drill:** in staging, set `Jobs:OffloadedToContainerJobs = []`, redeploy, verify in-process hosted services resume cleanly. Document the timing.

---

## Objective / assumptions / constraints

| | |
|--|--|
| **Objective** | Move 8 background workloads out of the API/Worker process boundary into independently scaled, schedule- or event-triggered Container Apps Jobs without introducing a second compute runtime. |
| **Assumptions** | `ArchLucid.Worker` image is the canonical container baseline; managed identity, OTel, Serilog, KV access already work; Terraform module **`infra/terraform-container-apps/`** is the deploy unit. |
| **Constraints** | Private endpoints required (port-445 / private-endpoint default rule); **`Microsoft.Extensions.Hosting`** patterns reused (no new framework); leader election removed in jobs (single replica per execution); critical-path outboxes (`IntegrationEventOutbox`, `RetrievalIndexingOutbox`) **stay in-process**; SQL DDL changes go through migrations + master `ArchLucid.sql`. |
| **Decision basis** | See top-of-document table; Container Apps Jobs wins on cost (free monthly grant + Consumption-tier VNet) and reuse (same image, same identity, same Terraform module). |

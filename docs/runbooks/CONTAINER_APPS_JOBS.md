> **Scope:** Runbook — Azure Container Apps Jobs (ArchLucid.Jobs.Cli) - full detail, tables, and links in the sections below.

# Runbook — Azure Container Apps Jobs (`ArchLucid.Jobs.Cli`)

**Last reviewed:** 2026-04-19  
**ADR:** [ADR 0018](../adr/0018-background-workloads-container-apps-jobs.md)

## Objective

Run **one-shot** background iterations (`--job <slug>`) on a schedule or (future) KEDA event triggers, without starting the long-lived `IHostedService` graph from `ArchLucid.Worker`.

## Nodes and edges

| Node | Role |
|------|------|
| `ArchLucid.Jobs.Cli` | Process entry; builds `WebApplication`, validates config, runs DbUp bootstrap, dispatches `ArchLucidJobRunner`. |
| `ArchLucid.Worker` | Long-lived host; must **not** run the same logical loop when the job is offloaded (`Jobs:OffloadedToContainerJobs`). |
| `azurerm_container_app_job` | Azure **Schedule** (cron) or **Event** (KEDA rules in `event_trigger_config`). |
| Log Analytics | Stdout from job replicas; query `JobStarted` / `JobCompleted` log lines. |
| OpenTelemetry | `archlucid_container_job_runs_total` + `archlucid_container_job_run_duration_ms` (labels `job_name`, `exit_class` / `exit_code`). |

## Configuration

| Key | Purpose |
|-----|---------|
| `Jobs:OffloadedToContainerJobs` | Indexed string list (`:0`, `:1`, …) of slugs to **remove** from Worker registration. |
| `Jobs:DeployedContainerJobNames` | Comma-separated superset Terraform sets after provisioning jobs; **Production Worker** validates offloaded ⊆ deployed. |

Canonical slugs: `ArchLucid.Host.Core.Jobs.ArchLucidJobNames`.

## Exit codes (`ArchLucid.Jobs.Cli`)

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Job threw or returned failure from `IArchLucidJob.RunOnceAsync` |
| 2 | Invalid CLI (`--job` missing) or `ArchLucidConfigurationRules` errors |
| 3 | Unknown `--job` name (no `IArchLucidJob` registration) |

## Terraform (`infra/terraform-container-apps/jobs.tf`)

- `trigger_type = "Schedule"` requires `cron_expression`.
- `trigger_type = "Event"` requires `event_scale_rules` (KEDA `custom_rule_type` + `metadata`; optional per-rule `auth` for secret-based scalers).
- Example (schedule):

```hcl
container_jobs = {
  advisory_scan = {
    trigger_type      = "Schedule"
    cron_expression   = "*/5 * * * *"
    args              = ["--job", "advisory-scan"]
  }
}
```

- Example (event-driven; replace metadata with your Service Bus topic subscription per [KEDA Azure Service Bus scaler](https://keda.sh/docs/scalers/azure-service-bus/)):

```hcl
container_jobs = {
  integration_events = {
    trigger_type = "Event"
    args         = ["--job", "servicebus-integration-events"]
    event_scale_rules = [
      {
        name             = "sb-msgs"
        custom_rule_type = "azure-servicebus"
        metadata = {
          topicName        = "your-topic"
          subscriptionName = "your-subscription"
          messageCount     = "5"
        }
      }
    ]
  }
}
```

- Set Worker app settings: `Jobs__OffloadedToContainerJobs__0=advisory-scan` and `Jobs__DeployedContainerJobNames=advisory-scan` **before** scaling Worker to rely on the job.

## CI manifest check

`scripts/ci/check_jobs_offload_manifest.py` compares comma-separated `--offloaded` vs `--deployed` lists (case-insensitive) so a pipeline can fail fast when Terraform has not yet declared a job name the Worker is told to offload.

## Manual operations

```bash
az containerapp job start \
  --name <job_name_from_terraform> \
  --resource-group <rg>
```

Rollback: remove the slug from `Jobs:OffloadedToContainerJobs` and redeploy Worker so the in-process `AdvisoryScanHostedService` resumes.

## Known limitations

- **`audit-retry-drain`** cannot move to a separate container until `IAuditRetryQueue` is backed by shared storage (ADR 0018).

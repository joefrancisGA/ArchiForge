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
| `azurerm_container_app_job` | Azure scheduler / (future) KEDA scaler target. |
| Log Analytics | Stdout from job replicas; query `JobStarted` / `JobCompleted` log lines. |

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

- Populate `var.container_jobs` with **Schedule** entries only (module checks enforce this until KEDA blocks land).
- Example (conceptual):

```hcl
container_jobs = {
  advisory_scan = {
    trigger_type    = "Schedule"
    cron_expression = "*/5 * * * *"
    args            = ["--job", "advisory-scan"]
  }
}
```

- Set Worker app settings: `Jobs__OffloadedToContainerJobs__0=advisory-scan` and `Jobs__DeployedContainerJobNames=advisory-scan` **before** scaling Worker to rely on the job.

## Manual operations

```bash
az containerapp job start \
  --name <job_name_from_terraform> \
  --resource-group <rg>
```

Rollback: remove the slug from `Jobs:OffloadedToContainerJobs` and redeploy Worker so the in-process `AdvisoryScanHostedService` resumes.

## Known limitations

- **`audit-retry-drain`** cannot move to a separate container until `IAuditRetryQueue` is backed by shared storage (ADR 0018).

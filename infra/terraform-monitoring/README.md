# Terraform: monitoring & alerting (Azure Monitor + optional Managed Grafana)

Optional root for **monitoring-as-code**:

- **`azurerm_monitor_action_group`** — email + optional HTTPS webhook (common alert schema).
- **`azurerm_monitor_metric_alert`** — optional **CPU** alerts on **API** and/or **Worker** Container Apps (`CpuUsageNanoCores`, 5-minute window). Threshold is in **nano cores** (e.g. `500000000` ≈ **0.5 vCPU** average).
- **`azurerm_dashboard_grafana`** (optional) — **Azure Managed Grafana** 11.x; assign **Monitoring Reader** (or Log Analytics roles) to the instance **managed identity** so operators can build dashboards against subscription metrics.

Dashboard JSON intended for import (Grafana Cloud, Managed Grafana, or self-hosted) lives under **`../grafana/dashboards/`**.

## Defaults

- **`enable_monitoring_stack = false`** — no resources; `terraform validate` in CI stays green.
- **`enable_managed_grafana = false`** — avoids Grafana subscription quota/cost until you opt in.

## Wiring after `terraform-container-apps`

1. Apply **`infra/terraform-container-apps`** (or note Container App **resource IDs** from Azure Portal).
2. Set **`api_container_app_resource_id`** / **`worker_container_app_resource_id`** and a non-zero **`container_cpu_nanos_threshold`** to create CPU alerts.
3. Run `terraform plan` / `apply` in this directory.

## Commands

```bash
cd infra/terraform-monitoring
terraform init
cp terraform.tfvars.example terraform.tfvars   # edit
terraform plan
terraform apply
```

## Security & cost

- **Webhook URLs** are sensitive; pass via **`TF_VAR_alert_webhook_uri`** or a pipeline secret, not git.
- **Managed Grafana** is a separate billed resource; tighten **public network access** and use **private endpoints** in hardened environments (extend `main.tf` as needed).

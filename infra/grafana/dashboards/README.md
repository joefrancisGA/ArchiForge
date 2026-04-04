# Grafana dashboards (as code)

JSON models in this folder are **imported** into:

- **Azure Managed Grafana** (`infra/terraform-monitoring` when `enable_managed_grafana = true`), or  
- **Grafana Cloud** / self-hosted Grafana with an **Azure Monitor** data source.

After import, open each panel’s query editor and select your **subscription / resource group / Container App**; the placeholders use static text only.

| File | Purpose |
|------|---------|
| `archiforge-container-apps-overview.json` | Orientation: links + placeholder row for CPU/replica panels. |

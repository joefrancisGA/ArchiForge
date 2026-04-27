# `infra/terraform-otel-collector/` — OpenTelemetry collector with tail-sampling

> **Scope:** Terraform stack for an Azure Container App that runs the OpenTelemetry collector with a **tail-sampling** policy. It exists because the in-process .NET SDK is **head-based** (decision at trace start), so high-value traces (errors, slow requests, `ArchLucid.AuthorityRun`) get silently dropped at production sampling ratios. See `docs/OBSERVABILITY.md` § Sampling strategy.
>
> **Status:** `main.tf` provisions an OpenTelemetry Collector Container App when `enable_otel_deployment = true` (default `false` for validate-only runs). Review `variables.tf` / `outputs.tf` before any `terraform apply`.

## Why this stack is separate

- It depends on `terraform-monitoring` (Application Insights / Log Analytics workspace IDs as inputs) and on a dedicated Container Apps environment from `terraform-container-apps`. Keeping it isolated lets operators upgrade the collector independently of the API.
- The tail-sampling policy is the **product-shaped** observability decision (which sources to always retain) — that belongs in source control next to ArchLucid, not inside a generic platform module.

## Tail-sampling policy (target)

| Match | Sample rate |
|---|---|
| `error.type` is set on the root span | 100% |
| Root span duration > 2s | 100% |
| `ActivitySource` is `ArchLucid.AuthorityRun` | 100% |
| `ActivitySource` is `ArchLucid.Agent.LlmCompletion` | 100% |
| Everything else | head-based ratio (configurable, default 10%) |

These are the same five activity sources called out in `docs/OBSERVABILITY.md` plus the wildcard fallback.

## Inputs (see `variables.tf`)

| Variable | Required | Notes |
|---|:---:|---|
| `resource_group_name` | yes | Existing RG — usually `archlucid-shared-rg` |
| `location` | yes | Match the API region; co-locate to avoid egress. |
| `container_apps_environment_id` | yes | From `terraform-container-apps`. |
| `application_insights_connection_string` | yes | From `terraform-monitoring`. |
| `tail_sampling_default_ratio` | no | Default 0.10. |
| `tail_sampling_always_keep_activity_sources` | no | Default list above. |

## Outputs (see `outputs.tf`)

- `otlp_grpc_endpoint` — wire into `OTEL_EXPORTER_OTLP_ENDPOINT` for API/Worker.
- `otlp_http_endpoint` — same, but for environments that block gRPC.

## TODO before first apply

- [ ] Decide image: vendored `otel/opentelemetry-collector-contrib:0.<pin>` vs an internally-maintained image.
- [ ] Define the collector config map (`otel-collector-config.yaml`) inline or via a referenced secret.
- [ ] Decide ingress (internal-only vs internal-with-private-link).
- [ ] Wire `azurerm_monitor_diagnostic_setting` for the collector container itself.
- [ ] Add a `checks.tf` postcondition that the OTLP endpoints respond to `/health` after apply.

With `enable_otel_deployment = false` (default), `terraform plan` is a no-op for resources. Set `enable_otel_deployment = true` plus a valid `container_apps_environment_id` and Application Insights connection string to materialize the collector.

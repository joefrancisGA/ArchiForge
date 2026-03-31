# SLO dashboards — Prometheus and Grafana

## Purpose

ArchiForge exposes **OpenTelemetry metrics** (including a Prometheus exporter when enabled) from the API host. This runbook explains how operators can turn those signals into **service level objectives (SLOs)** and **Grafana** dashboards without prescribing a single vendor topology.

## Enable Prometheus scraping

1. In configuration, set **`Observability:Prometheus:Enabled`** to **`true`** (see **`PipelineExtensions`** — OpenTelemetry Prometheus scraping endpoint).
2. Ensure the scrape target is reachable only from your **monitoring network** (private endpoint, firewall, or in-cluster `NetworkPolicy`), not from the public internet.
3. Confirm **`AddArchiForgeOpenTelemetry`** registers meters (**`ArchiForgeInstrumentation.MeterName`** = `ArchiForge`, plus schema validation meter where applicable).

## Metric families to anchor SLOs

These counters and histograms are defined in **`ArchiForge.Core.Diagnostics.ArchiForgeInstrumentation`**:

| Signal | Type | SLO angle |
|--------|------|-----------|
| `digest_delivery_succeeded` / `digest_delivery_failed` | Counter (`channel` label) | **Delivery reliability** for advisory digests. |
| `alert_evaluation_duration_ms` | Histogram (`rule_kind` = `simple` \| `composite`) | **Latency** of evaluation path; tail latency for burn-rate alerts. |
| `governance_resolve_duration_ms` | Histogram | **Governance** resolution time; correlate with cache metrics below. |
| `governance_pack_content_deserialize_cache_hits` / `_misses` | Counter | **Efficiency**; rising misses under steady load may indicate pack churn. |
| `persistence_json_fallback_used` | Counter (`entity_type`, `slice`, `read_mode`) | **Data path health**; spikes may indicate incomplete relational projections. |

**Availability SLOs** should also use **`/health/live`** and **`/health/ready`** (Kubernetes probes or synthetic checks), not only business metrics.

## Prometheus recording rules (examples)

Recording rules reduce dashboard cost and stabilize alert expressions. Adjust histogram bucket names to match your Prometheus OTel translation (`le` labels).

```yaml
groups:
  - name: archiforge_slos
    interval: 30s
    rules:
      - record: archiforge:digest_delivery_success_ratio_1h
        expr: |
          sum(rate(digest_delivery_succeeded_total[1h]))
          /
          (sum(rate(digest_delivery_succeeded_total[1h])) + sum(rate(digest_delivery_failed_total[1h])) + 1e-9)
```

Add similar ratios for **alert evaluation** error paths if you expose failure counters on those code paths in future iterations.

## Grafana dashboard panels (suggested rows)

1. **Red:** API ready probe success rate; 5xx rate from reverse proxy or App Gateway.
2. **Digest delivery:** `rate(digest_delivery_succeeded_total[5m])` by `channel` vs `digest_delivery_failed_total`.
3. **Alert evaluation:** p50/p90 of `alert_evaluation_duration_ms` histogram; split by `rule_kind`.
4. **Governance:** `governance_resolve_duration_ms` heatmap or percentiles; cache hit ratio = `hits / (hits + misses)`.
5. **Persistence:** `rate(persistence_json_fallback_used_total[15m])` by `entity_type`.

## Multi-window, multi-burn-rate alerts (sketch)

For a **99.9% monthly SLO** on digest delivery success:

- **Fast burn:** page if short-window error budget consumption exceeds threshold (e.g. 2% budget in 1 hour).
- **Slow burn:** ticket if medium window (e.g. 10% budget in 3 days).

Implement with **`prometheusrules`** CRDs (Kubernetes), Azure Monitor managed Prometheus alert rules, or Grafana Alerting — all are equivalent patterns; pick what your platform already runs.

## Cost and scale

- **Cardinality:** keep label sets small (`channel`, `rule_kind`); avoid high-cardinality user IDs in metric labels.
- **Scrape interval:** 15–30s is typical; sub-10s scales cost faster than it improves SLO fidelity for batch-style workloads (advisory scans).

## References in repo

- **`ArchiForge.Api/Startup/ObservabilityExtensions.cs`** — meter registration and exporters.
- **`ArchiForge.Api/Startup/PipelineExtensions.cs`** — Prometheus endpoint toggle.
- **`ArchiForge.Core/Diagnostics/ArchiForgeInstrumentation.cs`** — instrument names and descriptions.

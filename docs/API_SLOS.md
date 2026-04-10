# ArchLucid API — service level objectives (SLOs)

This document defines **customer-visible** HTTP objectives for the ArchLucid API and how they are **measured**. It complements `docs/runbooks/SLO_PROMETHEUS_GRAFANA.md` (Prometheus burn-rate math from OpenTelemetry) with an **external synthetic** view.

## 1. Objective

- State explicit **SLOs** (availability, error rate, latency) so reliability is **quantified**, not subjective.
- Align **internal** SLIs (Prometheus from `http.server.request.duration`) with **external** checks (scheduled probes from outside the cluster).
- Give operators a single place to answer: “Are we meeting the contract, from both the server’s and the internet’s perspective?”

## 2. Assumptions

- The HTTP availability SLO is **99.5%** over a **30-day** rolling window (matches `0.005` error budget in `infra/prometheus/archlucid-slo-rules.yml`). Leadership may change this number; then update recording rules, alerts, and this doc together.
- **“Good” requests** for the availability SLI are responses **without HTTP 5xx** (same proxy as in Prometheus rules; 4xx are excluded from “good”/“bad” in that formula unless you add a separate SLO).
- **Synthetic probes** call **anonymous** endpoints: `GET /health/live` (process up) and `GET /version` (build identity). They do **not** prove database connectivity; use `GET /health/ready` in a separate probe if you need readiness signal in SLO math — its JSON is **summary only** (status + per-check status, no exception text or build metadata). Full diagnostic health JSON (`GET /health`) requires **ReadAuthority** (API key or JWT with reader/operator/admin role).
- GitHub Actions runners reach your API over the **public** URL you configure (or private runner + internal URL). Network path differs from in-cluster scrapes.

## 3. Constraints

- **One probe per interval** does not, by itself, equal a **monthly** percentage; it is a **canary**. Rolling error budgets still come from **Prometheus** (or from exporting probe results to a time-series backend). Failing the workflow is an **immediate** “external path broken” signal.
- Synthetic jobs must **not** print API keys or OAuth tokens in logs. Use GitHub **secrets** only.
- Do not use synthetic traffic against **destructive** or **high-cost** routes.

## 4. Architecture overview

**Nodes:** ArchLucid API (health + version), GitHub Actions `ubuntu-latest`, optional secret `SYNTHETIC_API_BASE_URL`, Prometheus + OTel (in-cluster).

**Edges:** External runner → HTTPS → API; OTel → collector/Prometheus → SLO recording rules → alerts.

**Boundaries:** Internal SLO = all instrumented HTTP traffic. External SLO slice = probe endpoints only, from CI egress IPs.

## 5. Component breakdown

| Piece | Location | Role |
|-------|----------|------|
| Quantified HTTP SLO (5xx, availability ratio, burn) | `infra/prometheus/archlucid-slo-rules.yml` | Server-side SLI from request metrics |
| Burn-rate runbook | `docs/runbooks/SLO_PROMETHEUS_GRAFANA.md` | How alerts map to the 99.5% target |
| **Synthetic probe** | `.github/workflows/api-synthetic-probe.yml` | Periodic external `GET /health/live` + `GET /version`, latency check |
| Live/ready/detailed health maps | `ArchLucid.Api/Startup/PipelineExtensions.cs` | Anonymous: `/health/live` (minimal), `/health/ready` (summary JSON). `/health` is detailed JSON and requires `ReadAuthority`. |
| Version (anonymous) | `ArchLucid.Api/Controllers/VersionController.cs` | `GET /version` for build identity |

## 6. Data flow

1. **In-cluster:** Each HTTP request updates OTel metrics → Prometheus → `archlucid:slo:http_availability:ratio` and burn-rate alerts.
2. **External:** On a schedule, CI runs `curl` (or equivalent) to `{base}/health/live` and `{base}/version`, records **HTTP status** and **round-trip time**, fails the job on non-success or slow responses (configurable ceiling).
3. **Interpretation:** Prometheus shows **aggregate** user traffic health; synthetic shows **reachability + minimal app stack** from outside. Divergence (e.g. synthetic green, burn red) often points to **specific routes**, **dependencies**, or **auth** issues—not the probe paths.

## 7. Security model

- **Secrets:** `SYNTHETIC_API_BASE_URL` (required for meaningful runs), optional `SYNTHETIC_API_PROBE_KEY` with header `X-Api-Key` only if your deployment requires it for the probed paths (default ArchLucid mapping keeps `GET /health/live`, `GET /health/ready`, and `GET /version` anonymous; `GET /health` is **not** used by the default synthetic workflow).
- **Least privilege:** Probe uses read-only GET; no bearer tokens in repo.
- **Exposure:** Choosing a public base URL is intentional; it must not include credentials in the path or query string.

## 8. Operational considerations

### SLO table (HTTP API — contract)

| SLO | Target | Measurement window | SLI definition | Where measured |
|-----|--------|--------------------|----------------|----------------|
| **Availability** | **99.5%** | 30 days rolling | Ratio of successful requests: **non-5xx** / **all** (server-side) | Prometheus recording rules; Grafana |
| **Error rate** | ≤ **0.5%** 5xx (budget) | Same | 5xx count / all requests | Same |
| **Latency** | **p95 under 2 s** for API requests (initial guardrail; tune per environment) | 5m (Prometheus) | Histogram `http.server.request.duration` p95 | `archlucid:slo:http_p95_seconds`; alert policy optional |
| **Synthetic reachability** | **100%** of scheduled runs succeed (both endpoints **HTTP 2xx**, latency under ceiling) | Per run | `GET /health/live` + `GET /version` | GitHub Actions workflow; job summary |

**Note:** The **latency SLO** row is a **starting guardrail**; product owners should align it with pilot SLAs. Prometheus already records **p99** with `ArchLucidSloHttpP99High` at 5s as a **warning**—adjust thresholds to match the table once agreed.

### Rolling up synthetic results

To convert “every 15 minutes” into a monthly SLO, either:

- **Export** probe outcomes (status + latency) to **Prometheus** (push gateway or custom exporter) or **Azure Monitor**, or  
- Treat CI as **paging input** only and keep **authoritative** availability in Prometheus.

### When the synthetic workflow is skipped

If `SYNTHETIC_API_BASE_URL` is unset, the workflow exits **successfully** with a skip message so forks and inactive repos stay green. **Production** orgs should set the secret and optionally branch protection / required checks if this probe is part of release policy.

## 9. Terraform / IaC

- No dedicated cloud resource is **required** for GitHub-hosted synthetic probes: configuration is **repository secrets** + workflow YAML.
- If you move probes to **Azure Monitor standard tests** or **Container Apps** jobs, represent the resource (test group, identity, VNet) in Terraform and link it here in a future revision.

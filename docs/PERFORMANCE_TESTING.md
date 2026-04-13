# Performance testing (k6 smoke)

## Purpose

The **`tests/load/smoke.js`** script is a **short, read-only** load profile against the ArchLucid API. It complements:

- **`docs/PERFORMANCE.md`** — runtime caching and hot-path design notes.
- **`docs/API_SLOS.md`** — product SLOs and error budgets.

k6 establishes a **regression baseline** for latency and error rate on health, version, paged runs list, and audit search.

## Running locally

1. Start the API (for example `dotnet run --project ArchLucid.Api` with `ArchLucidAuth:Mode=DevelopmentBypass` and storage you prefer).
2. Install [k6](https://k6.io/docs/get-started/installation/).
3. From the repo root:

```bash
BASE_URL=http://127.0.0.1:5128 k6 run tests/load/smoke.js --out json=k6-results.json
```

Adjust **`BASE_URL`** if the API listens elsewhere.

## Scenarios and thresholds

| Scenario | Traffic | p95 target | Notes |
|----------|---------|------------|--------|
| `health` | Ramp to **10** VUs over **30s** | **&lt; 500ms** | `GET /health/live`, `GET /health/ready` |
| `version` | Ramp to **5** VUs, **30s** | **&lt; 500ms** | `GET /version` |
| `runs_list` | Ramp to **5** VUs, **30s** | **&lt; 2000ms** | `GET /v1/authority/projects/{defaultGuid}/runs?page=1&pageSize=20` |
| `audit_search` | Ramp to **3** VUs, **30s** | **&lt; 2000ms** | `GET /v1/audit/search?maxResults=50` |

Global: **`http_req_failed` rate &lt; 1%**.

Every request sends **`X-Correlation-ID: k6-smoke-{scenario}-{vu}-{iter}`** for log correlation.

## Tuning thresholds

Raise thresholds only when a change **intentionally** increases latency (for example new mandatory work in the hot path). Document the reason in the PR. If thresholds are too loose, the suite stops catching regressions.

## JSON output

`--out json=results.json` produces k6’s JSON stream (metrics and samples). In CI the artifact is uploaded for **trend comparison** and post-processing (for example Grafana k6 Cloud or a custom parser). For a quick local view, omit `--out` and read the end-of-test summary in the console.

## CI

See **`docs/TEST_EXECUTION_MODEL.md`** — job **`Performance: k6 smoke (API baseline)`** (non-blocking initially). The workflow starts the API with SQL (same pattern as live E2E), waits for **`/health/ready`**, runs k6 in Docker, and uploads **`k6-results.json`**.

### Docker: `permission denied` on `--out json=/out/...`

The **`grafana/k6`** image runs as a **non-root** user by default. If you bind-mount a host directory (for example **`RUNNER_TEMP`** in GitHub Actions) that is owned by another uid, k6 cannot create **`k6-results.json`**. The CI workflow passes **`docker run --user "$(id -u):$(id -g)"`** so the process matches the host user and can write to the mount. Reuse the same flag when reproducing the CI command locally.

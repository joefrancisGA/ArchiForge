# k6 load scripts (`tests/load`)

## Core pilot load baseline (Flow A)

**SLOs:** [../go-to-market/SLA_SUMMARY.md](../go-to-market/SLA_SUMMARY.md) — 99.5% availability, **p95 &lt; 2s** for API response time (5-minute windows; agent/LLM work may exceed this on longer paths). **Flow A:** [../library/ARCHITECTURE_FLOWS.md](../library/ARCHITECTURE_FLOWS.md) (create run → tasks/results → commit → manifest/artifacts).

**What it measures:** `core-pilot.js` drives the **pilot** HTTP path against a **local** API (default `http://127.0.0.1:5001`), **DevelopmentBypass** (no Entra/JWT), **InMemory** storage, and **Simulator** agent mode — see `ArchLucid.Api/appsettings.Development.json`. Do not point this at production or staging.

| Profile | VUs / duration (defaults) | Primary flows |
|--------|----------------------------|---------------|
| `core` (default) | 10 VUs, 5m | `POST /v1/architecture/request` → `POST .../execute` → `POST .../commit` → `GET /v1/authority/manifests/{id}/summary` → `GET /v1/artifacts/manifests/{id}` |
| `read` | 50 VUs, 5m | `GET /v1/authority/projects/{project}/runs` → `GET /v1/authority/runs/{id}` → optional `GET /v1/authority/manifests/{id}/summary` when a committed manifest exists |
| `mixed` | 5 writers + 25 readers, 10m | Same as `core` on writers, `read` on readers |

**Run (single profile):**

```text
# From repo root; k6 in PATH. Start API with Development + InMemory + Simulator, e.g.:
#   dotnet run --project ArchLucid.Api --urls http://127.0.0.1:5001
#   (optional) set RateLimiting__FixedWindow__PermitLimit=200000 for high VU

set ARCHLUCID_BASE_URL=http://127.0.0.1:5001
k6 run tests/load/core-pilot.js
set K6_LOAD_PROFILE=read & k6 run tests/load/core-pilot.js
set K6_LOAD_PROFILE=mixed & k6 run tests/load/core-pilot.js
```

On PowerShell, use `$env:K6_LOAD_PROFILE='read'` before `k6 run`.

**Output:** `handleSummary` writes a JSON fragment to `K6_BASELINE_PATH` or `tests/load/results/baseline-${K6_BASELINE_DATE}.json` (default `baseline-local.json` if unset). Each k6 `metrics` block includes **p50, p95, p99** on `http_req_duration` (global and per-`endpoint` tag), **http_req_failed** (error rate), and **http_reqs** (count + rate, ≈ rps in the test window).

**One merged baseline (all three profiles):** after installing **k6** and **Python 3**:

```powershell
# Quick dry capture (20s/20s/30s windows) — for full 5m/5m/10m runs, omit -Compress
./tests/load/record-baseline.ps1 -Date 2026-04-24 -BaseUrl http://127.0.0.1:5001 -Compress
```

**Committed snapshot:** [results/baseline-2026-04-24.json](results/baseline-2026-04-24.json) (example layout; **re-run `record-baseline.ps1` without `-Compress` on your machine to replace** with real k6 output for your CPU/network).

**Compare later runs:** Keep the new handleSummary JSON and diff against the stored baseline (or use `k6` `--summary-export` in addition). Watch **p(95)** on each `http_req_duration{endpoint:...}` and **http_req_failed**; regressions: higher p95 (especially above ~2000ms on lightweight read endpoints) or higher error rate.

| Variable | Default | Purpose |
|----------|---------|--------|
| `ARCHLUCID_BASE_URL` / `BASE_URL` | `http://127.0.0.1:5001` | API base URL (spec default for pilot baseline) |
| `K6_LOAD_PROFILE` | `core` | `core` \| `read` \| `mixed` |
| `K6_COMPRESS` | *off* | `1` or `true` = short dev durations (20s/20s/30s) |
| `K6_BASELINE_PATH` / `K6_BASELINE_DATE` | (see `core-pilot.js`) | Output fragment path for `handleSummary` |
| `K6_CORE_VUS` / `K6_READ_VUS` / `K6_MIXED_WRITERS` / `K6_MIXED_READERS` | 10 / 50 / 5 / 25 | VU counts |

**Install k6 (Windows):** `winget install k6 --source winget` (or [grafana k6](https://grafana.com/docs/k6/latest/set-up/install-k6/)).

---

## Real-mode E2E benchmark (time-to-value)

**Script:** `real-mode-e2e-benchmark.js`

Measures wall-clock time for a complete **real-mode** authority run (create → execute → poll until done → commit → retrieve manifest) against a live API using **actual Azure OpenAI** agent execution. This produces the defensible "time-to-value" figure for marketing, SLA, and buyer conversations.

**Unlike `core-pilot.js`**, this script targets real LLM execution — it is **not** for simulator/in-memory baselines. Run it against a staging or production API with `AgentExecution:Mode=AzureOpenAI`.

| Variable | Default | Purpose |
|----------|---------|---------|
| `ARCHLUCID_BASE_URL` | `http://127.0.0.1:5001` | API base URL |
| `ARCHLUCID_API_KEY` | *(unset)* | API key for authentication |
| `K6_POLL_INTERVAL_MS` | `2000` | Polling interval when waiting for run completion |
| `K6_POLL_TIMEOUT_MS` | `300000` (5 min) | Max wait before declaring timeout |
| `K6_ITERATIONS` | `3` | Number of benchmark iterations |
| `K6_SUMMARY_PATH` | `tests/load/results/real-mode-e2e-benchmark.json` | Output path |

**Run against staging:**

```bash
ARCHLUCID_BASE_URL=https://staging.archlucid.net \
ARCHLUCID_API_KEY=<staging-key> \
k6 run tests/load/real-mode-e2e-benchmark.js
```

**Custom metrics emitted:**

| Metric | What it measures |
|--------|-----------------|
| `e2e_wall_clock_ms` | Total wall-clock time from request creation to manifest retrieval |
| `step_create_ms` | Time to create the architecture request |
| `step_execute_ms` | Time for the execute HTTP call to return |
| `step_poll_wait_ms` | Total time spent polling until run completes |
| `step_commit_ms` | Time to commit the run |
| `step_manifest_retrieve_ms` | Time to retrieve the manifest summary |

**Thresholds:**

- `e2e_wall_clock_ms` p50 < 120s, p95 < 180s
- `e2e_fail_count` < 1 (all iterations must succeed)

---

## Environment variable matrix

All scripts accept **`ARCHLUCID_BASE_URL`** (preferred) or **`BASE_URL`** (alias) for the API base URL. Default: `http://127.0.0.1:5128`.

| Script | Required env | Optional env | Example one-liner | Summary path |
|--------|-------------|-------------|-------------------|-------------|
| **`real-mode-e2e-benchmark.js`** | `ARCHLUCID_BASE_URL` (staging/prod) | `ARCHLUCID_API_KEY`, `K6_POLL_INTERVAL_MS`, `K6_POLL_TIMEOUT_MS`, `K6_ITERATIONS`, `K6_SUMMARY_PATH` | `ARCHLUCID_BASE_URL=https://staging.archlucid.net k6 run tests/load/real-mode-e2e-benchmark.js` | `tests/load/results/real-mode-e2e-benchmark.json` (via `handleSummary`) |
| **`core-pilot.js`** | — | `K6_LOAD_PROFILE` (`core`\|`read`\|`mixed`), `K6_COMPRESS`, `K6_BASELINE_PATH`, `K6_BASELINE_DATE`, `K6_CORE_VUS`, `K6_READ_VUS`, `K6_MIXED_*` | `k6 run tests/load/core-pilot.js` | `tests/load/results/baseline-*.json` (via `handleSummary`) or merge `record-baseline.ps1` |
| **`k6-api-smoke.js`** | — | `ARCHLUCID_API_KEY`, `ARCHLUCID_AUTHORITY_PROJECT` (`default`), `K6_SCENARIO` (`smoke`\|`load`), `K6_SUMMARY_PATH` | `k6 run tests/load/k6-api-smoke.js` | `tests/load/results/k6-summary.json` (via `handleSummary`) |
| **`ci-smoke.js`** | — | — | `k6 run tests/load/ci-smoke.js --summary-export /tmp/k6-ci-summary.json` | `--summary-export` arg |
| **`smoke.js`** | — | — | `k6 run tests/load/smoke.js --out json=k6-results.json` | `--out` / `--summary-export` |
| **`soak.js`** | — | `SOAK_VUS` (`3`), `SOAK_DURATION` (`4m`) | `k6 run tests/load/soak.js --summary-export /tmp/k6-soak.json` | `--summary-export` arg |
| **`per-tenant-burst.js`** | — | `K6_BURST_DURATION` (`5m`), `ARCHLUCID_API_KEY`, `K6_SUMMARY_PATH` | `K6_BURST_DURATION=30s k6 run tests/load/per-tenant-burst.js --summary-export /tmp/k6-burst.json` | **`handleSummary`** default under `tests/load/results/` |

> **Tip:** CI sets **`RateLimiting__FixedWindow__PermitLimit=200000`** on the API process to avoid mass **`429`** from rate limiting. Do the same locally when running k6 at higher VU counts. With **`ArchLucid:StorageProvider=Sql`**, **`appsettings.Advanced.json`** enables **`SqlServer:RowLevelSecurity:ApplySessionContext`**; startup then needs **`ARCHLUCID_ALLOW_RLS_BYPASS=true`** and **`ArchLucid__Persistence__AllowRlsBypass=true`** (see **`scripts/ci/start_api_for_k6.sh`**).

---

## `k6-api-smoke.js` — operator path (CI + local)

Exercises: **`GET /health/ready`** (JSON **`status`** must be **`Healthy`**), **`GET /version`**, **`POST /v1/architecture/request`**, **`GET /v1/authority/projects/{project}/runs?take=10`** (default project slug **`default`**).

**Environment**

| Variable | Default | Purpose |
|----------|---------|---------|
| **`ARCHLUCID_BASE_URL`** | `http://127.0.0.1:5128` | API base URL (`BASE_URL` is accepted as alias) |
| **`ARCHLUCID_API_KEY`** | *(unset)* | Optional **`X-Api-Key`** for keyed auth |
| **`ARCHLUCID_AUTHORITY_PROJECT`** | `default` | Authority project slug for list runs |
| **`K6_SCENARIO`** | *(unset = smoke)* | Set to **`load`** for ~3m / 20 VU ramping scenario |
| **`K6_SUMMARY_PATH`** | `tests/load/results/k6-summary.json` | **`handleSummary`** output path |

**Local run**

1. Start SQL + API (e.g. `docker compose up -d` for infra, then `dotnet run --project ArchLucid.Api` with **`DevelopmentBypass`** and **`AgentExecution:Mode: Simulator`** recommended).
2. Ensure `tests/load/results/` exists (tracked via `.gitkeep`).
3. Run:

```bash
k6 run tests/load/k6-api-smoke.js
```

Summary JSON is written to **`tests/load/results/k6-summary.json`** (gitignored). Override with **`K6_SUMMARY_PATH`**.

**Load scenario**

```bash
K6_SCENARIO=load k6 run tests/load/k6-api-smoke.js --summary-export /tmp/k6-load-summary.json
```

**Thresholds (built into script)**

- **`http_req_failed`**: rate &lt; **1%**
- **`http_req_duration`**: **p95** &lt; **2000** ms, **p99** &lt; **5000** ms

**CI**

Job **`Performance: k6 API smoke (operator path)`** in **`.github/workflows/ci.yml`** runs after **`.NET: full regression (SQL)`**, installs native k6 on the runner, starts **`ArchLucid.Api`** against catalog **`ArchLucidK6Smoke`**, runs this script with **`K6_SCENARIO=smoke`**, then **`scripts/ci/assert_k6_ci_smoke_summary.py`** (same gate as k6 CI smoke: failed rate + p95). Artifact: **`k6-smoke-results`**.

## `ci-smoke.js` — read + write CI smoke

Scenarios: **health** (live + ready), **version**, **create_run** (write path), **list_runs**, **audit_search**. Per-tag k6 thresholds; CI asserts via **`scripts/ci/assert_k6_ci_smoke_summary.py --per-tag-ci-smoke`**.

**Local run**

```bash
k6 run tests/load/ci-smoke.js --summary-export /tmp/k6-ci-summary.json
```

**CI**

Job **`Performance: k6 CI smoke (read + write baseline)`** in **`.github/workflows/ci.yml`** runs after **`dotnet-fast-core`** (merge-blocking). Native k6.

## `soak.js` — scheduled / manual soak

Low-rate read-only mix (`health`, `version`, `runs_list`, `audit_search`). Duration and VUs configurable via **`SOAK_DURATION`** and **`SOAK_VUS`**. Relaxed thresholds.

**Workflow:** `.github/workflows/k6-soak-scheduled.yml` (weekly cron + `workflow_dispatch`). Set repository secret **`ARCHLUCID_SOAK_BASE_URL`** or the job no-ops.

## Other scripts

| File | Purpose |
|------|---------|
| **`smoke.js`** | Read-only paths; used for broader read mix (see **`docs/LOAD_TEST_BASELINE.md`**) |

Deeper baselines and Compose full-stack runs: **`docs/LOAD_TEST_BASELINE.md`**, **`scripts/load/README.md`**.

# k6 load scripts (`tests/load`)

## Environment variable matrix

All scripts accept **`ARCHLUCID_BASE_URL`** (preferred) or **`BASE_URL`** (alias) for the API base URL. Default: `http://127.0.0.1:5128`.

| Script | Required env | Optional env | Example one-liner | Summary path |
|--------|-------------|-------------|-------------------|-------------|
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

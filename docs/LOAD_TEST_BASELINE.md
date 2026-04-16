# Load test baseline (API hot paths)

## Objective

Record **repeatable** latency and throughput for the five highest-traffic API paths so horizontal scaling (Container Apps, read replicas, worker queue depth) is justified with numbers, not assumptions. Complement micro-benchmarks in `ArchLucid.Benchmarks` and cold-start guidance in `docs/PERFORMANCE_COLD_START_AND_TRIMMING.md`.

## Assumptions

- Load tests run against **Docker Compose `full-stack`** on a **dedicated** machine or GitHub Actions runner — not production or shared staging.
- The API uses **DevelopmentBypass** auth (Compose default); optional `API_KEY` is supported by `scripts/load/hotpaths.js` for keyed environments.
- First **numeric** baseline cells are filled by running `scripts/load/record_baseline.ps1` (Windows) or `scripts/load/record_baseline.sh` (Linux/macOS) with Docker, or the manual **Actions → Load test** workflow; see the table footnote below.

## Constraints

- **No public SMB or shared infra** for test data; Compose binds SQL/Redis/Azurite locally on the runner.
- **List endpoints:** prefer **keyset** cursors where the API exposes them (e.g. **`GET /v1/audit/search?beforeUtc=…`** for audit); offset pagination remains on some paths — see **`docs/API_CONTRACTS.md`**.
- **Merge-blocking** k6 operator-path smoke runs in **`.github/workflows/ci.yml`** after full regression (`tests/load/k6-api-smoke.js`). The **Compose full-stack** workflow **`.github/workflows/load-test.yml`** remains **manual only** for longer / heavier profiles.
- k6 **checks** rate threshold is **0.85**; **`http_req_duration` p(95)** cap is **2000** ms from the **Initial** baseline (2× ~773 ms p95, rounded up to 500 ms). Re-run the recorder after material infra or API changes and refresh this doc + `hotpaths.js`.

## Architecture overview

| Node | Role |
| --- | --- |
| k6 (runner) | Drives HTTP scenarios (create run, list runs, manifest, comparisons, retrieval search). |
| API container | `full-stack` profile, port `5000→8080`. |
| SQL Server / Redis / Azurite | Backing services per `docker-compose.yml`. |

**Flow:** k6 → HTTP → API → SQL / optional embeddings path (search may return 400 if retrieval is not configured — script accepts 200 or 400).

## Component breakdown

| Piece | Location |
| --- | --- |
| k6 script (manual, full write) | `scripts/load/hotpaths.js` |
| k6 script (CI smoke, read + write) | `tests/load/ci-smoke.js` |
| k6 script (CI smoke, read-only) | `tests/load/smoke.js` |
| k6 script (CI after full regression, operator path) | `tests/load/k6-api-smoke.js` |
| Local runbook | `scripts/load/README.md` |
| Manual CI workflow | `.github/workflows/load-test.yml` |
| Summary → Markdown | `scripts/ci/print_k6_summary_metrics.py` |
| List pagination guard | `scripts/ci/assert_list_endpoint_pagination.py` + `list_endpoint_pagination_allowlist.txt` |
| CPU baselines | `ArchLucid.Benchmarks` (merge + SQL paging fragments), compared in CI via `ci/benchmark-baseline.json` |

## Data flow

1. Compose starts **full-stack**; waiter polls `GET /health/live`.
2. k6 executes iterations: POST create run → GET list runs → GET manifest → GET comparisons (paged) → GET retrieval search → sleep.
3. `--summary-export k6-summary.json` captures aggregate trends; the workflow uploads the JSON and appends p50/p95/p99 to the job summary.

## Security model

- Default Compose auth is **DevelopmentBypass** — do not point the workflow at production URLs.
- Secrets: pass `API_KEY` only via GitHub **secrets** if you add a keyed environment later; never commit keys.

## Operational considerations

### Baseline table (refresh after each formal run)

| Run label | Date (UTC) | VUs | Duration | p50 `http_req_duration` (ms) | p95 | p99 | `http_reqs` rate | Commit / workflow run |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Initial | 2026-04-14 | 5 | 2m | 20.2 | 773 | 1232 | 13.34 | Local `pwsh ./scripts/load/record_baseline.ps1` (full-stack Compose); pair with `git rev-parse HEAD` on the commit that updates this table |

**How to capture metrics (same profile as this row: VUs=5, DURATION=2m, SLEEP_SEC=1):**

1. Start Docker Desktop (or Linux engine), then from repo root run **`pwsh ./scripts/load/record_baseline.ps1`** or **`bash scripts/load/record_baseline.sh`** (Compose full-stack + k6 in `grafana/k6` + `print_k6_summary_metrics.py` + teardown).  
2. Paste **p50 / p95 / p99 / rate** into the table above and set **`git rev-parse HEAD`** (or the GitHub Actions run URL) in the last column.  
3. Update **`scripts/load/hotpaths.js`**: replace the `http_req_duration` threshold with the **Suggested** line printed by the Python script (2× observed p95, rounded up to 500 ms).  

**CI alternative:** **Actions → Load test (k6, Compose full-stack)** — align inputs to **VUs 5** and **duration 2m** for parity; copy the job-summary k6 table and artifact `k6-summary.json`.

> **Note:** The **Initial** row was recorded with **`pwsh ./scripts/load/record_baseline.ps1`** against Docker Desktop (Compose **full-stack**). Re-run after major changes to compare regressions.

### Scaling thresholds (evidence-based, not hard SLOs)

| Signal | Interpretation |
| --- | --- |
| p95 `http_req_duration` **> ~2s** sustained on this profile for **list/search** | Inspect SQL plans, page sizes (`SqlPagingSyntax` / repository caps), replica lag, and cache before scaling out API replicas. |
| Create-run p95 **dominates** | Often idempotency, orchestration, or cold provider calls — profile with traces (`docs/PERFORMANCE_COLD_START_AND_TRIMMING.md`). |
| Throughput plateaus with low CPU | Check SQL locking, connection pool, and worker queue depth (scale workers separately from API). |

### BenchmarkDotNet regression suite

CI job **“.NET: benchmark regression (short job)”** enforces mean ceilings for:

- Agent dispatch ordering and simulated parallel batching (existing).
- **Decision engine merge** (`MergeTwoAgentResults`).
- **SQL paging fragment** construction (`FirstRowsOnlyFragment` for row caps used in repositories).

Raise `ci/benchmark-baseline.json` only when a change **intentionally** improves or stabilizes measured means (document the reason in the PR).

## CI smoke (automated)

`tests/load/ci-smoke.js` runs automatically on every push / PR via the **`k6-ci-smoke`** job in `.github/workflows/ci.yml` (Tier 2c). Unlike the read-only `tests/load/smoke.js` (which exercises health, version, list runs, and audit search), the CI smoke script adds a **write-path scenario** (`POST /v1/architecture/request`) so regressions in the create-run hot path are caught before merge.

### Merge gate (summary JSON)

After k6 finishes, **`scripts/ci/assert_k6_ci_smoke_summary.py`** parses `--summary-export` JSON. Two enforcement modes:

| Mode | Flag | What is checked |
| --- | --- | --- |
| **Global** (default) | `--max-p95-ms` | Overall `http_req_duration` p(95) |
| **Per-tag** | `--per-tag-ci-smoke` | Each `k6ci:*` tagged sub-metric against built-in caps matching `ci-smoke.js` thresholds (health_live ≤ 500 ms, health_ready ≤ 1500 ms, create_run ≤ 3000 ms, list_runs ≤ 1500 ms, audit_search ≤ 1500 ms, version ≤ 1500 ms, list_for_get_run ≤ 1500 ms, get_run_detail ≤ 2500 ms, client_error_telemetry ≤ 1500 ms). Falls back to global if tags are absent. |

Both modes check `http_req_failed` **rate** against `--max-failed-rate` (default **0**, CI passes **0.02**).

The **`k6-ci-smoke`** CI job uses **`--per-tag-ci-smoke`** so each scenario is individually gated. Raise caps only after a measured baseline change (update `ci-smoke.js` thresholds, `_CI_SMOKE_TAG_CAPS` in the Python script, and this table in the same PR). Prefer fixing flakes (API readiness, SQL cold start) over widening thresholds.

### Scenarios

| Scenario | Executor | VUs | Duration | Endpoint | Threshold |
| --- | --- | --- | --- | --- | --- |
| `health` | constant-vus | 5 | 20 s | `GET /health/live` (`k6ci:health_live`), `GET /health/ready` (`k6ci:health_ready`) | live p(95) < 500 ms; ready p(95) < 1500 ms (ready includes SQL / probes; a single combined tag was flaky vs 300 ms) |
| `create_run` | constant-vus | 2 | 30 s | `POST /v1/architecture/request` | p(95) < 3000 ms |
| `list_runs` | constant-vus | 3 | 20 s (`startTime: "5s"`) | `GET /v1/architecture/runs` | p(95) < 1500 ms |
| `audit_search` | constant-vus | 2 | 20 s | `GET /v1/audit/search?take=20` | p(95) < 1500 ms |
| `version` | constant-vus | 2 | 20 s | `GET /version` (`k6ci:version`) | p(95) < 1500 ms |
| `get_run_detail` | constant-vus | 2 | 20 s (`startTime: "8s"`) | `GET /v1/architecture/runs` then `GET /v1/architecture/run/{id}` (`k6ci:list_for_get_run`, `k6ci:get_run_detail`) | list p(95) < 1500 ms; detail p(95) < 2500 ms |
| `client_error_telemetry` | constant-vus | 1 | 18 s (`startTime: "10s"`) | `POST /v1/diagnostics/client-error` (expects **204**) | p(95) < 1500 ms |

Global failure threshold: `http_req_failed` rate < 2 %. Total wall-clock duration: ~30 s (longest scenario).

### Reader–writer contention (shared SQL on Actions)

PR **`k6-ci-smoke`** runs **create_run** (writes) and **list_runs** (reads) against the **same** SQL Server service container as the API. Inserts into **`dbo.Runs`** update clustered PK pages while **`GET /v1/architecture/runs`** used **`SELECT TOP … ORDER BY CreatedUtc DESC`**: a non-covering **`IX_Runs_Scope_CreatedUtc`** forced **key lookups** into the clustered index, producing **bimodal** latency (median ~ms, p(95) multi-second lock waits).

Mitigations in product code:

- **Covering index** **`IX_Runs_Scope_CreatedUtc`** with **`INCLUDE`** on all list columns (migration **`061_RunsScopeCreatedUtcCoveringIndex.sql`**, mirrored in **`ArchLucid.sql`**) so the list plan stays on the nonclustered index leaf.
- **`WITH (NOLOCK)`** on **dashboard-grade** **`SqlRunRepository`** list paths (**`ListRecentInScopeAsync`**, **`ListByProjectAsync`**, **`ListByProjectPagedAsync`**) and on **`DapperAuditRepository`** list/search/export **`SELECT`s** against **`dbo.AuditEvents`** — same staleness tolerance as **read-replica** lag; **not** used on **`GetByIdAsync`** or mutating paths.
- **Stagger:** **`list_runs`** uses **`startTime: "5s"`** so reads start after **`create_run`** has warmed the table and statistics, reducing cold-start worst cases.

### How it differs from the manual workflow

| Dimension | CI smoke (`ci-smoke.js`) | Manual dispatch (`hotpaths.js` via `load-test.yml`) |
| --- | --- | --- |
| **Trigger** | Automatic on push / PR | Manual `workflow_dispatch` |
| **Write paths** | `POST /v1/architecture/request` | Full: create, manifest, comparisons, retrieval search |
| **Read paths** | Health, list runs, audit search | Same plus version and manifest |
| **Duration** | ~30 s | Configurable (default 2 m) |
| **VU count** | Fixed (2–5 per scenario) | Configurable (default 5) |
| **Thresholds** | Generous (CI-safe) + Python assert on summary | Tighter (baseline-tracking) |
| **Blocking** | **Yes** — `k6-ci-smoke` is merge-blocking | Non-blocking (manual) |

### Local run

```bash
BASE_URL=http://127.0.0.1:5128 k6 run tests/load/ci-smoke.js --summary-export /tmp/k6-ci-summary.json
```

Or with Docker (soak / local; CI now uses native k6):

```bash
docker run --rm --network host \
  -v "$(pwd)/tests/load:/scripts:ro" \
  -e BASE_URL=http://127.0.0.1:5128 \
  grafana/k6:latest run /scripts/ci-smoke.js
```

## Soak profile (scheduled / manual)

**`tests/load/soak.js`** runs a **longer, low-rate** read-only mix against a **deployed** base URL. It differs from the CI smoke scripts in purpose and scope:

| Dimension | CI smoke (`ci-smoke.js`) | Soak (`soak.js`) |
| --- | --- | --- |
| **Trigger** | Automatic on push / PR | Weekly cron (`0 7 * * 0`) or `workflow_dispatch` |
| **Write paths** | Yes (`POST /v1/architecture/request`) | No (read-only) |
| **Duration** | ~30 s | Configurable via `SOAK_DURATION` (default **4 m**) |
| **VUs** | Fixed (2–5) | Configurable via `SOAK_VUS` (default **3**) |
| **Auth** | DevelopmentBypass (CI service container) | External API (secret **`ARCHLUCID_SOAK_BASE_URL`**); auth depends on target |
| **Blocking** | **Yes** (merge gate) | **No** (`continue-on-error: true`) |
| **Thresholds** | Per-tag p95 (500–3000 ms) | Relaxed: health/version p95 < 2000 ms, list/search p95 < 8000 ms |

### Workflow

`.github/workflows/k6-soak-scheduled.yml` — repository secret **`ARCHLUCID_SOAK_BASE_URL`** must be set (e.g. `https://api.staging.example`) or the job logs a skip message and exits cleanly. k6 runs via `grafana/k6:latest` Docker image.

Artifacts: **`k6-soak-summary`** (summary JSON) + job log printout via **`scripts/ci/print_k6_summary_metrics.py`**.

### Alerting guidance (on-call)

The soak workflow is **non-blocking** — failures do not break anything. Use the following triage process when soak fails:

1. **Single failure:** download the **`k6-soak-summary`** artifact and compare p95/p99 to previous runs. A one-off spike usually means transient infrastructure noise (runner, network, staging restart).
2. **Two consecutive failures:** investigate. Check staging deployment logs, SQL DTU / connection pool saturation, and any recent product changes that affect the exercised endpoints (`/health/live`, `/version`, `/v1/architecture/runs`, `/v1/audit/search`).
3. **Three or more consecutive failures:** open a ticket. Compare soak p95 to the CI smoke baseline; if CI smoke (which hits a fresh SQL container) is fine but soak (which hits staging) is not, the problem is likely infrastructure or data-volume related.

> **Never** make the soak workflow merge-blocking. Its purpose is to detect slow drift over time on real data, not to gate PRs.

## Pagination audit

`List*` / `Search*` `[HttpGet]` actions without obvious pagination parameters fail CI unless listed in `scripts/ci/list_endpoint_pagination_allowlist.txt`. Shrink the allowlist as endpoints gain `skip`/`take`/`limit`/query DTOs.

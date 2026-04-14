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
- CI load job is **manual only** (`.github/workflows/load-test.yml`) to avoid flaky PR gates and resource contention.
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

After k6 finishes, **`scripts/ci/assert_k6_ci_smoke_summary.py`** parses `--summary-export` JSON and **fails the job** when:

| Check | Default cap | Tune via workflow argv |
| --- | --- | --- |
| `http_req_failed` **rate** | **≤ 2%** | `--max-failed-rate` |
| `http_req_duration` **p(95)** | **≤ 3000 ms** | `--max-p95-ms` |

Raise caps only after a measured baseline change (update this table and the workflow step in the same PR). Prefer fixing flakes (API readiness, SQL cold start) over widening thresholds.

### Scenarios

| Scenario | Executor | VUs | Duration | Endpoint | Threshold |
| --- | --- | --- | --- | --- | --- |
| `health` | constant-vus | 5 | 20 s | `GET /health/live` (`k6ci:health_live`), `GET /health/ready` (`k6ci:health_ready`) | live p(95) < 500 ms; ready p(95) < 1500 ms (ready includes SQL / probes; a single combined tag was flaky vs 300 ms) |
| `create_run` | constant-vus | 2 | 30 s | `POST /v1/architecture/request` | p(95) < 3000 ms |
| `list_runs` | constant-vus | 3 | 20 s | `GET /v1/architecture/runs` | p(95) < 1500 ms |
| `audit_search` | constant-vus | 2 | 20 s | `GET /v1/audit/search?take=20` | p(95) < 1500 ms |

Global failure threshold: `http_req_failed` rate < 2 %. Total wall-clock duration: ~30 s (longest scenario).

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

Or with Docker (matches CI execution):

```bash
docker run --rm --network host \
  -v "$(pwd)/tests/load:/scripts:ro" \
  -e BASE_URL=http://127.0.0.1:5128 \
  grafana/k6:latest run /scripts/ci-smoke.js
```

## Pagination audit

`List*` / `Search*` `[HttpGet]` actions without obvious pagination parameters fail CI unless listed in `scripts/ci/list_endpoint_pagination_allowlist.txt`. Shrink the allowlist as endpoints gain `skip`/`take`/`limit`/query DTOs.

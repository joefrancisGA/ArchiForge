# Load test baseline (API hot paths)

## Objective

Record **repeatable** latency and throughput for the five highest-traffic API paths so horizontal scaling (Container Apps, read replicas, worker queue depth) is justified with numbers, not assumptions. Complement micro-benchmarks in `ArchLucid.Benchmarks` and cold-start guidance in `docs/PERFORMANCE_COLD_START_AND_TRIMMING.md`.

## Assumptions

- Load tests run against **Docker Compose `full-stack`** on a **dedicated** machine or GitHub Actions runner — not production or shared staging.
- The API uses **DevelopmentBypass** auth (Compose default); optional `API_KEY` is supported by `scripts/load/hotpaths.js` for keyed environments.
- First **numeric** baseline cells are filled by running `scripts/load/record_baseline.ps1` (Windows) or `scripts/load/record_baseline.sh` (Linux/macOS) with Docker, or the manual **Actions → Load test** workflow; see the table footnote below.

## Constraints

- **No public SMB or shared infra** for test data; Compose binds SQL/Redis/Azurite locally on the runner.
- CI load job is **manual only** (`.github/workflows/load-test.yml`) to avoid flaky PR gates and resource contention.
- k6 **checks** rate threshold is **0.85**; **`http_req_duration` p(95)** stays at **8000** ms until you paste the **Suggested** line from `scripts/ci/print_k6_summary_metrics.py` (2× measured p95, rounded up to 500 ms) into `scripts/load/hotpaths.js`.

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
| k6 script | `scripts/load/hotpaths.js` |
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
| Initial | 2026-04-13 | 5 | 2m | — | — | — | — | — |

**How to capture metrics (same profile as this row: VUs=5, DURATION=2m, SLEEP_SEC=1):**

1. Start Docker Desktop (or Linux engine), then from repo root run **`pwsh ./scripts/load/record_baseline.ps1`** or **`bash scripts/load/record_baseline.sh`** (Compose full-stack + k6 in `grafana/k6` + `print_k6_summary_metrics.py` + teardown).  
2. Paste **p50 / p95 / p99 / rate** into the table above and set **`git rev-parse HEAD`** (or the GitHub Actions run URL) in the last column.  
3. Update **`scripts/load/hotpaths.js`**: replace the `http_req_duration` threshold with the **Suggested** line printed by the Python script (2× observed p95, rounded up to 500 ms).  

**CI alternative:** **Actions → Load test (k6, Compose full-stack)** — align inputs to **VUs 5** and **duration 2m** for parity; copy the job-summary k6 table and artifact `k6-summary.json`.

> **Note:** The automation host that prepared this doc did not have a running Docker engine or k6; numeric cells remain **—** until someone runs the recorder above. The **checks** threshold in `hotpaths.js` is already **0.85**.

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

## Pagination audit

`List*` / `Search*` `[HttpGet]` actions without obvious pagination parameters fail CI unless listed in `scripts/ci/list_endpoint_pagination_allowlist.txt`. Shrink the allowlist as endpoints gain `skip`/`take`/`limit`/query DTOs.

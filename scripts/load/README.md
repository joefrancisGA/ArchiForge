# Load testing (k6)

Scripts target the five API hot paths documented in `docs/LOAD_TEST_BASELINE.md`.

## Prerequisites

- **Recommended:** Docker (for Compose + optional k6 container — no local k6 install).
- Or [k6](https://k6.io/docs/get-started/installation/) installed locally.
- API reachable at `BASE_URL` (default `http://127.0.0.1:5000`).
- For **Docker Compose full-stack** (`docker compose --profile full-stack up -d`), wait until `GET /health/live` returns 200 on the API port.

## Record baseline (Compose + k6 container + summary)

From repo root (fills `k6-summary.json`, prints metrics — see `docs/LOAD_TEST_BASELINE.md`):

```powershell
pwsh ./scripts/load/record_baseline.ps1
```

```bash
bash scripts/load/record_baseline.sh
```

## Run locally

```bash
export BASE_URL=http://127.0.0.1:5000
# Optional: export API_KEY=... when not using DevelopmentBypass
k6 run scripts/load/hotpaths.js
```

### Tune VUs and duration

```bash
VUS=10 DURATION=5m k6 run scripts/load/hotpaths.js
```

## CI

The workflow `.github/workflows/load-test.yml` runs on **manual** `workflow_dispatch` against Compose `full-stack` with fixed runner resources (see workflow). It uploads a summary snippet to the job log; copy p50/p95/p99 into `docs/LOAD_TEST_BASELINE.md` after each formal baseline run.

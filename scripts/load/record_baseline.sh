#!/usr/bin/env bash
# Record k6 hot-path baseline against Docker Compose full-stack (see docs/LOAD_TEST_BASELINE.md).
# From repo root: bash scripts/load/record_baseline.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

echo "Starting full-stack Compose..."
docker compose --profile full-stack up -d --build

for i in $(seq 1 60); do
  if curl -fsS "http://127.0.0.1:5000/health/live" >/dev/null; then
    echo "API healthy"
    break
  fi
  echo "waiting for API ($i/60)..."
  sleep 3
done

if ! curl -fsS "http://127.0.0.1:5000/health/live" >/dev/null; then
  echo "API did not become healthy in time" >&2
  exit 1
fi

echo "Running k6 in container..."
docker run --rm \
  --add-host=host.docker.internal:host-gateway \
  -v "$ROOT:/work" \
  -w /work \
  -e BASE_URL=http://host.docker.internal:5000 \
  -e VUS=5 \
  -e DURATION=2m \
  -e SLEEP_SEC=1 \
  -e K6_SUMMARY_TREND_STATS=med,p(95),p(99) \
  grafana/k6:latest run scripts/load/hotpaths.js --summary-export /work/k6-summary.json

python3 scripts/ci/print_k6_summary_metrics.py k6-summary.json

echo "Tearing down Compose..."
docker compose --profile full-stack down --remove-orphans

echo "Done. Update docs/LOAD_TEST_BASELINE.md and scripts/load/hotpaths.js using the printed metrics and suggested p(95) threshold."

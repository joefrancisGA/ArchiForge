#!/usr/bin/env bash
# Starts the ArchLucid Docker demo stack with demo seed + simulator agents.
# Prerequisites: Docker only.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_BASE="${REPO_ROOT}/docker-compose.yml"
COMPOSE_DEMO="${REPO_ROOT}/docker-compose.demo.yml"

if [[ ! -f "${COMPOSE_BASE}" ]]; then
  echo "Expected docker-compose.yml at ${COMPOSE_BASE}" >&2
  exit 1
fi

if [[ ! -f "${COMPOSE_DEMO}" ]]; then
  echo "Expected docker-compose.demo.yml at ${COMPOSE_DEMO}" >&2
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker does not appear to be running. Start Docker, then retry." >&2
  exit 1
fi

cd "${REPO_ROOT}"

echo "Building and starting ArchLucid demo stack (full-stack profile + demo overlay)..."
docker compose -f "${COMPOSE_BASE}" -f "${COMPOSE_DEMO}" --profile full-stack up -d --build

API_READY_URL="http://127.0.0.1:5000/health/ready"
end_ts=$(( $(date +%s) + 120 ))
ok=0

while (( $(date +%s) < end_ts )); do
  if curl -sf -o /dev/null --max-time 5 "${API_READY_URL}"; then
    ok=1
    break
  fi
  sleep 5
done

if [[ "${ok}" -ne 1 ]]; then
  echo ""
  echo "Timed out waiting for ${API_READY_URL} (120s)."
  echo "Check logs: docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack logs api"
  echo "Ensure ports 1433, 3000, 5000, 10000-10002, 6379 are free."
  exit 1
fi

echo "API is ready."
echo "Operator UI: http://localhost:3000/runs/new"

UI_URL="http://localhost:3000/runs/new"
if command -v xdg-open >/dev/null 2>&1; then
  xdg-open "${UI_URL}" >/dev/null 2>&1 || true
elif command -v open >/dev/null 2>&1; then
  open "${UI_URL}" >/dev/null 2>&1 || true
else
  echo "Open manually: ${UI_URL}"
fi

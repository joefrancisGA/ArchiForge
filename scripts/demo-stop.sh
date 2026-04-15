#!/usr/bin/env bash
# Stops the ArchLucid Docker demo stack and removes volumes.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_BASE="${REPO_ROOT}/docker-compose.yml"
COMPOSE_DEMO="${REPO_ROOT}/docker-compose.demo.yml"

cd "${REPO_ROOT}"

echo "Stopping ArchLucid demo stack and removing volumes..."
docker compose -f "${COMPOSE_BASE}" -f "${COMPOSE_DEMO}" --profile full-stack down -v

echo "Done."

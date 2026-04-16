#!/usr/bin/env bash
# Runs Terraform init/validate (default) across ArchLucid infra roots in dependency order.
# Usage:
#   ./scripts/provision-landing-zone.sh --dry-run
#   ./scripts/provision-landing-zone.sh --validate-only
# Env: run from repository root (script cd's to parent of scripts/).

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

DRY_RUN=0
VALIDATE_ONLY=1

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run) DRY_RUN=1; shift ;;
    --validate-only) VALIDATE_ONLY=1; shift ;;
    --plan) VALIDATE_ONLY=0; shift ;;
    *) echo "Unknown arg: $1" >&2; exit 2 ;;
  esac
done

ORDERED_ROOTS=(
  "infra/terraform-storage"
  "infra/terraform-private"
  "infra/terraform-container-apps"
  "infra/terraform-sql-failover"
  "infra/terraform-entra"
  "infra/terraform-openai"
  "infra/terraform-keyvault"
  "infra/terraform-monitoring"
  "infra/terraform-edge"
  "infra/terraform"
  "infra/terraform-servicebus"
  "infra/terraform-orchestrator"
)

for r in "${ORDERED_ROOTS[@]}"; do
  if [[ ! -d "$REPO_ROOT/$r" ]]; then
    echo "skip missing: $r" >&2
    continue
  fi
  echo "==> $r"
  pushd "$REPO_ROOT/$r" >/dev/null
  if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "  [dry-run] terraform init -backend=false && validate && fmt -check"
  else
    terraform init -backend=false
    terraform validate
    terraform fmt -check -recursive
  fi
  popd >/dev/null
done

echo "Done."

#!/usr/bin/env bash
# Points this repo at scripts/git-hooks (pre-push: OpenAPI snapshot gate).
# Run from repo root: bash scripts/git-hooks/install-git-hooks.sh

set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
git config core.hooksPath scripts/git-hooks
printf '%s\n' 'git core.hooksPath set to scripts/git-hooks (pre-push runs OpenAPI contract check when API-related paths change).'
printf '%s\n' 'Skip once: ARCHLUCID_SKIP_OPENAPI_PRE_PUSH=1 git push'
printf '%s\n' 'Always run on push: ARCHLUCID_OPENAPI_PRE_PUSH=all git push'

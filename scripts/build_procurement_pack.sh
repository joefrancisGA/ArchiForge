#!/usr/bin/env bash
set -euo pipefail
# Delegates to scripts/build_procurement_pack.py (canonical list + manifest + versions + redaction report).
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PYTHONUTF8=1
python3 "$ROOT/scripts/build_procurement_pack.py" "$@"

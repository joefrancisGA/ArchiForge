#!/usr/bin/env bash
# Post-deploy checks for GitHub CD (Azure Container Apps). Run from repo root after checkout.
# Usage: cd-post-deploy-verify.sh <base_url> [synthetic_path]
#
# Endpoints (anonymous where noted):
#   GET /health/live   — liveness (default ASP.NET writer)
#   GET /health/ready  — readiness summary JSON; must report top-level status "Healthy"
#   GET /openapi/v1.json — contract surface (must be HTTP 200 if exposed in this environment)
#   GET /version       — build identity (or synthetic_path)
#
# Environment (optional):
#   CD_POST_DEPLOY_MAX_ATTEMPTS — default 1; set e.g. 6 with CD_POST_DEPLOY_RETRY_WAIT_SECONDS=10 for cold-start retries
#   CD_POST_DEPLOY_RETRY_WAIT_SECONDS — seconds between attempts (default 10)

set -euo pipefail

usage() {
  echo "Usage: $0 <base_url> [synthetic_path]" >&2
  exit 2
}

if [ "${1:-}" = "-h" ] || [ "${1:-}" = "--help" ]; then
  usage
fi

if [ $# -lt 1 ] || [ -z "${1:-}" ]; then
  usage
fi

BASE="${1%/}"
SYN="${2:-/version}"
case "$SYN" in
  /*) ;;
  *) SYN="/$SYN" ;;
esac

MAX_ATTEMPTS="${CD_POST_DEPLOY_MAX_ATTEMPTS:-1}"
if ! [[ "$MAX_ATTEMPTS" =~ ^[0-9]+$ ]] || [ "$MAX_ATTEMPTS" -lt 1 ]; then
  MAX_ATTEMPTS=1
fi

RETRY_WAIT="${CD_POST_DEPLOY_RETRY_WAIT_SECONDS:-10}"
if ! [[ "$RETRY_WAIT" =~ ^[0-9]+$ ]]; then
  RETRY_WAIT=10
fi

annotate_error() {
  if [ -n "${GITHUB_ACTIONS:-}" ]; then
    echo "::error::$*"
  fi
  echo "POST-DEPLOY VALIDATION FAILED: $*" >&2
}

dump_body() {
  local file=$1
  local max=${2:-8192}
  if [ ! -s "$file" ]; then
    echo "(empty body)"
    return
  fi
  head -c "$max" "$file"
  echo ""
  local sz
  sz=$(wc -c < "$file" | tr -d ' ')
  if [ "$sz" -gt "$max" ]; then
    echo "... (truncated, total ${sz} bytes)"
  fi
}

# curl: write body to $2, return HTTP status code on stdout; non-zero exit on transport failure
http_get() {
  local url=$1
  local out=$2
  curl -sS -o "$out" -w "%{http_code}" --connect-timeout 20 --max-time 120 "$url"
}

run_one_attempt() {
  local tmp
  tmp=$(mktemp -d)
  # shellcheck disable=SC2064
  trap 'rm -rf "$tmp"' RETURN

  local code

  echo "======== Post-deploy validation (single pass) ========"
  echo "Base URL: $BASE"
  echo "Synthetic path: $SYN"
  echo ""

  echo "---- GET $BASE/health/live ----"
  code=$(http_get "$BASE/health/live" "$tmp/live.txt") || {
    annotate_error "curl failed for /health/live (connection/TLS timeout or reset)"
    return 1
  }
  echo "HTTP $code"
  if [ "$code" != "200" ]; then
    dump_body "$tmp/live.txt"
    annotate_error "/health/live expected HTTP 200, got $code"
    return 1
  fi
  dump_body "$tmp/live.txt" 512

  echo ""
  echo "---- GET $BASE/health/ready ----"
  code=$(http_get "$BASE/health/ready" "$tmp/ready.txt") || {
    annotate_error "curl failed for /health/ready"
    return 1
  }
  echo "HTTP $code"
  if [ "$code" != "200" ]; then
    dump_body "$tmp/ready.txt"
    annotate_error "/health/ready expected HTTP 200, got $code"
    return 1
  fi

  if ! command -v jq >/dev/null 2>&1; then
    annotate_error "jq is required on the runner to parse /health/ready JSON"
    return 1
  fi

  echo "Readiness JSON (summary):"
  jq -c . "$tmp/ready.txt" 2>/dev/null || dump_body "$tmp/ready.txt" 4096

  local overall
  overall=$(jq -r '.status // empty' "$tmp/ready.txt")
  if [ -z "$overall" ]; then
    dump_body "$tmp/ready.txt"
    annotate_error "/health/ready JSON missing top-level .status"
    return 1
  fi

  if [ "$overall" != "Healthy" ]; then
    echo "Per-check status:"
    jq -r '.entries[]? | "  - \(.name): \(.status)"' "$tmp/ready.txt" 2>/dev/null || true
    annotate_error "Readiness is not Healthy (overall status: $overall). Fix dependencies (e.g. SQL, blob) or see entries above."
    return 1
  fi

  echo ""
  echo "---- GET $BASE/openapi/v1.json ----"
  code=$(http_get "$BASE/openapi/v1.json" "$tmp/openapi.txt") || {
    annotate_error "curl failed for /openapi/v1.json"
    return 1
  }
  echo "HTTP $code"
  if [ "$code" != "200" ]; then
    dump_body "$tmp/openapi.txt" 2048
    annotate_error "/openapi/v1.json expected HTTP 200, got $code (if Production hides OpenAPI, map a Staging host or enable the document for this environment)"
    return 1
  fi
  echo "OpenAPI document present (first 200 chars):"
  head -c 200 "$tmp/openapi.txt"
  echo ""

  if ! jq -e '.info != null and (.info.title | type == "string") and (.info.title | length) > 0' "$tmp/openapi.txt" >/dev/null 2>&1; then
    dump_body "$tmp/openapi.txt" 2048
    annotate_error "/openapi/v1.json missing a non-empty .info.title (contract sanity for CD)"
    return 1
  fi

  echo ""
  echo "---- GET $BASE/version ----"
  code=$(http_get "$BASE/version" "$tmp/version.txt") || {
    annotate_error "curl failed for /version"
    return 1
  }
  echo "HTTP $code"
  if [ "$code" != "200" ]; then
    dump_body "$tmp/version.txt"
    annotate_error "/version expected HTTP 200, got $code"
    return 1
  fi
  echo "Version payload:"
  jq -c . "$tmp/version.txt" 2>/dev/null || dump_body "$tmp/version.txt" 2048

  if [ "$SYN" != "/version" ]; then
    echo ""
    echo "---- GET $BASE$SYN (SMOKE_SYNTHETIC_PATH) ----"
    code=$(http_get "$BASE$SYN" "$tmp/syn.txt") || {
      annotate_error "curl failed for $SYN"
      return 1
    }
    echo "HTTP $code"
    if [ "$code" != "200" ]; then
      dump_body "$tmp/syn.txt"
      annotate_error "Synthetic path $SYN expected HTTP 200, got $code"
      return 1
    fi
    dump_body "$tmp/syn.txt" 512
  else
    echo ""
    echo "---- SMOKE_SYNTHETIC_PATH is /version (already validated above) ----"
  fi

  echo ""
  echo "======== Post-deploy validation: all checks passed ========"
  return 0
}

attempt=1
while [ "$attempt" -le "$MAX_ATTEMPTS" ]; do
  echo ""
  echo ">>> Validation attempt $attempt of $MAX_ATTEMPTS <<<"
  if run_one_attempt; then
    exit 0
  fi

  if [ "$attempt" -ge "$MAX_ATTEMPTS" ]; then
    echo ""
    annotate_error "Exceeded $MAX_ATTEMPTS attempt(s). See logs above for HTTP codes and response excerpts."
    exit 1
  fi

  echo "Waiting ${RETRY_WAIT}s before retry..."
  sleep "$RETRY_WAIT"
  attempt=$((attempt + 1))
done

exit 1

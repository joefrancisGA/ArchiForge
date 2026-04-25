#!/usr/bin/env bash
# ArchLucid PR review gate — see README.md. Requires: curl, jq, mktemp.
# Environment (required unless noted):
#   ARCHLUCID_API_URL   — API base, e.g. https://api.example.com (no trailing slash)
#   ARCHLUCID_API_KEY   — X-Api-Key value
#   PR_TITLE            — title for the architecture request
#   PR_BODY             — description (min 10 chars) for the architecture request
#   PR_NUMBER           — pull request number (for the Markdown footer)
#   REPO_SLUG           — e.g. myorg/myrepo (for the Markdown footer)
# Optional:
#   ARCHLUCID_BLOCK_SEVERITY  — critical | high | medium | low | info (default: critical)
#   ARCHLUCID_SYSTEM_NAME     — systemName in request (default: PR review gate)
#   ARCHLUCID_ENVIRONMENT     — target environment string (default: pr-preview)
#   ARCHLUCID_MAX_WAIT_SEC    — poll budget (default: 1800)
#   ARCHLUCID_POST_COMMENT_CMD — if set, runs this shell string with $ARCHLUCID_COMMENT_FILE set to a file path
# Returns: 0 = pass (no findings at/above block severity), 1 = findings blocked, 2 = usage/API error

set -euo pipefail

if [[ -z "${ARCHLUCID_API_URL:-}" || -z "${ARCHLUCID_API_KEY:-}" ]]; then
  echo "archlucid-pr-gate: set ARCHLUCID_API_URL and ARCHLUCID_API_KEY" >&2
  exit 2
fi

if ((${#PR_TITLE} < 1)) || ((${#PR_BODY} < 10)); then
  echo "archlucid-pr-gate: set PR_TITLE and PR_BODY (body must be at least 10 characters)" >&2
  exit 2
fi

API_BASE="${ARCHLUCID_API_URL%/}"
BLOCK="${ARCHLUCID_BLOCK_SEVERITY:-critical}"
SYSNAME="${ARCHLUCID_SYSTEM_NAME:-PR review gate}"
ENVNAME="${ARCHLUCID_ENVIRONMENT:-pr-preview}"
MAX_WAIT="${ARCHLUCID_MAX_WAIT_SEC:-1800}"

sev_rank() {
  case "$(echo "$1" | tr '[:upper:]' '[:lower:]')" in
    critical) echo 4 ;;
    high) echo 3 ;;
    medium) echo 2 ;;
    low) echo 1 ;;
    info|"") echo 0 ;;
    *) echo 0 ;;
  esac
}
THRESHOLD="$(sev_rank "$BLOCK")"

REQ_JSON="$(jq -n \
  --arg rid "pr-gate-$(date -u +%Y%m%d%H%M%S)" \
  --arg desc "$PR_BODY" \
  --arg name "$SYSNAME" \
  --arg envn "$ENVNAME" \
  --arg title "$PR_TITLE" \
  '{
    requestId: $rid,
    description: ($title + " — " + $desc),
    systemName: $name,
    environment: $envn,
    cloudProvider: "Azure",
    constraints: ["Automated pull-request architecture review."],
    inlineRequirements: [($title)]
  }')"

CREATE_RES="$(mktemp)"
HTTP_CREATE="$(mktemp)"
curl -sS -o "$CREATE_RES" -w "%{http_code}" -X POST "$API_BASE/v1/architecture/request" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $ARCHLUCID_API_KEY" \
  -d "$REQ_JSON" > "$HTTP_CREATE" || true
CODE_CREATE="$(cat "$HTTP_CREATE")"
if [[ "$CODE_CREATE" != "201" && "$CODE_CREATE" != "200" ]]; then
  echo "archlucid-pr-gate: create run failed: HTTP $CODE_CREATE" >&2
  cat "$CREATE_RES" >&2
  rm -f "$CREATE_RES" "$HTTP_CREATE"
  exit 2
fi
RUN_ID="$(jq -r '.run.runId // empty' < "$CREATE_RES")"
if [[ -z "$RUN_ID" ]]; then
  echo "archlucid-pr-gate: no runId in create response" >&2
  cat "$CREATE_RES" >&2
  rm -f "$CREATE_RES" "$HTTP_CREATE"
  exit 2
fi
rm -f "$CREATE_RES" "$HTTP_CREATE"

EXEC_RES="$(mktemp)"
HTTP_EXEC="$(mktemp)"
curl -sS -o "$EXEC_RES" -w "%{http_code}" -X POST "$API_BASE/v1/architecture/run/$RUN_ID/execute" \
  -H "X-Api-Key: $ARCHLUCID_API_KEY" > "$HTTP_EXEC" || true
CODE_EXEC="$(cat "$HTTP_EXEC")"
if [[ "$CODE_EXEC" != "200" ]]; then
  echo "archlucid-pr-gate: execute failed: HTTP $CODE_EXEC" >&2
  cat "$EXEC_RES" >&2
  rm -f "$EXEC_RES" "$HTTP_EXEC"
  exit 2
fi
rm -f "$EXEC_RES" "$HTTP_EXEC"

START_TS="$(date +%s)"
DEADLINE=$((START_TS + MAX_WAIT))
RUN_JSON="$(mktemp)"
while :; do
  curl -sS -o "$RUN_JSON" -H "X-Api-Key: $ARCHLUCID_API_KEY" \
    "$API_BASE/v1/architecture/run/$RUN_ID" || true
  ST="$(jq -r '.run.status // empty' < "$RUN_JSON")"
  if [[ "$ST" == "4" || "$ST" == "5" || "$ST" == "6" || \
        "$ST" == "ReadyForCommit" || "$ST" == "Committed" || "$ST" == "Failed" ]]; then
    break
  fi
  NOW="$(date +%s)"
  if (( NOW >= DEADLINE )); then
    echo "archlucid-pr-gate: timeout after ${MAX_WAIT}s waiting for terminal run status" >&2
    rm -f "$RUN_JSON"
    exit 2
  fi
  sleep 3
done

ST="$(jq -r '.run.status // empty' < "$RUN_JSON")"
if [[ "$ST" == "6" || "$ST" == "Failed" ]]; then
  echo "archlucid-pr-gate: run $RUN_ID failed" >&2
  cat "$RUN_JSON" >&2
  rm -f "$RUN_JSON"
  exit 2
fi

# Count findings with severity at or above threshold
BLOCKED_COUNT="$(jq --argjson th "$THRESHOLD" '
  def rank(s):
    (s|tostring|ascii_downcase) as $l
    | if $l == "critical" then 4
      elif $l == "high" then 3
      elif $l == "medium" then 2
      elif $l == "low" then 1
      else 0 end;
  [ .results[]? | .findings[]? | select( rank(.severity // "info") >= $th ) ] | length
' < "$RUN_JSON")"
COMMENT_FILE="$(mktemp)"
{
  echo "### ArchLucid review — run \`$RUN_ID\`"
  if [[ "$BLOCKED_COUNT" -gt 0 ]]; then
    echo "Status: **BLOCKED** — at least one finding at or above **$BLOCK** severity."
  else
    echo "Status: **PASS** — no findings at or above **$BLOCK**."
  fi
  echo
  echo "Open run in the product UI or re-fetch via: \`GET $API_BASE/v1/architecture/run/$RUN_ID\`"
  echo
  if [[ -n "${PR_NUMBER:-}" && -n "${REPO_SLUG:-}" ]]; then
    echo "PR #$PR_NUMBER — $REPO_SLUG"
  fi
} > "$COMMENT_FILE"

if [[ -n "${ARCHLUCID_POST_COMMENT_CMD:-}" ]]; then
  export ARCHLUCID_COMMENT_FILE="$COMMENT_FILE"
  # shellcheck disable=SC2086
  eval "$ARCHLUCID_POST_COMMENT_CMD" || {
    echo "archlucid-pr-gate: ARCHLUCID_POST_COMMENT_CMD failed" >&2
    rm -f "$RUN_JSON" "$COMMENT_FILE"
    exit 2
  }
fi
rm -f "$RUN_JSON"

if [[ "$BLOCKED_COUNT" -gt 0 ]]; then
  rm -f "$COMMENT_FILE"
  exit 1
fi
rm -f "$COMMENT_FILE"
exit 0

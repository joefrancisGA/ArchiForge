#!/usr/bin/env bash
# Staging smoke: health, version, architecture happy path, authority manifest.
# Env: ARCHLUCID_BASE_URL or ARCHLUCID_API_BASE_URL; optional ARCHLUCID_API_KEY; STAGING_SMOKE_RESULTS_FILE.
set -euo pipefail

resolve_base() {
  if [[ -n "${ARCHLUCID_BASE_URL:-}" ]]; then
    echo "${ARCHLUCID_BASE_URL%/}"
    return
  fi
  if [[ -n "${ARCHLUCID_API_BASE_URL:-}" ]]; then
    echo "${ARCHLUCID_API_BASE_URL%/}"
    return
  fi
  echo "http://127.0.0.1:5000"
}

now_ms() {
  python3 -c 'import time; print(int(time.time() * 1000))'
}

BASE="$(resolve_base)"
OUT="${STAGING_SMOKE_RESULTS_FILE:-staging-smoke-results.json}"
BODY="$(mktemp)"
trap 'rm -f "$BODY"' EXIT

CURL=(curl -sS)
if [[ -n "${ARCHLUCID_API_KEY:-}" ]]; then
  CURL+=(-H "X-Api-Key: ${ARCHLUCID_API_KEY}")
fi

get() {
  local url="$1"
  "${CURL[@]}" -o "$BODY" -w "%{http_code}" "$url"
}

post_json() {
  local url="$1"
  local json="$2"
  "${CURL[@]}" -H "Content-Type: application/json" -d "$json" -o "$BODY" -w "%{http_code}" "$url"
}

fail() {
  echo "STAGING SMOKE FAIL: $*" >&2
  exit 1
}

t0="$(now_ms)"
code="$(get "${BASE}/health/live")"
ms_health_live=$(( $(now_ms) - t0 ))
[[ "$code" == "200" ]] || fail "/health/live HTTP $code $(cat "$BODY")"

t0="$(now_ms)"
code="$(get "${BASE}/health/ready")"
ms_health_ready=$(( $(now_ms) - t0 ))
[[ "$code" == "200" ]] || fail "/health/ready HTTP $code"

t0="$(now_ms)"
code="$(get "${BASE}/version")"
ms_version=$(( $(now_ms) - t0 ))
[[ "$code" == "200" ]] || fail "/version HTTP $code"
VERSION_JSON="$(cat "$BODY")"

REQ_ID="$(python3 -c 'import uuid; print(uuid.uuid4().hex)')"
CREATE_BODY="$(python3 - <<PY
import json
print(json.dumps({
    "requestId": "$REQ_ID",
    "systemName": "Staging Smoke",
    "description": "Automated staging smoke run.",
    "environment": "staging",
    "cloudProvider": "Azure",
    "constraints": ["smoke-test"],
    "requiredCapabilities": ["web", "sql"],
    "assumptions": ["staging-smoke.sh"],
}))
PY
)"

t0="$(now_ms)"
code="$(post_json "${BASE}/v1/architecture/request" "$CREATE_BODY")"
ms_create=$(( $(now_ms) - t0 ))
[[ "$code" == "200" || "$code" == "201" ]] || fail "POST request HTTP $code $(cat "$BODY")"
CREATE_RESP="$(cat "$BODY")"
RUN_ID="$(python3 -c "import json,sys; print(json.load(sys.stdin)['run']['runId'])" <<<"$CREATE_RESP")"
[[ -n "$RUN_ID" ]] || fail "missing runId"

post_json "${BASE}/v1/architecture/run/${RUN_ID}/execute" "{}" >/dev/null || true

t0="$(now_ms)"
DEADLINE=$(( $(now_ms) + 300000 ))
STATUS=""
while [[ $(now_ms) -lt $DEADLINE ]]; do
  code="$(get "${BASE}/v1/architecture/run/${RUN_ID}")"
  [[ "$code" == "200" ]] || fail "poll HTTP $code"
  DETAIL="$(cat "$BODY")"
  STATUS="$(python3 -c "import json,sys; print(json.load(sys.stdin)['run']['status'])" <<<"$DETAIL")"
  if [[ "$STATUS" == "ReadyForCommit" ]]; then
    break
  fi
  if [[ "$STATUS" == "Failed" ]]; then
    fail "run Failed"
  fi
  sleep 2
done
ms_poll=$(( $(now_ms) - t0 ))
[[ "$STATUS" == "ReadyForCommit" ]] || fail "timeout lastStatus=$STATUS"

t0="$(now_ms)"
code="$(post_json "${BASE}/v1/architecture/run/${RUN_ID}/commit" "{}")"
ms_commit=$(( $(now_ms) - t0 ))
[[ "$code" == "200" ]] || fail "commit HTTP $code $(cat "$BODY")"

t0="$(now_ms)"
code="$(get "${BASE}/v1/authority/runs/${RUN_ID}/manifest")"
ms_manifest=$(( $(now_ms) - t0 ))
[[ "$code" == "200" ]] || fail "manifest HTTP $code $(cat "$BODY")"

export SMOKE_OUT="$OUT" SMOKE_BASE="$BASE" SMOKE_RUN="$RUN_ID" SMOKE_REQ="$REQ_ID"
export SMOKE_VER_JSON="$VERSION_JSON" SMOKE_MAN_JSON="$(cat "$BODY")"
export SMOKE_MS_HEALTH_LIVE="$ms_health_live" SMOKE_MS_HEALTH_READY="$ms_health_ready"
export SMOKE_MS_VERSION="$ms_version" SMOKE_MS_CREATE="$ms_create" SMOKE_MS_POLL="$ms_poll"
export SMOKE_MS_COMMIT="$ms_commit" SMOKE_MS_MANIFEST="$ms_manifest"

python3 <<'PY'
import json, os

def parse_maybe(s):
    if not s or not str(s).strip():
        return None
    try:
        return json.loads(s)
    except json.JSONDecodeError:
        return s

out_path = os.environ["SMOKE_OUT"]
man_raw = os.environ["SMOKE_MAN_JSON"]
ver_raw = os.environ["SMOKE_VER_JSON"]
try:
    man_parsed = json.loads(man_raw)
    man_ok = True
except json.JSONDecodeError:
    man_parsed = None
    man_ok = False

payload = {
    "ok": True,
    "baseUrl": os.environ["SMOKE_BASE"],
    "runId": os.environ["SMOKE_RUN"],
    "requestId": os.environ["SMOKE_REQ"],
    "timingsMs": {
        "health_live": int(os.environ["SMOKE_MS_HEALTH_LIVE"]),
        "health_ready": int(os.environ["SMOKE_MS_HEALTH_READY"]),
        "version": int(os.environ["SMOKE_MS_VERSION"]),
        "create_run": int(os.environ["SMOKE_MS_CREATE"]),
        "poll_ready": int(os.environ["SMOKE_MS_POLL"]),
        "commit": int(os.environ["SMOKE_MS_COMMIT"]),
        "get_manifest": int(os.environ["SMOKE_MS_MANIFEST"]),
    },
    "version": parse_maybe(ver_raw),
    "manifest": {
        "jsonLength": len(man_raw),
        "parseOk": man_ok,
        "manifestId": str(man_parsed.get("manifestId")) if isinstance(man_parsed, dict) and man_parsed.get("manifestId") else None,
    },
}
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(payload, f, indent=2)
print(json.dumps(payload, indent=2))
PY

echo "STAGING SMOKE OK runId=$RUN_ID -> $OUT"

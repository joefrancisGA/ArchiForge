#!/usr/bin/env bash
# Validate X-ArchLucid-Webhook-Signature on raw POST body (same contract as
# templates/integrations/jira/jira-webhook-bridge-recipe.md).
# Usage:
#   export WEBHOOK_SHARED_SECRET='...'   # same as ArchLucid WebhookDelivery:HmacSha256SharedSecret
#   export EXPECTED_HEADER='sha256=...'  # value of X-ArchLucid-Webhook-Signature on the request
#   export RAW_BODY_FILE=/path/to/body.txt
# Exits 0 if valid, 1 if not.
set -euo pipefail
: "${WEBHOOK_SHARED_SECRET:?set WEBHOOK_SHARED_SECRET}"
: "${EXPECTED_HEADER:?set EXPECTED_HEADER to X-ArchLucid-Webhook-Signature}"
: "${RAW_BODY_FILE:?set RAW_BODY_FILE to a file with the exact request bytes}"
if [[ "$EXPECTED_HEADER" != sha256=* ]]; then
  echo "validate-inbound-hmac: header must start with sha256=" >&2
  exit 1
fi
HEX="${EXPECTED_HEADER#sha256=}"
# Compute HMAC-SHA256 over raw file (UTF-8 as received)
CALC="$(openssl dgst -sha256 -hmac "$WEBHOOK_SHARED_SECRET" -binary < "$RAW_BODY_FILE" | xxd -p -c 256 | tr -d '\n')"
if [[ "${HEX,,}" == "${CALC,,}" ]]; then
  exit 0
fi
echo "validate-inbound-hmac: HMAC mismatch" >&2
exit 1

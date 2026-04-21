#!/usr/bin/env bash
# Server-side pre-receive: reject pushes when gitleaks finds secrets in introduced commits.
# Install: see docs/security/GITLEAKS_PRE_RECEIVE.md
set -euo pipefail

if ! command -v gitleaks >/dev/null 2>&1; then
  echo "gitleaks not installed on Git server — refusing push (configure gitleaks or remove hook)." >&2
  exit 1
fi

top="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"

while read -r oldrev newrev refname; do
  if [[ "$newrev" == "0000000000000000000000000000000000000000" ]]; then
    continue
  fi

  if [[ "$oldrev" == "0000000000000000000000000000000000000000" ]]; then
    log_range="$newrev"
  else
    log_range="${oldrev}..${newrev}"
  fi

  if ! gitleaks git --verbose --log-opts="$log_range" "$top"; then
    echo "gitleaks: potential secrets in $refname ($log_range). See docs/security/GITLEAKS_PRE_RECEIVE.md" >&2
    exit 1
  fi
done

exit 0

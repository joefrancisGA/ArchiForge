#!/usr/bin/env bash
# CI guard: ensures the generated TypeScript API types match the current OpenAPI spec.
# Regenerates and diffs — fails if there are uncommitted changes.
set -euo pipefail

cd "$(dirname "$0")/../../archlucid-ui"

echo "Regenerating TypeScript API types from OpenAPI spec..."
npx openapi-typescript ../ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json \
  -o src/lib/api-types.generated.ts

if git diff --exit-code src/lib/api-types.generated.ts > /dev/null 2>&1; then
  echo "✅ api-types.generated.ts is in sync with the OpenAPI spec."
  exit 0
else
  echo "❌ api-types.generated.ts is out of sync. Run 'npm run generate:api-types' and commit."
  git diff src/lib/api-types.generated.ts | head -50
  exit 1
fi

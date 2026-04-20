> **Scope:** Runbook — Verifying a Day-1 Evidence Pack - full detail, tables, and links in the sections below.

# Runbook — Verifying a Day-1 Evidence Pack

> **Scope:** How an external auditor (or an internal reviewer) verifies a downloaded `evidence-pack.zip` produced by `GET /v1/support/evidence-pack.zip`. Companion to `docs/security/EVIDENCE_PACK.md`.
>
> **Status:** Draft (2026-04-20). Endpoint and daily Merkle job not yet implemented; this runbook is the contract the implementation must meet.

## You will need

- The downloaded `evidence-pack.zip`.
- Read access to the immutable blob container `audit-merkle` (or a delegated SAS URL).
- A machine with `unzip`, `sha256sum` (or `Get-FileHash` on PowerShell), and `jq`.

## Step 1 — verify the manifest

```bash
unzip -d ./pack ./evidence-pack.zip
cd ./pack

# MANIFEST.txt has one row per file: <relpath>  <bytes>  <sha256>
while IFS=$'\t' read -r relpath bytes sha; do
  actual=$(sha256sum "$relpath" | awk '{print $1}')
  if [ "$actual" != "$sha" ]; then
    echo "MISMATCH: $relpath claims $sha, computed $actual"
  fi
done < MANIFEST.txt
```

If any line prints `MISMATCH`, **stop**. The pack has been tampered with after generation. Do not trust any other claim in it; escalate per `docs/security/SYSTEM_THREAT_MODEL.md`.

## Step 2 — verify a daily Merkle root against the immutable blob

For each entry in `audit-merkle.json`:

```bash
day=2026-04-19
expected=$(jq -r --arg d "$day" '.[] | select(.day==$d) | .rootSha256' audit-merkle.json)

# Fetch the same day's root from the immutable blob:
az storage blob download \
  --account-name <archlucid-shared-storage> \
  --container-name audit-merkle \
  --name "${day}.json" \
  --file "/tmp/${day}.json"

immutable=$(jq -r '.rootSha256' "/tmp/${day}.json")

if [ "$expected" != "$immutable" ]; then
  echo "MISMATCH for $day: pack=$expected immutable=$immutable"
else
  echo "OK for $day"
fi
```

Two stores agreeing (the pack you were given, and the immutable container) is necessary but not sufficient — proceed to Step 3 for full assurance.

## Step 3 — independently recompute the root from `audit-export.jsonl`

```bash
# audit-export.jsonl has one canonical JSON object per line.
# Filter to the day, hash each row, build the Merkle tree.
day=2026-04-19
jq -c --arg d "$day" 'select(.OccurredUtc | startswith($d))' audit-export.jsonl \
  | sha256sum -                              # NOTE: this is the leaves hash; full tree code lives in
                                             #       ArchLucid.Worker.AuditMerkleDailyHostedService.
                                             #       Use the published 'archlucid audit verify-merkle --day <d>'
                                             #       CLI command (planned) for the full tree calculation.
```

The full tree calculation matches the contract in `docs/security/EVIDENCE_PACK.md` § "Daily Merkle root" (canonical row → SHA-256 leaf → balanced tree → root, duplicate the last leaf if the count is odd).

If all three values agree (pack, immutable blob, your independent computation from the audit export), the audit chain for that day is intact.

## Common failure modes

| Symptom | Likely cause | Action |
|---|---|---|
| `MANIFEST.txt` mismatch on `audit-export.jsonl` | Pack was modified in transit (unlikely with HTTPS) or by a downstream process | Re-fetch directly from `/v1/support/evidence-pack.zip`; do not trust the modified pack. |
| Pack root and immutable root agree, but independent recomputation disagrees | Audit export was filtered or truncated; OR the row canonicalisation in the verifier differs from the producer | Use the bundled CLI (`archlucid audit verify-merkle`) which shares the canonicalisation code. |
| Immutable blob missing for a day | Daily job failed and did not back-fill | Pack will mark `"immutableBlobMissing": true` for that day; chain is intact via SQL `dbo.AuditMerkleRoots` only — note in the audit report. |

## Related

- `docs/security/EVIDENCE_PACK.md`
- `docs/AUDIT_COVERAGE_MATRIX.md`
- `ArchLucid.Persistence/Migrations/051_AuditEvents_DenyUpdateDelete.sql`

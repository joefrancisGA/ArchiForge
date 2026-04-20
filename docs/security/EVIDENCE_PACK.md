> **Scope:** Day-1 Evidence Pack - full detail, tables, and links in the sections below.

# Day-1 Evidence Pack

> **Scope:** Defines a single auditor/admin-gated artefact (`/v1/support/evidence-pack.zip`) that bundles the documents a governance reviewer typically asks for in the first hour of a security or audit conversation. Includes a daily Merkle-root scheme over `dbo.AuditEvents` so the audit chain is tamper-evident even before the immutable-storage policy on the events table itself is invoked.
>
> **Status:** Specification draft (2026-04-20). The endpoint and hosted service are not yet implemented; this document is the contract that the implementation must satisfy.

## Why this exists

Today an operator who wants to satisfy a "show me your evidence" request must:

1. Run the audit export (`GET /v1/audit/export`).
2. Manually screenshot RLS settings.
3. Manually grab the content-safety configuration.
4. Manually compute SLO numbers from Prometheus.
5. Manually list installed policy packs and their versions.

Each step is its own runbook. The evidence pack collapses that into one signed artefact.

## Endpoint

```
GET /v1/support/evidence-pack.zip
Authorization: Bearer <jwt>            # role Auditor or Admin
Accept:        application/zip
```

Response: `200 application/zip` streaming the bundle. **403** for any role other than Auditor or Admin. **401** when unauthenticated.

The endpoint is **always audited**: a `SupportEvidencePackGenerated` event is appended to `dbo.AuditEvents` with `principalId`, `correlationId`, and a `sha256` of the produced ZIP.

## Bundle contents

| File | Source | Notes |
|---|---|---|
| `MANIFEST.txt` | computed | One row per file: relative path, size, sha256. The pack is signed by signing this file (next iteration). |
| `audit-export.jsonl` | existing audit-export pipeline | Same content as `GET /v1/audit/export`, scoped to the calling principal's tenant. |
| `rls-posture.json` | `SqlRowLevelSecurity*` configuration + Prometheus current value of `archlucid_rls_bypass_enabled_info` | **Boolean flags only — never values of secrets.** |
| `content-safety.json` | `ArchLucid:ContentSafety:*` configuration | Endpoint host (no key), threshold, fail-closed flag. |
| `policy-packs.json` | `dbo.PolicyPackAssignments` + `dbo.PolicyPackVersions` | Pack name, version, sha256 of serialised pack content, assignment scope. |
| `slo-30day.json` | Prometheus query against the 9 metrics named in `docs/API_SLOS.md` | Last-30-day burn rate per SLO. |
| `audit-merkle.json` | `dbo.AuditMerkleRoots` (last 30 rows) | See "Daily Merkle root" below. |

## Daily Merkle root

A new background hosted service (`AuditMerkleDailyHostedService`, in `ArchLucid.Worker`) runs at 00:30 UTC and:

1. Queries `dbo.AuditEvents` for rows whose `OccurredUtc` falls in the previous UTC day.
2. Canonicalises each row deterministically (sorted JSON keys, `OccurredUtc` in `O` format, `null` rather than missing keys).
3. Computes `sha256(rowCanonical)` per row → leaves of a balanced binary tree (duplicate the last leaf if odd count).
4. Computes the Merkle root.
5. Inserts `(Day, RowCount, RootSha256)` into `dbo.AuditMerkleRoots` (new table, migration `100_AuditMerkleRoots.sql`).
6. Uploads `{day}.json` containing `{ day, rowCount, rootSha256, computedAtUtc }` to the **immutable** blob container `audit-merkle` (Azure Storage `BlobImmutabilityPolicy` configured at the container level).

The pack embeds the last 30 of those rows in `audit-merkle.json`. An auditor can then:

- Download the pack.
- Independently fetch any of the referenced JSON blobs from the immutable container.
- Re-canonicalise the same day's audit rows from the audit export and recompute the root.
- Compare to the value embedded in the pack and the value in the immutable blob.

**Three independent stores** must agree (SQL row, immutable blob, the value embedded in a previously-shipped pack) for the chain to be considered intact.

## Failure modes and behaviour

| Failure | Behaviour |
|---|---|
| Prometheus unreachable | `slo-30day.json` written with `{ "status": "unavailable", "reason": "prometheus query failed", "queriedAtUtc": "..." }`. Pack still produced (do not fail closed for one optional source). |
| Immutable blob upload fails on the daily job | Job retries with exponential backoff up to 6 hours; alert fires (`ArchLucidAuditMerkleUploadFailed`); next pack still embeds the SQL-side root with `"immutableBlobMissing": true`. |
| Audit export size > 1 GB | Pack truncates to the last 30 days and includes `"truncated": true` in `MANIFEST.txt`. |
| Caller's principal has no tenant claim | `400 ProblemDetails`; do not produce a pack with cross-tenant data. |

## Tests (must exist before merge)

- `ArchLucid.Api.Tests` — HTTP tests covering 401 (anon), 403 (Reader, Operator), 200 (Auditor, Admin) on the endpoint.
- `ArchLucid.Persistence.Tests` — integration test that inserts known audit rows and verifies the Merkle root is reproducible.
- `ArchLucid.Worker.Tests` — unit tests for leaf hashing, balanced-tree construction, and the odd-leaf duplication rule.
- A pack-content lint that fails if any pre-listed secret-name pattern (`*Key`, `*Secret`, `ConnectionString`) appears in any file in the pack.

## Companion runbook

Operators verifying a downloaded pack: see `docs/runbooks/EVIDENCE_PACK_OPS.md`.

## Open questions for the user (block implementation)

1. Confirm the immutable blob container will live in the existing `archlucid-shared` storage account or in its own.
2. Confirm the policy-pack content sha256 is computed over the **canonical JSON** form (preferred) versus the as-stored bytes.
3. Confirm Prometheus is the canonical SLO source (vs. Application Insights KQL).

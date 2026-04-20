> **Scope:** GitHub Action — manifest delta PR check (ArchLucid) - full detail, tables, and links in the sections below.

# GitHub Action — manifest delta PR check

**Audience:** Platform engineers wiring ArchLucid into GitHub pull-request review.

**Purpose:** Surface **`GET /v1/compare`** (structured golden-manifest delta between two **committed** runs) in the Actions job summary so reviewers see added/removed/changed counts without opening the operator UI first.

**Action path:** [`integrations/github-action-manifest-delta/`](../../integrations/github-action-manifest-delta/) (composite action).

---

## Prerequisites

- Both runs must exist in the **same tenant scope** as the API key and must already have **golden manifests** (committed). Otherwise the API returns **404** — see [`docs/API_CONTRACTS.md`](../API_CONTRACTS.md) and [`docs/COMPARISON_REPLAY.md`](../COMPARISON_REPLAY.md).
- API key must satisfy **ReadAuthority** (same header as other automation): **`X-Api-Key`**.
- Respect **rate limiting** on `/v1/*` (`429` with backoff). The script performs a **single** GET; heavy matrices should use a dedicated workflow, not per-commit fan-out.

---

## Secrets

Store the API key as a GitHub Actions **secret** (e.g. `ARCHLUCID_READONLY_API_KEY`). **Never** commit keys to the repository. Rotate keys if a workflow log ever captured one.

---

## Example (copy-paste)

See **[`.github/workflows/example-manifest-delta.yml`](../../.github/workflows/example-manifest-delta.yml)** — `workflow_dispatch` only, with inputs for base/target run ids.

---

## Operator deep link

If your hosted operator UI supports a compare route, pass **`operator-compare-url-template`** so the summary includes a Markdown link. Use literal placeholders **`{baseRunId}`** and **`{targetRunId}`** in the template string.

---

## Related

- [`docs/API_CONTRACTS.md`](../API_CONTRACTS.md) — versioning and correlation.
- [`docs/operator-shell.md`](../operator-shell.md) — operator compare workflow.

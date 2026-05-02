> **Scope:** Rolling-window procedure for interpreting scheduled **hosted-saas-probe** workflow results — **no buyer-facing availability % without real history**.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).

# Hosted availability — 30-day rollup (operator runbook)

**Audience:** Platform / SRE assembling procurement- or Trust-Center-adjacent **reliability narrative** from scheduled HTTP probes.

## Objective

Summarize **whether** the public health endpoints (`/health/live`, `/health/ready`) for the configured staging base URL succeeded on each scheduled run, over a chosen window (e.g. **30 days**), **without** implying a production SLA unless that environment is explicitly in scope.

## Inputs

- GitHub Actions workflow **[`.github/workflows/hosted-saas-probe.yml`](../../.github/workflows/hosted-saas-probe.yml)** (cron + `workflow_dispatch`).
- Repository variable **`ARCHLUCID_STAGING_BASE_URL`**. When unset, the workflow exits cleanly and records a **skipped** probe artifact row.
- Per-successful-probe artifact **`probe-out/probe-result.json`** (UTC timestamp + base URL + endpoint results) uploaded from the workflow when probing runs.

## Procedure

1. In the Actions tab, filter **hosted-saas-probe** to the last **30** completed runs (or your reporting window).
2. Download artifacts for runs that **uploaded** `probe-result.json` (skipped runs still produce a small JSON with `"skipped": true` when implemented).
3. Count: **attempted** probes vs **both endpoints OK** vs **skipped** (no base URL).
4. Record in internal release notes or procurement Q&A: **method** (scheduled curl, staging-only), **window dates**, **counts** — not a polished **%** unless your legal/comms policy allows that wording for **staging**.

## Constraints

- **Do not** publish **“99.x% availability”** to buyers from this probe alone: it is **not** production monitoring, not multi-region, and not user-traffic SLO-backed.
- For buyer-facing language, pair with [TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md) posture and any **separate** production telemetry your organization approves.

## Optional automation

Use **`scripts/ops/summarize_hosted_probe_artifacts.py`** to merge multiple downloaded `probe-result.json` files into a printed summary (stdin list of paths or a directory).

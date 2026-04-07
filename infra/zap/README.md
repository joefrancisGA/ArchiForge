# OWASP ZAP baseline (tiered)

## Tiers

| Tier | Workflow | Behavior |
|------|-----------|----------|
| **PR** | `.github/workflows/ci.yml` → `security-zap-api-baseline` | `zap-baseline.py` with **`-I`** (do not fail the build on warnings) and **`-c config/baseline-pr.tsv`** to downgrade known low-value findings for a headless JSON API behind a reverse proxy. |
| **Scheduled** | `.github/workflows/zap-baseline-strict-scheduled.yml` | Same spider profile **without** **`-I`**: unresolved WARN/FAIL findings fail the step. The job uses **`continue-on-error: true`** so the repository is not blocked; operators review the failing run and tighten `baseline-pr.tsv` or fix the API. |

## Layout

- `baseline-pr.tsv` — tab-separated rule overrides (`RULE_ID`, `IGNORE` \| `INFO` \| `FAIL`, description). Mounted read-only into the ZAP container as `/zap/wrk/config/baseline-pr.tsv`.
- CI mounts a **writable** host directory at `/zap/wrk` (see workflows) so `zap-baseline.py` can create `zap.yaml` there; the official image often runs as a non-root user that cannot write the image’s default `/zap/wrk`.

## Operations

- If the scheduled run turns red, open the job log for the ZAP stdout summary, then either fix the finding or add a deliberate `IGNORE` with a short justification in `baseline-pr.tsv`.
- Do not mount SMB (445) or expose internal admin URLs to ZAP; the CI job targets only the local Docker network.

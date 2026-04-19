# OWASP ZAP baseline

## Tiers

| Tier | Workflow | Behavior |
|------|-----------|----------|
| **PR (merge gate)** | `.github/workflows/ci.yml` → `security-zap-api-baseline` | `zap-baseline.py` **without** **`-I`** and **`-c config/baseline-pr.tsv`**: **WARN and FAIL** both fail the job. |
| **Scheduled** | `.github/workflows/zap-baseline-strict-scheduled.yml` | Same command and config — second line of defense for drift; **no** `continue-on-error`. |

Rule format and triage: **[docs/security/ZAP_BASELINE_RULES.md](../docs/security/ZAP_BASELINE_RULES.md)**.

## Layout

- `baseline-pr.tsv` — tab-separated rule overrides (`RULE_ID`, `IGNORE` \| `INFO` \| `FAIL`, description). Mounted read-only into the ZAP container as `/zap/wrk/config/baseline-pr.tsv`.
- CI mounts a **writable** host directory at `/zap/wrk` (see workflows) so `zap-baseline.py` can create `zap.yaml` there; the official image often runs as a non-root user that cannot write the image’s default `/zap/wrk`. Workflows run `chmod -R a+rwx` on that host path because GitHub’s `runner` user (typical uid 1001) owns `RUNNER_TEMP` while the ZAP image’s `zap` user is often uid **1000**, which would otherwise get **Permission denied** on `zap.yaml`.

## Operations

- If ZAP fails in CI or on the schedule, open the job log for the summary (`FAIL-NEW`, `WARN-NEW`), then either fix the finding or add a deliberate `IGNORE` with a short justification in `baseline-pr.tsv` (see **ZAP_BASELINE_RULES.md**).
- Do not mount SMB (445) or expose internal admin URLs to ZAP; the CI job targets only the local Docker network.

## API container must listen before ZAP

The workflows run **`ArchLucid.Api`** from the **Release** Docker image with **`ASPNETCORE_ENVIRONMENT=Development`**. The API must reach **`/health/live`** on **127.0.0.1:8080** within the wait loop.

**CI env overrides (important):** jobs pass **`ArchLucid__StorageProvider=InMemory`**, **`ArchLucidAuth__Mode=DevelopmentBypass`**, **`IntegrationEvents__TransactionalOutboxEnabled=false`**, and **`Demo__SeedOnStartup=false`** so startup never depends on SQL on **`localhost`** inside the container (the base **`appsettings.json`** defaults to **Sql**). Without those overrides, DbUp / SQL can block or fail and **`curl`** will see **connection refused** until the job times out.

If the health check still fails, the workflow prints **`docker logs`** (last 200 lines) before exiting — use that output for root cause.

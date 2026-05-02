> **Scope:** Tier-1 support runbook (10-minute first response) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Tier-1 support runbook (10-minute first response)

## Objective

Resolve or **triage** common ArchLucid issues without data mutation, using only **read** APIs, **health** probes, and **support bundle** collection.

## Assumptions

- Caller has an **operator URL**, **correlation id** from a failing request, and either **JWT** or **API key** (per `ArchLucidAuth`).
- SQL connectivity is allowed from the operator’s network path for **`GET /health/ready`**.

## Constraints

- **Do not DELETE** rows in **production** from tier-1.
- **Do not** paste **secrets** into tickets — use [`archlucid support-bundle`](../../README.md) output **after** redaction (Bearer, `X-Api-Key`, connection secrets stripped in bundle writer).

## Architecture overview

**Client** → **ArchLucid.Api** → **SQL** / **blob** / **optional Service Bus** — see [../ARCHITECTURE_CONTAINERS.md](../library/ARCHITECTURE_CONTAINERS.md).

## Component breakdown — ordered checks

| # | Command / request | Expected | If failing |
|---|-------------------|----------|------------|
| 1 | `curl -sS <base>/health/live` | **200** healthy body | Network / TLS / wrong base URL |
| 2 | `curl -sS <base>/health/ready` | **200** when SQL configured | DbUp/migrations, SQL down, bad connection string |
| 3 | `curl -sS <base>/version` | JSON with version | Host not updated / wrong slot |
| 4 | `dotnet run --project ArchLucid.Cli -- doctor` | Exits **0**, prints versions | Local machine/toolchain |
| 5 | `dotnet run --project ArchLucid.Cli -- trace <runId>` | Prints trace id when SQL row exists | Missing run / wrong tenant scope |
| 6 | `GET /v1/explain/runs/{runId}/aggregate` with auth | **200** or **404** | **401/403** authZ — verify roles/policies |
| 7 | `archlucid support-bundle --zip` | Zip created | See CLI stderr; review zip locally |
| 8 | Grafana — import [GRAFANA_DASHBOARD_TIER_1.json](GRAFANA_DASHBOARD_TIER_1.json) | Panels populate | Prometheus scrape / MSI |
| 9 | Compare `X-Correlation-ID` to API logs | Match | Lost correlation middleware |
|10 | Open **SQL** `Runs.OtelTraceId` for run | Matches distributed trace | Sampling gap — see [../OBSERVABILITY.md](../library/OBSERVABILITY.md) |

## Symptom rescue matrix

| Symptom | First check | Evidence to collect | Escalate when |
|---------|-------------|---------------------|---------------|
| Cannot sign in | `GET /api/auth/me` in browser devtools or proxy logs | User email/domain, auth mode, HTTP status, correlation id | Repeated **401/403** after confirmed Entra/API-key setup |
| `/health/ready` fails | `curl -sS <base>/health/ready` | Full readiness JSON, dependency name, `/version` output | SQL, schema, Key Vault, or storage dependency is **Unhealthy** for more than one retry window |
| Run stuck before commit | `dotnet run --project ArchLucid.Cli -- status <runId>` | run id, status, pipeline timeline, correlation id from last execute call | Same stage remains in-progress or failed after one re-execute attempt |
| Commit blocked | Capture commit response body | ProblemDetails JSON, blocking finding ids, governance config state | Gate blocks unexpectedly for a pilot where pre-commit governance should be off |
| No artifacts after commit | `GET /v1/artifacts/manifests/{manifestId}` | manifest id, run id, commit response, artifact list response | Commit succeeded but artifact list remains empty after refresh |
| Finding looks wrong | `GET /v1/architecture/run/{runId}/agent-evaluation` | finding id, severity, evidence-chain pointers, structural/semantic scores | Evidence chain missing or semantic score is below release threshold |
| Export fails | Retry the exact export URL once | export URL, status code, content type, correlation id, run id | DOCX/PDF/ZIP corrupt or repeated 5xx |
| Audit row missing | `GET /v1/audit/search?runId=<runId>` where available | run id, actor, action time, expected event type | Mutating operation succeeded but durable audit is absent |
| Trace link not found | `dotnet run --project ArchLucid.Cli -- trace <runId>` | `Runs.OtelTraceId`, current `X-Trace-Id`, sampling settings | Trace id exists but backend has no matching trace and sampling should retain it |
| Webhook or Teams delivery failed | Check alert/digest delivery audit rows | event type, delivery target host, status code, correlation id | Repeated delivery failure or HMAC/signature mismatch |
| Trial signup failed | Review registration ProblemDetails and trial audit rows | email domain, slug, stage/reason, correlation id | Duplicate/conflict resolved but signup still fails |

## Data flow (failing run)

UI error → capture **correlation id** → **`/health/ready`** → **`/version`** → **`trace <runId>`** → **`DataConsistency`** metrics if suspicion of orphan rows → escalate with **support bundle**.

## Security model

Read-only tier-1 uses **ReadAuthority** endpoints where applicable; **`support-bundle`** is generated **client-side** from the operator workstation — treat archive as **confidential**.

## Operational considerations

- **Escalation** when **`archlucid_data_consistency_orphans_detected_total` > 0** sustained — see [../data-consistency/DATA_CONSISTENCY_ENFORCEMENT.md](../data-consistency/DATA_CONSISTENCY_ENFORCEMENT.md).
- **Dashboard provisioning:** import `GRAFANA_DASHBOARD_TIER_1.json` via Grafana **Import** UI; complements Terraform-managed dashboards under `infra/terraform-monitoring/grafana_dashboards.tf` (do not duplicate UID collisions).

## Related

- [../TROUBLESHOOTING.md](../TROUBLESHOOTING.md)
- [../PILOT_GUIDE.md](../library/PILOT_GUIDE.md) (what to attach when reporting issues)

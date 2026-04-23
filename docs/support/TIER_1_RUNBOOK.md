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

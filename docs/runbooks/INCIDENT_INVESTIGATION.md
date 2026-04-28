> **Scope:** Runbook for investigating production incidents on the ArchLucid hosted SaaS (health, dependencies, telemetry, SQL, worker); not a substitute for customer-specific IR plans or Azure-wide DR runbooks.

> **Spine doc:** [FIRST_5_DOCS.md](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# Incident investigation — hosted SaaS

**Audience:** Platform / on-call responding to **staging** or **production** degradation. Assumes Azure-hosted API (Container Apps), SQL, observability per [`infra/terraform-monitoring/README.md`](../../infra/terraform-monitoring/README.md).

---

## Severity (initial classification)

| Severity | Definition |
|----------|------------|
| **P1** | **`GET /health/live`** failing broadly, **all tenants** affected, or **credible data loss** / inability to recover writes. |
| **P2** | **Single tenant** or subset impaired; commits / governance **blocked** for production path; **sustained** 5xx for primary workflows. |
| **P3** | **Non-critical** path (advisory, digests, background enrichment) degraded; elevated errors with **workaround**. |
| **P4** | Cosmetic, doc-only, or **no customer impact** (monitoring noise, flake). |

Re-triage after 15 minutes if scope was misjudged.

---

## First five minutes

1. **Public health** — `GET {origin}/health/live` then **`/health/ready`**. Parse `entries[]` for first **Unhealthy** / **Degraded** (see [OBSERVABILITY.md](../library/OBSERVABILITY.md), [TROUBLESHOOTING.md](../TROUBLESHOOTING.md)).
2. **Build identity** — `GET {origin}/version` (correlate to the revision you rolled).
3. **Azure Container Apps** — Portal: **Revision state**, **Replicas**, recent **Events**; confirm traffic targets healthy revision.
4. **Application Insights** — **Failures** (last 15 min): exception type, volume, sample **`operation_Id`** / **request** correlation. Telemetry provisioned from [`application_insights.tf`](../../infra/terraform-monitoring/application_insights.tf) in the monitoring stack.
5. **SQL** — Portal: **DTU/CPU**, **blocking**, **deadlocks**, **connection failures**; confirm firewall/private endpoint path matches runtime.
6. **Synthetic probes** — [`.github/workflows/hosted-saas-probe.yml`](../../.github/workflows/hosted-saas-probe.yml) (and [`api-synthetic-probe.yml`](../../.github/workflows/api-synthetic-probe.yml)): did scheduled runs fail? Compare failure time to incident start.
7. **Redis / cache** (if enabled for your stack) — [REDIS_HEALTH.md](REDIS_HEALTH.md).

---

## Investigation paths

- **`/health/live` OK, `/health/ready` not healthy**  
  - Read each failing check name in JSON. **SQL** → verify connectivity string, **DbUp** migrations applied, pool exhaustion. **Disk/temp** → Container Apps ephemeral storage / mount. See [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) first-line steps.  
  - **Circuit breakers** (OpenAI) → [OBSERVABILITY.md](../library/OBSERVABILITY.md) § health + **`AzureOpenAI` / resilience** in [RESILIENCE_CONFIGURATION.md](../library/RESILIENCE_CONFIGURATION.md).

- **5xx spike, health still green**  
  - Application Insights **Failures** → group by **exception type** and **cloud_RoleName**. Pull **`X-Correlation-ID`** from support report → search **Traces/Requests** for that ID.  
  - Check **dependencies** slice (SQL, HTTP) for downstream saturation or timeouts.

- **Tenant reports stale data / missing side effects**  
  - **Worker / outbox** — Log Analytics **traces** for worker revision; search **outbox** / integration publish logs; confirm Service Bus / handler health if used in your deployment profile.

- **Governance workflow stuck**  
  - SQL: **`dbo.GovernanceApprovalRequests`** — rows **pending** unusually long vs SLA; compare **`CreatedUtc`** to now. **Webhooks** — delivery failures in app logs or integration telemetry.

- **Audit gaps (“event missing in UI / export”)**  
  - **Dual path:** durable **`dbo.AuditEvents`** vs baseline mutation log — confirm whether failure is **ingest**, **RLS scope**, or **search** (keyset / filters). Reference [AUDIT_COVERAGE_MATRIX.md](../library/AUDIT_COVERAGE_MATRIX.md) known gaps.

- **Burn-rate / SLO**  
  - [SLO_PROMETHEUS_GRAFANA.md](SLO_PROMETHEUS_GRAFANA.md); recording rules under [`infra/prometheus/archlucid-slo-rules.yml`](../../infra/prometheus/archlucid-slo-rules.yml). **Grafana:** Managed Grafana optional path in [`infra/terraform-monitoring/README.md`](../../infra/terraform-monitoring/README.md).

---

## Escalation

- **P1 / P2:** Page **owner / platform lead** on the channel your team uses for production (email/Teams/phone). Include: time window, **correlation IDs**, revision / commit, customer scope.  
- **P3 / P4:** Ticket in tracker; fix in next **business-hours** window unless trending toward P2.

---

## Post-incident

1. **Short report:** timeline (UTC), impact (tenants/routes), root cause (or best hypothesis), **customer comms** if any, remediation, **prevention** (runbook/code/monitor).  
2. **Update this runbook** if a new failure mode is repeatable.  
3. If **SEV-1**, schedule **blameless review** per team practice.

---

## Related docs

| Topic | Doc |
|-------|-----|
| Operator triage | [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) |
| Health / circuit breakers | [OBSERVABILITY.md](../library/OBSERVABILITY.md), [V1_SCOPE.md](../library/V1_SCOPE.md) (health surface) |
| Redis | [REDIS_HEALTH.md](REDIS_HEALTH.md) |
| SLO / burn | [SLO_PROMETHEUS_GRAFANA.md](SLO_PROMETHEUS_GRAFANA.md) |
| Deploy order / SaaS | [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md) |
| Monitoring Terraform | [`infra/terraform-monitoring/README.md`](../../infra/terraform-monitoring/README.md) |

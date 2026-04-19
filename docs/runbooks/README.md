# Runbooks index

**Last reviewed:** 2026-04-19

Operational guides for ArchLucid operators. Each runbook is self-contained; cross-links point to deeper design docs where useful.

**Availability policy:** [RTO / RPO targets by tier](../RTO_RPO_TARGETS.md) — development vs staging vs production (SQL geo-replication, RPO/RTO examples).

## Priority tags (convention)

| Tag | Meaning |
|-----|---------|
| **P1 — Critical** | Production incident, data integrity, **security rotation**, or **DR / failover** paths that must be executable under pressure. |
| **P2 — Important** | Recurring triage, degraded features, data hygiene, or observability workflows that restore normal operations. |
| **P3 — Reference** | Drills, load-test quirks, developer-oriented infra, or **deferred** one-off procedures (still version-controlled). |

Tags are **guidance for paging and training**; they do not replace your org’s own severity scale.

| Priority | Runbook | When to use |
|----------|---------|-------------|
| **P1** | [DATABASE_FAILOVER.md](./DATABASE_FAILOVER.md) | Azure SQL HA / geo-failover, listeners, RPO/RTO, post-failover checks. |
| **P1** | [SECRET_AND_CERT_ROTATION.md](./SECRET_AND_CERT_ROTATION.md) | Keys, SQL passwords, JWT, webhooks, TLS. |
| **P1** | [API_KEY_ROTATION.md](./API_KEY_ROTATION.md) | API key lifecycle for automation principals and smoke probes. |
| **P1** | [MIGRATION_ROLLBACK.md](./MIGRATION_ROLLBACK.md) | DbUp / SQL migration issues and rollback posture. |
| **P1** | [TRACE_A_RUN.md](./TRACE_A_RUN.md) | Reconstruct one run across audit (`CorrelationId` / `RunId`), traces (`otelTraceId`), and logs. |
| **P2** | [AGENT_EXECUTION_FAILURES.md](./AGENT_EXECUTION_FAILURES.md) | Architecture run execute fails (simulator vs real agents, traces, schema). |
| **P2** | [ALERT_DELIVERY_FAILURES.md](./ALERT_DELIVERY_FAILURES.md) | Alert routing subscriptions fire but destinations do not receive notifications. |
| **P2** | [ADVISORY_SCAN_FAILURES.md](./ADVISORY_SCAN_FAILURES.md) | Advisory scans fail or schedules do not fire. |
| **P2** | [COMPARISON_REPLAY_RATE_LIMITS.md](./COMPARISON_REPLAY_RATE_LIMITS.md) | Replay throttling, 429s, or batch replay partial failures. |
| **P2** | [COMPARISON_RECORD_ORPHAN_REMEDIATION.md](./COMPARISON_RECORD_ORPHAN_REMEDIATION.md) | Orphan `ComparisonRecords` / golden manifests / findings snapshots vs `dbo.Runs` (dry-run then delete). |
| **P2** | [DATA_ARCHIVAL_HEALTH.md](./DATA_ARCHIVAL_HEALTH.md) | `data_archival` health degraded or archival host errors. |
| **P2** | [PROVENANCE_INDEXING.md](./PROVENANCE_INDEXING.md) | Provenance indexing lag or failures. |
| **P2** | [SLO_PROMETHEUS_GRAFANA.md](./SLO_PROMETHEUS_GRAFANA.md) | Metrics, SLOs, Grafana panels. |
| **P2** | [INFRASTRUCTURE_OPS.md](./INFRASTRUCTURE_OPS.md) | Terraform stacks (APIM, Front Door, Entra, private endpoints): validate, roll out, triage. |
| **P3** | [GEO_FAILOVER_DRILL.md](./GEO_FAILOVER_DRILL.md) | **Scheduled drill:** measure RTO/RPO, record T0–T3, smoke after cutover. |
| **P3** | [LOAD_TEST_RATE_LIMITS.md](./LOAD_TEST_RATE_LIMITS.md) | Load testing against rate-limited endpoints. |
| **P3** | [REDIS_HEALTH.md](./REDIS_HEALTH.md) | Redis used for dev compose / cache patterns; connectivity and health checks. |
| **P3** | [LOGIC_APPS_STANDARD.md](./LOGIC_APPS_STANDARD.md) | Optional Logic App (Standard) hosts for Service Bus integration workflows (ADR 0019). |
| **P3** | [TERRAFORM_STATE_MV_PHASE_7_5.md](./TERRAFORM_STATE_MV_PHASE_7_5.md) | Deferred **Phase 7.5** resource rename: coordinated `terraform state mv` inventory. |
| **P3** | [COPILOT_CODE_REVIEW_SETUP.md](./COPILOT_CODE_REVIEW_SETUP.md) | One-time setup: enable GitHub Copilot auto-review on every PR; lives alongside `.github/copilot-instructions.md` + `CODEOWNERS`. |

**Related:** `infra/README.md` (Terraform roots and feature flags), `docs/CONTAINERIZATION.md` (Dockerfile and compose profiles).

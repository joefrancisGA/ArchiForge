# Runbooks index

Operational guides for ArchLucid operators. Each runbook is self-contained; cross-links point to deeper design docs where useful.

**Availability policy:** [RTO / RPO targets by tier](../RTO_RPO_TARGETS.md) — development vs staging vs production (SQL geo-replication, RPO/RTO examples).

| Runbook | When to use |
|--------|----------------|
| [AGENT_EXECUTION_FAILURES.md](./AGENT_EXECUTION_FAILURES.md) | Architecture run execute fails (simulator vs real agents, traces, schema). |
| [ALERT_DELIVERY_FAILURES.md](./ALERT_DELIVERY_FAILURES.md) | Alert routing subscriptions fire but destinations do not receive notifications. |
| [ADVISORY_SCAN_FAILURES.md](./ADVISORY_SCAN_FAILURES.md) | Advisory scans fail or schedules do not fire. |
| [COMPARISON_REPLAY_RATE_LIMITS.md](./COMPARISON_REPLAY_RATE_LIMITS.md) | Replay throttling, 429s, or batch replay partial failures. |
| [DATA_ARCHIVAL_HEALTH.md](./DATA_ARCHIVAL_HEALTH.md) | `data_archival` health degraded or archival host errors. |
| [DATABASE_FAILOVER.md](./DATABASE_FAILOVER.md) | Azure SQL HA / geo-failover, listeners, RPO/RTO, post-failover checks. |
| [INFRASTRUCTURE_OPS.md](./INFRASTRUCTURE_OPS.md) | Terraform stacks (APIM, Front Door, Entra, private endpoints): validate, roll out, triage. |
| [LOAD_TEST_RATE_LIMITS.md](./LOAD_TEST_RATE_LIMITS.md) | Load testing against rate-limited endpoints. |
| [MIGRATION_ROLLBACK.md](./MIGRATION_ROLLBACK.md) | DbUp / SQL migration issues and rollback posture. |
| [PROVENANCE_INDEXING.md](./PROVENANCE_INDEXING.md) | Provenance indexing lag or failures. |
| [REDIS_HEALTH.md](./REDIS_HEALTH.md) | Redis used for dev compose / cache patterns; connectivity and health checks. |
| [SECRET_AND_CERT_ROTATION.md](./SECRET_AND_CERT_ROTATION.md) | Keys, SQL passwords, JWT, webhooks, TLS. |
| [SLO_PROMETHEUS_GRAFANA.md](./SLO_PROMETHEUS_GRAFANA.md) | Metrics, SLOs, Grafana panels. |
| [TRACE_A_RUN.md](./TRACE_A_RUN.md) | Reconstruct one run across audit (`CorrelationId` / `RunId`), traces (`otelTraceId`), and logs. |

**Related:** `infra/README.md` (Terraform roots and feature flags), `docs/CONTAINERIZATION.md` (Dockerfile and compose profiles).

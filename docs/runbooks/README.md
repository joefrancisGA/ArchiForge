# Runbooks index

Operational guides for ArchiForge operators. Each runbook is self-contained; cross-links point to deeper design docs where useful.

| Runbook | When to use |
|--------|----------------|
| [ADVISORY_SCAN_FAILURES.md](./ADVISORY_SCAN_FAILURES.md) | Advisory scans fail or schedules do not fire. |
| [COMPARISON_REPLAY_RATE_LIMITS.md](./COMPARISON_REPLAY_RATE_LIMITS.md) | Replay throttling, 429s, or batch replay partial failures. |
| [DATA_ARCHIVAL_HEALTH.md](./DATA_ARCHIVAL_HEALTH.md) | `data_archival` health degraded or archival host errors. |
| [INFRASTRUCTURE_OPS.md](./INFRASTRUCTURE_OPS.md) | Terraform stacks (APIM, Front Door, Entra, private endpoints): validate, roll out, triage. |
| [LOAD_TEST_RATE_LIMITS.md](./LOAD_TEST_RATE_LIMITS.md) | Load testing against rate-limited endpoints. |
| [MIGRATION_ROLLBACK.md](./MIGRATION_ROLLBACK.md) | DbUp / SQL migration issues and rollback posture. |
| [PROVENANCE_INDEXING.md](./PROVENANCE_INDEXING.md) | Provenance indexing lag or failures. |
| [REDIS_HEALTH.md](./REDIS_HEALTH.md) | Redis used for dev compose / cache patterns; connectivity and health checks. |
| [SECRET_AND_CERT_ROTATION.md](./SECRET_AND_CERT_ROTATION.md) | Keys, SQL passwords, JWT, webhooks, TLS. |
| [SLO_PROMETHEUS_GRAFANA.md](./SLO_PROMETHEUS_GRAFANA.md) | Metrics, SLOs, Grafana panels. |

**Related:** `infra/README.md` (Terraform roots and feature flags), `docs/CONTAINERIZATION.md` (Dockerfile and compose profiles).

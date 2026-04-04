# Operations — Admin diagnostics API

**Last reviewed:** 2026-04-04

Privileged routes under **`GET /v1/admin/...`** require **`AdminAuthority`** policy.

| Route | Purpose |
|-------|---------|
| `GET /v1/admin/diagnostics/outboxes` | Pending authority pipeline and retrieval indexing work (depth snapshot). |
| `GET /v1/admin/diagnostics/leases` | SQL host leader lease rows (empty when not applicable). |
| `GET /v1/admin/features/async-authority-pipeline` | Effective feature flag state. |

## Runbooks

- Stuck outbox / backlog: `docs/TROUBLESHOOTING.md`, SQL tables referenced in `ArchiForge.Persistence.Data.*` repositories for pipeline work and retrieval outbox.
- Migrations / readiness failures: `GET /health/ready` and host startup logs (correlation id).
- LLM / quota: `docs/OPERATIONS_LLM_QUOTA.md`

## On-call quick path

1. Note **`X-Correlation-ID`** from the failing client response.
2. Check **`GET /v1/admin/diagnostics/outboxes`** for growing pending counts.
3. Verify worker role is running if using split **`Hosting:Role`** (`docs/DEPLOYMENT_TERRAFORM.md`).

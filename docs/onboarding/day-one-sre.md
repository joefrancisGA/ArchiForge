# Day one — SRE / Platform (week one)

**Goal:** Know how the **ArchLucid** service **starts**, **fails**, and **deploys** in your environment. **Not** full Terraform depth for every optional root.

**Ticket:** `ONBOARD-SRE-001` (copy into your work tracker)

---

## Scope (3–5 outcomes — check off by end of week one)

- [ ] **1. Health model** — Call **`GET /health/live`**, **`GET /health/ready`**, **`GET /health`** against a running instance; know which dependencies block readiness (SQL, schema, rule pack, blob, temp) ([BUILD.md](../BUILD.md), pipeline in `ArchLucid.Host.Core`).
- [ ] **2. Deploy order** — Read [DEPLOYMENT.md](../DEPLOYMENT.md) + [infra/README.md](../../infra/README.md) **Suggested order** (storage → compute → private endpoints → Entra → edge; failover group when used).
- [ ] **3. One Terraform root** — Pick **one** stack you own (e.g. `terraform-container-apps/`), run `terraform init -backend=false` + `terraform validate` locally; open that root’s `README.md` and `terraform.tfvars.example`.
- [ ] **4. Migrations posture** — Confirm **DbUp** (`DatabaseMigrator`) ordering matches your release process; skim [runbooks/MIGRATION_ROLLBACK.md](../runbooks/MIGRATION_ROLLBACK.md) for “forward-only” expectations.
- [ ] **5. Incident spine** — Bookmark [runbooks/DATABASE_FAILOVER.md](../runbooks/DATABASE_FAILOVER.md) and [RTO_RPO_TARGETS.md](../RTO_RPO_TARGETS.md); note **listener FQDN** vs single-server connection strings for SQL HA.

---

## Escalation

| Blocker | Where |
|---------|--------|
| Azure topology map | [DEPLOYMENT_TERRAFORM.md](../DEPLOYMENT_TERRAFORM.md) (Terraform roots + order), [DEPLOYMENT.md](../DEPLOYMENT.md) |
| Observability | [BUILD.md](../BUILD.md) (OpenTelemetry meter), [runbooks/SLO_PROMETHEUS_GRAFANA.md](../runbooks/SLO_PROMETHEUS_GRAFANA.md) |
| Operator commands | [OPERATOR_QUICKSTART.md](../OPERATOR_QUICKSTART.md) |

**Last reviewed:** 2026-04-04

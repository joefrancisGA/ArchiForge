> **Scope:** Day one — SRE / Platform (week one) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Day one — SRE / Platform (week one)

**Canonical operator action map:** [OPERATOR_ATLAS.md](../library/OPERATOR_ATLAS.md) — routes, APIs, and CLI in one table (health, deploy, governance surfaces).

**Goal:** Know how the **ArchLucid** service **starts**, **fails**, and **deploys** in your environment. **Not** full Terraform depth for every optional root.

> **Install order moved.** See [INSTALL_ORDER.md](../INSTALL_ORDER.md). This page now only covers SRE / Platform week-one tasks **after** install.

**Ticket:** `ONBOARD-SRE-001` (copy into your work tracker)

---

## Scope (3–5 outcomes — check off by end of week one)

- [ ] **1. Health model** — Call **`GET /health/live`**, **`GET /health/ready`**, **`GET /health`** against a running instance; know which dependencies block readiness (SQL, schema, rule pack, blob, temp) ([BUILD.md](../library/BUILD.md), pipeline in `ArchLucid.Host.Core`).
- [ ] **2. Deploy narrative** — Read [DEPLOYMENT.md](../library/DEPLOYMENT.md) + [DEPLOYMENT_TERRAFORM.md](../library/DEPLOYMENT_TERRAFORM.md) for how roots compose; **authoritative Azure apply order** is [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md) + [`infra/terraform-pilot`](../../infra/terraform-pilot/README.md).
- [ ] **3. One Terraform root** — Pick **one** stack you own (e.g. `terraform-container-apps/`), run `terraform init -backend=false` + `terraform validate` locally; open that root’s `README.md` and `terraform.tfvars.example`.
- [ ] **4. Migrations posture** — Confirm **DbUp** (`DatabaseMigrator`) ordering matches your release process; skim [runbooks/MIGRATION_ROLLBACK.md](../runbooks/MIGRATION_ROLLBACK.md) for “forward-only” expectations.
- [ ] **5. Incident spine** — Bookmark [runbooks/DATABASE_FAILOVER.md](../runbooks/DATABASE_FAILOVER.md) and [RTO_RPO_TARGETS.md](../library/RTO_RPO_TARGETS.md); note **listener FQDN** vs single-server connection strings for SQL HA.

---

## Escalation

| Blocker | Where |
|---------|--------|
| Azure topology map | [DEPLOYMENT_TERRAFORM.md](../library/DEPLOYMENT_TERRAFORM.md) (Terraform roots + order), [DEPLOYMENT.md](../library/DEPLOYMENT.md) |
| Observability | [BUILD.md](../library/BUILD.md) (OpenTelemetry meter), [runbooks/SLO_PROMETHEUS_GRAFANA.md](../runbooks/SLO_PROMETHEUS_GRAFANA.md) |
| Operator commands | [OPERATOR_QUICKSTART.md](../library/OPERATOR_QUICKSTART.md) |

**Last reviewed:** 2026-04-17

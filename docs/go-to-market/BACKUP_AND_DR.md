> **Scope:** ArchLucid — Backup, disaster recovery, and data lifecycle - full detail, tables, and links in the sections below.

# ArchLucid — Backup, disaster recovery, and data lifecycle

**Audience:** Security reviewers and procurement teams evaluating ArchLucid's data protection and recovery posture.

**Last reviewed:** 2026-04-15

This document describes ArchLucid's backup, disaster recovery, and data lifecycle posture **honestly** — stating what is in place, what uses Azure platform defaults, and what is roadmap.

---

## 1. Backup

### Azure SQL Database

| Property | Value |
|----------|-------|
| **Backup type** | Azure SQL automated backups (full, differential, transaction log) |
| **Point-in-time restore** | Azure SQL default retention window (7–35 days depending on service tier; standard default is **7 days**) |
| **Geo-redundant backup** | Available when configured via Terraform (`infra/terraform-sql-failover/`); enables restore to a paired region |
| **Encryption** | Backups are encrypted at rest via Transparent Data Encryption (TDE) — Azure platform default |

Operators should confirm the configured retention window in their Azure subscription and adjust if business requirements exceed the default.

### Blob storage

| Property | Value |
|----------|-------|
| **Soft delete** | Not configured by default in the current Terraform modules; **roadmap** item |
| **Versioning** | Not configured by default; **roadmap** item |
| **Geo-replication** | Available at the storage account level (GRS/RA-GRS); not enforced by default |

Blob storage holds optional agent execution traces and export artifacts. Operators deploying in production should enable soft delete and consider versioning based on data classification requirements.

---

## 2. Disaster recovery

### SQL failover group

ArchLucid's infrastructure includes a Terraform module for **Azure SQL failover groups** (`infra/terraform-sql-failover/`), enabling automatic failover to a secondary region.

| Property | Estimate |
|----------|----------|
| **RPO** (Recovery Point Objective) | **< 5 minutes** (Azure SQL async geo-replication; actual depends on replication lag) |
| **RTO** (Recovery Time Objective) | **< 1 hour** (includes DNS propagation, application reconnection, and verification) |

These are **current best estimates**, not contractual commitments. Formalized RTO/RPO targets will be documented in the commercial SLA when available.

### Geo-failover drill

An internal drill runbook exists and is exercised periodically to validate failover procedures, measure actual RTO/RPO, and identify gaps. Drill results inform infrastructure improvements.

### Application resilience

- **Connection resiliency:** `ResilientSqlConnectionFactory` with retry and circuit-breaker patterns.
- **Worker recovery:** Background services recover from transient failures; integration event outbox ensures at-least-once delivery.
- **Multi-host:** API and Worker can be deployed on separate compute instances for independent scaling and failure isolation.

---

## 3. Data lifecycle

### Retention defaults

ArchLucid retains customer data **until archived or deleted by operator workflows**. There is no automatic purge on a fixed schedule — operators control data lifecycle through:

- **Run archival:** Runs, golden manifests, and findings snapshots carry `ArchivedUtc` columns; archived data is excluded from active queries.
- **Audit events:** Append-only in SQL with export capabilities (CSV via `GET /v1/audit/export`). Retention is operator-managed.
- **Agent traces:** Optional full-prompt persistence in blob storage; lifecycle follows blob retention configuration.

### Data deletion on termination

On contract termination, ArchLucid deletes customer data per the timeline agreed in the [DPA](DPA_TEMPLATE.md) (§9). Customers may export data prior to termination using product export features (DOCX/ZIP exports, audit CSV).

### Data export

| Method | Scope | Access |
|--------|-------|--------|
| DOCX / ZIP export | Architecture artifacts, manifests | Operator or Admin role |
| Audit CSV | Typed audit events | Auditor or Admin role |
| API (JSON) | All data accessible via REST API | Per endpoint RBAC |

---

## 4. What we do NOT claim (yet)

| Capability | Status |
|------------|--------|
| Cross-region **active-active** | Not available; failover is active-passive |
| Customer-controlled **backup schedules** | Uses Azure platform defaults; not exposed to customers |
| Blob **geo-replication** enforcement | Available but not enforced by default |
| Customer-managed **encryption keys** (BYOK) | Not available; uses Azure-managed keys |
| Guaranteed **RTO/RPO** in SLA | Estimates only; formalization pending |

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [SLA_SUMMARY.md](SLA_SUMMARY.md) | Availability and latency objectives |
| [DPA_TEMPLATE.md](DPA_TEMPLATE.md) | Data deletion on termination (§9) |
| [TENANT_ISOLATION.md](TENANT_ISOLATION.md) | Data isolation architecture |
| [SUBPROCESSORS.md](SUBPROCESSORS.md) | Azure services and data residency |

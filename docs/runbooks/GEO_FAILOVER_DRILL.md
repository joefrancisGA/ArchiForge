> **Scope:** Geo-failover drill (Azure SQL) — executable runbook - full detail, tables, and links in the sections below.

# Geo-failover drill (Azure SQL) — executable runbook

**Last reviewed:** 2026-04-16

## 1. Objective

Validate **RTO** and **RPO** intent from **`docs/RTO_RPO_TARGETS.md`** using a **controlled** failover (production-like or dedicated drill subscription), not developer laptops.

## 2. Assumptions

- **Auto-failover group** or **geo-replication** is configured (**`infra/terraform-sql-failover`**).
- Application connection strings use the **failover group read/write listener** where required (**`docs/runbooks/DATABASE_FAILOVER.md`**).
- API and worker revisions exist and pass **`/health/ready`** on the primary region before the drill.

## 3. Constraints

- Do **not** target production without change control and a rollback owner.
- Expect **brief** write unavailability during cutover; clients must **retry with backoff**.

## 4. Prerequisites checklist

1. Current **replication lag** visible in Azure Monitor (seconds, not minutes, under normal load).
2. **`GET /health/ready`** green on API and worker **primary**.
3. Grafana / App Insights dashboards open (**`docs/runbooks/SLO_PROMETHEUS_GRAFANA.md`**).
4. On-call + DBA + app owner notified; **maintenance window** recorded.
5. **Rollback:** documented steps to fail back or revert connection strings (**`DATABASE_FAILOVER.md`**).
6. Smoke script or **`scripts/ci/cd-post-deploy-verify.sh`** parameters captured for post-cutover.
7. Latest **application image tag** and **DB migration version** recorded (no pending schema drift).

## 5. Execution steps

| Step | Action | Expected |
|------|--------|----------|
| 1 | **T0:** Record UTC time; confirm primary region healthy. | Baseline |
| 2 | Initiate **planned failover** (Azure portal or CLI per **`DATABASE_FAILOVER.md`**). | Role swap begins |
| 3 | **T1:** First **`/health/ready`** failure on old primary (or first SQL auth error in logs). | Transient errors OK |
| 4 | **T2:** **`/health/ready`** **Healthy** on API + worker against new primary. | Apps follow listener |
| 5 | Query **last run id** created before T0; confirm row visible and consistent. | RPO check |
| 6 | Run smoke: create run → execute → commit (or **`CD_POST_DEPLOY`** subset). | Functional check |
| 7 | **T3:** All smokes pass. | Drill complete |

## 6. Measurement template

| Metric | Target | Actual | Pass/Fail |
|--------|--------|--------|-----------|
| RTO (T3 − T1) | &lt; 60 min | | |
| RPO (max committed gap at failover) | &lt; 5 min (org-specific) | | |
| Health recovery (T2 − T1) | &lt; 5 min (informal) | | |
| Smoke pass rate | 100% | | |

## 7. Failure scenarios

- **Health never recovers:** verify listener FQDN, firewall, **Entra** / SQL auth, and that workers restarted with correct secrets.
- **Data older than RPO on new primary:** stop smoke writes; escalate to DBA; consider **point-in-time restore** per org policy (**not** automated here).
- **Outbox backlog spike:** scale worker replicas temporarily; inspect dead-letter metrics (**`docs/OBSERVABILITY.md`**).

## 8. Cadence

- **Quarterly** for production (or **annually** minimum if policy allows).
- **After** SQL SKU change, topology change, or connection-string change.

## Related

- **`docs/RTO_RPO_TARGETS.md`**
- **`docs/runbooks/DATABASE_FAILOVER.md`**
- **`docs/LOAD_TEST_BASELINE.md`** (capacity, not DR)

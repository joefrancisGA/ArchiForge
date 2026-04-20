> **Scope:** ArchLucid — Customer health scoring framework - full detail, tables, and links in the sections below.

# ArchLucid — Customer health scoring framework

**Audience:** Customer success team and product leadership.

**Last reviewed:** 2026-04-15

---

## 1. Purpose

Detect **churn risk** early, identify **expansion** opportunities, and give the CS team a **single composite health score** per account. This framework starts manual (Phase 1) and evolves toward in-product automation.

---

## 2. Health dimensions

| Dimension | Weight | Signals | Data source |
|-----------|--------|---------|-------------|
| **Engagement** | 30% | Runs per week, unique active operators, login frequency | `dbo.Runs` (created dates), `dbo.AuditEvents` (actor diversity) |
| **Breadth** | 20% | Finding engine types used, comparison runs, export frequency, workspaces active | Run metadata, audit events |
| **Quality** | 15% | Average agent output quality score, explainability trace completeness ratio | OTel metrics (`archlucid.authority.agent_output_quality`, `archlucid.explanation_trace_completeness_ratio`) |
| **Governance adoption** | 20% | Approval requests created/resolved, policy packs configured, segregation of duties active | `dbo.GovernanceApprovalRequests`, governance audit events |
| **Support** | 15% | Ticket volume, severity distribution, time-to-resolution, CSAT | External support tool (placeholder) |

---

## 3. Scoring model

### Per-dimension scale (1–5)

| Score | Label | Criteria (example: Engagement) |
|-------|-------|-------------------------------|
| **5** | Excellent | 10+ runs/week, 5+ active operators |
| **4** | Good | 5–9 runs/week, 3–4 active operators |
| **3** | Adequate | 2–4 runs/week, 2 active operators |
| **2** | Needs attention | 1 run/week, 1 active operator |
| **1** | At risk | No runs in 2+ weeks, no logins |

Each dimension has its own scale definition (adapt from template above). Document per-dimension thresholds when real data becomes available.

### Composite score

**Composite = Σ(dimension score × dimension weight)**

| Composite range | Health status | Color |
|----------------|---------------|-------|
| **4.0–5.0** | Healthy | Green |
| **2.5–3.9** | Needs attention | Yellow |
| **1.0–2.4** | At risk | Red |

---

## 4. Implementation phases

| Phase | Scope | Effort |
|-------|-------|--------|
| **Phase 1 (manual)** | CS team fills in a spreadsheet monthly using SQL queries and support data. Review in team standup. | Low — spreadsheet + ad hoc SQL |
| **Phase 2 (semi-automated)** | Scheduled SQL report (stored procedure or Python script) emailed to CS weekly. Support data manually appended. | Medium — script + scheduled job |
| **Phase 3 (in-product)** | Admin dashboard with health metrics per tenant/workspace. Alerting on Red accounts. Support integration via API. | High — UI + backend + integration |

**Start with Phase 1.** The goal is to build the **habit** of reviewing health before building the tooling.

### Phase 1 SQL queries (starter)

```sql
-- Engagement: runs per week for a tenant (last 4 weeks)
SELECT
    DATEPART(ISOWK, CreatedUtc) AS Week,
    COUNT(*) AS RunCount,
    COUNT(DISTINCT CreatedBy) AS UniqueOperators
FROM dbo.Runs
WHERE TenantId = @TenantId
  AND CreatedUtc >= DATEADD(WEEK, -4, GETUTCDATE())
GROUP BY DATEPART(ISOWK, CreatedUtc)
ORDER BY Week;

-- Governance adoption: approval requests in last 30 days
SELECT COUNT(*) AS ApprovalRequests
FROM dbo.GovernanceApprovalRequests
WHERE TenantId = @TenantId
  AND CreatedUtc >= DATEADD(DAY, -30, GETUTCDATE());
```

---

## 5. Action playbooks

| Health status | CS action |
|---------------|-----------|
| **Healthy** (Green) | Expansion conversation; request case study / reference; quarterly business review |
| **Needs attention** (Yellow) | Proactive check-in within 1 week; offer training session or feature walkthrough; identify blockers |
| **At risk** (Red) | Escalate to account exec within 48 hours; engage executive sponsor on customer side; assess root cause (product gap, onboarding failure, champion departure) |

---

## Related documents

| Doc | Use |
|-----|-----|
| [CUSTOMER_ONBOARDING_PLAYBOOK.md](CUSTOMER_ONBOARDING_PLAYBOOK.md) | Onboarding phases and signals |
| [RENEWAL_EXPANSION_PLAYBOOK.md](RENEWAL_EXPANSION_PLAYBOOK.md) | Renewal and expansion process |
| [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md) | Pilot measurement (feeds initial health data) |
| [../PRODUCT_LEARNING.md](../PRODUCT_LEARNING.md) | Product learning signals |

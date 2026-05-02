> **Scope:** ArchLucid — Service level objectives (buyer summary) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Service level objectives (buyer summary)

**Audience:** Procurement, security reviewers, and technical evaluators assessing ArchLucid's reliability commitments.

**Last reviewed:** 2026-04-29

ArchLucid targets **high availability and low latency** for the production API. This document translates internal engineering objectives into buyer-readable commitments. For engineering depth (Prometheus rules, OTel metrics, burn-rate math), see [../API_SLOS.md](../library/API_SLOS.md).

**Important:** These are **service level objectives** (targets), not contractual guarantees. Contractual SLA terms, including service credits, will be defined in the commercial agreement. See [ORDER_FORM_TEMPLATE.md](ORDER_FORM_TEMPLATE.md) for contract framing.

---

## 1. Availability

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Monthly availability** | **99.9%** | Ratio of successful API responses (**non-5xx**) to total requests, measured over a **30-day rolling window** (same SLI as Prometheus burn-rate rules in `infra/prometheus/archlucid-slo-rules.yml`). |

**What counts as downtime:** Periods where the API fails to meet the availability target above. **5xx rate** is the same signal: a **99.9%** target implies at most **0.1%** of requests may be **5xx** over the window for that measurement. Planned maintenance windows that are communicated in advance are **excluded** from the availability calculation.

### Error rate (5xx)

| Metric | Target | Measurement |
|--------|--------|-------------|
| **HTTP 5xx** | ≤ **0.1%** of requests | 30-day rolling window; server-side counts (pairs with availability SLI above). |

**LLM provider carve-out:** Contract may define a **separate** sub-budget for documented upstream model unavailability; see [../library/API_SLOS.md](../library/API_SLOS.md).

---

## 2. Latency

Latency is **tiered** so infrastructure probes, standard API traffic, and **AI-augmented** routes each have credible targets. Full table: [../library/API_SLOS.md](../library/API_SLOS.md) § *Latency tiers (customer-visible)*.

| Tier | Examples | **p95** (customer-visible) | **p99** (customer-visible) |
|------|----------|----------------------------|---------------------------|
| **1 — Infrastructure** | `GET /health/live`, `GET /version` | **< 300 ms** | **< 500 ms** |
| **2 — Synchronous API** | Typical reads/writes without LLM in the hot path | **< 800 ms** | **< 1.5 s** |
| **3 — AI-augmented** | Documented LLM-backed request paths | **< 8 s** | *tracked internally until pilot proof* |

**Async work:** Operations that return **202** + polling are measured on **polling** latency (Tier **2**), not end-to-end job duration.

Engineering detail (synthetic probes, Prometheus histograms, internal early warnings): [../library/API_SLOS.md](../library/API_SLOS.md).

---

## 3. Planned maintenance

| Commitment | Detail |
|------------|--------|
| **Advance notice** | **72 hours** minimum for scheduled maintenance that may affect availability. |
| **Maintenance windows** | Prefer off-peak hours; specific windows communicated per customer's primary region. |
| **Zero-downtime target** | Rolling deployments are the default; maintenance requiring downtime is exceptional and always communicated. |

---

## 4. Service credits

Service credit terms (e.g., percentage credit for availability below target) are **to be defined** in the commercial SLA attached to the subscription agreement. This document describes **objectives**, not contractual obligations.

---

## 5. Exclusions

The availability target does **not** apply to:

- **Scheduled maintenance** communicated per §3.
- **Force majeure** events (natural disasters, widespread infrastructure outages beyond ArchLucid's control).
- **Customer-caused issues** (misconfigured API keys, blocked network paths, excessive request volumes beyond agreed limits).
- **Beta or preview features** explicitly marked as such.

---

## 6. How we measure

- **Internal monitoring:** Continuous server-side metrics collected via OpenTelemetry, aggregated into availability ratios and latency percentiles. Burn-rate alerts detect budget consumption before it becomes visible to customers.
- **External probes:** Periodic synthetic checks from outside the cluster verify reachability and basic response correctness of health and version endpoints.
- **Engineering detail:** [../API_SLOS.md](../library/API_SLOS.md).

---

## 7. Incident response

When availability or latency targets are at risk, the incident communications policy governs customer notification:

- **SEV-1 (service unavailable):** Customer notification within **1 hour**; updates every **30 minutes**.
- **SEV-2 (degraded):** Notification within **4 hours**; updates every **2 hours**.
- Full details: [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md).

---

## 8. Status page

Public status URL is published in [TRUST_CENTER.md](TRUST_CENTER.md). Until a dedicated URL is live, incident updates are routed through [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) channels (`security@archlucid.net` fallback).

See [OPERATIONAL_TRANSPARENCY.md](OPERATIONAL_TRANSPARENCY.md) for the status page implementation plan.

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [../API_SLOS.md](../library/API_SLOS.md) | Engineering SLO detail |
| [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) | Incident classification and comms |
| [BACKUP_AND_DR.md](BACKUP_AND_DR.md) | Backup, DR, and data lifecycle |

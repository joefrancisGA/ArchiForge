# ArchLucid — Service level objectives (buyer summary)

**Audience:** Procurement, security reviewers, and technical evaluators assessing ArchLucid's reliability commitments.

**Last reviewed:** 2026-04-15

ArchLucid targets **high availability and low latency** for the production API. This document translates internal engineering objectives into buyer-readable commitments. For engineering depth (Prometheus rules, OTel metrics, burn-rate math), see [../API_SLOS.md](../API_SLOS.md).

**Important:** These are **service level objectives** (targets), not contractual guarantees. Contractual SLA terms, including service credits, will be defined in the commercial agreement. See [ORDER_FORM_TEMPLATE.md](ORDER_FORM_TEMPLATE.md) for contract framing.

---

## 1. Availability

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Monthly availability** | **99.5%** | Ratio of successful API responses (non-5xx) to total requests, measured over a **30-day rolling window**. |

**What counts as downtime:** Any period where the API returns HTTP 5xx errors at a rate exceeding the error budget (> 0.5% of requests). Planned maintenance windows that are communicated in advance are **excluded** from the availability calculation.

---

## 2. Latency

| Metric | Target | Measurement |
|--------|--------|-------------|
| **API response time (p95)** | **Under 2 seconds** | 95th percentile of HTTP request duration across all API endpoints, measured in 5-minute windows. |

This is an initial guardrail. Agent pipeline execution (architecture runs) may take longer due to LLM inference; the latency target applies to **API request handling**, not end-to-end run completion.

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
- **Engineering detail:** [../API_SLOS.md](../API_SLOS.md).

---

## 7. Incident response

When availability or latency targets are at risk, the incident communications policy governs customer notification:

- **SEV-1 (service unavailable):** Customer notification within **1 hour**; updates every **30 minutes**.
- **SEV-2 (degraded):** Notification within **4 hours**; updates every **2 hours**.
- Full details: [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md).

---

## 8. Status page

**[TBD — URL for public status page]**

See [OPERATIONAL_TRANSPARENCY.md](OPERATIONAL_TRANSPARENCY.md) for the status page implementation plan.

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [../API_SLOS.md](../API_SLOS.md) | Engineering SLO detail |
| [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) | Incident classification and comms |
| [BACKUP_AND_DR.md](BACKUP_AND_DR.md) | Backup, DR, and data lifecycle |

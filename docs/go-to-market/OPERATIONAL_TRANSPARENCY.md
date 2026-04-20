> **Scope:** ArchLucid — Operational transparency plan - full detail, tables, and links in the sections below.

# ArchLucid — Operational transparency plan

**Audience:** Product and engineering teams planning the public status page; buyers who ask "how will we know if the service is down?"

**Last reviewed:** 2026-04-15

---

## 1. Why

SaaS buyers — especially in enterprise and regulated environments — need confidence that service disruptions will be **visible**, **communicated**, and **resolved transparently**. A public status page is table stakes for trust. The [Incident Communications Policy](INCIDENT_COMMUNICATIONS_POLICY.md) defines **what** we communicate; this document defines **where** and **how**.

---

## 2. Status page options

| Option | Pros | Cons | Cost |
|--------|------|------|------|
| **Atlassian Statuspage** | Industry standard, subscriber notifications, API, components/groups, incident templates | Vendor dependency, monthly cost ($29–$399+/mo) | Medium |
| **Instatus** | Modern UI, generous free tier, API, custom domain | Smaller ecosystem, fewer enterprise references | Low |
| **GitHub repo + Actions** | Free, version-controlled, RSS via releases | Manual, lacks subscriber notifications, less professional appearance | Free |
| **Cachet (self-hosted)** | Full control, open-source | Operational overhead, maintenance burden | Low (infra cost) |

**Recommendation:** Start with **Instatus** (free tier) or **Atlassian Statuspage** (Starter) — lowest effort to a professional public page. Migrate to a higher tier or self-hosted solution if requirements grow.

---

## 3. Components to track

| Component | Maps to | Health source |
|-----------|---------|---------------|
| **API** | `ArchLucid.Api` | Synthetic probe (`GET /health/live`, `GET /version`) |
| **Web UI** | `archlucid-ui` (Next.js) | HTTP check on UI hostname |
| **Agent pipeline** | Run execution via Worker | Outbox convergence metric; run completion rate |
| **Authentication** | Entra ID / API key validation | Synthetic authenticated request or Entra status |
| **Background processing** | `ArchLucid.Worker` | Worker heartbeat, outbox age gauge |

---

## 4. Mapping to incident severity

| Status page state | Incident severity | Description |
|-------------------|-------------------|-------------|
| **Operational** | — | All components healthy |
| **Degraded performance** | SEV-3 | Minor impact, workaround available |
| **Partial outage** | SEV-2 | Subset of tenants or features impaired |
| **Major outage** | SEV-1 | Service unavailable for all or most tenants |
| **Under maintenance** | Planned | Scheduled maintenance window per [SLA_SUMMARY.md](SLA_SUMMARY.md) §3 |

---

## 5. Integration points

- **Prometheus/Grafana alerts** → Status page updates. **Phase 1:** Manual update by on-call when alert fires. **Phase 3:** Automate via status page API (e.g., Statuspage API `POST /incidents` triggered by alert webhook).
- **Incident communications policy** → Status page is the **primary public channel** for SEV-1 and SEV-2 incidents.
- **Synthetic probes** (GitHub Actions) → Feed uptime percentage displayed on the status page.

---

## 6. Implementation plan

| Phase | Scope | Timeline placeholder |
|-------|-------|---------------------|
| **Phase 1** | Choose provider, create page with 5 components, add URL to [TRUST_CENTER.md](TRUST_CENTER.md), [SLA_SUMMARY.md](SLA_SUMMARY.md), and [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) | Near-term |
| **Phase 2** | Manual incident updates aligned with comms policy; team trained on update workflow | With first production customer |
| **Phase 3** | Automated uptime checks feeding the page; alert-to-incident webhook integration | Post Phase 2 stabilization |

---

## 7. Placeholder references

The following documents contain **[TBD]** placeholders for the status page URL. Update them when the page is live:

- [SLA_SUMMARY.md](SLA_SUMMARY.md) §8
- [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) §4 (channels)

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [SLA_SUMMARY.md](SLA_SUMMARY.md) | Availability targets |
| [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) | Incident classification and comms |

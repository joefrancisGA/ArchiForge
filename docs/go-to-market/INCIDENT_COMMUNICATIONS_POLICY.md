> **Scope:** ArchLucid — Incident communications policy - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Incident communications policy

**Audience:** Customers and internal operators; complements internal runbooks (not duplicated here).

**Last reviewed:** 2026-04-15

This policy describes how ArchLucid classifies service and security incidents and **communicates** with customers in a **SaaS** context. It aligns with correlation and support practices in [../CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md) and service objectives in [../API_SLOS.md](../library/API_SLOS.md).

---

## 1. Objective

- Provide **timely**, **accurate** incident communication.
- Separate **service availability** incidents from **security** incidents (personal data breach) per [DPA_TEMPLATE.md](DPA_TEMPLATE.md).

---

## 2. Severity classification

| Severity | Description | Examples |
|----------|-------------|----------|
| **SEV-1** | Critical — service **unavailable** or **severely degraded** for **all** or **most** tenants | Regional outage, data plane unavailable, auth broken for Entra path |
| **SEV-2** | Major — **subset** of tenants or **material features** impaired | Elevated 5xx on critical paths, worker backlog causing governance delay |
| **SEV-3** | Minor — limited impact, **workaround** exists | Single feature degraded, non-critical background lag |
| **SEV-4** | Low — **no** material customer impact | Cosmetic UI, internal-only tooling |

---

## 3. Communication timelines (service incidents)

Targets are **goals**; actual events may require adjustment (e.g., unknown root cause).

| Severity | Initial customer-visible notice | Updates | Post-incident summary |
|----------|----------------------------------|---------|------------------------|
| **SEV-1** | Within **1 hour** of confirmed impact | At least every **30 minutes** while impact continues | Within **5 business days** (root cause, impact, remediation) |
| **SEV-2** | Within **4 hours** | At least every **2 hours** while impact continues | Within **10 business days** |
| **SEV-3** | Next business day or in scheduled report | As needed | Optional summary |
| **SEV-4** | Monthly operations / release notes | — | — |

**Channels (placeholders until published):** status page URL **[TBD]**, email distribution **[TBD]**, in-app banner for SEV-1/2 when available.

---

## 4. Security incidents and personal data breaches

If an incident involves **unauthorized access to** or **loss of** Personal Data (as defined in [DPA_TEMPLATE.md](DPA_TEMPLATE.md)):

- **Processor** notifies **Controller** **without undue delay** after becoming aware, and within **72 hours** where **GDPR Article 33** applies and Processor is responsible, unless a different timeline is required by law.
- Communication includes **known facts**, **containment** steps, and **recommended customer actions** (e.g., rotate API keys, review audit export).

Internal technical response may reference **[../runbooks/](../runbooks/)**; those runbooks are **not** customer-facing.

---

## 5. Customer responsibilities

- Include **`X-Correlation-ID`** on API requests when reporting issues so support can align logs across edge, API, and audit ([../CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md) §8).
- Provide a **security contact** on file for DPA and incident notices.

---

## 6. Post-incident review (internal)

Blameless review covers: **timeline**, **customer impact**, **root cause**, **remediation**, **preventive actions**. Outputs may feed **SOC 2** evidence ([SOC2_ROADMAP.md](SOC2_ROADMAP.md)).

---

## 7. Escalation contacts (placeholder)

| Role | Contact |
|------|---------|
| Security | `security@archlucid.com` |
| Support | **[TBD]** |

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [../API_SLOS.md](../library/API_SLOS.md) | HTTP SLOs (e.g. **99.5%** availability target) |
| [DPA_TEMPLATE.md](DPA_TEMPLATE.md) | Breach notification clause |

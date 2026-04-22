> **Scope:** Data Processing Agreement (DPA) — Template (ArchLucid) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Data Processing Agreement (DPA) — Template (ArchLucid)

**Important — not legal advice:** This document is a **working template** for negotiation with customers. It **does not** constitute legal advice. **Qualified legal counsel** must review and adapt it before execution.

**Parties:** Fill in legal names and addresses.

| Role | Party |
|------|--------|
| **Controller** | [Customer legal entity] |
| **Processor** | [ArchLucid vendor legal entity] |

**Effective date:** [date]

**Reference:** [Subscription / order form ID]

---

## 1. Definitions

- **“Personal Data”** means any information relating to an identified or identifiable natural person processed by Processor on behalf of Controller under this DPA.
- **“Processing”** has the meaning given in applicable data protection law (including the GDPR where applicable).
- **“Sub-processor”** means a third party engaged by Processor to process Personal Data.
- **“Services”** means the ArchLucid cloud service subscribed to by Controller.

Capitalized terms not defined here follow the [Subscription Agreement / Terms of Service] unless otherwise stated.

---

## 2. Scope and roles

2.1 **Processor** processes Personal Data **only** on documented instructions from **Controller** (including this DPA, the agreement, and documented configuration), unless applicable law requires otherwise (in which case Processor informs Controller unless prohibited).

2.2 **Controller** determines the purposes and means of Processing outside Processor’s documented product features (e.g., which users are invited, what content is submitted to architecture runs).

---

## 3. Categories of data subjects

Employees and contractors of Controller who use the Services; other individuals whose data appears in **free-text** architecture inputs, logs, or exports (e.g., names or identifiers pasted into runs).

---

## 4. Categories of Personal Data

Including where such data appears in user-provided architecture descriptions, Ask threads, audit trails, or exports:

- **Identity and access:** names, email addresses, Entra / OIDC subject identifiers, role assignments.
- **Professional content:** technical descriptions, system names, URLs, pasted logs or documents that may identify individuals or systems.
- **Usage and audit:** operational events recorded in the durable audit log (see product documentation), correlation identifiers, timestamps.

See also [../security/PII_RETENTION_CONVERSATIONS.md](../security/PII_RETENTION_CONVERSATIONS.md) and [../SECURITY.md](../SECURITY.md) (PII and conversation retention).

---

## 5. Duration

Processing continues for the **subscription term** and until deletion or return in accordance with §12, subject to backup retention and legal hold limitations disclosed in the Security & Trust documentation.

---

## 6. Processor obligations

6.1 **Confidentiality:** Personnel authorized to process Personal Data are bound by confidentiality obligations.

6.2 **Security:** Processor implements appropriate technical and organizational measures, including those described in [TRUST_CENTER.md](TRUST_CENTER.md), [TENANT_ISOLATION.md](TENANT_ISOLATION.md), and [../SECURITY.md](../SECURITY.md).

6.3 **Sub-processors:** Processor may engage Sub-processors listed in [SUBPROCESSORS.md](SUBPROCESSORS.md). Processor will impose data protection terms on Sub-processors. Controller may object to a **new** Sub-processor in accordance with the notification commitment in [SUBPROCESSORS.md](SUBPROCESSORS.md).

6.4 **Assistance:** Processor assists Controller with **data subject requests** and **DPIAs** as described in the agreement and applicable law, within reasonable scope.

6.5 **Breach notification:** Processor notifies Controller **without undue delay** after becoming aware of a **personal data breach**, in line with [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) and applicable law (including **72 hours** where GDPR Article 33 applies and Processor is responsible).

6.6 **Deletion / return:** At end of contract, Processor deletes or returns Personal Data per §9.

6.7 **Audit:** Processor makes available **SOC 2** reports when available ([SOC2_ROADMAP.md](SOC2_ROADMAP.md)) and reasonable information necessary to demonstrate compliance.

---

## 7. International transfers

Where Personal Data is processed in jurisdictions requiring safeguards, Processor uses mechanisms appropriate to the transfer (e.g., **Standard Contractual Clauses** or equivalent), aligned with Microsoft’s offerings and Controller’s Azure / Entra configuration. Document the **primary Azure region(s)** in the order form or security pack.

---

## 8. Security incidents

See [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) for severity classification and customer communication expectations.

---

## 9. Termination and data return

9.1 **Export:** Controller may export data using product features (e.g., DOCX/ZIP exports, audit CSV) subject to RBAC; see [../SECURITY.md](../SECURITY.md) (exports may contain sensitive content).

9.2 **Deletion:** After termination, Processor deletes Customer Data within **[e.g., 90]** days except where retention is required by law or documented backup cycles; backups roll off per Processor’s retention schedule.

---

## 10. Cross-tenant patterns opt-in

**Purpose (product):** Where the Services offer **anonymised industry guidance** (patterns derived from multiple customers’ committed architecture manifests, surfaced only when a statistical privacy floor is met), that processing is **optional** and **separate** from core tenant-private processing.

10.1 **Default:** Cross-tenant pattern participation is **OFF** unless Controller **explicitly** enables it in the product controls and, where required, documents that choice in the subscription or order form.

10.2 **What is processed:** **[Legal — describe]** — only **[non-identifying structural fingerprints / coarse aggregates]** as defined in the then-current product documentation (e.g. [ADR 0031 — Cross-tenant pattern library](../adr/0031-cross-tenant-pattern-library.md)); **not** free-text titles, URLs, user names, or customer-identifying labels in the operator-facing guidance surface.

10.3 **Privacy mechanism:** **[Legal — describe]** — Processor applies a **minimum cohort size** (product default **k ≥ 5** distinct contributing tenants per published bucket unless a stricter value is agreed) before showing any pattern to other tenants.

10.4 **Withdrawal:** When Controller disables the feature, Processor removes Controller’s contributions from publishable aggregates within **[e.g., 24]** hours, subject to documented backup and rebuild windows.

10.5 **Processor role:** For this optional feature, Processor may process **[categories of data]** solely to compute **[aggregates]** in line with documented instructions; Controller warrants it has **[lawful basis / authority]** where required.

**Important:** Qualified legal counsel must replace bracketed stubs and reconcile this section with Controller’s DPIA, industry rules, and the main agreement.

---

## 11. Signature

| Controller | Processor |
|------------|-----------|
| Name: | Name: |
| Title: | Title: |
| Date: | Date: |

---

## Related documents

| Doc | Use |
|-----|-----|
| [SUBPROCESSORS.md](SUBPROCESSORS.md) | Current subprocessor list |
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |

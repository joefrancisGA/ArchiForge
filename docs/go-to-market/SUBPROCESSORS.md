> **Scope:** ArchLucid — Subprocessors - full detail, tables, and links in the sections below.

# ArchLucid — Subprocessors

**Audience:** Customers and prospects who need a **subprocessor list** for security questionnaires and DPAs.

**Last reviewed:** 2026-04-15

ArchLucid uses the following **subprocessors** to deliver the hosted service. The list is derived from the **Azure-first** architecture described in [../CUSTOMER_TRUST_AND_ACCESS.md](../CUSTOMER_TRUST_AND_ACCESS.md), [../security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md), and repository `infra/` modules.

We will notify customers **at least 30 days** before engaging a **new** subprocessor that processes customer content or personal data, unless a shorter period is required by law or the change is immaterial (e.g., rename of an existing Microsoft service).

---

## Subprocessor table

| Subprocessor | Services used (representative) | Data typically processed | Region | Purpose |
|--------------|----------------------------------|---------------------------|--------|---------|
| **Microsoft Corporation** | **Azure Container Apps** (or equivalent compute), **Azure SQL**, **Azure Blob Storage**, **Azure Key Vault**, optional **Azure Service Bus**, **Azure Cache for Redis** (or compatible), **Azure Front Door**, optional **Azure API Management**, monitoring integrations | Customer architecture content, run metadata, manifests, findings, audit events, blobs (including optional agent traces), secrets by reference | **Primary Azure region(s)** chosen at deploy time via Terraform (see **Data residency** below) | Host application, store and encrypt data at rest, edge routing, optional queue/cache |
| **Microsoft Corporation** | **Microsoft Entra ID** | User / service principal identifiers, sign-in telemetry per Entra policy | Customer’s Entra tenant + Microsoft’s identity infrastructure | Authentication and app roles |
| **Microsoft Corporation** | **Azure OpenAI Service** | Prompts and completions for agent workflows (may include customer architecture text if submitted by users) | Azure OpenAI deployment region (per subscription configuration) | LLM inference |

**Non-Microsoft:** The product codebase does not require a separate non-Microsoft **runtime** subprocessor for core API functionality beyond Microsoft Azure services above. If you add third-party observability, CRM, or support tools that touch customer data, **update this table** before production use.

---

## Data residency

Production deployments are **Azure-region scoped**; the **primary region** is selected when infrastructure is provisioned (see `infra/` Terraform variables and [../terraform-azure-variables.md](../terraform-azure-variables.md)).

Until a single public **primary production region** is published for the ArchLucid SaaS offering, treat the region as **“per deployment / subscription — confirm in order form or security pack.”**

**Roadmap:** Document **multi-region** active/active or failover when offered; see [../runbooks/GEO_FAILOVER_DRILL.md](../runbooks/GEO_FAILOVER_DRILL.md) for operational drill context (internal).

---

## Change notification

- **New subprocessor:** **30 days’** advance notice to customer security contacts (email), except where a shorter period is required by law or the change is a **non-material** update (e.g., Microsoft service rename).
- **Material change:** Updated DPA schedule or subprocessors exhibit available on request; see [DPA_TEMPLATE.md](DPA_TEMPLATE.md).

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [DPA_TEMPLATE.md](DPA_TEMPLATE.md) | DPA template (subprocessors schedule) |
| [../security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md) | Product boundary and data flows |

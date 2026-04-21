> **Scope:** ADR 0020: Azure as the primary and permanent platform - full detail, tables, and links in the sections below.

# ADR 0020: Azure as the primary and permanent platform

**Status:** Accepted  
**Date:** 2026-04-20  
**Context:** Product positioning, infrastructure, and buyer narrative.

## Context

ArchLucid ships as a SaaS-oriented system with Azure-native building blocks (Entra ID, Azure SQL, Service Bus, storage, Container Apps, etc.). Some documentation historically used neutral “multi-cloud” phrasing to avoid locking narratives while capabilities were still stabilizing.

## Decision

**Azure is the primary, planned-permanent hosting and identity surface for ArchLucid.** Outward docs, Terraform, and operational runbooks should describe Azure-first deployment without implying equal first-class support for other public clouds unless a future ADR explicitly adds that scope.

Portability at the **application** layer (containers, standard protocols) remains valuable for **internal** engineering, **CI/CD**, security scanning, and **optional local evaluation** (for example `docker compose` in this repository). **Shipping production container images, Helm charts, or customer-operable “install from Docker” bundles as the licensed product is not a committed V1 path** — ArchLucid is **vendor-operated SaaS**; customer-facing artifacts are **CLI**, **API client libraries**, and **docs** (2026-04-21 product boundary; see **`docs/PENDING_QUESTIONS.md`** Resolved). This ADR does **not** require abstracting Azure service names in code paths that are already Azure-specific.

## Consequences

- **Positive:** Clearer buyer story, simpler SRE assumptions, alignment with IaC in `infra/`, and less hedging language to maintain.
- **Negative:** Organizations that mandate non-Azure production regions must treat ArchLucid as “Azure in their estate” or negotiate a separate product track (out of scope until documented elsewhere).
- **Clarification (2026-04-21):** Buyers who require **self-managed / on-premises** installation of the full product should be told **SaaS-only for V1** (see also **`docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md`**); reopening BYOC/BYOK-style delivery requires an explicit future ADR and commercial packaging, not implied by containerized builds alone.

## Compliance

- [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) and [go-to-market/POSITIONING.md](../go-to-market/POSITIONING.md) reference platform assumptions consistent with this ADR.
- Terraform guardrails remain: no legacy `archiforge` tokens in `.tf` (see CI).

## Related

- [REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md)
- [ADR 0016](0016-billing-provider-abstraction.md) (billing channels include Marketplace; still Azure-centric operations)

> **Scope:** ADR 0020: Azure as the primary and permanent platform - full detail, tables, and links in the sections below.

# ADR 0020: Azure as the primary and permanent platform

**Status:** Accepted  
**Date:** 2026-04-20  
**Context:** Product positioning, infrastructure, and buyer narrative.

## Context

ArchLucid ships as a SaaS-oriented system with Azure-native building blocks (Entra ID, Azure SQL, Service Bus, storage, Container Apps, etc.). Some documentation historically used neutral “multi-cloud” phrasing to avoid locking narratives while capabilities were still stabilizing.

## Decision

**Azure is the primary, planned-permanent hosting and identity surface for ArchLucid.** Outward docs, Terraform, and operational runbooks should describe Azure-first deployment without implying equal first-class support for other public clouds unless a future ADR explicitly adds that scope.

Portability at the **application** layer (containers, standard protocols) remains valuable for dev/test and for customer-controlled packaging later; this ADR does **not** require abstracting Azure service names in code paths that are already Azure-specific.

## Consequences

- **Positive:** Clearer buyer story, simpler SRE assumptions, alignment with IaC in `infra/`, and less hedging language to maintain.
- **Negative:** Organizations that mandate non-Azure production regions must treat ArchLucid as “Azure in their estate” or negotiate a separate product track (out of scope until documented elsewhere).

## Compliance

- [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) and [go-to-market/POSITIONING.md](../go-to-market/POSITIONING.md) reference platform assumptions consistent with this ADR.
- Terraform guardrails remain: no legacy `archiforge` tokens in `.tf` (see CI).

## Related

- [REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md)
- [ADR 0016](0016-billing-provider-abstraction.md) (billing channels include Marketplace; still Azure-centric operations)

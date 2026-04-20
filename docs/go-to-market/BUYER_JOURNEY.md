> **Scope:** Buyer journey (outside-in) - full detail, tables, and links in the sections below.

# Buyer journey (outside-in)

## Objective

Help enterprise architecture and platform leaders **hire ArchLucid** to turn messy architecture requests into **reviewable, versioned manifests, evidence, and governance-ready artifacts** in weeks instead of quarters — without replacing their existing EA tools wholesale.

## Assumptions

- The buyer already has **Confluence/Jira**, **draw.io or similar**, and **some** formal governance (even if inconsistent).
- **Entra ID** (or equivalent) exists; the team can approve an Azure-first pilot.
- Economic buyers may **not** have a labeled budget line** for “AI architecture OS”; value must map to **release risk**, **audit evidence**, or **review cycle time**.

## Constraints

- Sales motion is **multi-stakeholder** (EA, security reviewer, SRE, procurement).
- **LLM outputs are not legal proof**; value is in **traceability, versioning, and workflow discipline**.
- **Data residency** and **tenant isolation** are non-negotiable in regulated pilots; unsupported regions are a stop condition (see [NOT_A_FIT.md](NOT_A_FIT.md)).

## Architecture overview (buyer mental model)

ArchLucid is an **AI-assisted architecture workflow** with three product layers: **Core Pilot** (request → run → commit → manifest & artifacts), **Advanced Analysis** (compare, replay, provenance), **Enterprise Controls** (governance, policy, alerts). See [../PRODUCT_PACKAGING.md](../PRODUCT_PACKAGING.md).

## Component breakdown (what the buyer touches)

| Stage | Buyer touchpoints | Proof artifacts |
|-------|-------------------|-----------------|
| **Discovery** | Executive sponsor brief, pilot ROI companion, demo script | [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md), [PILOT_ROI_MODEL.md](../PILOT_ROI_MODEL.md) |
| **Pilot (30/60/90)** | Operator UI + CLI, API keys or Entra JWT, SQL / Container Apps deploy | Golden manifests, `Runs.OtelTraceId`, export records, audit events |
| **Expand** | Governance approvals, integration events, Service Bus consumers | `GovernanceApprovalRequests`, AsyncAPI catalog, [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../INTEGRATION_EVENTS_AND_WEBHOOKS.md) |

## Data flow

1. **Discovery → Pilot charter:** Pick one bounded system (one tenant/workspace), success metrics below, and a single sponsor owner.
2. **Day 0–30 — Core Pilot:** Produce **N** committed manifests with **reviewable** artifacts; measure time **request → first manifest commit**.
3. **Day 31–60 — Advanced (optional):** Run comparisons/replays on real change sets; measure drift findings closed.
4. **Day 61–90 — Enterprise (optional):** Route one approval workflow; measure approval **SLA adherence** vs baseline spreadsheet/email.
5. **Expand:** Roll to second product line only after metrics hold for one release train.

## Security model

Fail-closed API auth (`ArchLucidAuth`), **scope policies** (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority`), SQL **RLS session context** for tenant-scoped reads, **no SMB** on the public internet (private endpoints for Azure Storage). See [../SECURITY.md](../SECURITY.md) and [../onboarding/day-one-security.md](../onboarding/day-one-security.md).

## Operational considerations

- **Platform:** Terraform apply order in [../REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md); health gates `/health/live`, `/health/ready`.
- **Support:** `archlucid doctor`, `archlucid support-bundle --zip` (review before sharing); correlate with `X-Correlation-ID` and persisted trace id on runs.

## Three pilot success metrics (measurable)

| Metric | What “good” looks like | Proof in the system |
|--------|------------------------|---------------------|
| **Time-to-first committed manifest** | Median **&lt; X days** for pilot team (set X in charter) | `dbo.Runs` + `dbo.GoldenManifestVersions` / manifest commit timestamps vs `ArchitectureRequests.CreatedUtc` |
| **Traceable review package** | **100%** of pilot runs have **Otel + manifest + findings** for sponsor demo | `Runs.OtelTraceId`, `GoldenManifests`, `FindingsSnapshots` ([../DATA_MODEL.md](../DATA_MODEL.md)) |
| **Governance-ready evidence (if Enterprise layer)** | At least **one** approval or policy outcome **exportable** for audit | `GovernanceApprovalRequests`, `AuditEvents` / operator audit UI |

Canonical **buyer pitch** remains in [../EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) — this document aligns **field motion** to **persisted artifacts**, not a second narrative.

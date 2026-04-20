> **Scope:** State of AI-Assisted Architecture Design (2026) - full detail, tables, and links in the sections below.

# State of AI-Assisted Architecture Design (2026)

**Status:** Draft for publication (Marketability Improvement 6).  
**Audience:** Enterprise architects, platform engineering leaders, and AI governance stakeholders.

## Objective

Frame how **AI-assisted architecture design** is moving from novelty demos to **governed, evidence-backed engineering**—and where products like ArchLucid sit in that shift.

## Assumptions

- Model capabilities continue to improve faster than most enterprises can operationalize them.
- Regulated and safety-critical buyers will require **traceability**, **policy alignment**, and **human-in-the-loop** controls—not raw token output.

## Constraints

- Vendor-neutral claims should be anchored to observable product behaviors (runs, manifests, approvals) rather than model marketing.

## Architecture overview

**Nodes:** Human operators, AI agents, authority/orchestration services, knowledge graph, evidence stores, governance workflows.

**Edges:** Requirements and context flow **in**; findings, manifests, and audit trails flow **out** to stakeholders and existing ALM/security tools.

## Component breakdown

| Theme | What changed in 2025–2026 | Implication |
|-------|---------------------------|-------------|
| **Governance** | From “policy PDFs” to **machine-checkable** guardrails | Architecture tools must emit **policy-control objects** and support **approval** workflows. |
| **Evidence** | From screenshots to **structured provenance** | Buyers expect **artifact bundles** and replayable traces, not prose summaries. |
| **Evaluation** | From ad-hoc prompts to **golden manifests + regression** | Product teams ship **evaluation catalogs** alongside code. |

## Data flow

1. **Context ingestion** consolidates heterogeneous inputs (docs, IaC snippets, Terraform state JSON, topology hints).
2. **Authority runs** produce **findings** and **manifests** with explicit graph edges.
3. **Governance** gates promotion to production architecture records; **audit** captures operator and system actions.

## Security model

- **Tenant isolation** and **RLS** are baseline expectations for multi-tenant SaaS in this space.
- **Webhook and connector** surfaces must be **deny-by-default** with cryptographic verification where applicable.

## Operational considerations

- Cost scales with **run volume**, **retained traces**, and **graph size**—buyers should plan **retention tiers** and **archival** early.
- Community growth (SDKs, templates) depends on **clear extension points**—see `templates/archlucid-finding-engine/`.

## Open-source template

The repo ships a **`dotnet new archlucid-finding-engine`** template for custom finding logic. A standalone MIT/Apache-2.0 mirror can be published later; until then, consume the template from this repository (`templates/archlucid-finding-engine/README.md`).

> **Scope:** ArchLucid positioning - full detail, tables, and links in the sections below.

# ArchLucid positioning

**Audience:** Anyone who needs to explain what ArchLucid is and why it matters — in a sentence, a paragraph, or a two-minute conversation.

**Last reviewed:** 2026-04-15

**Grounding rule:** Every claim maps to a shipped V1 capability. See [V1_SCOPE.md](../V1_SCOPE.md) and [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) for evidence.

**Relationship to the sponsor brief:** [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) is the **dominant outward-facing buyer narrative**. This page supports **short explanations and proof-backed pillars** for conversations and datasheets; it must **not contradict** the brief. If wording here drifts broader than the brief, **tighten here** or promote a deliberate product change into the brief first, then realign.

**Platform:** First-party and reference deployments are **Azure-native**; see [ADR 0020](../adr/0020-azure-primary-platform-permanent.md).

---

## 1. Positioning statement

> For **enterprise architects and platform engineering leaders** who need architecture decisions that are **consistent, auditable, and governed**, ArchLucid is an **AI Architecture Intelligence platform** that orchestrates specialized AI agents to analyze system designs, produce explainable findings, and enforce governance workflows — all with a durable audit trail. Unlike **manual architecture review** which is slow, inconsistent, and undocumented, or **ad-hoc AI tools** which lack governance and traceability, ArchLucid combines the analytical speed of AI with the accountability enterprises require.

---

## 2. Three value pillars

### Pillar 1: AI-native architecture analysis

ArchLucid is not an architecture documentation tool with AI bolted on. It was built from day one around a **multi-agent pipeline** — four specialized AI agents (Topology, Cost, Compliance, Critic) analyze architecture requests through a structured pipeline: context ingestion → knowledge graph → findings → decisioning → artifact synthesis. The result is a **versioned golden manifest** with structured findings, not a chat conversation that disappears.

**Proof points:**
- 10 finding engine types running in parallel via `FindingsOrchestrator`
- Multi-vendor LLM support with automatic fallback (`ILlmProvider`, `FallbackAgentCompletionClient`)
- Deterministic simulator mode for testing and CI without LLM costs
- Agent output quality scoring (structural completeness + semantic quality) with configurable quality gates

### Pillar 2: Auditable decision trail

Every architecture recommendation ArchLucid produces comes with a complete chain of evidence. The `ExplainabilityTrace` on every finding records what was examined, what rules were applied, what decisions were taken, and why. The provenance graph connects evidence to decisions to manifest entries to artifacts. This is not "AI said so" — it is "AI analyzed these inputs, applied these rules, and reached this conclusion, and here is the full trail."

**Proof points:**
- `ExplainabilityTrace` with 5 structured fields on every finding (trace completeness measured by OTel metric)
- Provenance graph with nodes, edges, and graph algorithms (`ProvenanceBuilder`, `ProvenanceNode`, `ProvenanceEdge`)
- Explanation faithfulness checking (token overlap heuristic with aggregate fallback)
- Full agent execution trace persistence (prompt/response forensics in blob storage)

### Pillar 3: Enterprise governance

Architecture decisions in ArchLucid are not just analyzed — they are governed. Policy packs define compliance rules. Approval workflows enforce segregation of duties. Pre-commit gates block manifests when findings exceed severity thresholds. Approval SLAs track time-to-review and escalate breaches via webhooks. And 78 typed audit events in an append-only SQL store provide the evidence trail that regulators and auditors expect.

**Proof points:**
- Pre-commit governance gate with configurable severity thresholds and warning-only mode
- Approval workflow with segregation of duties (self-approval blocked, ordinal case-insensitive)
- Approval SLA tracking with `SlaDeadlineUtc` and webhook escalation on breach
- 78 typed audit event constants with CI guard, append-only enforcement (`DENY UPDATE/DELETE`)
- Policy packs with versioning, scope assignments, effective governance resolution
- Compliance drift trend tracking with operator UI chart

---

## 3. Elevator pitches

### 30-second pitch

"ArchLucid is an AI Architecture Intelligence platform. You describe a system you want to build, and our AI agents analyze it for topology, cost, compliance, and design quality — then produce a versioned manifest with every finding traced and explained. Think of it as an AI-powered architecture review board that runs in minutes instead of weeks, with a full audit trail."

### 60-second pitch

"Architecture review today is slow, inconsistent, and poorly documented. Different architects apply different standards, decisions happen in meetings with no record, and compliance gaps surface in production — not during design.

ArchLucid fixes this with AI Architecture Intelligence: a multi-agent pipeline that analyzes system designs for topology soundness, cost optimization, compliance coverage, and design quality. Every finding comes with a structured explainability trace — what was examined, what rules applied, what decisions were taken. Governance workflows enforce approval chains and segregation of duties, and a pre-commit gate blocks manifests when critical findings exist.

The result: architecture reviews that are fast, consistent, explainable, and fully auditable."

### 2-minute pitch

"Let me tell you about a problem I see in every enterprise I talk to.

Architecture review is a bottleneck. A small team of senior architects reviews every major design proposal. Reviews take weeks. Different architects apply different standards. Decisions are captured in email threads and slide decks that no one can find six months later. And compliance gaps surface during audits or — worse — in production incidents.

ArchLucid solves this with what we call AI Architecture Intelligence.

Here is how it works: An architect or engineer describes the system they want to build — system name, constraints, requirements, infrastructure context. ArchLucid orchestrates four specialized AI agents — for topology, cost, compliance, and critique — through a structured pipeline. The pipeline ingests the context, builds a knowledge graph, runs 10 finding engines in parallel, and produces a versioned golden manifest with structured findings.

Here is what makes ArchLucid different from just asking ChatGPT:

First, **every finding is explainable.** Our `ExplainabilityTrace` records exactly what was examined, what rules were applied, and what decisions were taken. This is not 'AI says so' — it is a complete decision trail.

Second, **decisions are governed.** Policy packs define your compliance rules. Pre-commit gates block manifests when critical findings exist. Approval workflows enforce segregation of duties — you cannot approve your own architecture change. SLAs track time-to-review and escalate breaches.

Third, **everything is auditable.** Seventy-eight typed audit events in an append-only store. When your auditor asks 'who reviewed this design, what did they find, and who approved it?' — ArchLucid has the answer.

The result: architecture reviews that used to take two weeks now take two hours. Quality goes up because every review runs the same engines. Compliance is shifted left because findings surface during design, not in production. And you have a full audit trail for every decision.

We are working with pilot customers in regulated enterprises today. I would love to show you a 15-minute demo."

---

## 4. Key proof points from the codebase

These are factual claims grounded in what the repository ships today.

> **See it live, not on a slide:** the operator shell ships a built-in proof page at **`/why-archlucid`** (Core Pilot tier, no extra authority required). It calls `GET /v1/pilots/why-archlucid-snapshot`, `GET /v1/pilots/runs/{runId}/first-value-report`, and `GET /v1/explain/runs/{runId}/aggregate` against the seeded **Contoso Retail Modernization** demo tenant and renders live `ArchLucidInstrumentation` counters, the sponsor first-value report, and the run explanation + citations. Every claim in the table below should reconcile against what shows on that page after `pilot up` (or `POST /v1/demo/seed`).

| Claim | Evidence |
|-------|----------|
| Multi-agent AI pipeline with 4 agent types | `IAuthorityRunOrchestrator`, agent types: Topology, Cost, Compliance, Critic |
| 10 finding engine types | `RequirementFindingEngine`, `ComplianceFindingEngine`, `SecurityBaselineFindingEngine`, `CostConstraintFindingEngine`, `TopologyCoverageFindingEngine`, `SecurityCoverageFindingEngine`, `PolicyApplicabilityFindingEngine`, `PolicyCoverageFindingEngine`, `RequirementCoverageFindingEngine`, topology gap findings via `FindingFactory` |
| Explainability trace on every finding | `ExplainabilityTrace`: `GraphNodeIdsExamined`, `RulesApplied`, `DecisionsTaken`, `AlternativePathsConsidered`, `Notes` |
| 78 typed audit event constants | `dbo.AuditEvents`, CI guard on count, append-only enforcement |
| Governance workflow with segregation of duties | `GovernanceApprovalRequests`, self-approval blocked with `GovernanceSelfApprovalException` |
| Pre-commit governance gate | `PreCommitGovernanceGate` with `BlockCommitMinimumSeverity` and warning-only mode |
| Approval SLA with escalation | `ApprovalSlaMonitor`, `SlaDeadlineUtc`, HMAC-signed webhook notifications |
| Provenance graph | `ProvenanceBuilder`, `ProvenanceNode`, `ProvenanceEdge`, `ProvenanceCompletenessAnalyzer` |
| Two-run comparison with drift detection | Structured golden-manifest deltas, comparison replay with verify mode (422 on drift) |
| Multi-vendor LLM with fallback | `ILlmProvider`, `LlmProviderDescriptor`, `FallbackAgentCompletionClient` |
| 30+ custom OTel metrics | `ArchLucidInstrumentation`, histograms/counters/gauges |
| Grafana dashboards committed in repo | Authority, SLO, LLM usage, container apps, run lifecycle dashboards |
| Policy packs with effective governance | `PolicyPackContentDocument`, scope assignments, `IEffectiveGovernanceResolver` |
| Compliance drift trend | `ComplianceDriftTrendService`, `ComplianceDriftChart` in operator UI |
| DOCX export with embedded diagrams | Consulting-grade report via `IDocxExportService`, Mermaid → PNG rendering |
| CLI for automation | `archlucid new`, `run`, `status`, `commit`, `artifacts`, `doctor`, `support-bundle`, `trace` |
| Enterprise auth (Entra ID + RBAC) | JwtBearer, API key, Admin/Operator/Reader/Auditor roles, `AuthSafetyGuard` |
| SQL RLS for multi-tenant isolation | `SESSION_CONTEXT`, scope columns, ADR 0003 |
| Private endpoints + WAF | Terraform modules for SQL/blob private endpoints, Front Door + WAF |
| Agent output quality scoring | Structural completeness + semantic quality, configurable quality gate |
| Prompt versioning | SHA-256 prompt catalog, prompt regression detection in CI |

---

## 5. Category definition

**AI Architecture Intelligence** is a new product category that combines:

1. **AI-driven analysis** of system designs (topology, cost, compliance, quality)
2. **Enterprise governance** (policy enforcement, approval workflows, compliance gates)
3. **Auditable decision trails** (explainability traces, provenance graphs, durable audit)

This category sits between traditional **Enterprise Architecture Management** (which catalogs and models but does not analyze) and **ad-hoc AI assistance** (which analyzes but lacks governance and traceability).

```
┌──────────────────────────────────────────────────────────────────┐
│                    Enterprise Architecture                       │
│                                                                  │
│  ┌─────────────┐    ┌──────────────────────┐    ┌─────────────┐ │
│  │ EA Mgmt     │    │ AI Architecture      │    │ Ad-hoc AI   │ │
│  │ (LeanIX,    │    │ Intelligence         │    │ (ChatGPT,   │ │
│  │  Ardoq)     │    │ (ArchLucid)          │    │  Copilot)   │ │
│  │             │    │                      │    │             │ │
│  │ Catalogs    │    │ Analyzes + Governs   │    │ Advises     │ │
│  │ Models      │    │ Traces + Audits      │    │ (ephemeral) │ │
│  │ Documents   │    │ Enforces + Exports   │    │             │ │
│  │             │    │                      │    │             │ │
│  │ No AI       │    │ AI + Governance      │    │ AI only     │ │
│  │ Manual      │    │ Automated            │    │ No govern.  │ │
│  └─────────────┘    └──────────────────────┘    └─────────────┘ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 6. Taglines (options for testing)

| Tagline | Angle |
|---------|-------|
| "Architecture decisions you can explain, govern, and audit." | Accountability focus |
| "AI-driven architecture review. Enterprise-grade governance." | Capability + trust |
| "From design to decision trail — in minutes, not weeks." | Speed + auditability |
| "The architecture review board that never sleeps." | Automation |
| "Every recommendation traced. Every decision governed." | Transparency + control |

---

## 7. Messaging "do" and "don't"

| Do | Don't |
|----|-------|
| Say "AI Architecture Intelligence" — own the category | Say "AI chatbot for architecture" — undervalues the pipeline |
| Emphasize **explainability and traceability** — this is the strongest differentiator | Claim "fully autonomous architecture design" — agents are orchestrated, not autonomous |
| Position as **complementary** to existing EA tools, not a replacement | Position as a **replacement** for LeanIX/Ardoq — different category |
| Be honest about V1 limitations (Azure-only, no import connectors) | Imply multi-cloud support or integrations that do not exist |
| Use "findings" and "manifest" language — these are domain-specific terms | Use "suggestions" or "recommendations" — these are vague and undifferentiated |
| Highlight the **governance workflow** — most competitors cannot match this | Lead with "AI" alone — every tool claims AI now |

---

## 8. Related documents

| Doc | Use |
|-----|-----|
| [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) | Competitor-by-competitor analysis and differentiation |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Who buys, why, and how to demo to them |
| [PRICING_PHILOSOPHY.md](PRICING_PHILOSOPHY.md) | **Locked list prices (2026)**, pilot pricing, re-rate gates, and sensitivity playbook — single source of truth for all price numbers |
| [../V1_SCOPE.md](../V1_SCOPE.md) | What V1 actually ships (grounding for all claims) |
| [../GLOSSARY.md](../GLOSSARY.md) | Domain terminology for consistent messaging |
| [../MARKETABILITY_ASSESSMENT_2026_04_15.md](../MARKETABILITY_ASSESSMENT_2026_04_15.md) | Full marketability quality assessment |
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust center — security overview, DPA template, subprocessors, incident comms, SOC 2 roadmap |
| [IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md) | ICP definition, scoring matrix, disqualifiers |
| [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md) | Available and planned integrations |
| [REFERENCE_NARRATIVE_TEMPLATE.md](REFERENCE_NARRATIVE_TEMPLATE.md) | Case study templates (3 fictional narratives) |

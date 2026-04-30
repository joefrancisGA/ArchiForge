> **Scope:** ArchLucid positioning - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid positioning

**Audience:** Anyone who needs to explain what ArchLucid is and why it matters — in a sentence, a paragraph, or a two-minute conversation.

**Last reviewed:** 2026-04-15

**Grounding rule:** Every claim maps to a shipped V1 capability. See [V1_SCOPE.md](../library/V1_SCOPE.md) and [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) for evidence.

**Relationship to the sponsor brief:** [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) is the **dominant outward-facing buyer narrative**. This page supports **short explanations and proof-backed pillars** for conversations and datasheets; it must **not contradict** the brief. If wording here drifts broader than the brief, **tighten here** or promote a deliberate product change into the brief first, then realign.

**Platform:** First-party and reference deployments are **Azure-native**; see [ADR 0020](../adr/0020-azure-primary-platform-permanent.md).

---

## 1. Positioning statement

> For **enterprise architects and the CTOs who sponsor their work**, ArchLucid turns scattered architecture evidence into a **prioritized, evidence-linked risk review** — complete with recommended actions, confidence ratings, and an exportable executive summary. Unlike **manual architecture review** which is slow, inconsistent, and undocumented, or **ad-hoc AI tools** which produce prose without accountability, ArchLucid delivers findings you can defend: every risk traced to evidence, every recommendation actionable, every decision auditable.

**Category:** AI Architecture Intelligence — sits between traditional Enterprise Architecture Management (which catalogs but does not analyze) and ad-hoc AI assistance (which analyzes but lacks governance and traceability).

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

**Live deep link in the staging funnel:**

The unauthenticated proof route **`/demo/explain`** (operator shell) renders the **provenance graph and the citations-bound aggregate explanation side-by-side**, sourced from the seeded Contoso Retail Modernization run. The route is hard-blocked from non-`Demo:Enabled=true` deployments by the `[FeatureGate(FeatureGateKey.DemoEnabled)]` filter — production hosts return `404` so the demo surface cannot leak. Sponsors and pilot evaluators can hit the staging URL directly:

- Staging deep link: `https://staging.archlucid.example.com/demo/explain` (replace host with the active staging deployment)
- Backing API: `GET /v1/demo/explain` — server-side `DemoReadModelClient` composes the same application services as `/v1/explain` and `/v1/provenance`, but **hard-pinned to the demo tenant scope** (the underlying authenticated routes are unchanged)
- Always returns `IsDemoData=true` and a "demo tenant — replace before publishing" status banner so screenshots cannot be quoted as production telemetry

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

"ArchLucid turns messy architecture evidence into a prioritized risk review — in minutes, not weeks. Upload your architecture materials, and ArchLucid's AI agents identify the top risks across topology, cost, compliance, and design quality. Every finding cites its evidence. Every recommendation is actionable. You get an executive-ready summary your CTO can actually read."

### 60-second pitch

"Architecture review is a bottleneck in every enterprise I talk to. A small team of senior architects reviews every design proposal. Reviews take weeks. Different architects apply different standards. Decisions are captured in email threads no one can find six months later. And compliance gaps surface in production — not during design.

ArchLucid solves this with evidence-linked architecture risk reviews.

You upload your architecture materials. ArchLucid runs a multi-agent analysis — topology, cost, compliance, design quality — and surfaces a prioritized findings board: each risk ranked by severity, confidence-rated, evidence-cited, and accompanied by a concrete recommended action.

The result: your architects get a defensible review package. Your CTO gets a clear executive summary. Your audit trail is complete. Reviews that took two weeks now take two hours."

### 2-minute pitch

"Let me describe a problem I see in every enterprise.

Architecture review is a bottleneck. A small group of senior architects reviews every major design. Reviews take weeks. Different architects apply different standards. Decisions are captured in email threads and slide decks that no one can find six months later. Compliance gaps surface during audits — or worse, in production incidents.

ArchLucid fixes this. Here is what it does.

You upload architecture evidence — diagrams, requirements, infrastructure notes, design documents. ArchLucid runs a structured multi-agent analysis: four specialized agents covering topology, cost, compliance, and design critique. The analysis runs in minutes and surfaces a prioritized findings board.

Each finding tells you: what the risk is, how severe it is, how confident the system is, what evidence it is based on, and what you should do about it. Not AI prose — a structured, evidence-linked, actionable finding.

This is the key question any buyer asks: can I trust this? ArchLucid is built for that. Every finding cites the evidence it used. Every recommendation traces back to a rule. The full decision chain is persisted in an append-only audit store. When your auditor asks 'who reviewed this, what did they find, and who approved it?' — ArchLucid has the answer.

For the executive, ArchLucid exports an executive summary: top risks, severity, business impact, recommended actions, readiness status. For the architect, it exports a full architecture review package — findings, evidence, rationale, decision trail.

Architecture review. Evidence-linked findings. An executive summary your CTO can act on.

That is ArchLucid. I would love to show you a 10-minute demo."

---

## 4. Key proof points from the codebase

These are factual claims grounded in what the repository ships today.

> **See it live, not on a slide:** the operator shell ships a built-in proof page at **`/why-archlucid`** (Core Pilot tier, no extra authority required). It calls `GET /v1/pilots/why-archlucid-snapshot`, `GET /v1/pilots/runs/{runId}/first-value-report`, and `GET /v1/explain/runs/{runId}/aggregate` against the seeded **Contoso Retail Modernization** demo tenant and renders live `ArchLucidInstrumentation` counters, the sponsor first-value report, and the run explanation + citations. Every claim in the table below should reconcile against what shows on that page after `pilot up` (or `POST /v1/demo/seed`).

> **Anonymous buyer self-qualification:** the public marketing site page **`/why`** links to **`GET /v1/marketing/why-archlucid-pack.pdf`** (via Next.js `/api/proxy/...`), which returns a single PDF sourced only from the same cached anonymous demo bundle as `GET /v1/demo/preview` — deterministic, no tenant data, and **404** (not 403) when `Demo:Enabled` is false. The PDF repeats the incumbent comparison with every competitive cell tied to `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` §2.1.

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
| Lead with the **buyer outcome**: "architecture risk review in minutes, findings your CTO can act on" | Lead with implementation: "multi-agent pipeline" or "10 finding engines" |
| Use buyer vocabulary: **risk, finding, recommended action, evidence, confidence, readiness** | Use internal vocabulary as first-impression words: "manifest", "run", "commit", "coordinator" |
| Say "AI Architecture Intelligence" when explaining the **category** | Say "AI-powered" as the headline — every tool says this now |
| Emphasize **evidence linkage**: every finding cites what it used | Claim "fully autonomous architecture design" — agents are orchestrated, not autonomous |
| Lead with **architecture review** — AI is the engine, not the promise | Over-promise on AI accuracy — frame findings as decision support, not legal attestation |
| Highlight the **executive summary export** — this is what gets budget approved | Position governance workflow as the first selling point (it is the second sale) |
| Position as **complementary** to existing EA tools (LeanIX, Ardoq), not a replacement | Position as a **replacement** for existing EA tools — different category |
| Be honest about V1 limitations (Azure-only, no import connectors yet) | Imply multi-cloud support or integrations that do not exist |
| Reference the **audit trail** for skeptical buyers: "every finding traces to evidence" | Lead with "AI" alone — every tool claims AI now |

---

## 8. Related documents

| Doc | Use |
|-----|-----|
| [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) | Competitor-by-competitor analysis and differentiation |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Who buys, why, and how to demo to them |
| [PRICING_PHILOSOPHY.md](PRICING_PHILOSOPHY.md) | **Locked list prices (2026)**, pilot pricing, re-rate gates, and sensitivity playbook — single source of truth for all price numbers |
| [../V1_SCOPE.md](../library/V1_SCOPE.md) | What V1 actually ships (grounding for all claims) |
| [../GLOSSARY.md](../library/GLOSSARY.md) | Domain terminology for consistent messaging |
| [../MARKETABILITY_ASSESSMENT_2026_04_15.md](../library/MARKETABILITY_ASSESSMENT_2026_04_15.md) | Full marketability quality assessment |
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust center — security overview, DPA template, subprocessors, incident comms, SOC 2 roadmap |
| [IDEAL_CUSTOMER_PROFILE.md](IDEAL_CUSTOMER_PROFILE.md) | ICP definition, scoring matrix, disqualifiers |
| [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md) | Available and planned integrations |
| [REFERENCE_NARRATIVE_TEMPLATE.md](REFERENCE_NARRATIVE_TEMPLATE.md) | Case study templates (3 fictional narratives) |

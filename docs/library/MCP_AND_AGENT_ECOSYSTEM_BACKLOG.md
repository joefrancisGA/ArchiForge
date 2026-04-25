> **Scope:** ArchLucid — MCP and agent-ecosystem backlog (ranked, version-aligned). One opinionated, version-aligned plan for how (and when) ArchLucid adopts Model Context Protocol (MCP), Responses-API-shaped agent abstractions, and tool approval classes, without compromising V1 determinism, RLS isolation, or the governance moat.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — MCP and agent-ecosystem backlog

**Audience:** product, architecture, security, and engineering leads who need one durable answer to *"should we adopt &lt;agent framework du jour&gt;?"* and to the broader question of how ArchLucid relates to the MCP / Responses-API era.

**Relationship:**

- [V1_SCOPE.md](V1_SCOPE.md) is the **V1 contract**. MCP is **out of V1** and a **V1.1 candidate** in **§3** (MCP row) — aligned with this backlog.
- [V1_DEFERRED.md](V1_DEFERRED.md) is the **doc inventory** of partial / V1.1+ stories; **§6d** records the **V1.1** MCP membrane commitment at the same level as other release-window pins.
- [adr/README.md](../adr/README.md) is the durable decision log. The accompanying ADR for the membrane is **`adr/0029-mcp-membrane-and-agent-ecosystem.md`** (draft).
- [ARCHITECTURE_ON_ONE_PAGE.md](../ARCHITECTURE_ON_ONE_PAGE.md), [dual-pipeline-navigator-superseded.md](../archive/dual-pipeline-navigator-superseded.md), [security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md) — the existing posture this plan must respect.

**Rules of the road:**

- This file ranks ideas; it does **not** authorize work. Promotion of a *V1.1 candidate* row into a sprint requires a program go-ahead **and** the corresponding ADR moving past *Draft*.
- "OpenClaw" and any other named third-party agent shell is treated as **unverified placeholder** until a maintainer, license, and repo URL are recorded here. The plan is intentionally framework-agnostic.
- Per workspace rules: Azure-native first; no SMB/445 exposure; private endpoints; deny-by-default; least privilege.
- Per workspace rules: do not introduce libraries, APIs, or services that are not explicitly verified — uncertainty is called out inline.

---

## 1. Objective

Decide how ArchLucid participates in the MCP / Responses-API / approval-class agent ecosystem without compromising the V1 contract or the audit / governance / RLS moat. Ship the smallest possible surface that earns ecosystem interop, and defer everything else.

The plan resolves to three architectural decisions:

1. **MCP is a membrane, not a runtime.** ArchLucid exposes (and later consumes) MCP through a thin façade that depends on `ArchLucid.Application`. ArchLucid never depends on MCP.
2. **Tool approval classes are first-class types.** A `ToolApprovalClass` enum lands in `ArchLucid.Contracts` and is enforced server-side, not by the LLM.
3. **The Responses API is a sibling of Chat Completions, not a replacement.** A new `IAgentToolLoop` abstraction sits behind `IAgentCompletionClient` and is implemented per provider; the existing `AzureOpenAiCompletionClient` continues to serve Chat Completions until a feature flag flips.

---

## 2. Assumptions

- The V1 contract in [V1_SCOPE.md](V1_SCOPE.md) is frozen. ADR 0021 forbids new coordinator-only HTTP surfaces; new external surfaces converge on Authority semantics or the unified read façade.
- Multi-vendor LLM remains a hard requirement. `ArchLucid.AgentRuntime/LlmProviderDescriptor.cs` already supports Azure OpenAI, Anthropic, Bedrock, OpenAI-compatible (Ollama / vLLM), and Offline (`Echo`) providers. Any agent-loop abstraction must preserve all five.
- SQL Server remains the system of record. RLS via `SESSION_CONTEXT` ([security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md)) is the tenant boundary for *every* read path, including new MCP tools.
- Existing governance primitives — `GovernanceApprovalRequest`, `GovernanceSelfApprovalException`, `GovernanceApprovalReviewConflictException`, `ApprovalSlaMonitor`, pre-commit governance gate — are reused, not replaced.
- The C# **MCP SDK** (`ModelContextProtocol`) is still pre-1.0 in the public ecosystem at the time of writing; *exact NuGet version is to be pinned at V1.1 kickoff and verified before commit*. Statement of uncertainty per workspace rule.
- The OpenAI **Assistants API** is on a deprecation path; the **Responses API** is the documented successor. Exact sunset dates must be re-verified against current OpenAI guidance before they are quoted in any pilot-facing doc.

---

## 3. Constraints

- **Security:** Entra ID JWT or API key, deny-by-default, RLS-scoped principal, no SMB/445, private endpoints only. Prompt-injection mitigations on `ContextIngestion` and `Retrieval` continue: retrieved content is data, never instructions; tool descriptions are not templated from retrieved content.
- **Determinism:** the authoritative commit/manifest path stays deterministic. Agent shells feed *proposals* into the existing draft / approval / governance queue. They do not bypass the pre-commit gate or self-approval block.
- **Cost:** every new tool-loop pattern reuses `LlmTokenQuotaWindowTracker`, `LlmCompletionAccountingClient`, `CachingAgentCompletionClient`, `CircuitBreakingAgentCompletionClient`, and adds a per-MCP-session cap.
- **Reuse:** new MCP tools map 1:1 to existing `ArchLucid.Application` services. No net-new business logic in the membrane layer.
- **Modularity:** each new class in its own file. LINQ over `foreach` unless performance dictates otherwise. Concrete types over `var`. Null checks always.
- **Audit:** every new external surface emits typed audit events. `AUDIT_COVERAGE_MATRIX.md` gains rows for each MCP tool class.

---

## 4. Architecture overview — the membrane model

### 4.1 Boundary diagram

```text
        ┌───────────────────────────────────────────────────────────────┐
        │   External agent shells (any: OpenAI Responses, Anthropic     │
        │   agent SDK, OpenHands, customer code, future "OpenClaw")     │
        └───────────────────────────┬───────────────────────────────────┘
                                    │ MCP (read-mostly, tenant-scoped)
                                    ▼
        ┌───────────────────────────────────────────────────────────────┐
        │   ArchLucid.Mcp.Server  (NEW — V1.1)                          │
        │   - stdio / Streamable HTTP                                   │
        │   - Entra ID JWT or API key                                   │
        │   - RLS principal propagation (SESSION_CONTEXT)               │
        │   - Tool approval class enforcement                           │
        │   - Typed audit events for every tool call                    │
        └───────────────────────────┬───────────────────────────────────┘
                                    │ in-proc / HTTP to ArchLucid.Api
                                    ▼
        ┌───────────────────────────────────────────────────────────────┐
        │   Authoritative pipeline (UNCHANGED)                          │
        │   Coordinator + Authority + Governance + Audit + Manifest +   │
        │   Provenance + RLS                                            │
        └───────────────────────────────────────────────────────────────┘
```

### 4.2 One-way dependency rule

`ArchLucid.Mcp.Server` depends on `ArchLucid.Application` and `ArchLucid.Contracts`. Nothing in the existing solution depends on the MCP SDK or any agent-shell package. This is the same pattern that keeps `ArchLucid.AgentRuntime` swappable across vendors and is the reason the membrane can be removed without affecting the authoritative path.

### 4.3 Dual-pipeline alignment

The membrane sits **outside** the persistence seam described in [dual-pipeline-navigator-superseded.md](../archive/dual-pipeline-navigator-superseded.md). It calls into the unified read façade for queries and into the Authority pipeline for proposal writes. It does **not** invoke the Coordinator string-run path directly. This keeps the membrane on the right side of [adr/0021-coordinator-pipeline-strangler-plan.md](../adr/0021-coordinator-pipeline-strangler-plan.md).

---

## 5. Component breakdown

| New / changed | Project | Purpose | Earliest version |
|---|---|---|---|
| `ToolApprovalClass` enum | `ArchLucid.Contracts/Governance/` | Declarative tool taxonomy: `ReadEvidence`, `AnalyzeDraft`, `ProposeChange`, `ExecuteRun`, `CommitManifest`, `AdminGovernance` | **V1 (types only, no runtime use)** |
| `IToolApprovalClassPolicy` + default impl | `ArchLucid.Application/Governance/` | Maps a tool class → required approval workflow / SoD / pre-commit gate behavior | V1.1 |
| `ArchLucid.Mcp.Contracts` (new csproj) | new | MCP DTOs, tool schemas, transport-agnostic types | V1.1 |
| `ArchLucid.Mcp.Server` (new csproj) | new | MCP server façade over `ArchLucid.Application` | V1.1 |
| Tenant-scoped MCP read tools | `ArchLucid.Mcp.Server/Tools/` | One class per tool: `GetRunStatusTool`, `GetManifestSummaryTool`, `CompareRunsTool`, `GetProvenanceGraphTool`, `GetGovernanceStatusTool`, `ListArtifactsTool`, `GetAuditSliceTool` | V1.1 |
| Audit event types for MCP | `ArchLucid.Core/Audit/AuditEventTypes.*.cs` | New typed events; `AUDIT_COVERAGE_MATRIX.md` gains rows | V1.1 |
| `IAgentToolLoop` abstraction | `ArchLucid.AgentRuntime/Tools/` | Vendor-neutral multi-turn tool-loop interface (Responses-API-shaped) | V1.2 |
| `AzureOpenAiResponsesCompletionClient` | `ArchLucid.AgentRuntime/Responses/` | Sibling of `AzureOpenAiCompletionClient`, behind feature flag `ArchLucid:Llm:UseResponsesApi` | V1.2 (feature-flagged) |
| Anthropic / Bedrock parity for `IAgentToolLoop` | `ArchLucid.AgentRuntime/Tools/` | Keep multi-vendor invariant of `LlmProviderDescriptor` | V1.2 |
| `ArchLucid.Mcp.Client` bridge | `ArchLucid.Mcp.Server/Client/` | Optional: ArchLucid as MCP **client** to a small allowlist of external read-only tool servers | V2 |
| Approval-interrupt loops & per-class autonomy budgets | `ArchLucid.AgentRuntime/Tools/` | Interrupt agent loop on a tool class threshold and route to `GovernanceApprovalRequest` | V2 |

Each new class lands in its own file per project rule. Every public type carries XML docs.

---

## 6. Data flow — MCP read tool, V1.1

1. External agent shell connects to `ArchLucid.Mcp.Server` over **stdio** (local pilot) or **Streamable HTTP** (hosted) — the latter only behind the existing private-endpoint posture.
2. Server authenticates the caller via the same code path as `ArchLucid.Api` — Entra ID JWT preferred, API key supported.
3. Server resolves tenant from claims and **propagates `SESSION_CONTEXT`** so RLS predicates apply identically to direct `ArchLucid.Api` calls.
4. Server resolves the requested tool, looks up its `ToolApprovalClass`, and applies `IToolApprovalClassPolicy`:
   - `ReadEvidence` → execute and return.
   - `AnalyzeDraft` → execute and return; result is marked *advisory*.
   - `ProposeChange` and above → write a draft proposal into the existing approval queue (`GovernanceApprovalRequest`) and return a **proposal id**, never an applied change.
5. Tool logic delegates to an `ArchLucid.Application` service. No raw SQL inside the membrane.
6. Server emits a typed audit event capturing `(tenantId, principal, toolName, toolApprovalClass, latencyMs, tokensIn, tokensOut, outcome, correlationId)`.
7. Token usage flows through `LlmCompletionAccountingClient` and the per-MCP-session cap; circuit breaker keys mirror the `OpenAiCircuitBreakerKeys` pattern.

---

## 7. Security model

- **No new public ports.** MCP-over-HTTP rides the same private endpoint as `ArchLucid.Api`. SMB/445 remains forbidden by workspace rule.
- **RLS first, MCP second.** The MCP server process must run as a tenant-scoped principal, not a god-mode service principal. There is no path through the membrane that escapes `SESSION_CONTEXT`.
- **Approval class is server-enforced.** Trusting an LLM to self-classify a tool's risk is the prompt-injection hole; classification lives on the tool registration, not in the prompt.
- **Tool descriptions are static.** They are never templated from retrieved content (which would let `Retrieval` results steer tool selection).
- **Secrets stay in `LlmProviderAuthScheme`.** No long-lived API keys are embedded in MCP tool definitions exposed to clients.
- **Outbound MCP (V2) is allowlisted.** When ArchLucid acts as an MCP client, it talks only to explicitly allowlisted servers, with their own approval class mapping.

This section satisfies the "every design must explicitly address security" rule.

---

## 8. Operational considerations

| Dimension | Stance | Evidence / control |
|---|---|---|
| **Scalability** | Membrane is stateless and horizontally scalable; bottleneck remains the SQL pipeline behind it. | Same hosting model as `ArchLucid.Api` per [adr/0001-hosting-roles-api-worker-combined.md](../adr/0001-hosting-roles-api-worker-combined.md). |
| **Reliability** | MCP server degrades to "API unavailable" cleanly; no stale tenant data on circuit-open. | Reuse `CircuitBreakingAgentCompletionClient`-style breaker; add `CircuitBreakerHealthCheck` rows for MCP. |
| **Cost** | Per-MCP-session token cap; reuse `LlmTokenQuotaWindowTracker`, `LlmCompletionResponseCacheOptions`. | Per [adr/0005-llm-completion-pipeline.md](../adr/0005-llm-completion-pipeline.md). |
| **Observability** | OTel spans per MCP tool call; metrics `archlucid.mcp.tool.calls`, `archlucid.mcp.tool.latency`, `archlucid.mcp.tool.tokens`, `archlucid.mcp.approval_class.outcome`. | Reuse `ArchLucidInstrumentation`. |
| **Supportability** | `archlucid doctor` and `support-bundle` extended to capture MCP session diagnostics; `/version` reports MCP SDK version. | Per [PILOT_GUIDE.md](PILOT_GUIDE.md). |
| **Audit / compliance** | One typed audit event per tool call; `AUDIT_COVERAGE_MATRIX.md` gains a new section. | Per [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md). |
| **Infrastructure-as-code** | New `ArchLucid.Mcp.Server` deployment lives in Terraform under `infra/`; no public ingress beyond existing private endpoints. | Per workspace IaC rule. |

---

## 9. Ranked backlog

The backlog is intentionally short. Promotion requires program go-ahead **and** the matching ADR moving past *Draft*.

### 9.1 Do now (V1, no scope change)

| # | Item | Effort | Owner hint | Definition of done |
|---|------|--------|------------|--------------------|
| 1 | Land **`ToolApprovalClass`** enum + XML docs in `ArchLucid.Contracts/Governance/` (no runtime behavior) | XS | Architecture | Type compiles, referenced from this doc, **not** wired into any code path. |
| 2 | Add **ADR draft** `adr/0029-mcp-membrane-and-agent-ecosystem.md` with status `Draft` | XS | Architecture | ADR exists, listed in `adr/README.md`, references this backlog. |
| 3 | Add a one-paragraph **non-goal** to [V1_SCOPE.md](V1_SCOPE.md) §3: *"Bespoke agent-shell runtime adoption (e.g., third-party agent frameworks)"* | XS | Product + Architecture | Sentence lands; future memos like this one have a stable home. |
| 4 | Add a row to [V1_DEFERRED.md](V1_DEFERRED.md) §1: *"MCP read façade — V1.1 candidate"* with link back to this backlog | XS | Product | Row present; pilot messaging stays consistent. |
| 5 | Capture this file in `docs/CHANGELOG.md` under the next change-set | XS | Docs | Entry present. |

These five items are pure documentation and type-only changes. They cost almost nothing and prevent the next OpenAI-style memo from landing in a vacuum.

### 9.2 V1.1 candidates (post-V1, requires program go-ahead)

| # | Item | Why now | Risk |
|---|------|---------|------|
| 6 | Build `ArchLucid.Mcp.Contracts` and `ArchLucid.Mcp.Server` with the seven read-only tools listed in §5 | Highest-leverage interop; maps 1:1 to existing services | Low — read-only; RLS unchanged |
| 7 | Implement `IToolApprovalClassPolicy` and wire it through `GovernanceApprovalRequest` | Establishes the seam for any future autonomy | Low — additive |
| 8 | Add typed MCP audit events; extend `AUDIT_COVERAGE_MATRIX.md` | Keeps the compliance narrative honest | Low |
| 9 | Pin the C# MCP SDK version in `Directory.Packages.props`; document in `docs/MCP_SURFACE.md` | Required before any commit | Medium — SDK is pre-1.0; verify before pinning |
| 10 | Extend `archlucid doctor` and `support-bundle` to cover MCP sessions | Supportability parity with `ArchLucid.Api` | Low |

### 9.3 V1.2 candidates

| # | Item | Why | Risk |
|---|------|-----|------|
| 11 | Introduce `IAgentToolLoop` abstraction in `ArchLucid.AgentRuntime/Tools/` | Vendor-neutral, Responses-API-shaped | Medium — must preserve `LlmProviderDescriptor` invariants |
| 12 | Add `AzureOpenAiResponsesCompletionClient` behind `ArchLucid:Llm:UseResponsesApi` | Migrate ahead of any Chat Completions / Assistants API churn | Medium — token cost and latency change |
| 13 | Anthropic + Bedrock parity for `IAgentToolLoop` | Multi-vendor invariant | Medium |
| 14 | Add structured-output JSON Schema response format on the Responses path | Stronger guarantees than `json_object`; fewer parse retries | Low |

### 9.4 V2 candidates

| # | Item | Why | Risk |
|---|------|-----|------|
| 15 | `ArchLucid.Mcp.Client` bridge to a small allowlist of external read-only MCP servers | Inbound interop is solved; outbound is the next frontier | Medium — outbound trust boundary |
| 16 | Approval-interrupt loops + per-class autonomy budgets | Lets ArchLucid host bounded multi-step agent work *without* breaking SoD | Medium-high |
| 17 | Reusable "task board" view over runs / stages / compare / replay surfaced via MCP | Aligns with operator-shell taskification trend | Medium |

### 9.5 Avoid entirely

| # | Anti-item | Why we will not do this |
|---|-----------|-------------------------|
| A | Adopt any third-party agent runtime ("OpenClaw" or otherwise) **inside** the authoritative commit/manifest path | Erodes determinism, RLS, audit, governance — the entire moat |
| B | Adopt the OpenAI **Assistants API** | On a deprecation path; do not cross the bridge twice |
| C | Pin OpenAI **model SKUs** in C# source | Breaks rotation; use Azure OpenAI deployment aliases (`archlucid-default-chat`, `archlucid-fast-chat`, `archlucid-embedding`) |
| D | Trust the LLM to self-classify a tool's approval class | Prompt-injection vector; classification is a property of the tool registration |
| E | Template MCP tool descriptions from `Retrieval` results | Lets retrieved content steer tool selection — same prompt-injection class |
| F | Marketplace / third-party agent stores | Out of scope per [V1_SCOPE.md](V1_SCOPE.md) §3 "speculative ecosystem" |
| G | Public-internet MCP transport without private-endpoint posture | Workspace security rule (private endpoints, no SMB/445) |

---

## 10. Cross-references to update when items promote

Promoting a row out of §9.1 into a sprint pulls a small ripple of doc updates. Listed once here so nothing is forgotten:

- [V1_SCOPE.md](V1_SCOPE.md) — §3 non-goals row when an item is *explicitly* deferred; new §2.x sub-section when an item ships.
- [V1_DEFERRED.md](V1_DEFERRED.md) — promote rows; remove on ship.
- [adr/README.md](../adr/README.md) — append `0029` (and any future MCP/agent ADRs) to the table.
- [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) — add MCP tool rows when audit events ship.
- [security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md) — note the MCP server principal pattern.
- [dual-pipeline-navigator-superseded.md](../archive/dual-pipeline-navigator-superseded.md) — confirm membrane sits outside the historical dual-path map.
- [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) — only when a customer-visible capability changes layer.
- `docs/MCP_SURFACE.md` (new with V1.1 §6) — full tool inventory, schemas, examples.
- `docs/TOOL_APPROVAL_CLASSES.md` (new with V1.1 §7) — taxonomy, mapping to governance workflow, examples.

---

## 11. Open questions

These are tracked here rather than in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) until program decides to promote any of them.

1. **Local vs hosted MCP first?** `stdio` for a developer/pilot demo is faster to ship; Streamable HTTP behind a private endpoint is what enterprise pilots will actually use.
2. **One MCP server per tenant or one shared with strict `SESSION_CONTEXT`?** Default plan is shared with strict scoping; revisit if a high-sensitivity pilot requires hard process isolation.
3. **Do `AnalyzeDraft` results get persisted as draft artifacts, or returned ephemerally?** Persistence simplifies audit but inflates storage and complicates retention; ephemeral is cheaper but harder to investigate after the fact.
4. **Approval-class taxonomy stability.** Is the six-class taxonomy in §5 the right granularity, or should `CommitManifest` split into `CommitManifest:NoBreakingChange` and `CommitManifest:BreakingChange`?
5. **MCP SDK risk.** Pre-1.0 churn vs. the cost of waiting for a 1.0 stable line. Decision should land in ADR 0029 before any V1.1 work begins.
6. **OpenAI deprecation timeline.** Re-verify the actual Assistants API sunset date against current OpenAI guidance before any pilot-facing message references it.

---

## 12. Change control

- This file is mutable. ADR 0029 (and any successor ADRs) are immutable once accepted.
- When a §9 row promotes, update §9 status and the ripple list in §10 in the same change-set.
- When OpenAI, MCP, or vendor-side guidance changes materially, add a dated subsection to §11 instead of editing prior assertions.

**Last reviewed:** 2026-04-21

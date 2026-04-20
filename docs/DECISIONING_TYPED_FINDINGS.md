> **Scope:** Typed findings in ArchLucid.Decisioning - full detail, tables, and links in the sections below.

## Typed findings in ArchLucid.Decisioning

ArchLucid.Decisioning uses a **Finding envelope** with **category-specific typed payloads**. This preserves a stable persisted shape while allowing engines and the decision engine to evolve with strongly typed data.

---

### Finding envelope

`ArchLucid.Decisioning.Models.Finding` includes:

- `FindingType` – rule matching key (e.g. `RequirementFinding`, `TopologyGap`)  
- `Category` – high-level domain grouping (e.g. `Requirement`, `Topology`, `Security`, `Cost`)  
- `Payload` – category/finding-type specific payload object (stored as `object`)  
- `PayloadType` – discriminator (e.g. `RequirementFindingPayload`)  

The rest of the envelope is durable metadata: severity, title/rationale, recommended actions, related graph nodes, and explainability trace.

---

### Typed payloads

Payload models live under:

`ArchLucid.Decisioning/Findings/Payloads/`

Currently included:

- `RequirementFindingPayload`
- `TopologyGapFindingPayload`
- `SecurityControlFindingPayload`
- `CostConstraintFindingPayload`
- `PolicyApplicabilityFindingPayload`
- `TopologyCoverageFindingPayload`
- `SecurityCoverageFindingPayload`
- `PolicyCoverageFindingPayload`
- `RequirementCoverageFindingPayload`

Engines should set:

- `finding.Category`
- `finding.PayloadType = nameof(ThePayloadType)`
- `finding.Payload = new ThePayloadType { ... }`

---

### Creating findings (recommended)

Use `ArchLucid.Decisioning.Findings.Factories.FindingFactory` for consistent creation:

- `CreateRequirementFinding(...)`
- `CreateTopologyGapFinding(...)`
- `CreatePolicyApplicabilityFinding(...)` / `CreatePolicyApplicabilityGapFinding(...)`

This ensures the correct `FindingType`, `Category`, `PayloadType`, and payload shape are always emitted.

---

### Rehydrating payloads

Because `Finding.Payload` is stored as `object`, persisted/reloaded findings may deserialize payloads as `JsonElement`.

Use:

`ArchLucid.Decisioning.Findings.Factories.FindingPayloadConverter`

Examples:

- `FindingPayloadConverter.ToRequirementPayload(finding)`
- `FindingPayloadConverter.ToTopologyGapPayload(finding)`
- `FindingPayloadConverter.ToSecurityControlPayload(finding)`
- `FindingPayloadConverter.ToCostConstraintPayload(finding)`
- `FindingPayloadConverter.ToPolicyApplicabilityPayload(finding)`

---

### Graph-aware finding engines

Several engines use **`ArchLucid.KnowledgeGraph.Models.GraphSnapshotExtensions`** over typed edges:

| Engine | Graph usage (examples) |
|--------|-------------------------|
| **`RequirementFindingEngine`** | **`RELATES_TO`** → expands **`RelatedNodeIds`** / trace |
| **`SecurityBaselineFindingEngine`** | **`PROTECTS`** → related topology node IDs |
| **`PolicyApplicabilityFindingEngine`** | **`APPLIES_TO`** → info vs gap (**Warning** when topology exists but no links) |
| **`TopologyCoverageFindingEngine`** | **`IGraphCoverageAnalyzer.AnalyzeTopology`** — category coverage vs network/compute/storage/data |
| **`SecurityCoverageFindingEngine`** | **`PROTECTS`** edges vs topology resources |
| **`PolicyCoverageFindingEngine`** | **`APPLIES_TO`** / policy node presence |
| **`RequirementCoverageFindingEngine`** | **`RELATES_TO`** from requirements to topology |

See **`docs/KNOWLEDGE_GRAPH.md`** for how those edges are produced.

---

### Category-aware finding engines

`IFindingEngine` now declares:

- `EngineType`
- `Category`

The orchestrator enforces:

- If an engine returns a finding with an empty `Category`, it is auto-filled from `engine.Category`.
- If a finding’s category does not match `engine.Category`, the orchestrator throws.

This prevents accidental cross-category emissions and keeps filtering consistent.

---

### Payload validation (optional hardening, enabled)

`IFindingPayloadValidator` validates findings before persistence.

Default implementation:

`FindingPayloadValidator`

Validations include:

- Required fields: `FindingType`, `Category`, `EngineType`
- Consistency: `PayloadType` must not be set when `Payload` is null
- Typed payload checks for known finding types

---

### Golden manifest population (Decisioning)

**`DefaultGoldenManifestBuilder`** (not `RuleBasedDecisionEngine`) maps findings + graph into **`GoldenManifest`**:

- **Requirements** — `RequirementFinding` → coverage + decisions.
- **Topology** — `TopologyGap` → gaps, warnings, unresolved issues; **plus** `TopologyResource` **labels** from **`GraphSnapshot`** (`PopulateTopologyFromGraph`).
- **Security** — `SecurityControlFinding` → controls, gaps, remediation decisions when `Status=missing`.
- **Cost** — `CostConstraintFinding` → max monthly cost and risk list.
- **Policy** — `PolicyApplicabilityFinding` → **Info**: assumptions (applicability count); **Warning**: warnings + `UnresolvedIssues` (`PolicyApplicabilityGap`).

**`RuleBasedDecisionEngine`** evaluates **`InMemoryDecisionRuleProvider`** rules against **`FindingType`** (accept/reject/trace) before the manifest is built; it does not deserialize payloads for manifest sections.

---

### Persistence note (dev / in-memory)

`InMemoryFindingsSnapshotRepository` stores snapshots as JSON strings and rehydrates them on read. This simulates durable storage and ensures payloads round-trip through JSON (including `JsonElement` cases), so the converter paths are exercised early.


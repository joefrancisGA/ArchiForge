> **Scope:** Agent output structural evaluation - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Agent output structural evaluation

## 1. Objective

Provide a **cheap, deterministic** check that persisted agent **`AgentExecutionTrace.ParsedResultJson`** still looks like a serialized **`AgentResult`**: correct JSON root shape and expected **top-level property names** (camelCase, matching **`JsonSerializerDefaults.Web`**). Support **on-demand HTTP inspection** per run and **optional OTEL metrics** for batch or post-run jobs—without calling an LLM.

## 2. Assumptions

- **Traces** store **`ParsedResultJson`** only when **`ParseSucceeded`** is true (handlers serialize the validated **`AgentResult`**).
- **Schema validation** already ran at execution time; this layer catches **drift**, **manual SQL edits**, or **future serializer changes** that leave traces readable but structurally incomplete.
- **Metrics emission** is triggered explicitly via **`AgentOutputEvaluationRecorder`** (not yet wired into the authority pipeline executor by default).

## 3. Constraints

- **CI fixture guard:** `scripts/ci/assert_agent_reference_baselines.py` (wired in `.github/workflows/ci.yml`) validates committed golden JSON under `ArchLucid.AgentRuntime.Tests/Fixtures/GoldenAgentResults/` listed in `scripts/ci/agent-reference-baselines.json`. Extend that array when adding new golden files.
- **No new external services**; only **`System.Text.Json`** and existing repositories.
- **Privacy**: evaluation reads **already-persisted** trace JSON; the **GET** endpoint requires the same **read authority** policy as other run reads.
- **Cardinality**: metric labels use **`agent_type`** only (four values).

## 4. Architecture Overview

```mermaid
flowchart LR
  subgraph api [ArchLucid.Api]
    RC[RunsController]
  end
  subgraph runtime [ArchLucid.AgentRuntime]
    EV[IAgentOutputEvaluator]
    AE[AgentOutputEvaluator]
    REC[AgentOutputEvaluationRecorder]
  end
  subgraph data [Persistence]
    TR[IAgentExecutionTraceRepository]
  end
  subgraph obs [Observability]
    M[ArchLucidInstrumentation]
  end
  RC --> TR
  RC --> EV
  RC --> SEM[IAgentOutputSemanticEvaluator]
  EV --> AE
  SEM --> ASE[AgentOutputSemanticEvaluator]
  REC --> TR
  REC --> EV
  REC --> SEM
  REC --> M
```

## 5. Component Breakdown

| Component | Responsibility |
|-----------|------------------|
| **`IAgentOutputEvaluator`** | Pure **`Evaluate(traceId, json, agentType)`** → **`AgentOutputEvaluationScore`**. |
| **`AgentOutputEvaluator`** | Expected key list for **`AgentResult`** JSON; parse; ratio = present / expected. |
| **`AgentOutputEvaluationRecorder`** | Load traces by **`runId`**; score; emit **`archlucid_agent_output_*`**; log low scores. |
| **`AgentOutputSemanticScore`** | Contract DTO for semantic ratios (also returned nested on each API score row). |
| **`AgentOutputEvaluationScore` / `AgentOutputEvaluationSummary`** | Contracts for API and tests. |
| **`GET …/run/{runId}/agent-evaluation`** | Structural + semantic scores per trace, **`blobUploadFailed`** from the trace row, and **`averageSemanticScore`** — does **not** record OTel metrics. |
| **`IAgentOutputEvaluationHarness` / `AgentOutputEvaluationHarness`** | Test-time and offline composition of structural + semantic evaluators with **`AgentOutputExpectation`** (min scores, required JSON keys, finding categories). |
| **`ISemanticScorer`** | Placeholder seam for **embedding-based** similarity vs reference text (not wired in DI today); current semantic path remains **`IAgentOutputSemanticEvaluator`**. |

## 6. Data Flow

1. **API**: **`GetByRunIdAsync`** → for each eligible trace, structural **`Evaluate`**, then **`IAgentOutputSemanticEvaluator.Evaluate`** (when structural parse is OK); set **`BlobUploadFailed`** from the trace; aggregate **`AverageStructuralCompletenessRatio`** and **`AverageSemanticScore`**; skipped count for traces without parsed JSON.
2. **Metrics job** (future caller): **`EvaluateAndRecordMetricsAsync(runId)`** → same loop → **`Histogram.Record`** / **`Counter.Add`** with **`agent_type`** tag.
3. **Parse failure** (invalid JSON or non-object root): **`IsJsonParseFailure`** true; metrics path increments **`archlucid_agent_output_parse_failures_total`** (no histogram point).

## 7. Security Model

- **Authorization**: **`ReadAuthority`** on **`RunsController`** (same as **`GET …/traces`**).
- **Data exposure**: Response includes **missing key names** and **scores** only—no raw prompts. Traces already scoped by **run repository** / **RLS** as elsewhere.
- **Abuse**: Rate limiting inherits controller **`fixed`** window; evaluation is CPU-only over in-memory JSON strings.

## Semantic evaluation

Beyond structural completeness, **`AgentOutputSemanticEvaluator`** performs a deeper deterministic inspection of the agent JSON output without calling an LLM.

### What it checks

| Dimension | Scoring rule |
|-----------|-------------|
| **Claims quality** | Fraction of items in `claims[]` that have non-empty `evidenceRefs[]` or a non-empty `evidence` string. |
| **Findings quality** | Fraction of items in `findings[]` with non-empty `severity`, `description` (>10 chars), and `recommendation` (>5 chars). |
| **Overall score** | Weighted average: Claims × **0.4** + Findings × **0.6**. When only one dimension is present, that dimension's ratio is the overall score. Zero when both arrays are absent or empty. |

### OTel metric

**`archlucid_agent_output_semantic_score`** (histogram, 0.0–1.0; label `agent_type`) — recorded alongside the structural histogram by **`AgentOutputEvaluationRecorder`**.

### Warning threshold

**`AgentOutputEvaluationRecorder`** logs a warning when **`OverallSemanticScore < 0.3`** (critical semantic emptiness). Structural low-score warnings still use **0.5**. The optional quality gate uses separate floors from **`AgentOutputQualityGateOptions`**.

### Interface

```csharp
IAgentOutputSemanticEvaluator.Evaluate(traceId, parsedResultJson, agentType) → AgentOutputSemanticScore (type lives in **`ArchLucid.Contracts.Agents`**).
```

Registered as **singleton** (`IAgentOutputSemanticEvaluator → AgentOutputSemanticEvaluator`).

## Quality gate (optional in Development)

**`IAgentOutputQualityGate`** classifies structural + semantic scores into **accepted / warned / rejected** using **`ArchLucid:AgentOutput:QualityGate`** (`AgentOutputQualityGateOptions`).

- **Shipped default (`ArchLucid.Api/appsettings.json`):** **`Enabled: true`** with **`StructuralWarnBelow`**, **`SemanticWarnBelow`**, **`StructuralRejectBelow`**, **`SemanticRejectBelow`** thresholds so production-like runs record gate outcomes on the evaluation path.
- **Local Development (`appsettings.Development.json`):** **`Enabled: false`** so prompt and handler iteration is not slowed by gate warnings.
- **`appsettings.Advanced.json`:** does not need to repeat the block unless you override thresholds per environment.

When enabled, **`AgentOutputEvaluationRecorder`** increments **`archlucid_agent_output_quality_gate_total`** (labels `agent_type`, `outcome`) and logs **warn** for **warned**/**rejected** outcomes. The gate does **not** change persisted traces or block merges by itself.

## Golden-set trace fixtures (regression)

**`ArchLucid.AgentRuntime.Tests`** includes **`GoldenAgentExecutionTraceTests`**, which load **`Fixtures/AgentExecutionTrace/*.json`** and assert:

- **`ModelDeploymentName`** / **`ModelVersion`** match expected values (including **`AgentExecutionTraceModelMetadata`** sentinels for simulator paths).
- **`ParseSucceeded`** and presence of **`ParsedResultJson`** where the fixture models a successful parse.

**Agent result JSON (parsed output shape):** **`GoldenAgentResultJsonEvaluationTests`** loads **`Fixtures/GoldenAgentResults/*.json`** through **`AgentOutputEvaluator`** and **`AgentOutputSemanticEvaluator`**. The pair **`golden-agent-result-valid.json`** vs **`golden-agent-result-claim-without-evidence.json`** guards regressions where **`claims[].evidenceRefs`** (or non-empty **`evidence`**) is removed but findings remain complete — semantic **`OverallSemanticScore`** must drop.

**Harness + round-trip JSON:** **`AgentOutputEvaluationHarnessGoldenFixtureTests`** deserializes **`harness-agent-result-topology.json`** and **`harness-agent-result-compliance.json`** to **`AgentResult`** (web JSON), then runs **`IAgentOutputEvaluationHarness.Evaluate`** with structural floors. A third test loads the topology fixture and clears **`Findings`** to assert the harness fails **`MinimumFindingCount`**.

Add a new JSON file per scenario (minimal fields only); keep fixtures **small** and **non-sensitive** (no customer text, no secrets).

## 8. Operational Considerations

- **Full prompts**: Blob upload (plus SQL **`Full*Inline`** fallback) runs after trace insert for **Real** execution; **Simulator** skips full-text blob/inline; see **`docs/AGENT_TRACE_FORENSICS.md`**. Optional JSON reference cases: **`AgentExecution:ReferenceEvaluation`** → **`archlucid_agent_output_reference_case_*`** metrics and **`dbo.AgentOutputEvaluationResults`**.
- **Dashboards**: **`archlucid_agent_output_structural_completeness_ratio`** (histogram), **`archlucid_agent_output_semantic_score`** (histogram), **`archlucid_agent_output_parse_failures_total`** (counter), and optional **`archlucid_agent_output_quality_gate_total`** (counter)—see **`docs/OBSERVABILITY.md`**.
- **Low score logs**: Recorder warns below **0.5** completeness for both structural and semantic scores (configurable in code if product asks).
- **Evolution**: Per-**`AgentType`** key lists live in **`GetExpectedKeys`** for future stricter Topology/Cost/Critic profiles.

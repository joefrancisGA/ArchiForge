> **Scope:** Explainability trace coverage - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Explainability trace coverage

## Objective

`ExplainabilityTrace` on each `Finding` records how the engine justified its output (`GraphNodeIdsExamined`, `RulesApplied`, `DecisionsTaken`, `AlternativePathsConsidered`, `Notes`). This document describes how completeness is measured, where it appears in advisory scans, and what engine authors should populate.

## Trace field coverage matrix (rule-based engines)

Target: **3/5** or **4/5** per finding, or **5/5** when `AlternativePathsConsidered` lists concrete remediation branches (rule engines may still populate short strings; LLM engines may add richer branches).

| Engine | GraphNodeIdsExamined | RulesApplied | DecisionsTaken | AlternativePathsConsidered | Notes | Typical ratio |
|--------|----------------------|-------------|----------------|---------------------------|-------|---------------|
| RequirementFindingEngine | yes | yes (`requirement-surface`) | yes | — | yes (related count, text length) | 4/5 |
| ComplianceFindingEngine | yes | yes (rule id) | yes | sentinel (`ExplainabilityTraceMarkers.RuleBasedDeterministicSinglePathNote`) | yes (rule pack) | 5/5 |
| SecurityBaselineFindingEngine | yes | yes (`security-baseline-coverage`) | yes | — | yes (PROTECTS count) | 4/5 |
| CostConstraintFindingEngine | yes | yes (`cost-constraint-surface`) | yes | — | yes (budget cap) | 4/5 |
| TopologyCoverageFindingEngine | empty when no topology; else all topology node ids | yes (`topology-coverage-presence` / `topology-coverage-categories`) | yes | yes (three concrete ingest / projection / scope alternatives per branch) | yes (expected categories / present+missing) | 5/5 when emitted |
| SecurityCoverageFindingEngine | yes (unprotected resource ids from analyzer) | yes (`security-coverage-protection`) | yes | yes (three concrete baseline / scope / compensating-control strings when unprotected resources exist) | yes (counts) | 5/5 when emitted |
| PolicyApplicabilityFindingEngine | via `FindingFactory` | yes (`policy-applicability-mapping` / `policy-applicability-gap`) | yes | — | yes (target count / policy label) | 4/5 |
| PolicyCoverageFindingEngine | when uncovered | yes (`policy-coverage-presence` / `policy-coverage-applicability`) | yes | — | yes (counts) | 3/5 (no policies) or 4/5 |
| RequirementCoverageFindingEngine | when uncovered | yes (`requirement-coverage-relation`) | yes | — | yes (totals) | 4/5 |
| Topology gap findings (`FindingFactory.CreateTopologyGapFinding`) | yes | yes (`topology-gap-{gapCode}`) | yes | — | — | 3/5 |

`AlternativePathsConsidered` is optional: many rule engines leave it empty. **Compliance** records a deterministic single-path sentinel; **topology coverage** and **security coverage** populate short, operator-facing remediation branches when the finding fires. Analyzers treat a list as populated when it contains at least one non-whitespace string (`ExplainabilityTraceCompletenessAnalyzer`).

## ExplainabilityTraceCompletenessAnalyzer

Location: `ArchLucid.Decisioning/Findings/ExplainabilityTraceCompletenessAnalyzer.cs`.

- **`AnalyzeFinding(Finding)`** returns `TraceCompletenessScore`: booleans per field, `PopulatedFieldCount` (0–5), and `CompletenessRatio` (0.0–1.0). A list counts as populated only if it contains at least one non-whitespace string.
- **`AnalyzeSnapshot(FindingsSnapshot)`** returns `TraceCompletenessSummary`: `TotalFindings`, `OverallCompletenessRatio` (mean of per-finding ratios), and `ByEngine` with per-engine `FindingCount`, mean `CompletenessRatio`, and counts of findings where each field is populated.

Pure functions: no I/O, suitable for tests and batch reporting.

### Property-based tests (FsCheck)

**`ExplainabilityTraceCompletenessAnalyzerPropertyTests`** (in **`ArchLucid.Decisioning.Tests`**) use **FsCheck** to assert invariants such as: populated list fields increase **`PopulatedFieldCount`**; **`CompletenessRatio`** stays in **[0, 1]**; and **`AnalyzeSnapshot`** mean ratios match the arithmetic mean of per-finding ratios. See **`docs/TEST_STRUCTURE.md`** (Tier 1 / property tests).

## Advisory scan `ResultJson`: `traceCompleteness`

On successful completion of `AdvisoryScanRunner` when at least one authority run exists, `AdvisoryScanExecution.ResultJson` includes:

- `schemaVersion`: `1`
- Existing fields: `runId`, `comparedToRunId`, `recommendationCount`, `digestId`, alert counts, etc.
- **`traceCompleteness`**:
  - `totalFindings`
  - `overallCompletenessRatio`
  - `byEngine[]`: `engineType`, `findingCount`, `completenessRatio`, `graphNodeIdsPopulatedCount`, `rulesAppliedPopulatedCount`, `decisionsTakenPopulatedCount`, `alternativePathsPopulatedCount`, `notesPopulatedCount`

`GET /v1/advisory/schedules/{id}/executions` returns these rows unchanged; consumers parse `ResultJson` as JSON.

## Explanation faithfulness (aggregate summary — heuristic)

When **`GET /v1/explain/runs/{runId}/aggregate`** builds a **`RunExplanationSummary`**, **`ExplanationFaithfulnessChecker`** compares **word-like tokens** from the **`ExplanationResult`** text fields to a flattened corpus of **`ExplainabilityTrace`** strings on persisted findings. The result is **not** semantic entailment; it is a **coarse** signal for regressions after prompt or engine changes.

- **Implementation:** `ArchLucid.Decisioning.Findings.ExplanationFaithfulnessChecker`, `IExplanationFaithfulnessChecker` (registered in host **Decisioning** DI).
- **OpenTelemetry:** `archlucid_explanation_faithfulness_ratio` (histogram 0.0–1.0), recorded when at least one token was checked (**`ClaimsChecked` > 0**). See **`docs/OBSERVABILITY.md`**.
- **Fallback counter:** `archlucid_explanation_aggregate_faithfulness_fallback_total` increments when the aggregate run explanation substitutes deterministic narrative for LLM output after a low faithfulness score (`RunExplanationSummaryService`).

### Aggregate faithfulness fallback — SLO budget (Prometheus)

- **Recording rule:** `archlucid:slo:explanation_faithfulness_fallback_budget` in **`infra/prometheus/archlucid-slo-rules.yml`** (group **`archlucid-slo-explanation-faithfulness`**). The value is a maximum sustained **rate** over a **`1h`** range window: `rate(counter[1h]) ≈ (increments in the last hour) / 3600`. The shipped default is **`3 / 3600`** events per second, aligning with the count-based **`ArchLucidExplanationFaithfulnessFallbackTrend`** guard (`increase(...[1h]) > 3`).
- **Budget alert:** **`ArchLucidExplanationFaithfulnessFallbackBudgetExceeded`** in **`infra/prometheus/archlucid-alerts.yml`** fires when `rate(archlucid_explanation_aggregate_faithfulness_fallback_total[1h])` exceeds that recording rule for **`30m`**. Tune the recording rule per environment if you intentionally rely on deterministic aggregate text more often.

## Structured per-finding explainability (`FindingExplainabilityEvidence`)

**`GET /v1/explain/runs/{runId}/findings/{findingId}/explainability`** returns **`FindingExplainabilityResult`**, including:

- **`evidence`:** deterministic **`FindingExplainabilityEvidence`** (`evidenceRefs`, `conclusion` from persisted finding rationale, `alternativePathsConsidered`, `ruleId` from trace rule ids). This object is always produced server-side from stored findings + trace; it is never sourced from LLM output.
- **`narrativeText`:** presentation-only plain text composed from the same trace fields (still no live LLM call on this route).

## OpenTelemetry metric

- **Name:** `archlucid_explainability_trace_completeness_ratio`
- **Type:** histogram (double)
- **Description:** Per-scan trace completeness ratio (0.0–1.0).
- **Label:** `scan_type` = `advisory` (recorded from `AdvisoryScanRunner` after summarizing the findings snapshot).

**Grafana:** chart the histogram or derived average over `scan_type` to detect regressions when engines stop filling traces. Combine with advisory scan success rate to avoid misreading failed runs.

## Recommendations for engine authors

1. **`DecisionsTaken`:** Always add at least one short, human-readable decision string (what was compared, what branch fired).
2. **`GraphNodeIdsExamined`:** List graph node ids that drove the finding when applicable.
3. **`RulesApplied`:** Use stable logical rule ids for implicit or explicit rules (e.g. `requirement-surface`, or compliance `ruleId`).
4. **`Notes`:** Optional structured hints (counts, caps, pack versions) that help operators and downstream LLMs.
5. **`AlternativePathsConsidered`:** Use either (a) concrete remediation branches when multiple credible resolutions exist, or (b) the shared deterministic sentinel when the engine is strictly single-path; omit empty lists rather than filler text.

Do not change the `ExplainabilityTrace` property names or types on persisted findings without a schema migration.

## Future: embedding-based faithfulness (design note, not V1)

Today’s **aggregate** faithfulness signal is **token overlap** between explanation text and flattened finding trace strings (`ExplanationFaithfulnessChecker`). A stronger signal would use **embedding cosine similarity** between (a) chunked explanation sentences and (b) a corpus built from trace fields.

| Topic | Proposal |
|--------|-----------|
| **Model** | Azure OpenAI **`text-embedding-3-small`** (or equivalent) via existing `IOpenAiEmbeddingClient` to avoid a new vendor. |
| **Cost** | One batch embed per aggregate request when findings exist — bounded by chunk count; monitor with existing LLM token / call metrics. |
| **Where to compute** | Async post-step after `ExplainRunAsync` **or** background advisory job — avoid blocking the hot HTTP path until latency budgets allow. |
| **Fallback** | Keep token overlap as a cheap default; use embeddings only when `ArchLucid:Explanation:Aggregate:EmbeddingFaithfulnessEnabled` (hypothetical) is true. |

This is **V2+** scope: requires caching, PII review (embeddings of customer text), and SLO validation against latency.

## UI: provenance graph

The operator UI (`archlucid-ui`) renders coordinator provenance with `ProvenanceGraphDiagram`: layered SVG (snapshots → findings → decisions → manifest → artifacts), click-to-scroll to the nodes table, and a type/color legend. No extra npm dependencies.

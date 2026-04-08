# Explainability trace coverage

## Objective

`ExplainabilityTrace` on each `Finding` records how the engine justified its output (`GraphNodeIdsExamined`, `RulesApplied`, `DecisionsTaken`, `AlternativePathsConsidered`, `Notes`). This document describes how completeness is measured, where it appears in advisory scans, and what engine authors should populate.

## Trace field coverage matrix (baseline audit)

| Engine | GraphNodeIdsExamined | RulesApplied | DecisionsTaken | AlternativePathsConsidered | Notes |
|--------|----------------------|-------------|----------------|---------------------------|-------|
| RequirementFindingEngine | yes | yes (`requirement-surface`) | yes | — | — |
| ComplianceFindingEngine | yes | yes (rule id) | yes | — | yes (rule pack) |
| SecurityBaselineFindingEngine | yes | yes (`security-baseline-coverage`) | yes | — | yes (PROTECTS count) |
| CostConstraintFindingEngine | yes | yes (`cost-constraint-surface`) | yes | — | yes (budget cap) |
| TopologyCoverageFindingEngine | — / partial | — | yes (both branches) | — | — |
| SecurityCoverageFindingEngine | — | — | yes | — | — |
| PolicyApplicabilityFindingEngine | via factory | — | via factory | — | — |
| PolicyCoverageFindingEngine | when uncovered | — | yes | — | — |
| RequirementCoverageFindingEngine | when uncovered | — | yes | — | — |

`AlternativePathsConsidered` is intentionally not required for rule-based engines in v1; analyzers report it as empty until LLM-style engines can justify branches.

## ExplainabilityTraceCompletenessAnalyzer

Location: `ArchLucid.Decisioning/Findings/ExplainabilityTraceCompletenessAnalyzer.cs`.

- **`AnalyzeFinding(Finding)`** returns `TraceCompletenessScore`: booleans per field, `PopulatedFieldCount` (0–5), and `CompletenessRatio` (0.0–1.0). A list counts as populated only if it contains at least one non-whitespace string.
- **`AnalyzeSnapshot(FindingsSnapshot)`** returns `TraceCompletenessSummary`: `TotalFindings`, `OverallCompletenessRatio` (mean of per-finding ratios), and `ByEngine` with per-engine `FindingCount`, mean `CompletenessRatio`, and counts of findings where each field is populated.

Pure functions: no I/O, suitable for tests and batch reporting.

## Advisory scan `ResultJson`: `traceCompleteness`

On successful completion of `AdvisoryScanRunner` when at least one authority run exists, `AdvisoryScanExecution.ResultJson` includes:

- `schemaVersion`: `1`
- Existing fields: `runId`, `comparedToRunId`, `recommendationCount`, `digestId`, alert counts, etc.
- **`traceCompleteness`**:
  - `totalFindings`
  - `overallCompletenessRatio`
  - `byEngine[]`: `engineType`, `findingCount`, `completenessRatio`, `graphNodeIdsPopulatedCount`, `rulesAppliedPopulatedCount`, `decisionsTakenPopulatedCount`, `alternativePathsPopulatedCount`, `notesPopulatedCount`

`GET /v1/advisory/schedules/{id}/executions` returns these rows unchanged; consumers parse `ResultJson` as JSON.

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
5. **`AlternativePathsConsidered`:** Reserve for engines that evaluate multiple branches; omit rather than invent placeholder text.

Do not change the `ExplainabilityTrace` property names or types on persisted findings without a schema migration.

## UI: provenance graph

The operator UI (`archlucid-ui`) renders coordinator provenance with `ProvenanceGraphDiagram`: layered SVG (snapshots → findings → decisions → manifest → artifacts), click-to-scroll to the nodes table, and a type/color legend. No extra npm dependencies.

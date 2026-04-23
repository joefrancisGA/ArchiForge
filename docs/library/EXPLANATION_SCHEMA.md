> **Scope:** Structured explanation schema - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Structured explanation schema

## Overview

Run-level explanations (`ExplanationResult`) include a `structured` object of type `StructuredExplanation` so clients can consume **reasoning**, **evidence references**, **confidence**, and **caveats** without scraping free-text. The LLM is asked to return JSON in this shape; when it does not, the server wraps the raw response so the pipeline **never fails** on malformed output.

HTTP responses return `ExplanationResult` as JSON (see `ExplanationController`). The `rawText` field holds the LLM completion after fence unwrapping for auditing.

## Fields

| JSON field | Type | Required | Description |
|------------|------|----------|-------------|
| `schemaVersion` | number | Default `1` | Schema version for forward compatibility. |
| `reasoning` | string | Yes (for structured responses) | Primary explanatory text. |
| `evidenceRefs` | string[] | No (default `[]`) | Provenance or decision IDs cited. |
| `confidence` | number \| null | No | Value in `[0, 1]` when present; server clamps out-of-range values. |
| `alternativesConsidered` | string[] \| null | No | Other options evaluated. |
| `caveats` | string[] \| null | No | Limitations or assumptions. |

Legacy LLM responses using `summary` and `detailedNarrative` are still supported; the service maps them into `summary`, `detailedNarrative`, and a `structured` envelope whose `reasoning` matches the narrative body.

## Versioning strategy

- Today `schemaVersion` is **1**.
- **Additive** optional fields do not require a version bump.
- **Removing or renaming** a field should bump the version and be documented here.
- Clients should ignore unknown JSON properties and branch on `schemaVersion` when breaking changes ship.

## Handling null fields

- `confidence: null` — no score was produced; avoid showing “0%”; show “not available” or omit the widget.
- `alternativesConsidered: null` — the pipeline did not report alternatives (distinct from `[]`, meaning none after evaluation).
- `caveats: null` — same pattern as alternatives.

## Fallback behavior

If the model returns **plain prose** (not JSON), or JSON without a non-empty `reasoning`:

- `structured.reasoning` is set to the trimmed raw text (or to a deterministic heuristic narrative when the model returns nothing).
- Optional fields are null or empty collections as applicable.
- `schemaVersion` on parsed structured payloads defaults to **1** when omitted.

When the model returns JSON that parses as an object but does not match the structured schema or legacy keys, the service uses heuristic summary/narrative text and sets `structured.reasoning` to that narrative (not the raw JSON string).

## Aggregate run explanation

The **aggregate** response wraps the same run-level **`ExplanationResult`** as granular explain, plus executive rollups for dashboards and the operator run detail page.

### HTTP

| Method | Path | Auth | Success | Failure |
|--------|------|------|---------|---------|
| `GET` | **`/v1/explain/runs/{runId}/aggregate`** | **ReadAuthority** (same as explain) | **200** `RunExplanationSummary` | **404** when the run is missing, out of scope, or has **no golden manifest** (same semantics as granular explain). |

Implementation: `ArchLucid.Api.Controllers.ExplanationController.AggregateRunExplanation` → `IRunExplanationSummaryService.GetSummaryAsync` (`ArchLucid.AgentRuntime.Explanation.RunExplanationSummaryService`).

### `RunExplanationSummary` JSON shape (camelCase)

| Field | Type | Description |
|-------|------|-------------|
| `explanation` | object | Full **`ExplanationResult`** (see above): `summary`, `keyDrivers`, `riskImplications`, `structured`, top-level `confidence`, `provenance`, `detailedNarrative`, etc. |
| `themeSummaries` | string[] | One line per **theme**: decision **`KeyDrivers`** grouped by category prefix (`Category: Title → SelectedOption`); non-matching lines are prefixed with **“Additional signals:”**. |
| `overallAssessment` | string | Single executive line combining **risk posture**, unresolved issue / compliance gap **counts**, and the explanation **summary** (or narrative fallback). |
| `riskPosture` | string | **`Low`**, **`Medium`**, **`High`**, or **`Critical`** — derived only from **manifest unresolved issue severities** (not from LLM text). See **Risk posture derivation** below. |
| `findingCount` | number | `FindingsSnapshot.Findings.Count` when the run detail load included a findings snapshot; otherwise **0**. |
| `decisionCount` | number | `GoldenManifest.Decisions.Count`. |
| `unresolvedIssueCount` | number | `GoldenManifest.UnresolvedIssues.Items.Count`. |
| `complianceGapCount` | number | `GoldenManifest.Compliance.Gaps.Count`. |
| `citations` | array | Optional **`CitationReference`**: `kind` (string enum), `id`, `label`, optional `runId` — persisted artifact links for the aggregate narrative (see [explainability/CITATION_BOUND_RENDERING.md](../explainability/CITATION_BOUND_RENDERING.md)). Older APIs may omit the array. |

Granular single-run narrative remains on **`GET /v1/explain/runs/{runId}/explain`** (`ExplanationResult` only). Aggregate adds themes, posture, counts, **citations**, and reuses the same LLM path once per request.

## ExplanationProvenance

**Record:** `ArchLucid.Core.Explanation.ExplanationProvenance` — JSON: `agentType`, `modelId`, `promptTemplateId`, `promptTemplateVersion`, `promptContentHash` (nullable strings for the last three when unset).

**Where it appears**

- On **`ExplanationResult`**: set when `ExplanationService` finalizes a run explanation (`FinalizeRunExplanation`), from **`IAgentCompletionClient.Descriptor`** (model / vendor) and optional **`ExplanationServiceOptions`** bound to **`AgentExecution:Explanation`** (`AgentType`, prompt catalog fields).
- Nested inside **`RunExplanationSummary.explanation`** on **`GET .../aggregate`** (same payload as granular explain).

**Purpose:** Operators can see **which logical agent label**, **deployment/model id**, and **prompt revision** produced the explanation without opening raw LLM text.

## Risk posture derivation (aggregate)

**Source of truth:** `RunExplanationSummaryService.DeriveRiskPosture` over **`GoldenManifest.UnresolvedIssues.Items`** only.

| Rule | `riskPosture` |
|------|----------------|
| **No** unresolved issues | **`Low`** |
| Else: map each issue’s **`Severity`** (case-insensitive) to a rank, then take the **worst** rank | See table below |

**Severity → rank** (internal integer; higher = worse)

| `Severity` value | Rank |
|------------------|------|
| `Critical` | 4 |
| `High` | 3 |
| `Medium`, `Warning` | 2 |
| `Low`, `Info` | 1 |
| Empty / unknown | **2** (treated like medium) |

**Rank → label**

| Worst rank | `riskPosture` |
|------------|----------------|
| ≥ 4 | **Critical** |
| 3 | **High** |
| 2 | **Medium** |
| 1 | **Low** |

This is **deterministic** from manifest data; it does not use **`StructuredExplanation.confidence`** or model prose.

## Confidence (top-level on `ExplanationResult`)

Besides **`structured.confidence`**, **`ExplanationResult`** exposes **`confidence`** as a **convenience mirror**: after `ExplainRunAsync`, the service sets **`confidence`** = **`structured?.Confidence`**.

- **Meaning:** Model-estimated trust in the explanation content, **0.0–1.0** when present.
- **Nullable:** **`null`** when the model omitted a score or the pipeline could not produce one — UI should show **“not available”** (same as **Handling null fields** for `structured.confidence` above).
- **Aggregate response:** The nested **`explanation`** object includes both **`structured.confidence`** and top-level **`confidence`** with the same value when set.

## Implementation notes

- Parsing and normalization: `ArchLucid.Core.Explanation.StructuredExplanationParser`.
- Production path: `ArchLucid.AgentRuntime.Explanation.ExplanationService.ExplainRunAsync`.
- Aggregate orchestration: `ArchLucid.AgentRuntime.Explanation.RunExplanationSummaryService.GetSummaryAsync`.

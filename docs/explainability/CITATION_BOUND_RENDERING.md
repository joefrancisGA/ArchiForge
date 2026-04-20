> **Scope:** Citation-bound aggregate explanations - full detail, tables, and links in the sections below.

# Citation-bound aggregate explanations

## Objective

Tie **aggregate** run explanations to **persisted artifacts** operators can inspect (manifest, findings, traces, optional bundle).

## Assumptions

- **`GET /v1/explain/runs/{runId}/aggregate`** returns **`RunExplanationSummary`** including **`citations`** (`CitationReference[]`).
- The operator UI renders **chips** linking to manifest / provenance / run anchors.

## Constraints

- **Read-only** authority: explanations require **`ReadAuthority`** (same as today).
- **Not** a substitute for causal proof — see [../EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) §10.

## Architecture overview

**Aggregates:** `RunExplanationCitationBuilder` builds citations from **`RunDetailDto`**. **Instrumentation:** `archlucid_explanation_citations_emitted_total{kind}`.

## Component breakdown

| Type | Location |
|------|----------|
| `CitationKind`, `CitationReference` | `ArchLucid.Contracts.Explanation` |
| Builder | `ArchLucid.AgentRuntime.Explanation.RunExplanationCitationBuilder` |
| Aggregate payload | `RunExplanationSummary.Citations` |

## Data flow

`IRunExplanationSummaryService.GetSummaryAsync` loads **`RunDetailDto`** → builds citations → attaches to JSON response → **`CitationChips`** in **`RunExplanationSection`**.

## Security model

Citation `Id` values are **scoped** to the same tenant/workspace as the run detail query — they are not cross-tenant navigable in normal API flows.

## Operational considerations

- Watch **`archlucid_explanation_citations_emitted_total`** after UI changes.
- If citations are empty on older APIs, the UI hides the block.

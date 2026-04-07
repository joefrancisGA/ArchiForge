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

## Implementation notes

- Parsing and normalization: `ArchLucid.Core.Explanation.StructuredExplanationParser`.
- Production path: `ArchLucid.AgentRuntime.Explanation.ExplanationService.ExplainRunAsync`.

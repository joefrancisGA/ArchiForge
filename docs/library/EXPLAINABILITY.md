> **Scope:** Explainability — operator surfaces (finding inspector contract, links to trace coverage and citation rendering).

# Explainability — operator surfaces

How operators trace a finding back to persisted authority artifacts without relying on raw LLM prompts at the API edge.

**Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).

## Finding inspector (`GET /v1/findings/{findingId}/inspect`)

**Policy:** `ReadAuthority` (same gate as architecture read routes).

**Purpose:** One JSON payload that bundles what was persisted when the finding was materialized:

| Field | Meaning |
|-------|---------|
| `findingId` | Stable finding identifier (matches `dbo.FindingRecords.FindingId`). |
| `typedPayload` | JSON object deserialized from relational `PayloadJson` when present; otherwise JSON `null`. **No LLM prompt or completion text** is included. |
| `decisionRuleId` / `decisionRuleName` | First applied rule id from `dbo.DecisioningTraces` when the run has a rule-audit trace; otherwise the first `FindingTraceRulesApplied` row. |
| `evidence` | Zero or more rows derived from `FindingRelatedNodes` (graph citation ids / excerpts). Optional `artifactId` / `lineRange` are reserved for bundle-backed citations when engines populate them. |
| `auditRowId` | Best-effort `dbo.AuditEvents.EventId` for `AuthorityCommittedChainPersisted` on the same `RunId` when SQL audit append succeeded. |
| `runId` | Owning authority run. |
| `manifestVersion` | `Runs.CurrentManifestVersion` at read time. |

**UI:** Operator route `/runs/{runId}/findings/{findingId}/inspect` (server component) calls the inspector via the same BFF/proxy stack as other `/v1` reads. A **Why?** link is available from the per-finding explainability table on the run detail page.

**Related docs and APIs:**

- Trace completeness and narrative (no inspector merge): [`EXPLAINABILITY_TRACE_COVERAGE.md`](EXPLAINABILITY_TRACE_COVERAGE.md)
- Citation rendering rules: [`../explainability/CITATION_BOUND_RENDERING.md`](../explainability/CITATION_BOUND_RENDERING.md)
- Per-run explainability JSON: `GET /v1/explain/runs/{runId}/findings/{findingId}/explainability`
- Redacted LLM audit (separate surface, still ReadAuthority): `GET /v1/explain/runs/{runId}/findings/{findingId}/llm-audit`

## Security and tenancy

Inspector queries are **scoped** to the current tenant / workspace / project (`dbo.Runs.ScopeProjectId`). Anonymous callers receive **401**; authenticated principals without `ReadAuthority` receive **403**. Missing findings return **404** with problem+JSON (including correlation id when middleware attaches it).

## Reliability notes

When `CosmosDb:AuditEventsEnabled` is false, SQL `dbo.AuditEvents` may still receive durable rows for authority commits depending on host configuration; `auditRowId` may be null even though the finding exists.

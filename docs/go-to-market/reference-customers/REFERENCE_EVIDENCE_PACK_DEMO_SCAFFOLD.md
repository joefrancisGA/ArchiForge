> **Scope:** Internal GTM authoring scaffold — shows the structure a real customer evidence pack must follow, populated with placeholder cells from the demo tenant. **Not** a publishable customer artefact; every cell in a real pack must trace to a customer-approved source per `REFERENCE_PUBLICATION_RUNBOOK.md`.

# Reference evidence pack — demo tenant scaffold (internal only)

**Status:** Scaffold — **not** a customer evidence pack. Every numeric and narrative cell in a real pack must come from **customer-approved** sources; see [`REFERENCE_PUBLICATION_RUNBOOK.md`](REFERENCE_PUBLICATION_RUNBOOK.md).

## How to use this file

1. Open [`REFERENCE_EVIDENCE_PACK_TEMPLATE.md`](REFERENCE_EVIDENCE_PACK_TEMPLATE.md).
2. For the **Measured deltas** table, paste field values from a real `pilot-run-deltas` export (`GET /v1/pilots/runs/{runId}/pilot-run-deltas`).
3. Until a paying PLG customer exists, you may copy **shape only** from [`samples/pilot-run-deltas.demo-tenant.json`](samples/pilot-run-deltas.demo-tenant.json) — keep the literal banner **demo tenant — replace before publishing** on every ArchLucid-side artifact.

## Mapping (template row → JSON field)

| Template metric row | JSON / API field |
|---------------------|------------------|
| Wall-clock request → committed manifest | `timeToCommittedManifestTotalSeconds` (convert to `HH:MM:SS` in the template) |
| Manifest committed at | `manifestCommittedUtc` |
| Run created at | `runCreatedUtc` |
| Findings by severity | `findingsBySeverity[]` |
| Audit rows | `auditRowCount`, `auditRowCountTruncated` |
| LLM completion calls | `llmCallCount` |
| Top finding | `topFindingId`, `topFindingSeverity` |
| Demo flag | `isDemoTenant` — must be **false** before external publication |

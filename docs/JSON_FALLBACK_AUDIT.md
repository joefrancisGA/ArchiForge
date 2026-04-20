> **Scope:** JSON fallback removal — relational-first reads (post-53R) - full detail, tables, and links in the sections below.

# JSON fallback removal — relational-first reads (post-53R)

## Objective

Document the **current** persistence read behavior after removal of the 53R `JsonFallbackPolicy` / `PersistenceReadMode` seam. Slice-level “read JSON when relational empty” is **gone** for the audited domains below; operators rely on **backfill + readiness** instead of runtime mode switches.

---

## Removed (no longer in the codebase)

| Item | Notes |
|------|--------|
| `JsonFallbackPolicy`, `PersistenceReadMode`, `RelationalDataMissingException` | Deleted |
| `RelationalFirstRead` | Deleted |
| `persistence_json_fallback_used` (`ArchLucidInstrumentation.JsonFallbackUsed`) | Removed |
| `*JsonFallback.cs` helpers | **Deleted** for ContextSnapshot, FindingsSnapshot, GoldenManifest phase-1 slices, ArtifactBundle **artifacts list** |
| DI registration of `JsonFallbackPolicy` in `ArchLucidStorageServiceCollectionExtensions` | Removed |
| Tests: `JsonFallbackPolicyTests`, `RelationalFirstReadTests`, `FallbackPolicyDiagnosticsTests`, `PolicyModeFallbackSqlIntegrationTests`, `ContextSnapshotJsonFallbackTests` | Deleted |

---

## Current read behavior by domain

| Domain | Relational slices | JSON still read? |
|--------|-------------------|------------------|
| **ContextSnapshot** | CanonicalObjects, Warnings, Errors, SourceHashes | **No** for those collections (empty if no child rows). Header JSON columns remain written for compatibility. |
| **FindingsSnapshot** | `FindingRecords` + children | **No** `FindingsJson` on read (empty findings if no records). `FindingsJson` still written on save. |
| **GoldenManifest** | Phase-1: Assumptions, Warnings, Decisions, Provenance tables | **No** fallback to slice JSON for those. Other sections (`MetadataJson`, etc.) remain JSON-primary (unchanged). |
| **ArtifactBundle** | `ArtifactBundleArtifacts` (+ metadata / decision links) | **No** `ArtifactsJson` on read. **`TraceJson`** still deserialized via `ArtifactBundleTraceJsonReader`; relational tables overlay list fields on the trace. |
| **GraphSnapshot** | Nodes, Warnings, Edges (from `GraphSnapshotEdges`), EdgeProperties | **No** JSON read for nodes/warnings when relational rows absent (empty lists). **Exception:** when `GraphSnapshotEdges` has rows but **`GraphSnapshotEdgeProperties` is empty**, label/properties are **merged from `EdgesJson`** for matching `EdgeId`s until all edge metadata is backfilled. |

`GraphSnapshotStorageMapper.ToSnapshot(row)` (single argument) still deserializes JSON for **unit tests** and callers that bypass relational hydration.

---

## Cutover readiness

`SqlCutoverReadinessService` / `ArchLucid.Backfill.Cli --readiness` still report per-slice coverage. **Semantic change:** “ready” means **safe for the current relational-only read paths**, not “safe to flip `RequireRelational`” (that mode no longer exists).

See **[SqlRelationalBackfill.md](SqlRelationalBackfill.md)** for backfill scope and CLI usage.

---

## Follow-ups (optional / future)

| Item | Reason |
|------|--------|
| **Stop dual-writing** legacy JSON columns once all consumers and restores are relational-only | Reduces storage and write amplification |
| **Backfill `GraphSnapshotEdgeProperties`** for all snapshots | Eliminates the remaining `EdgesJson` merge path |
| **Relational trace base** for `ArtifactBundle` | Replace `TraceJson` scalar base when a schema exists |

---

## Historical note (53R)

The long-form 53R policy design, OTel counter, and structured “JSON fallback used” logs applied **before** this removal. Git history retains the prior implementation if needed for incident review.

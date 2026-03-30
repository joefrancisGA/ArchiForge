# 53R — JSON fallback audit, centralized policy, and structured diagnostics

## Objective

Audit every remaining JSON-column fallback path in persistence, route the allow/deny decision through **one** policy seam (`JsonFallbackPolicy`), and add structured diagnostics so fallback usage is observable in production and test environments.

Operators and developers can now answer:

- **What** entity type fell back (e.g. `ContextSnapshot`, `GraphSnapshot`).
- **Which** slice fell back (e.g. `ContextSnapshot.CanonicalObjects`).
- **How often** fallback is still happening (OTel counter `persistence_json_fallback_used`).
- **Whether the system is ready** for `RequireRelational` mode (zero counter → safe to cut over).

---

## Policy seam

### `PersistenceReadMode` enum

| Value | Behavior | Log level |
|-------|----------|-----------|
| **`AllowJsonFallback`** (default) | Legacy behavior. Empty relational → read JSON column. | `Debug` |
| **`WarnOnJsonFallback`** | Same as Allow, but emits a structured `ILogger.LogWarning`. Use during migration roll-out to surface residual JSON reads. | `Warning` |
| **`RequireRelational`** | If relational child rows are absent, throws `RelationalDataMissingException`. Use after confirming all environments are fully backfilled. | N/A (throws) |

All modes (except `RequireRelational`) increment the **OTel counter** on fallback — see Diagnostics section below.

### `JsonFallbackPolicy`

Constructed with `(PersistenceReadMode mode, ILogger logger)`. Parameterless constructor defaults to `AllowJsonFallback` with `NullLogger`.

| Method | Purpose |
|--------|---------|
| `EvaluateFallback(int relationalRowCount, string sliceName, string entityType, string entityId)` | Full evaluation: returns `true` to fall back, `false` to use relational, or **throws** in `RequireRelational` mode. Also emits structured log + increments OTel counter on fallback. |
| `ShouldFallbackToJson(int relationalRowCount, string sliceName)` | Backward-compatible overload (delegates to `EvaluateFallback`). |

| Property | Purpose |
|----------|---------|
| `Mode` | Current `PersistenceReadMode`. |
| `AllowFallback` | Computed: `true` unless `RequireRelational`. |

Registered as a **singleton** in `ArchiForgeStorageServiceCollectionExtensions` (SQL provider path only).

### `RelationalDataMissingException`

Thrown by `RequireRelational` mode. Properties: `EntityType`, `EntityId`, `SliceName`. Message includes a recommendation to run `SqlRelationalBackfillService`.

### `RelationalFirstRead.ReadSliceAsync` (updated)

Policy-aware overload accepts `JsonFallbackPolicy?`, `sliceName`, `emptyDefault`, and optional `entityType`/`entityId` for diagnostics. The old two-argument overload (no policy) still compiles for backward compat.

---

## Fallback seams audited (53R-1) and policy-wired (53R-2)

| Domain | File | Slice(s) | Pattern before 53R | Pattern after 53R |
|--------|------|----------|---------------------|-------------------|
| **ContextSnapshot** | `ContextSnapshotRelationalRead.cs` | CanonicalObjects, Warnings, Errors, SourceHashes | `RelationalFirstRead.ReadSliceAsync` (no policy) | Policy-aware `ReadSliceAsync` with slice names |
| **GoldenManifest** | `GoldenManifestPhase1RelationalRead.cs` | Assumptions, Warnings, Decisions | `RelationalFirstRead.ReadSliceAsync` (no policy) | Policy-aware `ReadSliceAsync` with slice names |
| **GoldenManifest** | `GoldenManifestPhase1RelationalRead.cs` | Provenance | **Ad-hoc** `if (count > 0) … else deserializeJson` | Routes through `policy.EvaluateFallback` |
| **FindingsSnapshot** | `FindingsSnapshotRelationalRead.cs` | Findings (full snapshot) | **Ad-hoc** `if (records.Count == 0) return JsonFallback` | Routes through `policy.EvaluateFallback` |
| **FindingsSnapshot** | `SqlFindingsSnapshotRepository.cs` | Findings (duplicate entry point) | **Ad-hoc** `if (recordCount == 0) return JsonFallback` | Routes through `policy.EvaluateFallback` + passes policy to `LoadRelationalSnapshotAsync` |
| **GraphSnapshot** | `GraphSnapshotRelationalRead.cs` | Nodes, Warnings, Edges, EdgeProperties (merge) | **Ad-hoc** null-override → mapper deserializes JSON | Mapper receives `fallbackPolicy`; edge-merge gated by policy |
| **GraphSnapshot** | `GraphSnapshotStorageMapper.cs` | Nodes, Edges, Warnings (when override is null) | `override is null → deserialize JSON` | `ResolveOverrideOrFallback` helper consults policy |
| **ArtifactBundle** | `ArtifactBundleRelationalRead.cs` | Artifacts | `RelationalFirstRead.ReadSliceAsync` (no policy) | Policy-aware `ReadSliceAsync` with slice name |
| **ArtifactBundle** | `ArtifactBundleRelationalRead.cs` | Trace (base) | Always deserializes JSON (scalar header fields) | **Unchanged** — intentional; trace base holds non-list scalar fields not yet relational |

---

## Files changed

### Production (53R-1 + 53R-2 + 53R-3 + 53R-4)

| File | Change |
|------|--------|
| `ArchiForge.Persistence/RelationalRead/PersistenceReadMode.cs` | **New** — enum: `AllowJsonFallback`, `WarnOnJsonFallback`, `RequireRelational` |
| `ArchiForge.Persistence/RelationalRead/RelationalDataMissingException.cs` | **New** — exception with `EntityType`, `EntityId`, `SliceName` |
| `ArchiForge.Persistence/RelationalRead/JsonFallbackPolicy.cs` | Upgraded: constructor takes `PersistenceReadMode` + `ILogger`; `EvaluateFallback` emits structured log + OTel counter on fallback; `ShouldFallbackToJson` backward compat |
| `ArchiForge.Core/Diagnostics/ArchiForgeInstrumentation.cs` | Added `JsonFallbackUsed` counter (`persistence_json_fallback_used`) with `entity_type`, `slice`, `read_mode` tags |
| `ArchiForge.Persistence/RelationalRead/RelationalFirstRead.cs` | Policy-aware overload with `entityType`/`entityId` params; backward-compat overload preserved |
| `ArchiForge.Persistence/ContextSnapshots/ContextSnapshotRelationalRead.cs` | `HydrateAsync` accepts optional `fallbackPolicy`; all 4 slices pass `entityType`+`entityId` |
| `ArchiForge.Persistence/GoldenManifests/GoldenManifestPhase1RelationalRead.cs` | `HydrateAsync` accepts optional `fallbackPolicy`; provenance uses `EvaluateFallback` with entity context |
| `ArchiForge.Persistence/Findings/FindingsSnapshotRelationalRead.cs` | `LoadRelationalSnapshotAsync` accepts optional `fallbackPolicy`; uses `EvaluateFallback` with entity context |
| `ArchiForge.Persistence/Repositories/SqlFindingsSnapshotRepository.cs` | Constructor accepts optional `JsonFallbackPolicy`; `GetByIdAsync` uses `EvaluateFallback` with entity context |
| `ArchiForge.Persistence/GraphSnapshots/GraphSnapshotRelationalRead.cs` | `HydrateAsync` accepts optional `fallbackPolicy`; edge-merge uses `EvaluateFallback` with entity context |
| `ArchiForge.Persistence/Repositories/GraphSnapshotStorageMapper.cs` | `ToSnapshot` accepts optional `fallbackPolicy`; `ResolveOverrideOrFallback` passes `entityId` through |
| `ArchiForge.Persistence/ArtifactBundles/ArtifactBundleRelationalRead.cs` | `HydrateBundleAsync` accepts optional `fallbackPolicy`; passes `entityType`+`entityId` |
| `ArchiForge.Api/Configuration/ArchiForgeStorageServiceCollectionExtensions.cs` | Registers `JsonFallbackPolicy` singleton with `ILoggerFactory` |
| `ArchiForge.Persistence/Backfill/CutoverSliceReadiness.cs` | **New** — per-slice readiness model with computed `IsReady` and `HeadersMissingRelationalRows` |
| `ArchiForge.Persistence/Backfill/CutoverReadinessReport.cs` | **New** — aggregate report with `IsFullyReady`, `SlicesNotReady`, deduplicated `TotalHeaderRows` |
| `ArchiForge.Persistence/Backfill/ICutoverReadinessService.cs` | **New** — interface for readiness assessment |
| `ArchiForge.Persistence/Backfill/SqlCutoverReadinessService.cs` | **New** — SQL implementation using `WHERE EXISTS` correlated subqueries for efficient counting |
| `ArchiForge.Backfill.Cli/Program.cs` | Added `--readiness` mode with tabular console output and exit code 3 |

### Tests

| File | Tests |
|------|-------|
| `ArchiForge.Persistence.Tests/JsonFallbackPolicyTests.cs` | 13 tests: default, allow/warn/require modes, `AllowFallback` property, `ShouldFallbackToJson` backward compat, warn logs, require throws with entity context, allow debug logging, no-log when relational exists |
| `ArchiForge.Persistence.Tests/RelationalFirstReadTests.cs` | 6 tests: relational exists, allow/warn/require modes, null policy, backward-compat overload |
| `ArchiForge.Persistence.Tests/CutoverReadinessReportTests.cs` | 11 tests: slice ready/not-ready/zero-header, report fully-ready/partial/empty, deduplication, full-pipeline scenario |

### Docs

| File | Change |
|------|--------|
| `docs/JSON_FALLBACK_AUDIT.md` | Updated with 53R-3 diagnostics section, OTel counter, structured log format, and cutover readiness guide |

---

## Structured diagnostics (53R-3)

### OTel counter

| Counter name | Tags | Description |
|-------------|------|-------------|
| `persistence_json_fallback_used` | `entity_type`, `slice`, `read_mode` | Incremented once per slice fallback decision, not per nested field. Exposed through `ArchiForgeInstrumentation.JsonFallbackUsed` on the shared `ArchiForge` meter. |

Tags allow dashboards and alerts to aggregate by entity type, individual slice, or read mode.

### Structured log events

Every fallback decision emits a single structured log at the slice decision point (not inside JSON deserializers):

```
JSON fallback used — slice={SliceName}, entityType={EntityType}, entityId={EntityId}, readMode={ReadMode}. Run SqlRelationalBackfillService to eliminate fallback reads.
```

| Read mode | Log level |
|-----------|-----------|
| `AllowJsonFallback` | `Debug` — silent in production unless verbose logging is enabled. |
| `WarnOnJsonFallback` | `Warning` — surfaces in default production log sinks. |
| `RequireRelational` | No log — throws `RelationalDataMissingException` instead. |

### Where diagnostics fire

All diagnostics are emitted from `JsonFallbackPolicy.EvaluateFallback`, which is called by:

| Caller | Slices logged |
|--------|---------------|
| `RelationalFirstRead.ReadSliceAsync` | ContextSnapshot.CanonicalObjects, Warnings, Errors, SourceHashes; GoldenManifest.Assumptions, Warnings, Decisions; ArtifactBundle.Artifacts |
| `GoldenManifestPhase1RelationalRead` (direct) | GoldenManifest.Provenance |
| `FindingsSnapshotRelationalRead` (direct) | FindingsSnapshot.Findings |
| `SqlFindingsSnapshotRepository` (direct) | FindingsSnapshot.Findings |
| `GraphSnapshotRelationalRead` (direct) | GraphSnapshot.EdgeProperties (merge decision) |
| `GraphSnapshotStorageMapper.ResolveOverrideOrFallback` | GraphSnapshot.Nodes, Edges, Warnings |

Entity type and entity ID are now passed through all call sites for full traceability.

### What is NOT logged

- **Relational reads that succeed** — no noise; the counter and log only fire on fallback.
- **Nested JSON deserialization** — only the top-level slice decision is logged, not each field within the JSON.
- **ArtifactBundle trace base** — always reads JSON (scalar header fields); not a fallback scenario.

### Cutover readiness check

When `persistence_json_fallback_used` counter shows zero over a sustained period, the system is ready to switch to `RequireRelational`. The cutover process:

1. Deploy with `WarnOnJsonFallback` → monitor the counter and log warnings.
2. Run `SqlRelationalBackfillService` until counter reaches zero.
3. Switch to `RequireRelational` → any remaining un-backfilled rows throw `RelationalDataMissingException` with actionable guidance.

---

## Out of scope for 53R

| Item | Reason |
|------|--------|
| **ArtifactBundle trace base JSON read** | Not a fallback — holds scalar fields with no relational column yet. Separate migration needed. |
| **GoldenManifest sections still JSON-primary** (`Metadata`, `Requirements`, `Topology`, `Security`, `Compliance`, `Cost`, `Constraints`, `UnresolvedIssues`) | Full JSON columns with no relational child tables. Phase-2 decomposition. |
| **`FindingPayloadJsonCodec` (per-finding payload sidecar)** | Typed JSON sidecar on relational rows, not a legacy fallback. |
| **`SqlDecisionTraceRepository` JSON columns** | Serialize/deserialize only; no relational child tables. |
| **Configuration-driven mode selection** | `PersistenceReadMode` is set in code at DI registration. Binding to `appsettings.json` or feature flags is a follow-up if needed. |
| **Per-slice mode overrides** | All slices share one mode today. If certain slices need `RequireRelational` before others, add a per-slice override map to `JsonFallbackPolicy`. |

---

## Cutover readiness report (53R-4)

### What it does

`SqlCutoverReadinessService` runs read-only aggregate SQL queries against each entity type's header and child tables. For every slice, it reports:

| Field | Meaning |
|-------|---------|
| `TotalHeaderRows` | Count of rows in the parent table (e.g. `dbo.ContextSnapshots`). |
| `HeadersWithRelationalRows` | How many of those have ≥1 row in the child table. |
| `HeadersMissingRelationalRows` | `Total - WithRelational` — rows still depending on JSON fallback. |
| `IsReady` | `true` when missing = 0. |

`CutoverReadinessReport.IsFullyReady` is `true` only when **every** slice is ready.

### Slices assessed

| Entity type | Slices |
|------------|--------|
| ContextSnapshot | CanonicalObjects, Warnings, Errors, SourceHashes |
| GraphSnapshot | Nodes, Edges, Warnings, EdgeProperties |
| FindingsSnapshot | Findings |
| GoldenManifest | Assumptions, Warnings, Decisions, Provenance (any of 3 sub-tables) |
| ArtifactBundle | Artifacts |

### CLI usage

```bash
# Run the readiness assessment (read-only, no schema changes)
ArchiForge.Backfill.Cli --readiness --connection "Server=...;Database=ArchiForge;..."

# Or use the ARCHIFORGE_SQL environment variable
export ARCHIFORGE_SQL="Server=...;Database=ArchiForge;..."
ArchiForge.Backfill.Cli --readiness
```

Example output:

```
=== Relational Cutover Readiness Report ===

Slice                                          Total   Ready   Missing     Status
--------------------------------------------------------------------------------
ContextSnapshot.CanonicalObjects                 200     200         0      READY
ContextSnapshot.Warnings                         200     200         0      READY
ContextSnapshot.Errors                           200     200         0      READY
ContextSnapshot.SourceHashes                     200     195         5  NOT READY
GraphSnapshot.Nodes                              200     200         0      READY
...
--------------------------------------------------------------------------------

1 slice(s) NOT READY. Run backfill before enabling RequireRelational.
```

Exit codes: `0` = all ready, `3` = one or more slices not ready.

### Service usage (programmatic)

```csharp
ICutoverReadinessService readiness = provider.GetRequiredService<ICutoverReadinessService>();
CutoverReadinessReport report = await readiness.AssessAsync(ct);

if (report.IsFullyReady)
    // safe to switch to PersistenceReadMode.RequireRelational
else
    // report.SlicesNotReady lists which slices need backfill
```

---

## How to cut over (future)

1. Run `SqlRelationalBackfillService` across all environments.
2. Run `ArchiForge.Backfill.Cli --readiness` to confirm all slices report READY.
3. Change DI registration to `PersistenceReadMode.WarnOnJsonFallback`; deploy; monitor `persistence_json_fallback_used` counter and `Warning`-level logs for remaining fallback hits.
4. When the counter is zero over a sustained period, run `--readiness` one more time to confirm, then change to `PersistenceReadMode.RequireRelational`; deploy; any un-backfilled rows throw `RelationalDataMissingException` with clear entity context and remediation advice.
5. Delete `*JsonFallback.cs` helpers, remove the backward-compat `ReadSliceAsync` overload, remove the JSON column reads from `GraphSnapshotStorageMapper`, and retire the `persistence_json_fallback_used` counter.

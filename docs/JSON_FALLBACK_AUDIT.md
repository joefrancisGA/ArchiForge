# 53R-1 — JSON fallback audit and centralized policy

## Objective

Audit every remaining JSON-column fallback path in persistence and route the allow/deny decision through **one** policy seam (`JsonFallbackPolicy`) so that:

- Fallback usage is **explicit**, not ad-hoc.
- Future removal of fallback requires changing **one boolean**, not hunting through six files.
- Current behavior is **preserved by default** (`AllowFallback = true`).

---

## Policy seam

### `ArchiForge.Persistence.RelationalRead.JsonFallbackPolicy`

| Property | Default | Effect |
|----------|---------|--------|
| `AllowFallback` | `true` | `true` = legacy behavior (empty relational → read JSON column). `false` = empty relational → empty collection (surfaces un-backfilled rows as data gaps instead of silently loading JSON). |

`ShouldFallbackToJson(int relationalRowCount, string sliceName)` is the single method every caller uses. `sliceName` is a diagnostic label (e.g. `"ContextSnapshot.CanonicalObjects"`) ready for logging/telemetry when needed.

Registered as a **singleton** in `ArchiForgeStorageServiceCollectionExtensions` (SQL provider path only; in-memory providers don't use JSON columns).

### `RelationalFirstRead.ReadSliceAsync` (updated)

New overload accepts `JsonFallbackPolicy?`, `sliceName`, and `emptyDefault`. The old two-argument overload still compiles (backward compat, always falls back).

---

## Fallback seams audited

| Domain | File | Slice(s) | Pattern before 53R-1 | Pattern after 53R-1 |
|--------|------|----------|----------------------|---------------------|
| **ContextSnapshot** | `ContextSnapshotRelationalRead.cs` | CanonicalObjects, Warnings, Errors, SourceHashes | `RelationalFirstRead.ReadSliceAsync` (no policy) | Policy-aware `ReadSliceAsync` with slice names |
| **GoldenManifest** | `GoldenManifestPhase1RelationalRead.cs` | Assumptions, Warnings, Decisions | `RelationalFirstRead.ReadSliceAsync` (no policy) | Policy-aware `ReadSliceAsync` with slice names |
| **GoldenManifest** | `GoldenManifestPhase1RelationalRead.cs` | Provenance | **Ad-hoc** `if (count > 0) … else deserializeJson` | Routes through `policy.ShouldFallbackToJson(totalProvCount, "GoldenManifest.Provenance")` |
| **FindingsSnapshot** | `FindingsSnapshotRelationalRead.cs` | Findings (full snapshot) | **Ad-hoc** `if (records.Count == 0) return JsonFallback` | Routes through `policy.ShouldFallbackToJson(0, "FindingsSnapshot.Findings")` |
| **FindingsSnapshot** | `SqlFindingsSnapshotRepository.cs` | Findings (duplicate entry point) | **Ad-hoc** `if (recordCount == 0) return JsonFallback` | Routes through `policy.ShouldFallbackToJson(0, "FindingsSnapshot.Findings")` + passes policy to `LoadRelationalSnapshotAsync` |
| **GraphSnapshot** | `GraphSnapshotRelationalRead.cs` | Nodes, Warnings, Edges, EdgeProperties (merge) | **Ad-hoc** null-override → mapper deserializes JSON | Mapper receives `fallbackPolicy`; edge-metadata merge gated by `ShouldFallbackToJson` |
| **GraphSnapshot** | `GraphSnapshotStorageMapper.cs` | Nodes, Edges, Warnings (when override is null) | `override is null → deserialize JSON` | `ResolveOverrideOrFallback` helper consults policy; returns `[]` when denied |
| **ArtifactBundle** | `ArtifactBundleRelationalRead.cs` | Artifacts | `RelationalFirstRead.ReadSliceAsync` (no policy) | Policy-aware `ReadSliceAsync` with slice name |
| **ArtifactBundle** | `ArtifactBundleRelationalRead.cs` | Trace (base) | Always deserializes JSON (scalar header fields) | **Unchanged** — intentional; trace base holds non-list scalar fields not yet relational |

---

## Files changed

### Production

| File | Change |
|------|--------|
| `ArchiForge.Persistence/RelationalRead/JsonFallbackPolicy.cs` | **New** — centralized policy class |
| `ArchiForge.Persistence/RelationalRead/RelationalFirstRead.cs` | New policy-aware overload; old overload preserved |
| `ArchiForge.Persistence/ContextSnapshots/ContextSnapshotRelationalRead.cs` | `HydrateAsync` accepts optional `fallbackPolicy`; all 4 slices use policy-aware `ReadSliceAsync` |
| `ArchiForge.Persistence/GoldenManifests/GoldenManifestPhase1RelationalRead.cs` | `HydrateAsync` accepts optional `fallbackPolicy`; assumptions/warnings/decisions use policy-aware `ReadSliceAsync`; provenance ad-hoc branch routes through policy |
| `ArchiForge.Persistence/Findings/FindingsSnapshotRelationalRead.cs` | `LoadRelationalSnapshotAsync` accepts optional `fallbackPolicy`; empty-records branch routes through policy |
| `ArchiForge.Persistence/Repositories/SqlFindingsSnapshotRepository.cs` | Constructor accepts optional `JsonFallbackPolicy`; `GetByIdAsync` routes through policy and passes it to `LoadRelationalSnapshotAsync` |
| `ArchiForge.Persistence/GraphSnapshots/GraphSnapshotRelationalRead.cs` | `HydrateAsync` accepts optional `fallbackPolicy`; passes to mapper; edge-merge gated by policy |
| `ArchiForge.Persistence/Repositories/GraphSnapshotStorageMapper.cs` | `ToSnapshot` accepts optional `fallbackPolicy`; new `ResolveOverrideOrFallback` helper |
| `ArchiForge.Persistence/ArtifactBundles/ArtifactBundleRelationalRead.cs` | `HydrateBundleAsync` accepts optional `fallbackPolicy`; artifacts slice uses policy-aware `ReadSliceAsync` |
| `ArchiForge.Api/Configuration/ArchiForgeStorageServiceCollectionExtensions.cs` | Registers `JsonFallbackPolicy` singleton (`AllowFallback = true`) |

### Tests

| File | Change |
|------|--------|
| `ArchiForge.Persistence.Tests/JsonFallbackPolicyTests.cs` | **New** — 4 tests: default true, relational-exists short-circuit, allow, deny |
| `ArchiForge.Persistence.Tests/RelationalFirstReadTests.cs` | **New** — 5 tests: relational path, fallback-allow, fallback-deny, null-policy, backward-compat overload |

### Docs

| File | Change |
|------|--------|
| `docs/JSON_FALLBACK_AUDIT.md` | **New** — this file |

---

## Out of scope for 53R

| Item | Reason |
|------|--------|
| **ArtifactBundle trace base JSON read** | Not a fallback — it holds scalar fields (e.g. `StartedUtc`, `CompletedUtc`) that have no relational column yet. Separate migration needed before this can be policy-gated. |
| **GoldenManifest sections still JSON-primary** (`Metadata`, `Requirements`, `Topology`, `Security`, `Compliance`, `Cost`, `Constraints`, `UnresolvedIssues`) | These are full JSON columns with no relational child tables. Phase-2 relational decomposition is a separate work item. |
| **`FindingPayloadJsonCodec` (per-finding payload sidecar)** | This is a typed JSON sidecar column on `FindingRecords` (relational rows), not a legacy fallback. It stays as-is. |
| **`SqlDecisionTraceRepository` JSON columns** | Serialize/deserialize only (no relational child tables exist). Future decomposition, not fallback. |
| **Removing fallback entirely** | Requires confirming all environments are fully backfilled. Set `AllowFallback = false`, run integration tests, then delete `*JsonFallback.cs` helpers and the backward-compat `ReadSliceAsync` overload. |
| **Logging/telemetry on fallback usage** | `sliceName` parameter is wired but unused beyond the policy decision. Adding structured logging or a counter is a follow-up. |

---

## How to cut over (future)

1. Run `SqlRelationalBackfillService` across all environments.
2. Set `JsonFallbackPolicy.AllowFallback = false` (config or code).
3. Run full test suite — any data gaps surface as empty collections instead of silent JSON reads.
4. Delete `*JsonFallback.cs` helpers, remove the backward-compat `ReadSliceAsync` overload, and remove the JSON column reads from `GraphSnapshotStorageMapper`.

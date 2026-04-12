# Performance — caching and hot paths (ArchLucid)

**Audience:** Operators and developers tuning latency, cache behavior, and LLM cost for the API and worker.

**Scope:** High-level behavior of **read-through caches** and related configuration. For the full DI map, see **[DI_REGISTRATION_MAP.md](DI_REGISTRATION_MAP.md)**; for metric names, see **[OBSERVABILITY.md](OBSERVABILITY.md)**.

---

## Aggregate run explanation summary (`CachingRunExplanationSummaryService`)

**What:** The **`IRunExplanationSummaryService`** implementation can be wrapped in **`CachingRunExplanationSummaryService`** when **`HotPathCache:Enabled`** is **`true`** (see **`RegisterRunExplanationSummaryService`** in **`ServiceCollectionExtensions.CoordinatorAndArtifacts.cs`**).

**Cache key:** `explanation:aggregate:{runId}:{hex(RowVersion)}`, where **`RowVersion`** is the SQL **`ROWVERSION`** (or equivalent) on the authority run row, read via **`IAuthorityQueryService.GetRunDetailAsync`** before the inner service runs.

**TTL:** Entry lifetime follows **`HotPathCacheOptions.AbsoluteExpirationSeconds`** (clamped by the **`IHotPathReadCache`** implementation, same as manifest/run/policy-pack hot-path entries). Configure under the **`HotPathCache`** configuration section.

**Invalidation without explicit deletes:** When a run row is updated in a way that advances **`ROWVERSION`** (for example after manifest re-commit or other persistence that bumps the row version), the **hex suffix** in the key changes, so the next request **misses** the old entry and recomputes the aggregate summary. Entries for superseded keys expire naturally by TTL.

**Bypass:** If **`HotPathCache:Enabled`** is **`false`**, **`RunExplanationSummaryService`** is registered directly and no explanation summary caching occurs. If **`Run.RowVersion`** is missing on the detail DTO, the decorator delegates to the inner service **without** caching.

---

## Related documents

- [OBSERVABILITY.md](OBSERVABILITY.md) — **`archlucid_explanation_cache_*`** and other business KPI metrics.
- [DI_REGISTRATION_MAP.md](DI_REGISTRATION_MAP.md) — conditional **`IRunExplanationSummaryService`** registration.
- [ArchLucid.Persistence.Coordination/Caching/HotPathCacheOptions.cs](../ArchLucid.Persistence.Coordination/Caching/HotPathCacheOptions.cs) — options type (source).

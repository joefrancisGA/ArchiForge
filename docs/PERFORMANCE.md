> **Scope:** Performance — caching and hot paths (ArchLucid) - full detail, tables, and links in the sections below.

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

## k6 operator-path smoke (CI baseline)

**What:** After a green **`.NET: full regression (SQL)`**, CI runs **`tests/load/k6-api-smoke.js`** (native k6 on the Ubuntu runner) against a fresh **`ArchLucid.Api`** process and SQL catalog **`ArchLucidK6Smoke`**. The script hits **`/health/ready`** (expects JSON **`status: "Healthy"`**), **`/version`**, **`POST /v1/architecture/request`**, and **`GET /v1/authority/projects/default/runs?take=10`**.

**Job:** **`Performance: k6 API smoke (operator path)`** in **`.github/workflows/ci.yml`**. **~60s** profile: ramp to **5** VUs (10s hold 40s ramp 10s). **Merge-blocking** thresholds: **`http_req_failed` &lt; 1%**, **`http_req_duration` p95 &lt; 2000 ms** (Python gate **`scripts/ci/assert_k6_ci_smoke_summary.py`**; k6 also enforces **p99 &lt; 5000 ms**). Artifact **`k6-smoke-results`** holds the summary JSON.

**Local:** See **[`tests/load/README.md`](../tests/load/README.md)**. Full hot-path baselines and Compose **`full-stack`**: **[LOAD_TEST_BASELINE.md](LOAD_TEST_BASELINE.md)**.

## k6 per-tenant burst (weekly)

**What:** **[`tests/load/per-tenant-burst.js`](../tests/load/per-tenant-burst.js)** runs **10** fixed tenant scopes (HTTP **`x-tenant-id`** / workspace / project GUIDs), each at **5** iterations/s for **5 minutes** (override with **`K6_BURST_DURATION`**). Each iteration executes the operator path: **`POST /v1/architecture/request`** → **`POST …/seed-fake-results`** → **`POST …/commit`** → **`GET /v1/artifacts/manifests/{manifestId}`**.

**Job:** **`.github/workflows/k6-per-tenant-burst-scheduled.yml`** (weekly **Monday 06:15 UTC** + **`workflow_dispatch`**). Thresholds: **`scripts/ci/assert_k6_ci_smoke_summary.py`** with **`--max-p95-ms 3000`** and failed-rate cap **5%** (burstier than merge-blocking smokes). Summary artifact: **`k6-per-tenant-burst-summary`**.

**Why:** Exercises **per-tenant burst** against **Simulator** mode (same pattern as **`start_api_for_k6.sh`**) without live LLM spend.

---

## Related documents

- [OBSERVABILITY.md](OBSERVABILITY.md) — **`archlucid_explanation_cache_*`** and other business KPI metrics.
- [DI_REGISTRATION_MAP.md](DI_REGISTRATION_MAP.md) — conditional **`IRunExplanationSummaryService`** registration.
- [ArchLucid.Persistence.Coordination/Caching/HotPathCacheOptions.cs](../ArchLucid.Persistence.Coordination/Caching/HotPathCacheOptions.cs) — options type (source).

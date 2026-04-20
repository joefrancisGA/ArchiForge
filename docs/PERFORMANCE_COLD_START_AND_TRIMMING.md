> **Scope:** Cold start, profiling, and trimming (API) - full detail, tables, and links in the sections below.

# Cold start, profiling, and trimming (API)

**Objective:** Reduce first-request latency and deployment size where safe.

**Assumptions:** Default shipping remains **non-trimmed** until each feature area is audited for reflection/DI edge cases.

## Profiling

- Capture **Startup** and **first request** with `dotnet-trace` (`.NET Runtime` + `ASP.NET Core` providers) or your APM vendor.
- Watch **JIT**, **R2R** (if enabled), **SQL migration** (`DatabaseMigrator.Run`), and **first OpenAI/embedding** calls — these dominate cold paths more than minor assembly savings.

## Trimming (optional)

- `PublishTrimmed` and `TrimMode` can shrink containers but break **reflection-based** registration (some serializers, certain DI conveniences). Enable only after testing a **published** build end-to-end (health, migrations, OpenAPI, one replay path).
- Prefer **tiered publishing**: trimmed image for **stateless read-only** roles only if split in the future; keep the main API untrimmed until validated.

## Container layers

- Multi-stage Dockerfiles (`ArchLucid.Api/Dockerfile`) already separate restore/publish/runtime — layer cache hits matter more than trimming for most teams.

## See also

- Sustained throughput and p50/p95/p99 baselines: `docs/LOAD_TEST_BASELINE.md` (k6 against Compose `full-stack`, plus scaling thresholds).

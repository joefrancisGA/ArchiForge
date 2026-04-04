# Load testing (k6)

**Objective:** Exercise HTTP paths under configurable concurrency without treating results as production SLAs.

## Scripts

| File | Purpose |
|------|--------|
| [k6-expensive-rate-limit.js](k6-expensive-rate-limit.js) | Burst traffic to validate **429** on expensive rate-limit policies. |
| [k6-scenarios.js](k6-scenarios.js) | **Multi-scenario** read load: `GET /v1/compare`, `GET /v1/architecture/run/{id}`, `GET /v1/advisory/runs/{id}/recommendations`. Optional **`POST /v1/architecture/request`** when `ARCHIFORGE_LOAD_TEST_WRITES=true` (Simulator + ExecuteAuthority key). |

## Compare / run detail / advisory (scenarios)

1. Start the API with **ReadAuthority** credentials your k6 script will send (`X-Api-Key` when using ApiKey mode).
2. Set **real** GUIDs if you expect **200**; placeholders return **404** but still validate auth, routing, and rate limits.

```bash
k6 run scripts/load/k6-scenarios.js

k6 run --vus 15 --duration 45s \
  -e ARCHIFORGE_BASE_URL=https://localhost:7123 \
  -e ARCHIFORGE_API_KEY=your-reader-key \
  -e ARCHIFORGE_RUN_ID=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee \
  -e ARCHIFORGE_COMPARE_BASE_RUN_ID=... \
  -e ARCHIFORGE_COMPARE_TARGET_RUN_ID=... \
  scripts/load/k6-scenarios.js
```

Per-scenario VUs/durations (optional): `K6_COMPARE_VUS`, `K6_COMPARE_DURATION`, `K6_RUN_DETAIL_VUS`, `K6_ADVISORY_VUS`, etc.

## Authority run pipeline (POST — opt-in)

`POST /v1/architecture/request` drives the coordinator + agent batch (LLM cost in **Real** mode). The shared [k6-scenarios.js](k6-scenarios.js) script includes a write scenario **only** when:

```bash
-e ARCHIFORGE_LOAD_TEST_WRITES=true
```

**Requirements:** API in **`AgentExecution:Mode=Simulator`**, dedicated non-prod scope/data, an API key with **ExecuteAuthority**, and low `K6_ARCH_REQUEST_VUS` (defaults to 2). Without the env var, the scenario object is omitted entirely so `k6 run` stays read-only.

## Benchmarks (.NET)

Micro-benchmarks and simulated parallel batch timing live in **`ArchiForge.Benchmarks`**. Run locally (Release):

```bash
dotnet run -c Release --project ArchiForge.Benchmarks -- --filter '*'
```

CI compiles this project via the main solution build; **no baseline regression gate** (wall times are runner-dependent). Use BenchmarkDotNet output for before/after comparisons on the same machine.

## Related docs

- [docs/runbooks/LOAD_TEST_RATE_LIMITS.md](../../docs/runbooks/LOAD_TEST_RATE_LIMITS.md)
- [docs/SQL_TOP5_QUERY_PLANS.md](../../docs/SQL_TOP5_QUERY_PLANS.md) (frequent read SQL + plan capture)

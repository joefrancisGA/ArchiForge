> **Scope:** Developers interpreting in-process `Category=Slow` / core-pilot timing targets in API tests — not production latency SLOs or load-test methodology.

# Performance baselines (in-process)

Targets for the **core pilot flow** regression tests in `ArchLucid.Api.Tests` (`[Trait("Category", "Slow")]`). These are **in-process** measurements against the default API test host: **AgentExecution:Mode=Simulator** and **ArchLucid:StorageProvider=InMemory** — not representative of production (no production SQL, no external clients).

| Operation | Target | Measured (CI / local) | Environment |
|----------|--------|------------------------|-------------|
| Create run | Contributes to E2E < 10s; per-step ms in test output | *placeholder* | In-process simulator + in-memory |
| Seed results | Contributes to E2E < 10s; per-step ms in test output | *placeholder* | Same |
| Commit | Contributes to E2E < 10s; per-step ms in test output | *placeholder* | Same |
| Retrieve manifest | Contributes to E2E < 10s; per-step ms in test output | *placeholder* | Same |
| Manifest p95 (10× GET) | < 500ms | *placeholder* | Same |

**E2E gate:** create run → seed fake results → commit → retrieve manifest must complete in **< 10 seconds** total (generous in-process cap).

**Test class:** `ArchLucid.Api.Tests/Performance/CorePilotFlowPerformanceTests.cs`.

**Run (this class only, from repo root):** `dotnet test ArchLucid.Api.Tests --filter "FullyQualifiedName~CorePilotFlowPerformanceTests"`.

**Run (all `Category=Slow` in API tests):** `dotnet test ArchLucid.Api.Tests --filter "Category=Slow"`.

**Note:** Filling the "measured" column is optional: copy from test output or CI when comparing branches; no checked-in published baseline file is required for this tier.

---

## Real-mode E2E benchmark (time-to-value)

For a **defensible, publishable** time-to-value figure using actual Azure OpenAI execution (not simulator), run the dedicated benchmark script:

```bash
ARCHLUCID_BASE_URL=https://staging.archlucid.net \
ARCHLUCID_API_KEY=<staging-key> \
k6 run tests/load/real-mode-e2e-benchmark.js
```

This measures wall-clock time from request creation through real LLM-powered analysis to manifest retrieval. See [`tests/load/README.md`](../../tests/load/README.md) for environment variables, thresholds, and custom metrics.

| Metric | Target | Measured | Environment |
|--------|--------|----------|-------------|
| `e2e_wall_clock_ms` p50 | < 120s | *pending first staging run* | Staging, real Azure OpenAI |
| `e2e_wall_clock_ms` p95 | < 180s | *pending first staging run* | Same |
| `step_poll_wait_ms` p50 | < 90s | *pending first staging run* | Same |

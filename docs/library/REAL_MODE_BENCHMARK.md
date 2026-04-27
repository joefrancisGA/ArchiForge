> **Scope:** Real-mode end-to-end benchmark (request → committed manifest) — how to run, interpret, and compare results.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Real-mode end-to-end benchmark

**Audience:** Engineers and evaluators who want to measure wall-clock time from architecture request submission to a committed manifest when the API uses Azure OpenAI (real mode) rather than the deterministic simulator.

## What it measures

The benchmark exercises the full request-to-manifest pipeline:

| Phase | API call | What happens |
| --- | --- | --- |
| **Create** | `POST /v1/architecture/request` | Validates the brief, creates a run, dispatches agent tasks. |
| **Execute** | `POST /v1/architecture/run/{id}/execute` + poll `GET /v1/architecture/run/{id}` | Agents produce results (LLM calls in real mode, deterministic stubs in simulator). |
| **Commit** | `POST /v1/architecture/run/{id}/commit` | Merges agent results into a versioned architecture manifest. |

The script records wall-clock milliseconds for each phase and prints a JSON summary.

## How to run

### Prerequisites

1. The ArchLucid API is running and reachable (e.g. via `scripts/demo-start.ps1` or Docker Compose).
2. For **real-mode** results, the API stack must be started with `AgentExecution__Mode=Real` and Azure OpenAI credentials configured per [`docs/library/FIRST_REAL_VALUE.md`](FIRST_REAL_VALUE.md).
3. Set **`ARCHLUCID_REAL_AOAI=1`** in the shell where you run the script. Without this, the script warns that results reflect simulator latency.

### Run the script

```powershell
# Simulator mode (no AOAI — fast, deterministic)
pwsh ./scripts/benchmark-real-mode-e2e.ps1

# Real mode (AOAI required)
$env:ARCHLUCID_REAL_AOAI = "1"
pwsh ./scripts/benchmark-real-mode-e2e.ps1

# Custom base URL and timeout
pwsh ./scripts/benchmark-real-mode-e2e.ps1 -BaseUrl http://localhost:5128 -TimeoutSeconds 300
```

### Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `-BaseUrl` | `$env:ARCHLUCID_API_BASE_URL` or `http://localhost:5000` | API base URL. |
| `-TimeoutSeconds` | `600` (10 minutes) | Max seconds to wait for execution to complete. |
| `-PollIntervalSeconds` | `5` | Seconds between status polls during execution. |

### Output

The script prints a JSON object to stdout:

```json
{
  "mode": "Real",
  "totalMs": 142370.12,
  "createMs": 312.45,
  "executeMs": 141805.30,
  "commitMs": 252.37,
  "runId": "a1b2c3d4...",
  "timestamp": "2026-04-26T22:00:00.0000000+00:00"
}
```

Exit code **0** on success, **1** on any failure (API unreachable, timeout, run enters `Failed` status).

## Expected time ranges

| Phase | Simulator | Real (Azure OpenAI) | Notes |
| --- | --- | --- | --- |
| **Create** | < 1 s | < 1 s | Network + SQL insert; independent of execution mode. |
| **Execute** | < 5 s | 30 s – 5 min | Dominates total time. In real mode, depends on LLM latency, token budget, and agent count. |
| **Commit** | < 2 s | < 2 s | Merge + SQL write; independent of execution mode. |
| **Total** | < 10 s | 30 s – 5 min | **Target: under 5 minutes from request to committed manifest.** |

## Target

> **Under 5 minutes** from `POST /v1/architecture/request` to a committed manifest in real mode.

This target reflects a design goal for evaluator experience — an architecture run should complete within a single "wait" interaction, not require the user to come back later.

## What affects execution time

| Factor | Impact | Mitigation |
| --- | --- | --- |
| **LLM model latency** | High — each agent makes one or more completion calls. | Use a deployment with adequate TPM (tokens per minute). GPT-4o is faster than GPT-4. |
| **Token budget** | Moderate — `AZURE_OPENAI_MAX_COMPLETION_TOKENS` caps output length. Lower values finish faster but may truncate agent results. | Default is 1024 tokens per agent call; increase for richer output, decrease for speed. |
| **Agent count** | Moderate — more agent types means more LLM round trips. | Agents execute in parallel where the orchestrator allows; parallelism limits are internal. |
| **Network** | Low — API-to-AOAI latency. | Co-locate API and Azure OpenAI in the same region. |
| **SQL / infrastructure** | Low — create and commit are fast. | Cold-start on first run may add 1–3 s; warm runs are sub-second. See [`LOAD_TEST_BASELINE.md`](LOAD_TEST_BASELINE.md). |
| **Polling interval** | Cosmetic — does not affect execution, but a long interval adds up to one interval of observation delay. | Default 5 s is a good balance; lower for CI, higher for manual runs. |

## How to interpret results

1. **Compare `executeMs` across runs.** Create and commit are bounded by infrastructure and should be stable. Execution time is the variable that reflects LLM and orchestration performance.
2. **Simulator vs. real delta** tells you the LLM overhead. If simulator takes 3 s and real takes 180 s, the LLM adds ~177 s — use this to evaluate model / deployment choices.
3. **Consistent timeouts** mean either the LLM deployment is throttled (check Azure OpenAI metrics for 429s), the token budget is too high (agents produce long outputs that take more time), or agents are failing and retrying.
4. **`Failed` status** during polling means an agent could not complete. Check `docs/runbooks/AGENT_EXECUTION_FAILURES.md` for triage.

## Relationship to other benchmarks

| Benchmark | Purpose | Location |
| --- | --- | --- |
| **This script** | Real-mode E2E wall-clock (request → manifest) | `scripts/benchmark-real-mode-e2e.ps1` |
| **`benchmark-e2e-time.ps1`** | General E2E with `-Mode` switch and `-Repeat` for multi-run stats | `scripts/benchmark-e2e-time.ps1` |
| **Load test baseline** | Throughput and latency under concurrent load (k6) | `scripts/load/hotpaths.js`, [`LOAD_TEST_BASELINE.md`](LOAD_TEST_BASELINE.md) |
| **BenchmarkDotNet micro** | CPU-level merge, paging, dispatch micro-benchmarks | `ArchLucid.Benchmarks/` |
| **CI smoke** | Merge-blocking latency gates on hot paths | `tests/load/ci-smoke.js` |

## Security

- **Never commit Azure OpenAI keys.** The script reads `ARCHLUCID_API_KEY` and Azure OpenAI credentials from the environment only.
- The script is a read/write HTTP client; it creates runs and commits manifests. Run against a **development or demo** instance, not production.

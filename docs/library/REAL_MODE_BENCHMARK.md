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

The script records wall-clock milliseconds for each phase, prints JSON to stdout, and (unless `-SkipArtifact`) writes **`artifacts/benchmark-real-mode-latest.json`** at the repository root. For how to combine this file with k6 outputs and ROI tables for sponsors, see [`PROOF_OF_VALUE_SNAPSHOT.md`](PROOF_OF_VALUE_SNAPSHOT.md).

## How to run

### Prerequisites

1. The ArchLucid API is running and reachable (e.g. via `scripts/demo-start.ps1` or Docker Compose).
2. For **real-mode** results, the API stack must be started with `AgentExecution__Mode=Real` and Azure OpenAI credentials configured per [`docs/library/FIRST_REAL_VALUE.md`](FIRST_REAL_VALUE.md).
3. Optionally set **`ARCHLUCID_REAL_AOAI=1`** in the shell — the value is recorded in the benchmark JSON artifact (metadata only); **real-mode execution depends on API host configuration** (`AgentExecution__Mode=Real` plus Azure OpenAI per [`FIRST_REAL_VALUE.md`](FIRST_REAL_VALUE.md)).

### Run the script

```powershell
# Default: writes artifacts/benchmark-real-mode-latest.json (UTF-8, no BOM — folder is gitignored) and prints JSON
pwsh ./scripts/benchmark-real-mode-e2e.ps1 -SkipArtifact   # stdout only (no artifact file)

# Real-mode evaluation metadata in JSON (does not toggle server mode — configure the API host for AOAI)
$env:ARCHLUCID_REAL_AOAI = "1"
pwsh ./scripts/benchmark-real-mode-e2e.ps1

# Custom base URL, timeout, and an extra CI copy beside the canonical artifact
pwsh ./scripts/benchmark-real-mode-e2e.ps1 -BaseUrl http://localhost:5128 -TimeoutSeconds 600 -OutputFile $env:TEMP/real-mode-benchmark.json

# Disable the default artifact entirely (stdout only; add -OutputFile if you need a file)
pwsh ./scripts/benchmark-real-mode-e2e.ps1 -SkipArtifact

# Alternate canonical artifact path (still respects -SkipArtifact)
pwsh ./scripts/benchmark-real-mode-e2e.ps1 -ArtifactPath ./local-proofs/real-mode-benchmark.json
```

### Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `-BaseUrl` | `$env:ARCHLUCID_API_BASE_URL` or `http://localhost:5000` | API base URL. |
| `-TimeoutSeconds` | `300` | Max seconds to wait for execution to reach `ReadyForCommit`. |
| `-PollIntervalSeconds` | `3` | Seconds between status polls during execution. |
| `-ArtifactPath` | `<repo root>/artifacts/benchmark-real-mode-latest.json` | Canonical JSON artifact path (`artifacts/` exists at repo root; ignored by Git). |
| `-SkipArtifact` | off | Omit writing the `-ArtifactPath` file — stdout JSON only (`-OutputFile` still honored). |
| `-OutputFile` | none | Additional copy of the same JSON (e.g. CI upload staging). |

### Artifact schema (`kind`: `archlucid.benchmark.realModeE2e.v1`)

The emitted JSON matches **`schemaVersion` `1.0.0`**. Completed runs expose phase timings under **`timings`**, **`run`** metadata, **`targets`** (including five-minute wall-clock), and **`environment`** (**no secrets** — only booleans/nulls/strings such as **`archLucidRealAoai`** and **`apiKeyEnvPresent`**). When **`status`** is **`api_unreachable`**, the document includes **`error.message`** instead of **`timings`** / **`run`** / **`targets`**.

Successful run (truncated illustrative values):

```json
{
  "schemaVersion": "1.0.0",
  "kind": "archlucid.benchmark.realModeE2e.v1",
  "status": "completed",
  "environment": {
    "baseUrl": "http://localhost:5000",
    "timeoutSeconds": 300,
    "pollIntervalSeconds": 3,
    "archLucidRealAoai": null,
    "apiKeyEnvPresent": false
  },
  "run": {
    "runId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "executionMode": "Real",
    "pilotRealHeaderApplied": true
  },
  "timings": {
    "createMs": 312.45,
    "executeMs": 141805.3,
    "commitMs": 252.37,
    "totalMs": 142370.12,
    "totalSec": 142.37
  },
  "targets": {
    "totalWallClockUnderFiveMinutes": true
  },
  "meta": {
    "timestampUtc": "2026-04-26T22:00:00.0000000+00:00"
  }
}
```

Exit codes: **0** when the artifact is produced or when the API is unreachable (deterministic unreachable JSON — still useful for tooling). **1** when the run fails validation, poll errors, timeouts, **`Failed`** status mid-run, or commit errors.

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
| **Polling interval** | Cosmetic — does not affect execution, but a long interval adds up to one interval of observation delay. | Default 3 s is a balance; tighten for CI, loosen for noisy networks. |

## How to interpret results

1. **Compare `timings.executeMs` across runs.** Create and commit are bounded by infrastructure and should be stable. Execution time is the variable that reflects LLM and orchestration performance.
2. **Simulator vs. real delta** tells you the LLM overhead. If simulator takes 3 s and real takes 180 s, the LLM adds ~177 s — use this to evaluate model / deployment choices.
3. **Consistent timeouts** mean either the LLM deployment is throttled (check Azure OpenAI metrics for 429s), the token budget is too high (agents produce long outputs that take more time), or agents are failing and retrying.
4. **`Failed` status** during polling means an agent could not complete. Check `docs/runbooks/AGENT_EXECUTION_FAILURES.md` for triage.

## Relationship to other benchmarks

| Benchmark | Purpose | Location |
| --- | --- | --- |
| **This script** | Real-mode E2E wall-clock (request → manifest) | `scripts/benchmark-real-mode-e2e.ps1` |
| **`benchmark-e2e-time.ps1`** | General E2E with `-Mode` switch and `-Repeat` for multi-run stats | `scripts/benchmark-e2e-time.ps1` |
| **Load test baseline** | Throughput and latency under concurrent load (k6 `--summary-export` JSON) | `scripts/load/hotpaths.js`, [`LOAD_TEST_BASELINE.md`](LOAD_TEST_BASELINE.md) — see [K6 summary export JSON](#k6-summary-export-json) below |
| **Proof-of-value bundle** | Single sponsor narrative from bench + load + ROI + trace completeness | [`PROOF_OF_VALUE_SNAPSHOT.md`](PROOF_OF_VALUE_SNAPSHOT.md) |
| **BenchmarkDotNet micro** | CPU-level merge, paging, dispatch micro-benchmarks | `ArchLucid.Benchmarks/` |
| **CI smoke** | Merge-blocking latency gates on hot paths | `tests/load/ci-smoke.js` |

## K6 summary export JSON

[k6 ends each run with aggregated metrics](https://grafana.com/docs/k6/latest/results-output/end-of-test/json/). Typical CI usage:

```bash
k6 run tests/load/ci-smoke.js --summary-export ./k6-ci-summary.json
```

The JSON has a **`metrics`** object. Each logical metric (**`http_req_duration`**, **`http_req_failed`**, **`http_reqs`**, **`checks`**, per-tag names such as **`http_req_duration{k6ci:create_run}`**, …) exposes either a **`values`** map ( **`med`**, **`p(95)`**, **`p(99)`**, **`rate`**, …) or duplicate trend keys on the metric object — parsers in this repo accept both (**`scripts/ci/assert_k6_ci_smoke_summary.py`**, **`scripts/ci/print_k6_summary_metrics.py`**).

**`http_req_failed`** uses **`rate`** (0–1 fraction of failed HTTP requests). Use these aggregates alongside **`timings`** from the PowerShell real-mode artifact: PowerShell benchmarks **one** full run-to-commit latency; k6 summarizes **many** synthetic HTTP iterations (often concurrently).

Abbreviated fragment:

```json
{
  "metrics": {
    "http_req_duration": {
      "type": "trend",
      "values": { "med": 38.9, "p(95)": 150.2, "p(99)": 400.0 }
    },
    "http_req_failed": { "values": { "rate": 0.0 } },
    "http_req_duration{k6ci:health_live}": { "values": { "p(95)": 12.5 } }
  }
}
```

## Security

- **Never commit Azure OpenAI keys.** The script reads `ARCHLUCID_API_KEY` and Azure OpenAI credentials from the environment only.
- The script is a read/write HTTP client; it creates runs and commits manifests. Run against a **development or demo** instance, not production.

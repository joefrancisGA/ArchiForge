> **Scope:** Agent execution failures - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Agent execution failures

**Last reviewed:** 2026-04-24

**Audience:** Operators and on-call engineers triaging failed or stuck architecture runs after `POST .../runs/{runId}/execute` (or internal `ExecuteRunAsync`).

## Symptoms

- HTTP **500** / **409** from execute, or run stuck in **TasksGenerated** / **WaitingForResults** while logs show agent errors.
- Audit events such as **Architecture.RunFailed** with exception type names after **Architecture.RunStarted**.
- **Real** mode: Azure OpenAI timeouts, 429s, or empty model output; **Simulator** mode: handler gaps or invalid synthetic payloads.

## System boundaries (for diagrams)

- **Nodes:** API → `ArchitectureRunService` → `IAgentExecutor` → per-`AgentType` handlers → optional LLM / tools; persistence: `AgentResults`, `AgentEvidencePackages`, `AgentExecutionTraces`, `Runs` (authority header).
- **Edges:** Request + tasks + evidence package in; results + evaluations + status **ReadyForCommit** out.
- **Flows:** Happy path persists evidence package, bulk results, evaluations, then status update inside a transaction.

## Triage checklist

1. **Confirm run state**  
   Load the run row: expected path is **TasksGenerated** (or **WaitingForResults**) before execute, **ReadyForCommit** after success. If status is **ReadyForCommit** / **Committed** with no results, storage may be inconsistent (see **Conflict** behavior in application logs).

2. **Check `AgentExecution:Mode`**  
   - **Simulator:** failures are usually deterministic (missing handler, validation).  
   - **Real:** verify `AzureOpenAI:*` (endpoint, key, deployment), quotas, and network egress (private endpoints, firewall).

2a. **Local `archlucid try --real` (first real value)**  
   - Preconditions: shell **`ARCHLUCID_REAL_AOAI=1`**, **`AZURE_OPENAI_ENDPOINT`**, **`AZURE_OPENAI_API_KEY`**, **`AZURE_OPENAI_DEPLOYMENT_NAME`** (CLI preflight).  
   - If execute returns **4xx/5xx** or the run reaches **Failed** while the CLI is in real mode without **`--strict-real`**, the operator loop **falls back** to **`seed-fake-results`** with **`pilotTryRealModeFellBack=true`**, sets **`Runs.RealModeFellBackToSimulator`**, emits **`FirstRealValueRunFellBackToSimulator`**, and prepends a **warning callout** to the first-value Markdown.  
   - **`--strict-real`:** same path but **no fallback** — the command fails so CI or smoke cannot mask a broken AOAI configuration.  
   - Full operator narrative: **[`docs/library/FIRST_REAL_VALUE.md`](../library/FIRST_REAL_VALUE.md)**.

3. **Inspect traces**  
   When SQL storage is enabled, `dbo.AgentExecutionTraces` (and logs) show parse success/failure and redacted prompts. Correlation: **RunId** + **TaskId**.

4. **Schema validation**  
   Invalid agent JSON may fail merge or persistence. Ensure schema files configured under `SchemaValidation:*SchemaPath` exist on the host and match the contract version.

5. **Retry posture**  
   Execute is designed to be retried with transactional persistence; if partial failure occurred, check for duplicate-key or orphan rows only if a bug regressed (contract tests cover replace semantics for evidence packages and results per run).

## Security

- Traces may contain sensitive prompts; restrict SQL and log access; do not paste raw traces into untrusted channels.

## Reliability & cost

- **Real** mode: monitor token usage and rate limits; backoff and circuit breakers live in the OpenAI client path.  
- **Simulator:** prefer for CI and load tests to avoid spend.

## Related docs

- `docs/BUILD.md` — configuration and test SQL variables.  
- `docs/ALERTS.md` — alert routes (separate from agent execution).  
- `SECRET_AND_CERT_ROTATION.md` — API keys and endpoints.

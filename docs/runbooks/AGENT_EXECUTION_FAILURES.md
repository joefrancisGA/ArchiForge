# Agent execution failures

**Audience:** Operators and on-call engineers triaging failed or stuck architecture runs after `POST .../runs/{runId}/execute` (or internal `ExecuteRunAsync`).

## Symptoms

- HTTP **500** / **409** from execute, or run stuck in **TasksGenerated** / **WaitingForResults** while logs show agent errors.
- Audit events such as **Architecture.RunFailed** with exception type names after **Architecture.RunStarted**.
- **Real** mode: Azure OpenAI timeouts, 429s, or empty model output; **Simulator** mode: handler gaps or invalid synthetic payloads.

## System boundaries (for diagrams)

- **Nodes:** API → `ArchitectureRunService` → `IAgentExecutor` → per-`AgentType` handlers → optional LLM / tools; persistence: `AgentResults`, `AgentEvidencePackages`, `AgentExecutionTraces`, `ArchitectureRuns`.
- **Edges:** Request + tasks + evidence package in; results + evaluations + status **ReadyForCommit** out.
- **Flows:** Happy path persists evidence package, bulk results, evaluations, then status update inside a transaction.

## Triage checklist

1. **Confirm run state**  
   Load the run row: expected path is **TasksGenerated** (or **WaitingForResults**) before execute, **ReadyForCommit** after success. If status is **ReadyForCommit** / **Committed** with no results, storage may be inconsistent (see **Conflict** behavior in application logs).

2. **Check `AgentExecution:Mode`**  
   - **Simulator:** failures are usually deterministic (missing handler, validation).  
   - **Real:** verify `AzureOpenAI:*` (endpoint, key, deployment), quotas, and network egress (private endpoints, firewall).

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

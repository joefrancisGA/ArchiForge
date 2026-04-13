# Agent execution trace forensics (full prompt storage)

## Objective

Enable **replayable** inspection of exact LLM inputs and outputs for a single agent step when investigations or quality regressions require more than the **8 192-character** truncated fields on **`AgentExecutionTrace`**.

## Model

| Element | Behavior |
|---------|----------|
| **Config gate** | **`AgentExecution:TraceStorage:PersistFullPrompts`** (default **true**; override per environment if retention policy forbids full prompts). |
| **Blob layout** | Container **`agent-traces`**; blobs **`{runId}/{traceId}/system-prompt.txt`**, **`user-prompt.txt`**, **`response.txt`**. |
| **Stored pointers** | **`FullSystemPromptBlobKey`**, **`FullUserPromptBlobKey`**, **`FullResponseBlobKey`** on the trace row (opaque URI from **`IArtifactBlobStore.WriteAsync`** — e.g. **`file://`** locally, **`https://`** in Azure). |
| **Truncation** | Unchanged: **`SystemPrompt`**, **`UserPrompt`**, **`RawResponse`** in **`TraceJson`** stay capped at **8192** chars for quick SQL reads. |
| **Model metadata** | **`ModelDeploymentName`** and **`ModelVersion`** on **`AgentExecutionTrace`** when callers supply them (optional). |

## Retrieving content for a trace

1. Load the trace row (API internal path or SQL **`AgentExecutionTraces`**).
2. Read the three blob key columns (or the same properties inside **`TraceJson`**).
3. Call **`IArtifactBlobStore.ReadAsync(blobUri)`** (or your cloud console / `az storage blob download`) using that URI.

If keys are **null**, full-text persistence was **off**, **failed**, or not yet completed (async).

## Privacy and retention

Full prompts may contain **customer architecture details, credentials in prose, or personal data**. Treat blob containers with the same classification as **application secrets adjacent data**. Align lifecycle with **`docs/AUDIT_RETENTION_POLICY.md`** and your org’s data-retention policy; do not enable **`PersistFullPrompts`** in environments where long-lived sensitive prompt retention is prohibited.

## DDL

Schema additions ship in migration **`053_AgentExecutionTrace_FullPromptBlobKeys.sql`** and **`ArchLucid.Persistence/Scripts/ArchLucid.sql`**.

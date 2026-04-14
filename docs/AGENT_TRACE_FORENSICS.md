# Agent execution trace forensics (full prompt storage)

## Objective

Enable **replayable** inspection of exact LLM inputs and outputs for a single agent step when investigations or quality regressions require more than the **8 192-character** truncated fields on **`AgentExecutionTrace`**.

## Model

| Element | Behavior |
|---------|----------|
| **Full prompts (Real LLM)** | After each trace insert, **`AgentExecutionTraceRecorder`** writes full system prompt, user prompt, and raw response to blob with retries; on failure or timeout, **`Full*Inline`** SQL columns are patched so each field has **either** a blob key **or** inline text (never both absent for Real execution). **`AgentExecution:TraceStorage:BlobPersistenceTimeoutSeconds`** caps blob wall time (default **30** s, clamped **5–300**). |
| **Simulator traces** | **`SimulatorExecutionTraceRecordingExecutor`** passes **`isSimulatorExecution: true`** — **no** blob writes and **no** inline full-text patch; forensic surface is the **8192-character** truncated columns on the row (deterministic payloads). |
| **Blob layout** | Container **`agent-traces`**; blobs **`{runId}/{traceId}/system-prompt.txt`**, **`user-prompt.txt`**, **`response.txt`**. |
| **Stored pointers** | **`FullSystemPromptBlobKey`**, **`FullUserPromptBlobKey`**, **`FullResponseBlobKey`** on the trace row (opaque URI from **`IArtifactBlobStore.WriteAsync`** — e.g. **`file://`** locally, **`https://`** in Azure). |
| **SQL inline fallback** | When a blob write fails or times out, **`FullSystemPromptInline`**, **`FullUserPromptInline`**, **`FullResponseInline`** (`NVARCHAR(MAX)`, migrations **062**+) store the missing full text so forensics can recover without blob (same privacy class as blobs). |
| **Truncation** | Unchanged: **`SystemPrompt`**, **`UserPrompt`**, **`RawResponse`** in **`TraceJson`** stay capped at **8192** chars for quick SQL reads. |
| **Model metadata** | **`ModelDeploymentName`** and **`ModelVersion`** on **`AgentExecutionTrace`** — see **sentinel values** below. |

### Sentinel values (nullable columns, non-null strings)

The SQL columns **`ModelDeploymentName`** and **`ModelVersion`** are **nullable**, but persisted traces use **non-null sentinel strings** when the runtime does not have a real deployment name or version:

| Scenario | `ModelDeploymentName` | `ModelVersion` |
| --- | --- | --- |
| Real LLM path, value missing/blank | **`unspecified-deployment`** | **`unspecified-model-version`** |
| Simulator / offline | **`AgentExecution:Simulator`** | **`deterministic-1.0`** |

Constants live in **`ArchLucid.Contracts.Agents.AgentExecutionTraceModelMetadata`**. Forensics queries should filter on real names (exclude these sentinels) when building “model mix” dashboards.

## Retrieving content for a trace

1. Load the trace row (API internal path or SQL **`AgentExecutionTraces`**).
2. Read the three blob key columns (or the same properties inside **`TraceJson`**).
3. Call **`IArtifactBlobStore.ReadAsync(blobUri)`** (or your cloud console / `az storage blob download`) using that URI.

If blob keys are **null**, check the three **`Full*Inline`** columns (or the same properties in **`TraceJson`**) for text persisted when blob upload failed. **Simulator** rows normally have **null** blob keys and **null** inlines by design — use truncated columns. For **Real** rows, if blob keys and inlines are all empty for a field after a successful record path, treat as an incident (unexpected).

## Privacy and retention

Full prompts may contain **customer architecture details, credentials in prose, or personal data**. Treat blob containers **and** SQL **`Full*Inline`** columns with the same classification as **application secrets adjacent data**. Align lifecycle with **`docs/AUDIT_RETENTION_POLICY.md`** and your org’s data-retention policy. There is **no** configuration flag to skip Real-mode full prompt persistence; only **Simulator** mode avoids blob/inline full-text writes.

## Reliability

### Inline persistence (after trace insert)

**`AgentExecutionTraceRecorder`** awaits full-prompt/blob writes **after** the trace row is created (still outside the SQL transaction that owns the row — blobs use **`IArtifactBlobStore`**). A **linked cancellation token** caps wall-clock time using **`AgentExecution:TraceStorage:BlobPersistenceTimeoutSeconds`** (default **30**, clamped **5–300**). On **timeout**, **partial blob failure**, or **unexpected exception**, the recorder:

- **Patches** the trace row with whatever blob keys succeeded and sets **`BlobUploadFailed`** when appropriate.
- Emits **durable audit** **`AgentTraceBlobPersistenceFailed`** (payload includes `traceId`, `runId`, `agentType`, `reason`, `failedBlobTypes`).
- **Mandatory inline + verification (Real mode):** after blob issues, the recorder patches missing parts into SQL **`Full*Inline`** (same as before), then **reloads** the trace row and checks that every **non-empty** prompt/response segment has either a blob key or inline text. If the inline **`UPDATE`** throws, the trace row is missing on read, or verification still fails, it sets **`InlineFallbackFailed`** (migration **064**, nullable `BIT`), emits **`AgentTraceInlineFallbackFailed`**, and logs — **without** changing the agent step outcome (same contract as blob audit).
- Records histogram **`archlucid_agent_trace_blob_persist_duration_ms`** with label **`agent_type`**.

Execute path latency includes this work; the trade-off is **forensic completeness** (operators see **`BlobUploadFailed`** / **`InlineFallbackFailed`** and audit rows instead of silent missing blobs).

### Retry behaviour

Each blob write (system prompt, user prompt, response) is retried up to **3 total attempts** with a **fixed 500 ms** delay between failed attempts (so at most ~1 s of backoff per blob before the final try). If all attempts fail for any blob, the `BlobUploadFailed` flag is set to **true** on the trace row (nullable `BIT` column added in migration **056**). Operators can query for traces with failed uploads:

```sql
SELECT TraceId, RunId, CreatedUtc
FROM dbo.AgentExecutionTraces
WHERE BlobUploadFailed = 1;
```

Traces where mandatory inline fallback or post-patch verification failed:

```sql
SELECT TraceId, RunId, CreatedUtc
FROM dbo.AgentExecutionTraces
WHERE InlineFallbackFailed = 1;
```

### OTel counters

| Name | Labels | When incremented |
|------|--------|------------------|
| **`archlucid_agent_trace_blob_upload_failures_total`** | `agent_type`, `blob_type` | Each blob part that exhausts all retries without a key. |
| **`archlucid_agent_trace_prompt_inline_fallback_total`** | `agent_type`, `blob_type` (`system_prompt` / `user_prompt` / `response`) | **Real** execution only: each full-text field stored in SQL **`Full*Inline`** because the blob key for that part is missing. |

Correlate with blob-storage availability metrics in dashboards.

### Design rationale

**Awaited** persistence (with timeout) ensures the run does not move on while operators still see **null** blob keys for content that was intended to be retained. The retry loop adds transient-fault tolerance without introducing a dependency on Polly in the recorder. The `BlobUploadFailed` flag and **`AgentTraceBlobPersistenceFailed`** audit give a queryable signal to re-upload or investigate without scanning logs alone.

## DDL

Schema additions ship in migrations **`053`**, **`056`**, **`062`** (inline columns), **`064`** (**`InlineFallbackFailed`**), **`065`** (filtered index **`IX_AgentExecutionTraces_InlineFallbackFailed`**), and **`ArchLucid.Persistence/Scripts/ArchLucid.sql`**.

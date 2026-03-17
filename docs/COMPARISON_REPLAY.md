## Comparison replay in ArchiForge

Comparison replay lets you take a **previously persisted comparison record** and:

- Regenerate the comparison summary from the stored payload  
- Export the comparison again in various formats (Markdown / HTML / DOCX / PDF\*)  
- Optionally **persist the replay** as a new comparison record  
- Optionally **verify** that a regenerated comparison still matches the stored payload

Supported comparison types:

- **End-to-end replay** – full run‑vs‑run comparison (`ComparisonType = "end-to-end-replay"`)  
- **Export-record diff** – diff between two export records (`ComparisonType = "export-record-diff"`)

\*PDF is currently supported for end‑to‑end replay only.

---

### Core concepts

- **Comparison record** (`ComparisonRecord`)  
  Created by comparison APIs (e.g. end‑to‑end run compare, export-record diff summary) when `persist` is enabled. Stores:
  - `ComparisonRecordId`
  - `ComparisonType` (`"end-to-end-replay"` or `"export-record-diff"`)
  - `PayloadJson` – the serialized comparison payload
  - `SummaryMarkdown` – stored summary, when applicable

- **Replay modes** (`ReplayMode`)  
  Controlled by the `replayMode` field on the replay request:
  - `"artifact"` (default) – **artifact replay**; use the stored payload as‑is  
  - `"regenerate"` – rerun the comparison from the original runs/exports  
  - `"verify"` – regenerate and compare against stored payload, returning **drift analysis**

- **Export profiles** (`Profile`)  
  For end‑to‑end replay, you can select an export profile:
  - `"default"` (or `null`)  
  - `"short"` – shorter report, limited sections  
  - `"detailed"` – full sections  
  - `"executive"` – emphasis on executive summary

- **Persisting replays** (`PersistReplay`)  
  When `PersistReplay = true`, a replay operation **creates a new comparison record** linked to the original, and the new ID is returned in `PersistedReplayRecordId`.

---

### Replay request models

**API request body** – `ArchiForge.Api.Models.ReplayComparisonRequest`:

```jsonc
{
  "format": "markdown",      // markdown | html | docx | pdf (pdf: end-to-end only; docx: end-to-end + export-diff)
  "replayMode": "artifact",  // artifact | regenerate | verify
  "profile": "default",      // end-to-end only: default | short | detailed | executive
  "persistReplay": false     // when true, persist this replay as a new comparison record
}
```

**Application request** – `ArchiForge.Application.Analysis.ReplayComparisonRequest`:

- `ComparisonRecordId` – taken from the route parameter  
- `Format` – `"markdown"`, `"html"`, `"docx"`, or `"pdf"`  
- `ReplayMode` – `"artifact"`, `"regenerate"`, `"verify"`  
- `Profile` – optional export profile for end‑to‑end  
- `PersistReplay` – whether to persist a new comparison record for this replay

---

### Replay endpoints

#### 1. Replay as file

```http
POST /v1/architecture/comparisons/{comparisonRecordId}/replay
Content-Type: application/json
```

Body: `ReplayComparisonRequest` (see above).

Behaviors:

- **End-to-end replay** (`ComparisonType = "end-to-end-replay"`):
  - `format = "markdown"` → `text/markdown` file
  - `format = "html"` → `text/html` file
  - `format = "docx"` → Word document
  - `format = "pdf"` → PDF document

- **Export-record diff** (`ComparisonType = "export-record-diff"`):
  - `format = "markdown"` → `text/markdown` file
  - `format = "docx"` → Word document

If the requested format is not supported for the record type, the API returns **400 Bad Request**.

Response headers:

- `X-ArchiForge-ComparisonRecordId` – original comparison record ID  
- `X-ArchiForge-ComparisonType` – `"end-to-end-replay"` or `"export-record-diff"`  
- `X-ArchiForge-ReplayMode` – `"artifact" | "regenerate" | "verify"`  
- `X-ArchiForge-VerificationPassed` – `true`/`false` for verify mode  
- `X-ArchiForge-VerificationMessage` – human‑readable verification message (optional)  
- `X-ArchiForge-LeftRunId` / `X-ArchiForge-RightRunId` – run IDs (when available)  
- `X-ArchiForge-LeftExportRecordId` / `X-ArchiForge-RightExportRecordId` – export record IDs (when available)  
- `X-ArchiForge-CreatedUtc` – original comparison record timestamp (ISO‑8601)  
- `X-ArchiForge-Format-Profile` – export profile used (end‑to‑end)  
- `X-ArchiForge-PersistedReplayRecordId` – **new comparison record ID**, when `persistReplay = true`

**Example – artifact replay as Markdown**

```bash
curl -X POST \
  "http://localhost:5128/v1/architecture/comparisons/${COMPARISON_RECORD_ID}/replay" \
  -H "Content-Type: application/json" \
  -o comparison.md \
  -d '{
    "format": "markdown",
    "replayMode": "artifact",
    "persistReplay": false
  }'
```

**Example – end-to-end replay as DOCX, persisted as a new record**

```bash
curl -X POST \
  "http://localhost:5128/v1/architecture/comparisons/${COMPARISON_RECORD_ID}/replay" \
  -H "Content-Type: application/json" \
  -D headers.txt \
  -o comparison.docx \
  -d '{
    "format": "docx",
    "replayMode": "regenerate",
    "profile": "executive",
    "persistReplay": true
  }'
```

After the call:

- The DOCX file is written to `comparison.docx`.  
- `headers.txt` will include a new `X-ArchiForge-PersistedReplayRecordId` which you can store or use later.

**Example – export-record diff replay as DOCX**

```bash
curl -X POST \
  "http://localhost:5128/v1/architecture/comparisons/${EXPORT_DIFF_COMPARISON_RECORD_ID}/replay" \
  -H "Content-Type: application/json" \
  -o export-diff.docx \
  -d '{
    "format": "docx",
    "replayMode": "artifact",
    "persistReplay": false
  }'
```

---

#### 2. Replay metadata only

```http
POST /v1/architecture/comparisons/{comparisonRecordId}/replay/metadata
Content-Type: application/json
```

Body: `ReplayComparisonRequest` (same as above).

Returns JSON body:

```jsonc
{
  "comparisonRecordId": "abc123...",
  "comparisonType": "end-to-end-replay",
  "format": "markdown",
  "fileName": "comparison_abc123.md"
}
```

Use this endpoint when you only need the **shape** of the replay (type, format, filename) without downloading the file itself.

---

### Typical flows

#### A. Persisted end-to-end comparison, then replay as Markdown

1. Run an end‑to‑end comparison and **persist** it (e.g., `POST /v1/architecture/run/compare/end-to-end/summary?leftRunId=...&rightRunId=...` with `{ "persist": true }`).  
2. Read `X-ArchiForge-ComparisonRecordId` from the response headers.  
3. Replay and download the summary as Markdown:

```bash
curl -X POST \
  "http://localhost:5128/v1/architecture/comparisons/${COMPARISON_RECORD_ID}/replay" \
  -H "Content-Type: application/json" \
  -o comparison.md \
  -d '{ "format": "markdown" }'
```

#### B. Verify replay and inspect drift

1. Call replay with `replayMode = "verify"`:

```bash
curl -X POST \
  "http://localhost:5128/v1/architecture/comparisons/${COMPARISON_RECORD_ID}/replay" \
  -H "Content-Type: application/json" \
  -D headers.txt \
  -o comparison.md \
  -d '{
    "format": "markdown",
    "replayMode": "verify"
  }'
```

2. Inspect headers:
   - `X-ArchiForge-VerificationPassed: true` / `false`  
   - `X-ArchiForge-VerificationMessage` – high‑level drift message

3. (Optional) Use drift analysis APIs if exposed, or log the replay metadata for audit.

---

### Observability

- **Structured logging**  
  Each replay (file or metadata) is logged with structured properties: `ComparisonRecordId`, `ComparisonType`, `Format`, `ReplayMode`, `PersistReplay`, `DurationMs`, and (for file replay) `VerificationPassed`. Failures (not found, validation) are logged as warnings with the error message.

- **Replay diagnostics endpoint**  
  `GET /v1/architecture/comparisons/diagnostics/replay?maxCount=50` returns the last replay operations (in-memory ring buffer, default capacity 100). Query parameter `maxCount` (1–100) limits how many entries are returned. Each entry includes timestamp, comparison record ID, type, format, replay mode, duration, success flag, verification result (when applicable), and optional error message. Use this to inspect recent replay activity and any verification failures without parsing logs.

---

### Notes and limitations

- **PDF format** is currently only available for end‑to‑end replay (`ComparisonType = "end-to-end-replay"`).  
- **Export-record diff** replays support **Markdown** and **DOCX**.  
- If the underlying runs or export records have been deleted, **regenerate** and **verify** modes may fail; artifact replay continues to work as long as the stored payload is present.  
- Comparison records are immutable; replay persistence creates **new** records rather than mutating existing ones.


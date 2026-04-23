> **Scope:** Comparison replay in ArchLucid - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


## Comparison replay in ArchLucid

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

**API request body** – `ArchLucid.Api.Models.ReplayComparisonRequest`:

```jsonc
{
  "format": "markdown",      // markdown | html | docx | pdf (pdf: end-to-end only; docx: end-to-end + export-diff)
  "replayMode": "artifact",  // artifact | regenerate | verify
  "profile": "default",      // end-to-end only: default | short | detailed | executive
  "persistReplay": false     // when true, persist this replay as a new comparison record
}
```

**Application request** – `ArchLucid.Application.Analysis.ReplayComparisonRequest`:

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

- `X-ArchLucid-ComparisonRecordId` – original comparison record ID  
- `X-ArchLucid-ComparisonType` – `"end-to-end-replay"` or `"export-record-diff"`  
- `X-ArchLucid-ReplayMode` – `"artifact" | "regenerate" | "verify"`  
- `X-ArchLucid-VerificationPassed` – `true`/`false` for verify mode  
- `X-ArchLucid-VerificationMessage` – human‑readable verification message (optional)  
- `X-ArchLucid-LeftRunId` / `X-ArchLucid-RightRunId` – run IDs (when available)  
- `X-ArchLucid-LeftExportRecordId` / `X-ArchLucid-RightExportRecordId` – export record IDs (when available)  
- `X-ArchLucid-CreatedUtc` – original comparison record timestamp (ISO‑8601)  
- `X-ArchLucid-Format-Profile` – export profile used (end‑to‑end)  
- `X-ArchLucid-PersistedReplayRecordId` – **new comparison record ID**, when `persistReplay = true`

---

### Comparison record search (paging + sorting)

`GET /v1/architecture/comparisons` supports searching persisted comparison records with paging:

- `skip` / `limit` for paging  
- `sortDir=asc|desc` (defaults to `desc`)  
- Filters: `comparisonType`, `leftRunId`, `rightRunId`, `leftExportRecordId`, `rightExportRecordId`, `label`, `tag`, `tags`, `createdFromUtc`, `createdToUtc`

Example:

```bash
curl "http://localhost:5128/v1/architecture/comparisons?comparisonType=end-to-end-replay&sortDir=desc&skip=0&limit=20"
```

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
- `headers.txt` will include a new `X-ArchLucid-PersistedReplayRecordId` which you can store or use later.

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
2. Read `X-ArchLucid-ComparisonRecordId` from the response headers.  
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
   - `X-ArchLucid-VerificationPassed: true` / `false`  
   - `X-ArchLucid-VerificationMessage` – high‑level drift message

3. (Optional) Use drift analysis APIs if exposed, or log the replay metadata for audit.

---

### Recipe page

When the API is running, **GET /Docs/replay-recipes** returns an HTML page with a step-by-step flow: list comparisons → get record → replay as file → replay metadata → export drift report. It includes curl examples and a link to Swagger UI.

### Observability

- **Structured logging**  
  Each replay (file or metadata) is logged with structured properties: `ComparisonRecordId`, `ComparisonType`, `Format`, `ReplayMode`, `PersistReplay`, `DurationMs`, and (for file replay) `VerificationPassed`. Failures (not found, validation) are logged as warnings with the error message.

- **Replay diagnostics endpoint**  
  `GET /v1/architecture/comparisons/diagnostics/replay?maxCount=50` returns the last replay operations (in-memory ring buffer, default capacity 100). Query parameter `maxCount` (1–100) limits how many entries are returned. Each entry includes timestamp, comparison record ID, type, format, replay mode, duration, success flag, verification result (when applicable), and optional error message. Use this to inspect recent replay activity and any verification failures without parsing logs.

---

### Tagging and labelling

Comparison records support an optional **label** (short string, e.g. `release-1.2`, `incident-42`) and **tags** (list of strings) for filtering and grouping.

- **PATCH** `/v1/architecture/comparisons/{comparisonRecordId}` with body `{ "label": "…", "tags": ["t1", "t2"] }` to set or update label and tags (pass `null` or empty to clear).
- **GET** search and single-record responses include `label` and `tags`.
- **GET** `/v1/architecture/comparisons?tag=release-1.2` returns only records that have that tag.
- **GET** `/v1/architecture/comparisons?skip=0&limit=20&sortBy=createdUtc&sortDir=desc` supports paging and sorting.
- **Cursor paging**: `/v1/architecture/comparisons?limit=20&sortBy=createdUtc&sortDir=desc` returns `nextCursor`, which you can pass back as `cursor=<nextCursor>` to fetch the next page (cursor format: `<utcTicks>:<comparisonRecordId>`).
- CLI: `archlucid comparisons list` shows label and tags; `archlucid comparisons tag <id> --label x --tag t1 --tag t2` updates them.

### Batch replay

To replay multiple saved comparisons in one request, use:

- **POST** `/v1/architecture/comparisons/replay/batch` → returns a ZIP file containing one exported artifact per record.

### Summary endpoint

To fetch the stored comparison summary (or a regenerated markdown summary if none is stored):

- **GET** `/v1/architecture/comparisons/{comparisonRecordId}/summary`

### Drift report export

**GET** `/v1/architecture/comparisons/{comparisonRecordId}/drift-report?format=markdown|html|docx` runs a stored-vs-regenerated comparison and returns a report file summarizing differences (same data as the verify replay drift analysis, but as a standalone export). Requires `CanReplayComparisons` when authorization is enabled.

### Notes and limitations

- **PDF format** is currently only available for end‑to‑end replay (`ComparisonType = "end-to-end-replay"`).  
- **Export-record diff** replays support **Markdown** and **DOCX**.  
- If the underlying runs or export records have been deleted, **regenerate** and **verify** modes may fail; artifact replay continues to work as long as the stored payload is present.  
- Comparison records are immutable; replay persistence creates **new** records rather than mutating existing ones.


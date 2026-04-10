## Runbook: debugging replay & drift issues

Audience: internal engineers who need to understand or debug comparison replay / drift verification.

---

### 1. Confirm the comparison record exists and is valid

1. **Lookup by ID**
   - `GET /v1/architecture/comparisons/{comparisonRecordId}`
   - Verify:
     - `comparisonType` is what you expect.
     - linkage fields (`leftRunId`, `rightRunId`, `leftExportRecordId`, `rightExportRecordId`) are non-empty when needed.

2. **Inspect persisted payload**
   - From `ComparisonRecord`:
     - `payloadJson` should be non-empty and deserializable into the expected payload type.

3. **Check stored summary (optional)**
   - If `summaryMarkdown` is present, `GET /v1/architecture/comparisons/{id}/summary` should return it directly without replay.

---

### 2. Reproduce the replay via API

1. **Replay as Markdown (artifact mode)**

```bash
curl -X POST \
  "http://localhost:5128/v1/architecture/comparisons/{id}/replay" \
  -H "Content-Type: application/json" \
  -D headers.txt \
  -o replay.md \
  -d '{ "format": "markdown", "replayMode": "artifact" }'
```

2. **Inspect headers**
   - `X-ArchLucid-ComparisonRecordId`
   - `X-ArchLucid-ComparisonType`
   - `X-ArchLucid-ReplayMode`
   - `X-ArchLucid-LeftRunId` / `RightRunId`
   - `X-ArchLucid-LeftExportRecordId` / `RightExportRecordId`
   - `X-ArchLucid-Format-Profile`
   - `X-ArchLucid-PersistedReplayRecordId` (if `persistReplay=true`)

3. **If replay fails**
   - HTTP 400 with error JSON:
     - look for format/mode/type mismatch messages.
   - HTTP 404:
     - record not found; re-check ID and persistence step.

---

### 3. Use verify mode to detect drift

1. **Run verify replay**

```bash
curl -X POST \
  "http://localhost:5128/v1/architecture/comparisons/{id}/replay" \
  -H "Content-Type: application/json" \
  -D headers.txt \
  -o verify.md \
  -d '{ "format": "markdown", "replayMode": "verify" }'
```

2. **Check verification headers**
   - `X-ArchLucid-VerificationPassed: true|false`
   - `X-ArchLucid-VerificationMessage`

3. **If verification fails (drift detected)**
   - Use `POST /v1/architecture/comparisons/{id}/drift` to get structured `DriftAnalysisResponse`:
     - `driftDetected`
     - `summary`
     - `items` with `category`, `path`, `storedValue`, `regeneratedValue`, `description`

---

### 4. Use diagnostics to see replay history

1. **Call diagnostics endpoint**

```bash
curl "http://localhost:5128/v1/architecture/comparisons/diagnostics/replay?maxCount=50"
```

2. **Look for recent entries**
   - Correlate on:
     - `comparisonRecordId`
     - `success`
     - `durationMs`
     - `errorMessage`
     - `metadataOnly` (true for metadata-only replays)

3. **Common patterns**
   - Many failures with the same error:
     - suggests a systematic format/mode mismatch or missing linkage.
   - Very long `durationMs` in regenerate/verify mode:
     - check dependency availability (runs/exports still present?).

---

### 5. CLI shortcuts

From a project directory configured to talk to the API:

- **List comparisons**:

```bash
archlucid comparisons list --type end-to-end-replay --limit 10
```

- **Replay to file**:

```bash
archlucid comparisons replay <comparisonRecordId> --format markdown --mode verify --out ./replays --persist
```

- **Drift summary**:

```bash
archlucid comparisons drift <comparisonRecordId>
```

- **Replay diagnostics**:

```bash
archlucid comparisons diagnostics --limit 20
```


# How to add a new comparison type

This guide walks you through adding a new comparison type end-to-end: contracts, persistence, replay, and tests.
All steps are numbered; skip steps that do not apply to your type.

---

## 1. Objective

A "comparison type" in ArchiForge is an artifact produced by comparing two runs, two exports, or any two structured payloads.
It is persisted as a `ComparisonRecord` and later replayed to a format (Markdown, HTML, DOCX, PDF) on demand.

Examples already in the system:

| Type constant | Description |
|---|---|
| `end-to-end` | Full run-to-run replay comparison |
| `export-record-diff` | Comparison between two `RunExportRecord` payloads |

---

## 2. Assumptions

- You are adding a type, not changing existing comparison behaviour.
- The new type is persisted as a row in `ComparisonRecords` (no new table required unless you add type-specific payload columns).
- Replay will be Markdown only initially (you can add DOCX/HTML/PDF later by following the same pattern).

---

## 3. Constraints

- Comparison type strings must be unique and stable; they are persisted to the database.
- All replay paths must be idempotent and not alter stored state unless `PersistReplay = true`.
- Do not couple a new type to a specific controller; route through the shared `ComparisonsController`.

---

## 4. Architecture overview

```
ArchitectureRequest
       │
       ▼
[New comparison service]        ── produces ──▶  ComparisonRecord (persisted)
       │
       ▼ (on replay)
[New replay formatter]          ── produces ──▶  Markdown / DOCX / HTML / PDF
       │
       ▼
ComparisonsController           ── standard replay endpoint (no new route needed)
```

---

## 5. Step-by-step

### 5.1 Define the type constant

Add the new type string to `ComparisonTypes.cs` in `ArchiForge.Application.Analysis`:

```csharp
public static class ComparisonTypes
{
    public const string EndToEnd = "end-to-end";
    public const string ExportRecordDiff = "export-record-diff";
    public const string MyNewType = "my-new-type";   // ← add here
}
```

### 5.2 Create application-layer service and formatter

Create two new files in `ArchiForge.Application/Analysis/`:

| File | Interface | Responsibility |
|---|---|---|
| `IMyNewTypeComparisonService.cs` | `IMyNewTypeComparisonService` | Produces a `ComparisonRecord` from input |
| `IMyNewTypeReplayFormatter.cs` | `IMyNewTypeReplayFormatter` | Formats a stored record into a reply artifact |

Use `IEndToEndReplayComparisonService` and `IEndToEndReplayComparisonSummaryFormatter` as reference models.

### 5.3 Implement the service

```csharp
public sealed class MyNewTypeComparisonService(
    IComparisonRecordRepository comparisonRecordRepository)
    : IMyNewTypeComparisonService
{
    public async Task<ComparisonRecord> CompareAsync(
        MyNewTypeCompareRequest request,
        CancellationToken ct)
    {
        // 1. Compute diff / comparison payload.
        // 2. Persist via comparisonRecordRepository.SaveAsync(...).
        // 3. Return the persisted record.
    }
}
```

Key rules:
- Set `ComparisonType = ComparisonTypes.MyNewType` on the persisted record.
- Serialize comparison payload JSON into `PayloadJson` (or a dedicated column if the schema warrants it).
- Use `ConfigureAwait(false)` on every `await`.
- Null-check all arguments.

### 5.4 Implement the replay formatter

```csharp
public sealed class MyNewTypeReplayFormatter : IMyNewTypeReplayFormatter
{
    public Task<string> FormatMarkdownAsync(
        MyNewTypePayload payload,
        CancellationToken ct)
    {
        // Render a Markdown string from the payload.
        return Task.FromResult(markdown);
    }
}
```

### 5.5 Wire up replay in `ComparisonReplayService`

`ComparisonReplayService.ReplayAsync` dispatches by `ComparisonType`. Add a branch:

```csharp
ComparisonTypes.MyNewType => await ReplayMyNewTypeAsync(record, request, ct),
```

Add the corresponding private method that calls your formatter and returns a `ReplayComparisonResult`.

### 5.6 Register in DI

In `ArchiForge.Api/Startup/ServiceCollectionExtensions.cs`, add:

```csharp
services.AddScoped<IMyNewTypeComparisonService, MyNewTypeComparisonService>();
services.AddScoped<IMyNewTypeReplayFormatter, MyNewTypeReplayFormatter>();
```

### 5.7 Expose via controller (if needed)

If your comparison type requires a new trigger endpoint (most do), add an action to the relevant controller
or extend `ComparisonsController`. Use the shared replay endpoint (`POST /v1/architecture/comparisons/{id}/replay`)
for replay — no new route is needed there.

### 5.8 OpenAPI / Swagger

- Add `[ProducesResponseType(StatusCodes.Status200OK)]` and `[ProducesResponseType(StatusCodes.Status404NotFound)]` to any new action.
- Add an example or description to the Swagger doc if the request body is non-trivial.

### 5.9 SQL

No new migration is required unless you add type-specific columns to `ComparisonRecords`.
If you do:

1. Create `ArchiForge.Data/Migrations/0NN_MyNewTypeComparisonColumns.sql` (idempotent `IF NOT EXISTS`).
2. Add the same columns to `ArchiForge.sql` (SQL Server consolidated).
3. Update `docs/DATA_MODEL.md` and `docs/SQL_SCRIPTS.md`.

See `docs/SQL_SCRIPTS.md` §5 for the full change checklist.

---

## 6. Data flow

```
Client POST /v1/architecture/comparisons/my-new-type
    │
    ▼
Controller action
    │   validates input, calls service
    ▼
MyNewTypeComparisonService.CompareAsync
    │   computes diff
    ▼
IComparisonRecordRepository.SaveAsync     ── writes ComparisonRecord (type = "my-new-type")
    │
    ▼
Controller returns 200 { comparisonRecordId }

Later: POST /v1/architecture/comparisons/{id}/replay
    │
    ▼
ComparisonReplayService.ReplayAsync
    │   dispatches on ComparisonType == "my-new-type"
    ▼
MyNewTypeReplayFormatter.FormatMarkdownAsync
    │
    ▼
ReplayComparisonResult (content + headers)
```

---

## 7. Security model

- Use the existing `CanReplayComparisons` policy on replay actions.
- Do not expose raw payload JSON in the replay response; only formatted output.
- Ensure `ComparisonRecord` rows are tenant/workspace/project scoped if your input carries those identifiers.

---

## 8. Operational considerations

- Add `[Trait("Category", "Integration")]` to your integration tests.
- Use `ComparisonReplayTestFixture.PersistEndToEndComparisonAsync` as a reference for how tests persist records and call replay.
- Run `dotnet test --filter "Category=Integration"` after wiring to confirm no regression.
- Replay is idempotent by design; ensure your formatter produces deterministic output for the same input.

---

## 9. Tests to add

| Test | File | What to assert |
|---|---|---|
| `CompareAsync_PersistsRecord_WithCorrectType` | `MyNewTypeComparisonServiceTests.cs` (unit) | `comparisonRecordRepository` called with `ComparisonType == "my-new-type"` |
| `FormatMarkdown_GivenPayload_ContainsExpectedSections` | `MyNewTypeReplayFormatterTests.cs` (unit) | Returned string contains header and at least one diff entry |
| `ReplayEndpoint_Returns200_WithCorrectHeaders` | `ArchiForge.Api.Tests` (integration) | Full round-trip: persist → replay → assert X-ArchiForge-* headers and body not empty |
| `ReplayEndpoint_UnknownType_Returns422` | `ArchiForge.Api.Tests` (integration) | POST replay on a record with an unsupported type returns 422 |

---

## 10. Reference implementations

| Pattern | File |
|---|---|
| Service that persists a comparison | `EndToEndReplayComparisonService.cs` |
| Replay dispatcher | `ComparisonReplayService.cs` |
| Markdown formatter | `IEndToEndReplayComparisonSummaryFormatter.cs` / `MarkdownDriftReportFormatter.cs` |
| Integration test | `ArchitectureComparisonReplayTests.cs`, `ComparisonReplayTestFixture.cs` |

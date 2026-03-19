# Next five refactorings

Candidates for the next round of refactors, in rough priority order.

---

## 1. Unify Api.Tests JSON options

**Problem:** Several tests that extend `IntegrationTestBase` still use `new JsonOptions().JsonSerializerOptions` instead of the inherited `JsonOptions` (and some use it for `ReadFromJsonAsync` only, while the base also provides `JsonContent(object)`).

**Change:**
- In **ArchitectureControllerTests**, **ArchitectureDiagramTests**, and **ArchitectureSummaryTests**, replace every `new JsonOptions().JsonSerializerOptions` with `JsonOptions`, and use `JsonContent(...)` from the base where building request bodies.
- Optionally add a single shared `JsonSerializerOptions` in a test helper if any tests don’t inherit `IntegrationTestBase` but need the same options.

**Outcome:** One place to tune JSON behavior; consistent test style.

---

## 2. Use ComparisonReplayTestFixture in end-to-end comparison tests

**Problem:** **ArchitectureEndToEndComparisonExportTests** and **ArchitectureEndToEndComparisonTests** (and any similar) repeat the same flow: create run → execute → commit → replay, then call compare/export by `leftRunId`/`rightRunId`. Only **ComparisonReplayVerifyDriftIntegrationTests** currently uses **ComparisonReplayTestFixture**.

**Change:**
- In **ArchitectureEndToEndComparisonExportTests** and **ArchitectureEndToEndComparisonTests**, use `ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(Client, JsonOptions)` to obtain `(runId, replayRunId)`, then call the compare/export endpoints with those IDs.
- Where a test needs a **persisted** comparison (e.g. for replay by `comparisonRecordId`), also use `PersistEndToEndComparisonAsync` and then hit the comparisons replay endpoint.

**Outcome:** Less duplicated setup; changes to the create/execute/commit/replay flow live in one fixture.

---

## 3. RunComparisonController: optional facade for end-to-end services

**Problem:** **RunComparisonController** injects three application services for end-to-end comparison: `IEndToEndReplayComparisonService`, `IEndToEndReplayComparisonSummaryFormatter`, `IEndToEndReplayComparisonExportService`. That’s a lot of constructor parameters and ties the API to three separate abstractions.

**Change (optional):**
- Introduce an application-level facade, e.g. **`IEndToEndComparisonFacade`** (or **`IRunComparisonAppService`**), in **ArchiForge.Application**, with methods that delegate to the existing three services. Register the facade in **Program.cs** and inject it into **RunComparisonController** for the end-to-end summary/export actions; keep agent compare and audit as-is (or also behind the facade if you want a single “run comparison” entry point).
- Alternatively, leave the controller as-is and document that we intentionally keep the three services explicit for clarity and testability.

**Outcome:** Either a thinner controller and a single “comparison” dependency for those operations, or a documented decision to keep fine-grained dependencies.

---

## 4. Health check documentation

**Problem:** The API registers `AddHealthChecks()` with a database check and maps `/health`, but this isn’t documented for operators or in BUILD/README.

**Change:**
- In **README.md** or **docs/BUILD.md**, add a short “Health” section: what `/health` returns, that it includes a DB check, and that failure is unhealthy. Optionally mention readiness vs liveness if you later split them (e.g. liveness = no deps, readiness = DB).

**Outcome:** Clear contract for monitoring and runbooks.

---

## 5. Comparison replay request validation

**Problem:** The comparison replay endpoint accepts a body (format, replayMode, profile, persistReplay, etc.). Validation may be ad hoc or missing; OpenAPI and 400 responses could be more consistent.

**Change:**
- Add a **FluentValidation** validator for the comparison replay request DTO (e.g. **ReplayComparisonRequest** or whatever the bound model is). Validate format enum, replayMode, optional profile, etc.
- Register it with **AddValidatorsFromAssemblyContaining** (or the existing pattern). Ensure the controller uses the validated model so 400 responses and Swagger reflect the same rules.
- Optionally add a short note in **API_CONTRACTS.md** or Swagger description that validation errors return 400 with problem details.

**Outcome:** Consistent validation and better API docs for replay request shape.

---

## 6. Use ComparisonReplayTestFixture in ArchitectureComparisonReplayTests

**Problem:** **ArchitectureComparisonReplayTests** repeats the same create→execute→commit→replay flow, then persists via end-to-end summary and finally calls `POST comparisons/{comparisonRecordId}/replay`. Only the last step is unique; the rest matches **ComparisonReplayTestFixture**.

**Change:**
- Use `ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(Client, JsonOptions)` to get `(runId, replayRunId)`.
- Use `ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(Client, runId, replayRunId)` to get `comparisonRecordId`.
- Then `POST /v1/architecture/comparisons/{comparisonRecordId}/replay` with the desired body (e.g. `{ format = "markdown" }`).

**Outcome:** One less place with duplicated run/replay/persist setup; consistent with other E2E comparison tests.

---

## 7. Audit remaining Api.Tests for JsonOptions / JsonContent

**Problem:** After refactorings 1–2, some test files may still use `new JsonOptions().JsonSerializerOptions` or construct request bodies without using the base `JsonContent(object)`. Inconsistencies make it harder to change JSON behavior in one place.

**Change:**
- Grep for `new JsonOptions()` or `JsonSerializerOptions` in **ArchiForge.Api.Tests** and replace with inherited `JsonOptions` where the test extends **IntegrationTestBase**.
- Where request bodies are built with `new StringContent(JsonSerializer.Serialize(...))`, prefer the base `JsonContent(value)` if the test has access to it.
- Optionally add a one-line note in **TEST_STRUCTURE.md** that integration tests should use the base `JsonOptions` and `JsonContent`.

**Outcome:** Full consistency across Api.Tests; single place to tune JSON for tests.

---

## Checklist (for “integrate all changes” later)

- [x] 1. Api.Tests: use `JsonOptions` / `JsonContent` from base everywhere
- [x] 2. Api.Tests: use `ComparisonReplayTestFixture` in E2E comparison and export tests
- [x] 3. Application + Api: optional `IEndToEndComparisonFacade` and controller refactor (or document “no facade”)
- [x] 4. Docs: health check section in README or BUILD.md
- [x] 5. Api: FluentValidation for comparison replay request + docs/OpenAPI alignment
- [x] 6. Api.Tests: use `ComparisonReplayTestFixture` in **ArchitectureComparisonReplayTests** (create→execute→commit→replay→persist via fixture, then call `comparisons/{id}/replay`)
- [x] 7. Api.Tests: audit remaining tests for `JsonOptions` / `JsonContent` — any file still using `new JsonOptions().JsonSerializerOptions` or not using base `JsonContent` should be updated for consistency

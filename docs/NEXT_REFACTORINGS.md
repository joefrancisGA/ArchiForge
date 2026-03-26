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

---

## 8. Rate limiting documentation

**Problem:** The API configures three rate-limit policies (`fixed`, `expensive`, `replay`) and README only briefly mentions “100 requests per minute”. Operators and clients don’t have a clear reference for policy names, behavior, and config keys.

**Change:**
- Add a short **Rate limiting** section in **README.md** or **docs/BUILD.md**: policy names (`fixed` = general, `expensive` = execute/commit/replay, `replay` = comparison replay with light/heavy by format); that 429 is returned when exceeded; config keys `RateLimiting:FixedWindow:*`, `RateLimiting:Expensive:*`, `RateLimiting:Replay:Light:*`, `RateLimiting:Replay:Heavy:*`.

**Outcome:** Clear contract for tuning and runbooks.

---

## 9. ReplayComparisonRequest validation test

**Problem:** We added **ReplayComparisonRequestValidator** but have no test that asserts invalid request body returns 400 with validation problem details.

**Change:**
- In **ArchiForge.Api.Tests**, add a test (e.g. in **ComparisonReplayVerify422Tests** or a new **ComparisonReplayValidationTests**) that uses a valid comparison record ID, POSTs to `comparisons/{id}/replay` with body `{ "format": "invalid", "replayMode": "bad" }`, and asserts status 400 and response body contains validation error messages (or problem details type).

**Outcome:** Regression protection for replay request validation.

---

## 10. CreateRunAndExecuteAsync helper (optional)

**Problem:** Tests like **ArchitectureReplayTests** (first test) do create run → execute, then call replay. They don’t need commit or replay from the fixture; a smaller helper would reduce duplication.

**Change:**
- Add **ComparisonReplayTestFixture.CreateRunAndExecuteAsync(Client, JsonOptions, requestId)** that returns `runId` (create + execute only). Use it in **ArchitectureReplayTests** where only runId after execute is needed. Leave tests that need commit/replay payloads as-is unless they can use the full fixture.

**Outcome:** Less duplicated create+execute setup in replay-focused tests.

---

## 11. Program.cs: extract service registration into extension methods

**Problem:** **Program.cs** has a long block of `AddScoped`/`AddSingleton`/`Configure` calls. Harder to scan and to test registration in isolation.

**Change:**
- Create **ArchiForge.Api/Startup/ServiceCollectionExtensions.cs** (or similar) with extension methods such as `AddArchiForgeApplicationServices(this IServiceCollection services, IConfiguration configuration)` and `AddArchiForgeApiServices(this IServiceCollection services)` that move the relevant registrations out of **Program.cs**. Call them from **Program.cs** so the host file stays short and grouped by feature (e.g. AddControllers, AddRateLimiter, AddArchiForgeApplicationServices, MapEndpoints).

**Outcome:** Clearer **Program.cs** and a single place to see all application service wiring.

---

## 12. Trait("Category", "Integration") and TEST_STRUCTURE

**Problem:** Only a few tests are tagged `[Trait("Category", "Integration")]`. Filtering “fast vs integration” is inconsistent; TEST_STRUCTURE doesn’t list which test classes are considered integration.

**Change:**
- Add `[Trait("Category", "Integration")]` to test classes that use **WebApplicationFactory** and hit the full API (e.g. **ArchitectureControllerTests**, **ArchitectureComparisonReplayTests**, **ArchitectureEndToEndComparisonExportTests**). Optionally tag at class level so `dotnet test --filter "Category!=Integration"` excludes all of them. Update **docs/TEST_STRUCTURE.md** with a short list or rule: “All tests in Api.Tests that extend IntegrationTestBase are integration tests; tag with Category=Integration for filtering.”

**Outcome:** Consistent filtering and documented convention.

---

## Checklist (continued)

- [x] 8. Docs: rate limiting section (policies, 429, config keys)
- [x] 9. Api.Tests: test that invalid replay request body returns 400 with validation errors
- [x] 10. Api.Tests: optional CreateRunAndExecuteAsync helper; use in ArchitectureReplayTests where it fits
- [x] 11. Api: extract Program.cs service registration into extension methods
- [x] 12. Api.Tests: add [Trait("Category", "Integration")] to integration test classes; document in TEST_STRUCTURE

---

## 13. CORS documentation

**Problem:** The API configures CORS (policy name `ArchiForge`, config key `Cors:AllowedOrigins`) but this isn’t documented. Operators don’t know how to allow cross-origin calls.

**Change:**
- In **README.md** or **docs/BUILD.md**, add a short **CORS** note: config key `Cors:AllowedOrigins` (array of origins); if empty or missing, no origins are allowed (`SetIsOriginAllowed(_ => false)`). Policy name `ArchiForge` is used by `UseCors("ArchiForge")`.

**Outcome:** Clear setup for SPA or cross-origin API clients.

---

## 14. BatchReplayComparisonRequest validation

**Problem:** **ReplayComparisonRequest** has a FluentValidation validator; **BatchReplayComparisonRequest** (used by batch replay endpoint) has similar properties (Format, ReplayMode, Profile, PersistReplay) plus `ComparisonRecordIds` but no validator.

**Change:**
- Add **BatchReplayComparisonRequestValidator** (FluentValidation): require `ComparisonRecordIds` not empty; reuse or mirror Format/ReplayMode/Profile rules from **ReplayComparisonRequestValidator**. Register with existing `AddValidatorsFromAssemblyContaining`. Optionally document 400 for batch replay in **API_CONTRACTS.md**.

**Outcome:** Consistent validation and 400 responses for invalid batch replay body.

---

## 15. ComparisonReplayVerify422Tests trait

**Problem:** **ComparisonReplayVerify422Tests** uses a custom **ComparisonVerify422ApiFactory** (not **IntegrationTestBase**) but is still an integration test (full API, swapped service). It isn’t tagged, so `dotnet test --filter "Category=Integration"` doesn’t include it.

**Change:**
- Add `[Trait("Category", "Integration")]` at class level to **ComparisonReplayVerify422Tests** so it’s grouped with other integration tests for filtering.

**Outcome:** Consistent integration test filtering.

---

## 16. Api.Tests unit-style tests in TEST_STRUCTURE

**Problem:** Some tests in **ArchiForge.Api.Tests** don’t use **WebApplicationFactory** (e.g. **AgentResultDiffServiceTests**, **ManifestDiffServiceTests**, **ApiProblemDetailsExceptionFilterTests**, **ArchitectureApplicationServiceTests**, **DatabaseMigrationScriptTests**). They’re unit or in-process tests. TEST_STRUCTURE doesn’t mention them.

**Change:**
- In **docs/TEST_STRUCTURE.md**, add a short paragraph: Api.Tests also contains tests that don’t extend **IntegrationTestBase** (e.g. service/filter unit tests, migration script tests). These don’t spin up the full API. Optionally add `[Trait("Category", "Unit")]` to such classes so `dotnet test --filter "Category=Unit"` runs only them (and similar in other projects).

**Outcome:** Clear distinction and optional filtering for unit vs integration in Api.Tests.

---

## 17. Program.cs: extract rate limiter and CORS into extensions

**Problem:** **Program.cs** still contains the **AddRateLimiter** and **AddCors** blocks (and possibly auth policies). Moving them into extension methods would make Program.cs even thinner and group config by concern.

**Change:**
- Add **AddArchiForgeRateLimiting(this IServiceCollection services, IConfiguration configuration)** in **Startup/** (e.g. in a new **RateLimitingExtensions.cs** or in **ServiceCollectionExtensions.cs**) and move the existing **AddRateLimiter** lambda there. Add **AddArchiForgeCors(this IServiceCollection services, IConfiguration configuration)** and move the **AddCors** block. Call both from **Program.cs** after **AddArchiForgeApplicationServices**. Optionally move **AddAuthorization** policy configuration into an **AddArchiForgeAuthorization** extension.

**Outcome:** Shorter Program.cs; rate limiting and CORS config in one place each.

---

## Checklist (continued)

- [x] 13. Docs: CORS section (config key, policy name, behavior when empty)
- [x] 14. Api: FluentValidation for BatchReplayComparisonRequest + optional API_CONTRACTS note
- [x] 15. Api.Tests: [Trait("Category", "Integration")] on ComparisonReplayVerify422Tests
- [x] 16. Docs: TEST_STRUCTURE note on unit-style tests in Api.Tests; optional Category=Unit trait
- [x] 17. Api: extract AddRateLimiter and AddCors (and optionally AddAuthorization) into extension methods

---

## 18. Extract AddAuthorization into extension

**Problem:** **Program.cs** still contains the **AddAuthorization** block with five policies (CanCommitRuns, CanSeedResults, CanExportConsultingDocx, CanReplayComparisons, CanViewReplayDiagnostics). Moving it into an extension would match the pattern used for rate limiting and CORS.

**Change:**
- Add **AddArchiForgeAuthorization(this IServiceCollection services)** in **Startup/InfrastructureExtensions.cs** (or a dedicated **AuthorizationExtensions.cs**) and move the existing **AddAuthorization** lambda there. Call it from **Program.cs** after **AddAuthentication**.

**Outcome:** Shorter Program.cs; all auth-related registration in one place.

---

## 19. Authentication and API key documentation

**Problem:** The API uses **X-Api-Key** header and config **Authentication:ApiKey:Enabled** / key value, and authorization policies require claims (e.g. `permission: commit:run`). README doesn’t document how to send the key or what permissions exist.

**Change:**
- In **README.md** (or **docs/API_CONTRACTS.md**), add a short **Authentication** or **API key** section: send **X-Api-Key** header; config key **Authentication:ApiKey:Enabled** and the key value (e.g. from User Secrets or env). Optionally list the permission claims used by policies (commit:run, seed:results, export:consulting-docx, replay:comparisons, replay:diagnostics) so clients know what to expect when authorized.

**Outcome:** Clear contract for API key and permissions.

---

## 20. Batch replay validation test

**Problem:** We added **BatchReplayComparisonRequestValidator** but have no test that asserts invalid batch replay body (e.g. empty **comparisonRecordIds**) returns **400** with validation errors.

**Change:**
- In **ArchiForge.Api.Tests**, add a test (e.g. in **ComparisonReplayValidationTests** or a new **BatchReplayValidationTests**) that POSTs to the batch replay endpoint with body `{ "comparisonRecordIds": [], "format": "invalid" }` (or similar) and asserts status **400** and response contains validation error messages.

**Outcome:** Regression protection for batch replay request validation.

---

## 21. Extract OpenTelemetry setup into extension

**Problem:** **Program.cs** still contains the **AddOpenTelemetry** block (tracing, metrics, Prometheus, console exporter). Moving it into an extension would shorten Program.cs and group observability config.

**Change:**
- Add **AddArchiForgeOpenTelemetry(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)** in **Startup/** and move the **AddOpenTelemetry** chain (ConfigureResource, WithTracing, WithMetrics) there. Use **configuration** and **environment** for Prometheus/console flags and service name. Call from **Program.cs** before **AddArchiForgeRateLimiting**.

**Outcome:** Shorter Program.cs; observability config in one place.

---

## 22. Swagger document description or link to API_CONTRACTS

**Problem:** The Swagger document has a title and version but no description or link to **docs/API_CONTRACTS.md**. API consumers may not discover the contract doc.

**Change:**
- When building the Swagger doc in **AddSwaggerGen**, set **OpenApiInfo.Description** to a short paragraph and/or add an extension (e.g. **x-contracts-url** or **x-docs**) pointing to the API contracts doc (e.g. relative URL or repo link). Alternatively add a **IDocumentFilter** that injects a "See also" link into the document.

**Outcome:** Better discoverability of API behavior (422, 404 run-not-found, 409, validation).

---

## Checklist (continued)

- [x] 18. Api: extract AddAuthorization into AddArchiForgeAuthorization extension
- [x] 19. Docs: Authentication / API key and permissions section
- [x] 20. Api.Tests: test that invalid batch replay body returns 400 with validation errors
- [x] 21. Api: extract AddOpenTelemetry into AddArchiForgeOpenTelemetry extension
- [x] 22. Swagger: document description or link to API_CONTRACTS

---

## 23. Extract app pipeline into extension

**Problem:** **Program.cs** still contains the middleware and endpoint pipeline (UseMiddleware, UseExceptionHandler, UseHttpsRedirection, UseCors, UseRateLimiter, UseAuthentication, UseAuthorization, MapHealthChecks, Prometheus scraping, MapControllers). Moving it into an extension would leave Program.cs with just builder setup, build, and Run.

**Change:**
- Add **UseArchiForgePipeline(this WebApplication app)** in **Startup/** (e.g. **PipelineExtensions.cs** or in **InfrastructureExtensions.cs**). Move the block from **app.UseMiddleware&lt;CorrelationIdMiddleware&gt;** through **app.MapControllers()** (and the Prometheus conditional) into that method. Call **app.UseArchiForgePipeline()** from **Program.cs** after **var app = builder.Build()** and the migration check.

**Outcome:** Minimal Program.cs; pipeline logic in one place.

---

## 24. Document migration failure behavior

**Problem:** README says "Migrations run automatically on startup via DbUp" but doesn't state that if migration fails, the API throws and does not start. Operators may not expect a hard failure.

**Change:**
- In **README.md** (Database Setup or Running the API), add one sentence: if the connection string is set and migration fails, the API throws and does not start (no fallback). Optionally mention that integration tests use in-memory SQLite and skip this path.

**Outcome:** Clear runbook for migration failures.

---

## 25. Shared replay validation constants

**Problem:** **ReplayComparisonRequestValidator** and **BatchReplayComparisonRequestValidator** both define **ValidFormats**, **ValidReplayModes**, and **ValidProfiles** with the same values. Duplication can drift.

**Change:**
- Create **ReplayValidationConstants** (or **ReplayValidationAllowedValues**) in **ArchiForge.Api/Validators/** with static readonly **ValidFormats**, **ValidReplayModes**, **ValidProfiles** (e.g. `HashSet<string>` with `StringComparer.OrdinalIgnoreCase`). Use them in both validators.

**Outcome:** Single source of truth for allowed format/replayMode/profile values.

---

## 26. README: link TEST_STRUCTURE in Running Tests

**Problem:** **docs/TEST_STRUCTURE.md** describes test categories and filtering but README only links to **docs/BUILD.md** in the Running Tests section. New contributors may miss TEST_STRUCTURE.

**Change:**
- In **README.md** under "Running Tests", add a line: see **docs/TEST_STRUCTURE.md** for test categories (Integration vs Unit) and filter examples (`Category=Integration`, `Category=Unit`).

**Outcome:** Discoverability of test structure docs.

---

## 27. Extract AddSwaggerGen into extension

**Problem:** **Program.cs** still contains the **AddSwaggerGen** block (SwaggerDoc with Title, Version, Description, and the three operation filters). Moving it into an extension would keep Program.cs thin and group Swagger config.

**Change:**
- Add **AddArchiForgeSwagger(this IServiceCollection services)** in **Startup/** (e.g. **SwaggerExtensions.cs**) and move the **AddSwaggerGen** lambda there (including the Description and operation filters). Call **builder.Services.AddArchiForgeSwagger()** from **Program.cs** instead of the inline **AddSwaggerGen**.

**Outcome:** Shorter Program.cs; Swagger config in one place.

---

## Checklist (continued)

- [x] 23. Api: extract app pipeline (middleware + Map*) into UseArchiForgePipeline extension
- [x] 24. Docs: migration failure behavior (throw, no start)
- [x] 25. Api: shared ReplayValidationConstants for replay validators
- [x] 26. README: link TEST_STRUCTURE in Running Tests
- [x] 27. Api: extract AddSwaggerGen into AddArchiForgeSwagger extension

---

## 28. Extract MVC/API service registration into extension

**Problem:** **Program.cs** still contains **AddControllers** (with filter), **AddProblemDetails**, **AddApiVersioning** (and AddMvc, AddApiExplorer), **AddFluentValidationAutoValidation**, **AddValidatorsFromAssemblyContaining**, **AddOpenApi**, **AddEndpointsApiExplorer**, and **AddArchiForgeSwagger**. Moving these into an extension would leave Program with only builder configuration, Build(), migration check, and UseArchiForgePipeline().

**Change:**
- Add **AddArchiForgeMvc(this IServiceCollection services)** (or **AddArchiForgeApi**) in **Startup/** and move the block from **AddControllers** through **AddArchiForgeSwagger** there. Call **builder.Services.AddArchiForgeMvc()** from **Program.cs**. Ensure the extension has the necessary usings (Asp.Versioning, FluentValidation, etc.).

**Outcome:** Minimal Program.cs; MVC/API/versioning/Swagger registration in one place.

---

## 29. API versioning documentation

**Problem:** The API uses URL path versioning (e.g. `/v1/architecture/...`) and reports API versions, but this isn't documented for clients. README and API_CONTRACTS don't explain how to request a version or what the default is.

**Change:**
- In **README.md** or **docs/API_CONTRACTS.md**, add a short **API versioning** note: URL path segment `v1`; default version 1.0 when unspecified; response headers report supported versions; link to Asp.Versioning behavior if needed.

**Outcome:** Clear contract for API versioning.

---

## 30. Correlation ID documentation

**Problem:** **CorrelationIdMiddleware** sets **X-Correlation-ID** (request and response) and enriches tracing/logging, but this isn't documented. Operators and clients don't know they can send or use the header.

**Change:**
- In **README.md** (e.g. under Running the API or a new "Observability" bullet) or **docs/BUILD.md**, add one sentence: the API supports **X-Correlation-ID**; if the client sends it, the same value is returned and used for tracing and logs; if omitted, the server generates one (e.g. from TraceIdentifier).

**Outcome:** Clear contract for correlation IDs.

---

## 31. ReplayValidationConstants unit test

**Problem:** **ReplayValidationConstants** defines the allowed format, replayMode, and profile values. A change (e.g. adding a format or typo) could break validation or API contracts with no test coverage.

**Change:**
- Add a unit test (e.g. in **ArchiForge.Api.Tests** in a new **ReplayValidationConstantsTests.cs** or alongside validator tests) that asserts **ValidFormats** contains "markdown", "html", "docx", "json"; **ValidReplayModes** contains "artifact", "regenerate", "verify"; **ValidProfiles** contains "default", "short", "detailed", "executive". Tag with **Category=Unit**.

**Outcome:** Regression protection for allowed values.

---

## 32. Key documentation index in README

**Problem:** Important docs (BUILD.md, TEST_STRUCTURE.md, API_CONTRACTS.md, CLI_USAGE.md) are mentioned in different sections. New contributors may miss one.

**Change:**
- In **README.md**, add a short **Key documentation** section (e.g. after Prerequisites or at the end before Architecture docs) that lists in one place: **docs/BUILD.md** (build, CPM, project refs), **docs/TEST_STRUCTURE.md** (test categories, filtering), **docs/API_CONTRACTS.md** (422, 404, 409, validation), **docs/CLI_USAGE.md** (CLI reference). One line each. Optionally link **docs/COMPARISON_REPLAY.md** and **docs/ARCHITECTURE_INDEX.md** as next steps.

**Outcome:** Single entry point for key docs.

---

## Checklist (continued)

- [x] 28. Api: extract MVC/API service registration (AddControllers through AddArchiForgeSwagger) into AddArchiForgeMvc extension
- [x] 29. Docs: API versioning (URL v1, default, report versions)
- [x] 30. Docs: Correlation ID (X-Correlation-ID header, request/response, tracing)
- [x] 31. Api.Tests: unit test for ReplayValidationConstants allowed values
- [x] 32. README: Key documentation section (BUILD, TEST_STRUCTURE, API_CONTRACTS, CLI_USAGE)

---

## 33. Centralize Api → Application replay request mapping

**Problem (current code):** **ComparisonsController** builds **`AppReplayComparisonRequest`** in at least four places (replay, metadata, drift-related paths, batch replay) with the same property mapping from **`ApiReplayComparisonRequest`** and route IDs. That is easy to drift when a new field is added.

**Change:**
- Add a small mapper (e.g. static **`ReplayComparisonRequestMapper.ToApplication(string comparisonRecordId, ApiReplayComparisonRequest request)`** or an extension on **`ApiReplayComparisonRequest`**) and use it everywhere **`new AppReplayComparisonRequest { … }`** appears for replay flows.

**Outcome:** One place to update when the replay contract changes.

---

## 34. Extract replay result → response headers helper

**Problem (current code):** After **`comparisonReplayApiService.ReplayAsync`**, **ComparisonsController** sets many **`X-ArchiForge-*`** headers on **`Response`** in a long, repeated pattern (comparison id, type, replay mode, verification flags, run/export ids).

**Change:**
- Add a private method or small static helper (e.g. **`ApplyReplayComparisonResultHeaders(HttpResponse response, ReplayComparisonResult result)`**) and call it from each action that returns a replay artifact so header logic lives in one place.

**Outcome:** Less duplication; consistent headers across replay endpoints.

---

## 35. Split or segment `AddArchiForgeApplicationServices`

**Problem (current code):** **`ServiceCollectionExtensions.AddArchiForgeApplicationServices`** is a long, single method registering repositories, analysis, comparison, agents, DecisionEngine, etc. Hard to navigate and review in PRs.

**Change:**
- Split into private methods inside the same file (**`AddRepositories`**, **`AddComparisonAndReplay`**, **`AddAgentExecution`**, etc.) *or* separate extension methods (**`AddArchiForgeRepositories`**, **`AddArchiForgeAnalysisServices`**, …) composed from **`AddArchiForgeApplicationServices`**. Keep **one** public entry point for **Program.cs**.

**Outcome:** Easier maintenance and clearer dependency grouping.

---

## 36. Align comparison history query validation with FluentValidation

**Problem (current code):** **ComparisonsController** uses a static **`ComparisonHistoryQueryValidator`** and manual **`ValidateAsync`** + **`BadRequestProblem`**, while most bodies use **FluentValidation** auto-validation.

**Change:**
- Replace with **`AbstractValidator<ComparisonHistoryQuery>`** registered in DI (same assembly scan as other validators) and either use a filter/pipeline that validates `[FromQuery]` models or keep explicit validation but through the same validator type for consistency. Document if query binding cannot use auto-validation.

**Outcome:** One validation story for API consumers and contributors.

---

## 37. Finish the “thin Program + docs + constants” backlog

**Problem (current code):** **Program.cs** still inlines **AddControllers** through **AddArchiForgeSwagger** (see item 28). **ReplayValidationConstants** still has no dedicated unit test (item 31). README still lacks a consolidated **Key documentation** index and explicit notes on **API versioning** and **X-Correlation-ID** (items 29–30, 32).

**Change:**
- Implement **AddArchiForgeMvc** (or **AddArchiForgeApi**) extension; add **ReplayValidationConstantsTests**; add short README/API_CONTRACTS sections for versioning and correlation ID; add README **Key documentation** list.

**Outcome:** Closes the remaining checklist items 28–32 with concrete deliverables.

---

## Checklist (analysis — Feb 2026)

- [x] 33. Api: mapper for Api replay request → AppReplayComparisonRequest (ComparisonsController)
- [x] 34. Api: helper for ReplayComparisonResult → X-ArchiForge-* response headers
- [x] 35. Api: split or segment AddArchiForgeApplicationServices registration
- [x] 36. Api: ComparisonHistoryQuery → FluentValidation alignment
- [x] 37. Bundle: thin Program (28) + docs (29–30, 32) + ReplayValidationConstants test (31)

---

## 38. Centralize controller InvalidOperationException → ProblemDetails mapping

**Problem:** `RunsController` and `AnalysisReportsController` repeat many `try/catch (InvalidOperationException)` blocks and string-match `ex.Message.Contains("not found")` to choose 404 vs 400. This is brittle and duplicates behavior.

**Change:**
- Introduce a small exception mapping helper (or a controller filter) that converts known application exceptions to consistent ProblemDetails (`#run-not-found`, validation, conflict, etc.).
- Replace per-action message parsing with typed mapping and common helpers.

**Outcome:** More reliable status mapping and much less controller boilerplate.

---

## 39. Add typed query model + validator for run-to-run compare endpoints

**Problem:** `RunComparisonController` repeats `leftRunId` / `rightRunId` query parameters and run existence checks across multiple endpoints.

**Change:**
- Add a `RunPairQuery` model (`LeftRunId`, `RightRunId`) with FluentValidation rules.
- Add a shared helper (or service) to resolve/validate both runs once and reuse in compare/summary/export actions.

**Outcome:** One validation/lookup path for run-pair endpoints, less copy/paste.

---

## 40. Move comparison and run endpoint response shaping into mappers

**Problem:** Controllers still construct response DTOs inline (`new ...Response { ... }`) in many actions. Mapping logic is mixed with orchestration, making actions long.

**Change:**
- Introduce focused response mappers (e.g. `RunResponseMapper`, `ComparisonResponseMapper`) for recurring DTO shaping.
- Keep controllers focused on orchestration + HTTP concerns only.

**Outcome:** Thinner actions and easier test coverage for mapping behavior.

---

## 41. Add unit tests for replay mapping/header helpers

**Problem:** New helpers (`ReplayComparisonRequestMapper`, `ReplayComparisonResultHeaders`) are untested; regressions would silently change API behavior.

**Change:**
- Add unit tests for:
  - query/body format precedence in `ToApplicationForReplayEndpoint`
  - summary/batch mapping defaults
  - expected `X-ArchiForge-*` headers in full vs metadata modes.

**Outcome:** Safe refactoring surface for replay API contract behavior.

---

## 42. Simplify DI registrations with type aliases for verbose namespaces

**Problem:** `ServiceCollectionExtensions` uses many fully-qualified `Decisioning.*` and `ContextIngestion.*` type names, which hurts readability even after segmentation.

**Change:**
- Add `using` aliases at file top for frequently used verbose namespaces/interfaces.
- Keep registration list compact while preserving explicitness.

**Outcome:** Faster scanning and lower maintenance overhead for service registration changes.

---

## Checklist (analysis — next five)

- [x] 38. Api: centralize InvalidOperationException → ProblemDetails mapping
- [x] 39. Api: RunPairQuery + shared run lookup/validation for compare endpoints
- [x] 40. Api: extract response DTO mappers from controllers
- [x] 41. Api.Tests: unit tests for ReplayComparisonRequestMapper + ReplayComparisonResultHeaders
- [x] 42. Api: add using aliases to simplify ServiceCollectionExtensions registrations

---

## 43. Single source of truth for exception → ProblemDetails (filter + extensions)

**Problem:** `ApiProblemDetailsExceptionFilter` maps `InvalidOperationException` with “not found” in the message to **`#resource-not-found`**, while controller helpers such as **`InvalidOperationProblem(..., notFoundType: ProblemTypes.RunNotFound)`** intentionally use **`#run-not-found`** for run-scoped actions. The two paths can drift and confuse clients.

**Change:**
- Extract a small shared mapper (e.g. `ApplicationExceptionProblemMapper.TryMap(Exception, ProblemDetailsOptions)`) used by both the filter and `ProblemDetailsExtensions`.
- Support per-route or per-exception-type defaults (run vs generic resource) without duplicating string rules.

**Outcome:** Consistent problem `type` and status codes whether the exception is handled in a catch block or bubbles to the filter.

---

## 44. Replace string-based “not found” checks in `RunsController` with typed outcomes

**Problem:** `SubmitAgentResult` and `SeedFakeResults` still branch on **`result.Error.Contains("not found", ...)`** to choose 404 vs 400. That duplicates the fragile message convention used elsewhere.

**Change:**
- Extend application/API contracts so these operations return a discriminated result (e.g. `NotFound`, `ValidationFailed`, `Success`) or throw **`RunNotFoundException`** / **`InvalidOperationException`** with a stable code — then map in one place (item 43).

**Outcome:** No substring matching on user-facing error strings; clearer tests.

---

## 45. Consolidate duplicate `Controllers` folder paths (casing)

**Problem:** The repo shows both **`ArchiForge.Api/Controllers/`** and **`ArchiForge.Api\Controllers\`** (same files mirrored by path casing). On Windows this is confusing and risks editing the wrong copy or merge noise.

**Change:**
- Pick one canonical folder name (e.g. `Controllers`), **`git mv`** / normalize so only one path exists; ensure `.csproj` default includes remain valid.

**Outcome:** One obvious location for API controllers; fewer duplicate paths in tooling search results.

---

## 46. Extract shared `ArchitectureAnalysisRequest` builders for consulting DOCX

**Problem:** `AnalysisReportsController` builds a large **`ArchitectureAnalysisRequest`** from **`ConsultingDocxExportRequest`** twice (sync download and async job), with nearly identical property mapping.

**Change:**
- Add a static factory or mapper (e.g. `ConsultingDocxAnalysisRequestFactory.From(string runId, ConsultingDocxExportRequest request)`).

**Outcome:** One place to update when analysis options or consulting flags change.

---

## 47. Further slim `RegisterDecisioningEngines` (aliases or partial class)

**Problem:** After item 42, **`RegisterDecisioningEngines`** still repeats long **`Decisioning.Interfaces.*`** type names for engines, orchestrator, validator, etc.

**Change:**
- Add **`using` aliases** for `IFindingEngine`, `IFindingsOrchestrator`, `IGoldenManifestBuilder`, … **or** move registration into **`ServiceCollectionExtensions.Decisioning.cs`** as a `partial` class for readability.

**Outcome:** Shorter, scannable registration blocks without changing behavior.

---

## 48. Document the context ingestion pipeline (connectors, parsers, API fields)

**Problem:** Multi-connector ingestion (`IContextConnector`, parsers, deduplication) is not described in **`docs/ARCHITECTURE_CONTEXT.md`**, **`docs/DATA_MODEL.md`**, or **`docs/API_CONTRACTS.md`**. New `ArchitectureRequest` fields (`InlineRequirements`, `Documents`, `PolicyReferences`, `TopologyHints`, `SecurityBaselineHints`) are invisible to integrators.

**Change:**
- Add a short **“Context ingestion”** subsection (or new **`docs/CONTEXT_INGESTION.md`**) listing connector types, **`PlainTextContextDocumentParser`** line prefixes (`REQ:`, `POL:`, `TOP:`, `SEC:`), dedupe key idea (`ObjectType|Name|text`), and how **`ProjectId`** maps from **`SystemName`**.
- In **`API_CONTRACTS.md`**, document the expanded create-run JSON body and that documents are inline (name, contentType, content), not file upload multipart (unless you add that later).

**Outcome:** Onboarding and CLI/Swagger users know how to feed the pipeline.

---

## 49. Reintroduce real delta semantics in `ContextIngestionService`

**Problem:** **`ContextIngestionService`** always passes **`null`** as **`previous`** to **`DeltaAsync`**, so summaries are always the “initial ingestion” branch and **`IContextSnapshotRepository.GetLatestAsync`** is unused. That was an intentional step; it’s now technical debt.

**Change:**
- Inject **`IContextSnapshotRepository`** (or a narrow **`IContextSnapshotLookup`**) and load **`GetLatestAsync(request.ProjectId, ct)`** once per ingest.
- Pass **`previous`** into **`DeltaAsync`** (define what **`ContextSnapshot?`** means for “previous”: latest committed snapshot for project, or previous run only).
- Optionally extract **`IContextDeltaComparer`** if connector-specific delta logic grows.

**Outcome:** Meaningful **`DeltaSummary`** and a path to graph-level “what changed” features.

---

## 50. Harden `CanonicalDeduplicator` identity for non-`text` properties

**Problem:** Dedupe key uses **`Properties["text"]`** only. **`PolicyReferenceConnector`** emits **`reference`** (and **`status`**) but often no **`text`**, so collisions are keyed only by **`ObjectType|Name|`** — weaker than intended for long or duplicate names.

**Change:**
- Extend **`GetStableText`** (or rename to **`GetDedupeFingerprint`**) to fall back to **`reference`**, then other stable fields, and document the precedence in code comments or **`docs/CONTEXT_INGESTION.md`**.
- Add unit tests for “same policy reference from two connectors → one object”.

**Outcome:** Safer merging when multiple sources describe the same policy/control.

---

## 51. Extract `ArchitectureRequest` → `ContextIngestionRequest` mapping

**Problem:** **`CoordinatorService`** now owns a large **`new ContextIngestionRequest { ... }`** block. That couples the coordinator to ingestion shape and is awkward to unit test in isolation.

**Change:**
- Add **`IContextIngestionRequestFactory`** or static **`ContextIngestionRequestMapper.From(ArchitectureRequest)`** in **`ArchiForge.ContextIngestion`** (or **Application**) with tests for document mapping and list copies.

**Outcome:** Single place to evolve the API ↔ ingestion boundary; thinner coordinator.

---

## 52. Validate and order multi-connector registration explicitly

**Problem:** Connector **order** affects concatenated **`DeltaSummary`** and (if added) side effects. Registration in **`ServiceCollectionExtensions`** relies on implicit **`IEnumerable<IContextConnector>`** order. Empty documents / invalid **`ContentType`** are silently skipped in **`DocumentConnector`** with no warning on the snapshot.

**Change:**
- Document intended order (e.g. static → inline → documents → policy → topology → security) **or** register a single **`IContextConnector[]`** / ordered composite built in one method.
- Optionally add **`FluentValidation`** for **`ArchitectureRequest`** document entries (non-empty **`Name`**, **`Content`**, known **`ContentType`** or warning list on **`ContextSnapshot.Warnings`** when no parser matches).

**Outcome:** Predictable summaries and clearer failure modes for bad document input.

---

## Checklist (analysis — next five)

- [x] 43. Api: unify exception → ProblemDetails mapping (filter + ProblemDetailsExtensions)
- [x] 44. Application/Api: typed outcomes for submit/seed (remove string “not found” parsing)
- [x] 45. Repo: single canonical Controllers folder path (no duplicate casing)
- [x] 46. Api: extract consulting DOCX → ArchitectureAnalysisRequest builder
- [x] 47. Api: aliases or partial class for RegisterDecisioningEngines verbosity

## Checklist (context ingestion — suggested next five)

- [x] 48. Docs: context ingestion pipeline + expanded create-run contract
- [x] 49. Refactor: `ContextIngestionService` + prior snapshot for real deltas
- [x] 50. Refactor: deduplicator fingerprint for `reference` / non-text properties
- [x] 51. Refactor: extract API → `ContextIngestionRequest` mapper
- [x] 52. Refactor/docs: explicit connector order + validation or warnings for documents

---

## 53. Single source of truth for supported document content types

**Problem:** **`ContextDocumentRequestValidator`** and **`PlainTextContextDocumentParser`** both encode which MIME types are supported (`text/plain`, `text/markdown`). Adding a parser for `application/json` requires editing two places and risks API validation rejecting what ingestion could parse (or the reverse).

**Change:**
- Add a small shared type in **`ArchiForge.ContextIngestion`** (e.g. **`SupportedContextDocumentContentTypes`** static class, or **`IContextDocumentParserRegistry`** that exposes `IsSupported(string contentType)`), used by the validator via a reference from **Api** to the same definitions **or** by generating the FluentValidation rule from the parser collection at startup.
- Update **`docs/CONTEXT_INGESTION.md`** to say “supported types are defined in X”.

**Outcome:** One list to extend when adding **`IContextDocumentParser`** implementations.

---

## 54. `ArchiForge.ContextIngestion.Tests` + move ingestion unit tests

**Problem:** **`CanonicalDeduplicatorTests`** and **`ContextIngestionRequestMapperTests`** live in **`ArchiForge.Coordinator.Tests`**, which couples coordinator tests to ingestion internals. There is no dedicated test project for **`ArchiForge.ContextIngestion`**.

**Change:**
- Add **`ArchiForge.ContextIngestion.Tests`** (xUnit, same pattern as **Coordinator.Tests**).
- Move dedupe/mapper tests there; add tests for **`PlainTextContextDocumentParser`** (line prefixes), **`DocumentConnector`** warnings when no parser matches, and **`ContextIngestionService`** behavior with a fake **`IContextSnapshotRepository`** + fake connectors if useful.

**Outcome:** Ingestion changes get fast, local tests without pulling the full coordinator graph.

---

## 55. API integration test: create run with full ingestion payload

**Problem:** Multi-field **`ArchitectureRequest`** (documents, inline requirements, policy/topology/security hints) is validated by FluentValidation but not covered by an end-to-end test that proves the run completes and authority persistence sees expected context shape.

**Change:**
- In **`ArchiForge.Api.Tests`**, add a test (integration, **`IntegrationTestBase`**) that **`POST`**s create run with a minimal valid body including **`inlineRequirements`**, one **`documents`** entry (`text/plain` with a `REQ:` line), and one **`policyReferences`** entry.
- Assert **200** and optionally query an internal/debug endpoint or DB (if the test stack exposes it) **or** assert response payload includes run id and no error — tighten over time to assert snapshot canonical object count via a test-only hook if needed.

**Outcome:** Regression protection for the public create-run contract and ingestion wiring.

---

## 56. Richer connector delta summaries (counts / diff hints)

**Problem:** **`DeltaAsync`** messages are binary (“Initial …” vs “Updated …”) and do not reflect how many objects a connector produced or how they differ from **`previous`** canonical sets. **`DeltaSummary`** is a long concatenated string with limited operational value.

**Change:**
- Optionally inject a small **`IContextDeltaSummaryBuilder`** that, given **`previous`** and **`current` `NormalizedContextBatch`**, emits a short per-connector line (e.g. “documents: +2 Requirement, +1 PolicyControl”).
- Keep backward-compatible **`DeltaSummary`** text or add structured **`ContextDelta.Details`** (list of strings) later.

**Outcome:** Easier debugging and future UI/audit without full graph diff yet.

---

## 57. OpenAPI / Swagger examples for expanded `ArchitectureRequest`

**Problem:** Swagger shows **`ArchitectureRequest`** properties but not a realistic example with **`documents`**, **`inlineRequirements`**, etc., so integrators miss the ingestion features described in **`docs/CONTEXT_INGESTION.md`**.

**Change:**
- Add **`IOperationFilter`** or schema example for the create-run request body (minimal JSON with **`documents`**: one object, **`inlineRequirements`**: one string, optional **`policyReferences`**).
- Cross-link in description to **`docs/CONTEXT_INGESTION.md`** or duplicate one sentence on line prefixes (`REQ:`, …).

**Outcome:** Discoverability parity between docs and interactive API clients.

---

## Checklist (context ingestion — next five)

- [x] 53. Refactor: single source of truth for supported document content types (validator + parsers)
- [x] 54. Tests: add `ArchiForge.ContextIngestion.Tests`; move dedupe/mapper tests; add parser/connector/service tests
- [x] 55. Api.Tests: integration test create run with full ingestion payload
- [x] 56. Refactor: richer per-connector delta summaries (counts or diff hints vs `previous`)
- [x] 57. Api: OpenAPI examples (and optional description) for expanded `ArchitectureRequest`

---

## 58. Policy pack API: FluentValidation for create / publish / assign bodies

**Problem:** `PolicyPacksController` accepts `CreatePolicyPackRequest`, `PublishPolicyPackVersionRequest`, and `AssignPolicyPackRequest` with only ad hoc checks (e.g. empty version → `BadRequest`). Other controllers use FluentValidation for consistent 400 + problem details.

**Change:**
- Add `AbstractValidator<>` implementations for the three DTOs (non-empty `Name` / `PackType` on create, semantic version format optional, non-empty `Version` on publish/assign, `InitialContentJson` / `ContentJson` valid JSON when non-empty).
- Register with existing `AddValidatorsFromAssemblyContaining`; enable auto-validation for those actions or validate explicitly to match project convention.

**Outcome:** Same validation story as comparison replay; Swagger/clients see predictable 400s.

---

## 59. Api.Tests: integration coverage for policy pack lifecycle

**Status:** Implemented under `/v1/policy-packs` in `PolicyPacksIntegrationTests` / `PolicyPackRequestValidationTests` (lifecycle, publish idempotency, list versions, assign unknown version → 404, invalid JSON → 400).

**Original problem:** Policy pack endpoints were not covered by `ArchiForge.Api.Tests`.

**Outcome:** Regression protection for governance packaging and API wiring.

---

## 60. Document policy packs and effective governance filtering

**Problem:** Operators and integrators must read code to learn how assignments resolve, how `effective-content` merges packs, and that non-empty `alertRuleIds` / `compositeAlertRuleIds` filter alert evaluation.

**Change:**
- Add a **Policy packs** subsection to `docs/API_CONTRACTS.md` or `docs/BUILD.md`: list endpoints, assignment semantics, merge rules (union IDs, advisory defaults last-wins), and the “empty ID list = no filter” rule for alerts.
- Optionally one sentence in README under Key documentation linking to that section.

**Outcome:** Runbooks and SaaS onboarding without spelunking `PolicyPackResolver` / `AlertService`.

---

## 61. Centralize `JsonSerializerOptions` for policy pack content JSON

**Problem:** `EffectiveGovernanceLoader` owns a private `PackJsonOptions`; any future writer (seed tooling, tests, or API) may duplicate options and drift on camelCase / trailing commas.

**Change:**
- Extract `PolicyPackJsonSerializerOptions` (static readonly) or register `IOptions<JsonSerializerOptions>` named `"PolicyPacks"` in DI; use in `EffectiveGovernanceLoader` and anywhere else that serializes `PolicyPackContentDocument`.
- Document the options in code (why case-insensitive read).

**Outcome:** One place to adjust JSON behavior for pack payloads.

---

## 62. Avoid duplicate `LoadEffectiveContentAsync` in a single evaluation pipeline

**Problem:** `AlertService` and `CompositeAlertService` each call `IEffectiveGovernanceLoader.LoadEffectiveContentAsync` per evaluation. When both run in the same scheduled scan, effective content is loaded twice with identical scope.

**Change:**
- Prefer passing `PolicyPackContentDocument?` on `AlertEvaluationContext` (set once by the orchestration path that runs both), **or** add a scoped `IEffectiveGovernanceCache` / `AsyncLocal` keyed by tenant/workspace/project for the duration of one “scan” operation.
- Fall back to loading when context has no cached document (tests, standalone calls).

**Outcome:** Half the resolver/merge work on hot paths; clearer lifecycle for “effective governance for this run.”

---

## Checklist (governance / policy packs — next five)

- [x] 58. Api: FluentValidation for policy pack create / publish / assign DTOs
- [x] 59. Api.Tests: integration tests for policy pack create → publish → assign → effective(-content)
- [x] 60. Docs: policy packs + effective governance filtering (`API_CONTRACTS` or `BUILD` + optional README link)
- [x] 61. Refactor: shared `JsonSerializerOptions` (or options type) for policy pack content JSON
- [x] 62. Refactor: cache or pass-through effective governance document when both alert services run in one pipeline

---

## 63. Swagger: policy pack request examples (create / publish / assign)

**Problem:** Integrators discover `PolicyPackContentDocument` shape only from code or `API_CONTRACTS.md`; Swagger did not show example JSON for governance POST bodies.

**Change:** Add **`PolicyPackExamplesOperationFilter`** (markdown JSON blocks on **PolicyPacksController** `Create`, `Publish`, `Assign`) and register it in **`AddArchiForgeSwagger`**.

**Outcome:** Interactive docs parity with architecture create-run examples.

---

## 64. OpenAPI: document `#policy-pack-version-not-found` on assign

**Problem:** `POST .../policy-packs/{id}/assign` returns **404** with a specific problem type; Swagger 404 descriptions did not mention it.

**Change:** Extend **`ProblemDetailsResponsesOperationFilter`** when path contains `policy-packs` and `assign`.

**Outcome:** Swagger describes assign-not-found alongside run-not-found.

---

## 65. Unit tests: `ComplianceRulePackGovernanceFilter`

**Problem:** Compliance rule filtering by policy keys/GUIDs is easy to regress without fast tests.

**Change:** Add **`ComplianceRulePackGovernanceFilterTests`** in **`ArchiForge.Decisioning.Tests`** (empty filter, keys case-insensitive, GUID `RuleId`, combined keys+ids).

**Outcome:** Local regression protection without API harness.

---

## 66. Api.Tests: `effective-content` merges `complianceRuleKeys`

**Problem:** Integration coverage asserted advisory defaults and metadata but not merged **`complianceRuleKeys`**.

**Change:** Add **`EffectiveContent_MergesComplianceRuleKeys_FromAssignedPack`** to **`PolicyPacksIntegrationTests`**; extend response DTO with **`ComplianceRuleKeys`**.

**Outcome:** End-to-end proof that resolver + merge surface string compliance rule IDs.

---

## 67. Swagger document description: governance + alert `v1` routes

**Problem:** Top-level Swagger blurb listed architecture and ingestion but not where policy packs and operator alerts live.

**Change:** Append one sentence to **`SwaggerExtensions`** `SwaggerDoc` description listing **`/v1/policy-packs`** and alert/digest/tuning/simulation paths.

**Outcome:** First-screen discoverability for governance and alerting APIs.

---

## Checklist (governance / policy packs — follow-up five)

- [x] 63. Api: Swagger operation filter with policy pack JSON examples (create / publish / assign)
- [x] 64. Api: ProblemDetails OpenAPI hint for assign → `#policy-pack-version-not-found`
- [x] 65. Decisioning.Tests: `ComplianceRulePackGovernanceFilter` unit tests
- [x] 66. Api.Tests: integration test for merged `complianceRuleKeys` on `effective-content`
- [x] 67. Api: Swagger top-level description mentions `v1` policy packs + alert-related routes

---

## Batch (suggested §68–87 — integrated subset, non-breaking)

The following were implemented together for safer operator APIs, tests, and background scope (no SemVer breaking change on publish, no assign behavior change).

| Item | Done |
|------|------|
| Ambient scope for advisory scan + `HttpScopeContextProvider` fallback | `AmbientScopeContext`, advisory `RunScheduleAsync` |
| Swagger: alert POST examples | `AlertExamplesOperationFilter` |
| Swagger: tag grouping (Governance / Alerts & routing / Digest) | `SwaggerExtensions.TagActionsBy` |
| Route constants for tests | `ApiV1Routes` |
| FluentValidation: simulation, compare-candidates, tuning, alert/composite rule bodies | `*Validator` types in `ArchiForge.Api/Validators` |
| `AlertEvaluationContextFactory` | `ForAdvisoryScan` + `AdvisoryScanRunner` |
| Structured logs on policy publish/assign | `ILogger<PolicyPacksController>` |
| Tests: `PolicyPackGovernanceFilter`, `ImprovementPlan` JSON, alert list smoke, two-assignment merge, assign 404 `type` | `Decisioning.Tests`, `Api.Tests` |
| Docs: multi-assignment merge, ambient scope, rate limit pointer | `API_CONTRACTS.md` |

---

## Batch (post–§67 — “next five” integrated)

| # | Item | Notes |
|---|------|--------|
| 1 | OpenAPI schema enrichment for `PolicyPackContentDocument` | `PolicyPackContentDocumentSchemaFilter` (description + pointer to POST examples). |
| 2 | UI route constants | `archiforge-ui/src/lib/api-v1-routes.ts` + `api.ts` uses `ApiV1Routes`; fixed stray `/api/...` for policy packs & digest toggle. |
| 3 | SemVer 2 validation | `PolicyPackRequestValidationRules.BePolicyPackSemVerVersion` on publish + assign; `API_CONTRACTS.md` updated. |
| 4 | `IPolicyPacksAppService` / `PolicyPacksAppService` | Create, publish, assign + audit orchestration; thin `PolicyPacksController`. |
| 5 | Integration tests | `PublishPolicyPack_InvalidSemVerVersion_Returns400`; `EffectiveContent_MergesAlertRuleIds_FromAssignedPack`; repaired `ResolvedPackResponse` DTO. |
| — | `ApiV1Routes` (C#) | Extended to mirror UI segments (composite alerts, simulation, tuning, routing, digest). |

---

## §88 — KnowledgeGraph.Tests project

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.KnowledgeGraph.Tests` project (xUnit + FluentAssertions + Moq). Added to solution under the `tests` folder.
- `GraphValidatorTests` — 7 cases covering null/blank NodeId, blank NodeType, missing edge node, case-insensitive node lookup, valid graph.
- `GraphNodeFactoryTests` — 8 cases covering NodeId prefix, NodeType, Label, Category from property, SourceType/SourceId passthrough, and null guard.
- `DefaultGraphEdgeInfererTests` — 8 cases covering null guards, topology CONTAINS, security PROTECTS, policy APPLIES_TO, requirement RELATES_TO (text match), network→subnet CONTAINS_RESOURCE, edge deduplication.
- `KnowledgeGraphServiceTests` — 5 cases: null snapshot, builder+validator call order, IDs propagated, nodes/edges copied, validator exception propagated.

---

## §89 — First-class `PolicySection` on `GoldenManifest`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning/Manifest/Sections/PolicySection.cs` — `SatisfiedControls`, `Violations`, `Exemptions`, `Notes`.
- `ArchiForge.Decisioning/Manifest/Sections/PolicyControlItem.cs` — `ControlId`, `ControlName`, `PolicyPack`, `Description`.
- `ArchiForge.Decisioning/Manifest/Sections/PolicyExemption.cs` — `ControlId`, `Justification`, `ExpiresUtc`.
- `ArchiForge.Decisioning/Models/GoldenManifest.cs` — added `PolicySection Policy { get; set; } = new();`.

**Follow-up:**
- SQL column (e.g. `PolicyJson NVARCHAR(MAX)`) is a future migration if policy data must be queryable outside manifests. For now it serialises with the manifest blob.
- `GoldenManifestBuilder` should populate `Policy.SatisfiedControls` from `ComplianceSection.Controls` where status is satisfied, and `Policy.Violations` for gaps.

---

## §90 — Schema validation service: result caching + OTel metrics

**Status:** Done (Mar 2026).

**What was built:**
- `SchemaValidationOptions.EnableResultCaching` (bool, default false) and `ResultCacheMaxSize` (int, default 256).
- `SchemaValidationService`: when caching is enabled, results are keyed by SHA-256(`schemaName|json`) in a `ConcurrentDictionary`. Cache is cleared (not evicted) when it reaches `ResultCacheMaxSize`.
- OTel `Meter` (`ArchiForge.DecisionEngine.SchemaValidation`): `schema_validation_total` counter tagged by schema name + outcome; `schema_validation_duration_ms` histogram tagged by schema name.
- `ValidateCore` replaces the old `Validate` private method; caching is an outer wrapper.

**Future:**
- Replace clear-on-max with an LRU eviction pattern if hot-path caching matters.
- Wire `MeterName` into `AddArchiForgeOpenTelemetry` so Prometheus sees it.

---

## §91 — CLI scaffold hardening

**Status:** Done (Mar 2026).

**What was built:**
- `ScaffoldOptions.ConnectionString` property (nullable string). `RegisterProject = true` now throws `InvalidOperationException` when `ConnectionString` is null or whitespace — no hardcoded server address.
- `docs/CLI_API_IMPLEMENTATION_PLAN.md` Current State table updated to reflect all phases complete.

---

## §92 — Test coverage: `ExportReplayService` + `ComparisonDriftAnalyzer`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Api.Tests/ExportReplayServiceTests.cs` — 6 unit tests for `BuildReplayFileName` (via reflection): blank/whitespace/null → fallback, simple `.docx` suffix, dotted name, no-extension case.
- `ArchiForge.Api.Tests/ComparisonDriftAnalyzerTests.cs` — 9 unit tests: no drift, scalar value change, property added/removed, array length/element change, type change, nested path, summary count.

---

## §93 — CI migration + seeding regression loop (docs)

**Status:** Done (Mar 2026).

**What was built:**
- `docs/CI_MIGRATION_CHECKLIST.md` — local pre-push command sequence; per-migration and per-seed-change sub-checklists; CI YAML snippet; security notes.
- `docs/SQL_SCRIPTS.md §6` — expanded to a structured checklist with Required/data-access/seeding/CI-gate sections and a link to `CI_MIGRATION_CHECKLIST.md`.

---

## §94 — `docs/HOWTO_ADD_COMPARISON_TYPE.md`

**Status:** Done (Mar 2026).

**What was built:**
- Full how-to: type constant, service+formatter interfaces, implementation rules, dispatcher branch, DI registration, OpenAPI, SQL migration checklist, data flow diagram, security model, test inventory, reference implementations.

---

## Checklist (§88–94)

- [x] 88. KnowledgeGraph.Tests: project + GraphValidator / GraphNodeFactory / DefaultGraphEdgeInferer / KnowledgeGraphService tests
- [x] 89. Decisioning: PolicySection + PolicyControlItem + PolicyExemption on GoldenManifest
- [x] 90. DecisionEngine: schema validation result caching + OTel metrics (counter + histogram)
- [x] 91. Cli: ScaffoldOptions.ConnectionString; remove hardcoded server address
- [x] 92. Api.Tests: ExportReplayServiceTests + ComparisonDriftAnalyzerTests
- [x] 93. Docs: CI_MIGRATION_CHECKLIST.md; expanded SQL_SCRIPTS §6 checklist
- [x] 94. Docs: HOWTO_ADD_COMPARISON_TYPE.md

---

## §95 — `FindingTypes` constants class

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning/Findings/FindingTypes.cs` — 10 `const string` fields replacing bare string literals in `DefaultGoldenManifestBuilder.GetByType(...)` calls.
- All 10 call-sites updated. Compile-time safety; typo-silent-failures eliminated.

---

## §96 — `GoldenManifestValidator`: PolicySection null guard + tests

**Status:** Done (Mar 2026).

**What was built:**
- `GoldenManifestValidator.Validate` — added `Policy is null` guard mirroring existing section guards.
- `ArchiForge.Decisioning.Tests/GoldenManifestValidatorTests.cs` — 10 unit tests (one per guarded field including the new Policy guard).

---

## §97 — `ManifestHashService`: PolicySection in canonical hash

**Status:** Done (Mar 2026).

**What was built:**
- `ManifestHashService.ComputeHash` — `manifest.Policy` added to the anonymous projection before SHA-256 serialization.
- `ArchiForge.Decisioning.Tests/ManifestHashServiceTests.cs` — 6 tests: stable hash for equal manifests, distinct hash when ManifestId/PolicyViolation/PolicySatisfiedControl differ, null throws, hex format validated.

---

## §98 — `AdaptiveRecommendationScorer` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/AdaptiveRecommendationScorerTests.cs` — 8 tests covering: null profile passthrough, individual weight application (category/urgency/signalType), all-weights product, missing-key defaults, null signalType skipping weight, and zero-base-score edge case.

---

## §99 — `DefaultGraphEdgeInferer`: explicit parent/child `CONTAINS_RESOURCE`

**Status:** Done (Mar 2026).

**What was built:**
- `DefaultGraphEdgeInferer.InferExplicitParentChildContainment` — new private method called before heuristic `InferTopologyContainment`. Reads a `parentNodeId` property from each node's Properties dictionary and emits a `CONTAINS_RESOURCE` edge when the parent exists in the snapshot.
- O(n) lookup via `Dictionary<string, GraphNode>` built once per `InferEdges` call.

---

## §100 — `ImprovementSignalAnalyzer` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/ImprovementSignalAnalyzerTests.cs` — 12 tests: null guards, empty manifest, one signal per analyzer branch (requirement/security/compliance/topology/cost/unresolvedIssue), regression/identical security delta, cost increase/decrease, decision removed.

---

## §101 — `DefaultGraphBuilder` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.KnowledgeGraph.Tests/DefaultGraphBuilderTests.cs` — 6 tests: null snapshot throws, empty snapshot produces context-only node, context node id matches snapshotId, canonical objects call node factory, inferred edges appear in result, context node properties contain snapshotId/runId/projectId.

---

## §102 — `RecommendationGenerator` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/RecommendationGeneratorTests.cs` — 8 tests using Moq for `IAdaptiveRecommendationScorer`: null guard, empty list, signal-type→title mapping, unknown type fallback, ordering by priority score, finding-id propagation, urgency mapping, and impact text per category.
- Added `<PackageReference Include="Moq" />` to `ArchiForge.Decisioning.Tests.csproj`.

---

## §103 — `docs/KNOWLEDGE_GRAPH.md`: Suggested next refactors updated

**Status:** Done (Mar 2026).

**What was built:**
- "Suggested next refactors" section rewritten with a done/open table. Items 1, 2, 4, 5 marked complete; item 3 (policy/requirement targeting) and 4 new open items (weighted edges, persistence indexes, incremental rebuild) documented.

---

## §104 — OTel: SchemaValidation meter wired into `AddArchiForgeOpenTelemetry`

**Status:** Done (Mar 2026).

**What was built:**
- `ObservabilityExtensions.WithMetrics` — `metrics.AddMeter(SchemaValidationService.MeterName)` added so `schema_validation_total` and `schema_validation_duration_ms` are exported to Prometheus/OTLP without any additional configuration.
- `using ArchiForge.DecisionEngine.Validation;` added (project reference already present).

---

## Checklist (§95–104)

- [x] 95. `FindingTypes` constants class; 10 `GetByType` string literals replaced
- [x] 96. `GoldenManifestValidator`: PolicySection null guard + 10 unit tests
- [x] 97. `ManifestHashService`: PolicySection in canonical hash + 6 unit tests
- [x] 98. `AdaptiveRecommendationScorer`: 8 unit tests
- [x] 99. `DefaultGraphEdgeInferer`: explicit `parentNodeId` → `CONTAINS_RESOURCE`
- [x] 100. `ImprovementSignalAnalyzer`: 12 unit tests
- [x] 101. `DefaultGraphBuilder`: 6 unit tests in `KnowledgeGraph.Tests`
- [x] 102. `RecommendationGenerator`: 8 unit tests; Moq added to Decisioning.Tests
- [x] 103. `docs/KNOWLEDGE_GRAPH.md`: next refactors table updated
- [x] 104. OTel: SchemaValidation meter wired into `AddArchiForgeOpenTelemetry`

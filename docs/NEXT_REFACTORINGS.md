# Next refactorings

**Last updated:** 1 April 2026 (§329–338 delivered).

Early items **1–7** (JSON test options, `ComparisonReplayTestFixture`, comparison facade decision, health and replay validation docs, fixture reuse, Api.Tests JSON audit) are **done**. Their original write-ups are preserved under [Archive (completed items 1–7)](#archive-completed-items-17) near the bottom of this file (immediately before batch §88).

Numbered sections **8+** below continue the living backlog (rate limits, traits, CORS, OpenTelemetry extraction, …).

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
- In **README.md** (Database Setup or Running the API), add one sentence: if the connection string is set and migration fails, the API throws and does not start (no fallback). Integration tests use SQL Server with DbUp on the test host.

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

## Archive (completed items 1–7)

<a id="archive-completed-items-17"></a>

Historical detail for the first integration batch (all checkboxes done). Kept for provenance; skip when scanning for **open** work.

---

### 1. Unify Api.Tests JSON options

**Problem:** Several tests that extend `IntegrationTestBase` still use `new JsonOptions().JsonSerializerOptions` instead of the inherited `JsonOptions` (and some use it for `ReadFromJsonAsync` only, while the base also provides `JsonContent(object)`).

**Change:**
- In **ArchitectureControllerTests**, **ArchitectureDiagramTests**, and **ArchitectureSummaryTests**, replace every `new JsonOptions().JsonSerializerOptions` with `JsonOptions`, and use `JsonContent(...)` from the base where building request bodies.
- Optionally add a single shared `JsonSerializerOptions` in a test helper if any tests don’t inherit `IntegrationTestBase` but need the same options.

**Outcome:** One place to tune JSON behavior; consistent test style.

---

### 2. Use ComparisonReplayTestFixture in end-to-end comparison tests

**Problem:** **ArchitectureEndToEndComparisonExportTests** and **ArchitectureEndToEndComparisonTests** (and any similar) repeat the same flow: create run → execute → commit → replay, then call compare/export by `leftRunId`/`rightRunId`. Only **ComparisonReplayVerifyDriftIntegrationTests** currently uses **ComparisonReplayTestFixture**.

**Change:**
- In **ArchitectureEndToEndComparisonExportTests** and **ArchitectureEndToEndComparisonTests**, use `ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(Client, JsonOptions)` to obtain `(runId, replayRunId)`, then call the compare/export endpoints with those IDs.
- Where a test needs a **persisted** comparison (e.g. for replay by `comparisonRecordId`), also use `PersistEndToEndComparisonAsync` and then hit the comparisons replay endpoint.

**Outcome:** Less duplicated setup; changes to the create/execute/commit/replay flow live in one fixture.

---

### 3. RunComparisonController: optional facade for end-to-end services

**Problem:** **RunComparisonController** injects three application services for end-to-end comparison: `IEndToEndReplayComparisonService`, `IEndToEndReplayComparisonSummaryFormatter`, `IEndToEndReplayComparisonExportService`. That’s a lot of constructor parameters and ties the API to three separate abstractions.

**Change (optional):**
- Introduce an application-level facade, e.g. **`IEndToEndComparisonFacade`** (or **`IRunComparisonAppService`**), in **ArchiForge.Application**, with methods that delegate to the existing three services. Register the facade in **Program.cs** and inject it into **RunComparisonController** for the end-to-end summary/export actions; keep agent compare and audit as-is (or also behind the facade if you want a single “run comparison” entry point).
- Alternatively, leave the controller as-is and document that we intentionally keep the three services explicit for clarity and testability.

**Outcome:** Either a thinner controller and a single “comparison” dependency for those operations, or a documented decision to keep fine-grained dependencies.

---

### 4. Health check documentation

**Problem:** The API registers `AddHealthChecks()` with a database check and maps `/health`, but this isn’t documented for operators or in BUILD/README.

**Change:**
- In **README.md** or **docs/BUILD.md**, add a short “Health” section: what `/health` returns, that it includes a DB check, and that failure is unhealthy. Optionally mention readiness vs liveness if you later split them (e.g. liveness = no deps, readiness = DB).

**Outcome:** Clear contract for monitoring and runbooks.

---

### 5. Comparison replay request validation

**Problem:** The comparison replay endpoint accepts a body (format, replayMode, profile, persistReplay, etc.). Validation may be ad hoc or missing; OpenAPI and 400 responses could be more consistent.

**Change:**
- Add a **FluentValidation** validator for the comparison replay request DTO (e.g. **ReplayComparisonRequest** or whatever the bound model is). Validate format enum, replayMode, optional profile, etc.
- Register it with **AddValidatorsFromAssemblyContaining** (or the existing pattern). Ensure the controller uses the validated model so 400 responses and Swagger reflect the same rules.
- Optionally add a short note in **API_CONTRACTS.md** or Swagger description that validation errors return 400 with problem details.

**Outcome:** Consistent validation and better API docs for replay request shape.

---

### 6. Use ComparisonReplayTestFixture in ArchitectureComparisonReplayTests

**Problem:** **ArchitectureComparisonReplayTests** repeats the same create→execute→commit→replay flow, then persists via end-to-end summary and finally calls `POST comparisons/{comparisonRecordId}/replay`. Only the last step is unique; the rest matches **ComparisonReplayTestFixture**.

**Change:**
- Use `ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(Client, JsonOptions)` to get `(runId, replayRunId)`.
- Use `ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(Client, runId, replayRunId)` to get `comparisonRecordId`.
- Then `POST /v1/architecture/comparisons/{comparisonRecordId}/replay` with the desired body (e.g. `{ format = "markdown" }`).

**Outcome:** One less place with duplicated run/replay/persist setup; consistent with other E2E comparison tests.

---

### 7. Audit remaining Api.Tests for JsonOptions / JsonContent

**Problem:** After refactorings 1–2, some test files may still use `new JsonOptions().JsonSerializerOptions` or construct request bodies without using the base `JsonContent(object)`. Inconsistencies make it harder to change JSON behavior in one place.

**Change:**
- Grep for `new JsonOptions()` or `JsonSerializerOptions` in **ArchiForge.Api.Tests** and replace with inherited `JsonOptions` where the test extends **IntegrationTestBase**.
- Where request bodies are built with `new StringContent(JsonSerializer.Serialize(...))`, prefer the base `JsonContent(value)` if the test has access to it.
- Optionally add a one-line note in **TEST_STRUCTURE.md** that integration tests should use the base `JsonOptions` and `JsonContent`.

**Outcome:** Full consistency across Api.Tests; single place to tune JSON for tests.

---

### Checklist (items 1–7)

- [x] 1. Api.Tests: use `JsonOptions` / `JsonContent` from base everywhere
- [x] 2. Api.Tests: use `ComparisonReplayTestFixture` in E2E comparison and export tests
- [x] 3. Application + Api: optional `IEndToEndComparisonFacade` and controller refactor (or document “no facade”)
- [x] 4. Docs: health check section in README or BUILD.md
- [x] 5. Api: FluentValidation for comparison replay request + docs/OpenAPI alignment
- [x] 6. Api.Tests: use `ComparisonReplayTestFixture` in **ArchitectureComparisonReplayTests** (create→execute→commit→replay→persist via fixture, then call `comparisons/{id}/replay`)
- [x] 7. Api.Tests: audit remaining tests for `JsonOptions` / `JsonContent` — any file still using `new JsonOptions().JsonSerializerOptions` or not using base `JsonContent` should be updated for consistency

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
- `docs/SQL_SCRIPTS.md` (change checklist section) — structured checklist with Required/data-access/seeding/CI-gate sections and a link to `CI_MIGRATION_CHECKLIST.md`.

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
- [x] 93. Docs: CI_MIGRATION_CHECKLIST.md; expanded SQL_SCRIPTS change checklist
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

---

## §105 — `InMemoryBackgroundJobQueue`: `ILogger` + `LogError` on job failure

**Status:** Done (Mar 2026).

**What was built:**
- Primary constructor `InMemoryBackgroundJobQueue(ILogger<InMemoryBackgroundJobQueue> logger)`; `catch` in `ExecuteAsync` calls `logger.LogError(ex, "Background job {JobId} failed.", item.JobId)` before updating failed job state.

---

## §106 — `ArchiForgeApiClient`: stderr logging on unexpected failures

**Status:** Done (Mar 2026).

**What was built:**
- `LogCliFailure(operation, ex)` writes to `Console.Error` with exception type and message.
- Bare `catch` blocks on health, get run/manifest, comparison history/drift/summary/diagnostics, update comparison replaced with `catch (Exception ex)` + `LogCliFailure`. `TryParseError` uses `catch (Exception)` with a comment (no stderr noise for garbage JSON).

---

## §107 — `SimpleScanScheduleCalculator` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/SimpleScanScheduleCalculatorTests.cs` — `@hourly` / `@daily` / `@weekly`, `0 7 * * *` before/after 07:00 UTC, unknown cron → +1 day, whitespace trim.

---

## §108 — `ArchitectureDigestBuilder` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/ArchitectureDigestBuilderTests.cs` — empty recommendations/alerts text, top-5 slice from seven items, alert lines, `MetadataJson` counts, null plan throws, `ComparedToRunId` line.

---

## §109 — `FindingsOrchestrator` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/FindingsOrchestratorTests.cs` — null graph throws, two engines invoked, engine exception propagates, category mismatch throws, dedupe by type+title, empty category filled from engine.

---

## §110 — `PolicyCoverageFindingEngine` + `RequirementCoverageFindingEngine` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `PolicyCoverageFindingEngineTests.cs` — no policy nodes, full coverage empty, uncovered resources payload; stable `EngineType`/`Category`.
- `RequirementCoverageFindingEngineTests.cs` — all related → empty; unrelated → finding + payload; stable `EngineType`/`Category`.

---

## §111 — `docs/ARCHITECTURE_INDEX.md`: more doc links

**Status:** Done (Mar 2026).

**What was built:**
- New subsections: API and contracts; Build, CLI, operations; Contributing and process — linking `API_CONTRACTS`, `ALERTS`, findings schema docs, `BUILD`, `CLI_*`, `demo-quickstart`, `RUNBOOK_REPLAY_DRIFT`, `TEST_STRUCTURE`, `FORMATTING`, `METHOD_DOCUMENTATION`, `NEXT_REFACTORINGS`.

---

## §112 — `ArchitectureRunService.CommitRunAsync`: extracted private helpers

**Status:** Done (Mar 2026).

**What was built:**
- `TryReturnCommittedManifestAsync`, `EnforceCommitAllowedForStatus`, `EnsureCommitPrerequisitesAsync`, `FailRunAfterMergeFailureAsync`, `PersistCommittedRunAsync`; public `CommitRunAsync` is orchestration-only.

---

## §113 — `InMemoryBackgroundJobQueue` unit tests (capacity + eviction + failure)

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Api.Tests/InMemoryBackgroundJobQueueTests.cs` — channel full → `InvalidOperationException`; work throws → failed state + `Error` message; 201 sequential completions with small delay → oldest job evicted from `GetInfo`.

---

## §114 — `docs/NEXT_REFACTORINGS.md`: archive items 1–7; front matter trimmed

**Status:** Done (Mar 2026).

**What was built:**
- Top of file: short “last updated” note + pointer to anchor `archive-completed-items-17`; living backlog starts at §8.
- Before §88: full **Archive (completed items 1–7)** section with original §1–§7 text and checklist.

---

## Checklist (§105–114)

- [x] 105. `InMemoryBackgroundJobQueue`: `ILogger` + `LogError` on failure
- [x] 106. `ArchiForgeApiClient`: `LogCliFailure` / explicit exception catches
- [x] 107. `SimpleScanScheduleCalculator` tests
- [x] 108. `ArchitectureDigestBuilder` tests
- [x] 109. `FindingsOrchestrator` tests
- [x] 110. Policy + requirement coverage engine tests
- [x] 111. `ARCHITECTURE_INDEX.md` expanded
- [x] 112. `CommitRunAsync` refactor into private methods
- [x] 113. `InMemoryBackgroundJobQueue` tests (Api.Tests)
- [x] 114. `NEXT_REFACTORINGS` archive + intro

---

## §115 — `ImprovementSignalCategories` + `ImprovementSignalSeverities`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning/Advisory/Models/ImprovementSignalCategories.cs` — Requirement, Security, Compliance, Topology, Cost, Risk.
- `ArchiForge.Decisioning/Advisory/Models/ImprovementSignalSeverities.cs` — Critical, High, Medium.
- `ImprovementSignalAnalyzer` and `RecommendationGenerator` use these constants (`BuildImpact`, `ComputePriority` / `SeverityBonus`).

---

## §116 — `InMemoryGraphSnapshotRepository` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.KnowledgeGraph.Tests/InMemoryGraphSnapshotRepositoryTests.cs` — latest-by-context, empty indexed edges, ordered edge list.

---

## §117 — `DefaultGoldenManifestBuilder.PopulatePolicySection` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/DefaultGoldenManifestBuilderPolicySectionTests.cs` — policy applicability (info/warning), policy coverage (per-resource + empty list).

---

## §118 — `SqlGraphSnapshotRepository` batch edge insert

**Status:** Done (Mar 2026).

**What was built:**
- `InsertIndexedEdgesAsync` uses a single Dapper `ExecuteAsync` with an `IEnumerable` of parameter objects (one command preparation for all rows).

---

## §119 — `DecisionEngineService.MergeResults` early-path tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.DecisionEngine.Tests/DecisionEngineServiceMergeTests.cs` — blank runId / manifestVersion / empty results; schema failure; happy path with `PassthroughSchemaValidationService`.

---

## §120 — `PolicyReferenceConnector` + stable topology ids for `applicableTopologyNodeIds`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.ContextIngestion/Topology/TopologyHintStableObjectIds.cs` — deterministic 32-hex `ObjectId` from hint name (SHA-256, first 16 bytes).
- `PolicyReferenceConnector.FetchAsync` copies `TopologyHints`; `NormalizeAsync` sets `applicableTopologyNodeIds` to `obj-{stableId}` when policy reference and hint overlap (substring, case-insensitive).
- `TopologyHintsConnector` sets `ObjectId` from the same helper so graph `APPLIES_TO` targets align.

---

## §121 — `TopologyHintsConnector`: `parentNodeId` from `parent/child` hints

**Status:** Done (Mar 2026).

**What was built:**
- Hints containing `/` set `Properties["parentNodeId"]` = `obj-{TopologyHintStableObjectIds(parentSegment)}` so `DefaultGraphEdgeInferer` can emit `CONTAINS_RESOURCE` when a separate hint exists for the parent name.

---

## §122 — `GraphSnapshotReuseEvaluator` + tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.KnowledgeGraph/Services/GraphSnapshotReuseEvaluator.cs` — centralizes reuse vs rebuild; `AuthorityRunOrchestrator` calls it (private `ResolveGraphSnapshotAsync` removed).
- `GraphSnapshotReuseEvaluatorTests.cs` — prior null, fingerprint diff, equivalent without prior graph, equivalent with clone (no `BuildSnapshotAsync`).

---

## §123 — This checklist block (§115–124)

**Status:** Done (Mar 2026). Documents the integrated batch; no separate code change beyond this file.

---

## §124 — `PolicyViolation` signal + analyzer + generator

**Status:** Done (Mar 2026).

**What was built:**
- `ImprovementSignalTypes.PolicyViolation`; `AnalyzePolicyViolationSignals` in `ImprovementSignalAnalyzer` over `manifest.Policy.Violations` (null-safe).
- `RecommendationGenerator` title/action/impact for policy violations (Compliance category).

---

## Checklist (§115–124)

- [x] 115. `ImprovementSignalCategories` + `ImprovementSignalSeverities`; analyzer + generator wiring
- [x] 116. `InMemoryGraphSnapshotRepository` tests
- [x] 117. `DefaultGoldenManifestBuilder` policy section tests
- [x] 118. SQL graph snapshot edge batch insert
- [x] 119. `DecisionEngineServiceMergeTests`
- [x] 120. `TopologyHintStableObjectIds` + policy/topology connector linking
- [x] 121. Topology slash hints → `parentNodeId`
- [x] 122. `GraphSnapshotReuseEvaluator` + orchestrator + tests
- [x] 123. `NEXT_REFACTORINGS` §115–124 documentation
- [x] 124. `PolicyViolation` advisory signal end-to-end

---

## §125 — `FindingPayloadValidator`: replace private constants with `FindingTypes.*`

**Status:** Done (Mar 2026).

**What was built:**
- Deleted 10 private string constants from `FindingPayloadValidator`.
- All finding-type comparisons now reference `FindingTypes.RequirementFinding`, `FindingTypes.TopologyGap`, etc.
- Added `using ArchiForge.Decisioning.Findings;`. Compiler now catches any type-name drift.

---

## §126 — `AlertCategories` + `AlertUrgencies` constant classes

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning/Alerts/AlertCategories.cs` — Advisory, Compliance, Security, Cost, Recommendation, Learning, CompositeAlert.
- `ArchiForge.Decisioning/Alerts/AlertUrgencies.cs` — Critical, High.
- `AlertEvaluator` replaced all six category literals and two urgency literals; `CompositeAlertService` replaced its private `CompositeAlertCategory` constant.

---

## §127 — `AlertEvaluatorTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/AlertEvaluatorTests.cs` — 12 tests across all six rule types (disabled-rule guard; CriticalRecommendationCount: below/at-threshold/null-plan; NewComplianceGapCount: below/at; CostIncreasePercent: no-delta/below/at; DeferredHighPriorityAge: recent/old-enough/low-score; RejectedSecurityRecommendation: non-security/security; AcceptanceRateDrop: null-profile/above/below threshold).

---

## §128 — `AlertNoiseScorerTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/AlertNoiseScorerTests.cs` — 10 tests covering CoverageScore (zero/scales/cap-at-40), NoisePenalty (below-min/above-max/in-band), SuppressionPenalty (high-ratio/none), DensityPenalty (above-one/at-one), FinalScore arithmetic, and always-present summary notes.

---

## §129 — `PolicyApplicabilityFindingEngineTests` expanded

**Status:** Done (Mar 2026).

**What was built:**
- Added two new scenarios to `PolicyApplicabilityFindingEngineTests.cs`:
  - `NoPolicyNodes_ReturnsEmpty` — only topology nodes, no engine output.
  - `PolicyNodeWithNoTopologyResources_ReturnsEmpty` — policy node present but `topologyCount == 0`, engine skips gap finding.
- Existing two tests (info-finding with APPLIES_TO, warning-finding without APPLIES_TO) preserved.

---

## §130 — `AlertService`: extract `PersistAndDeliverAlertAsync`

**Status:** Done (Mar 2026).

**What was built:**
- Private `PersistAndDeliverAlertAsync(alert, context, ct) → bool` extracted from `EvaluateAndPersistAsync` inner loop.
- Returns `false` when a duplicate dedup key already exists; `true` when alert was newly persisted, audited, and delivered.
- `EvaluateAndPersistAsync` loop body is now a single call + conditional `persisted.Add`.

---

## §131 — `AlertGovernanceResolver` shared helper

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Persistence/Alerts/Helpers/AlertGovernanceResolver.cs` — internal static `ResolveAsync(context, loader, ct)` that short-circuits when `EffectiveGovernanceContent` is already set.
- Both `AlertService` and `CompositeAlertService` replaced their inline governance-loading expressions with `AlertGovernanceResolver.ResolveAsync(...)`.

---

## §132 — `AuthorityReplayServiceTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/AuthorityReplayServiceTests.cs` — 4 tests:
  - Unknown RunId → returns null.
  - `ReconstructOnly` → decision engine and artifact synthesis never called.
  - `RebuildManifest` → decision engine called once; artifact synthesis not called; `RebuiltManifest` set.
  - `RebuildArtifacts` → both decision engine and artifact synthesis called once; `RebuiltArtifactBundle` set.

---

## §133 — `GoldenManifestValidator`: `PolicySection` list null guards

**Status:** Done (Mar 2026).

**What was built:**
- `GoldenManifestValidator.Validate` now checks `Policy.SatisfiedControls`, `Policy.Violations`, and `Policy.Exemptions` for null after the section-level null check.
- Added three tests to `GoldenManifestValidatorTests.cs`: each list null → `InvalidOperationException` with the list name in the message.

---

## §134 — `InfrastructureDeclarationConnector.DeltaAsync`: object-count summary

**Status:** Done (Mar 2026).

**What was built:**
- `DeltaAsync` now compares `current.CanonicalObjects.Count` vs `previous?.CanonicalObjects.Count`.
- Returns `"Initial infrastructure declaration ingestion: {n} object(s)."` when no prior snapshot exists.
- Returns `"… no count change."` when counts match; `"… (Δ{diff:+#;-#;0} from prior snapshot)."` when they differ.
- Removed the `_ = current;` discard — `current` is now meaningfully consumed.

---

## Checklist (§125–134)

- [x] 125. `FindingPayloadValidator` — replace 10 private constants with `FindingTypes.*`
- [x] 126. `AlertCategories` + `AlertUrgencies` constant classes; wire into `AlertEvaluator` + `CompositeAlertService`
- [x] 127. `AlertEvaluatorTests` — 12 tests across all 6 rule types
- [x] 128. `AlertNoiseScorerTests` — 10 score-component tests
- [x] 129. `PolicyApplicabilityFindingEngineTests` — 2 additional scenarios (total 4)
- [x] 130. `AlertService.PersistAndDeliverAlertAsync` extraction
- [x] 131. `AlertGovernanceResolver` shared helper wired into both alert services
- [x] 132. `AuthorityReplayServiceTests` — 4 path tests
- [x] 133. `GoldenManifestValidator` `PolicySection` list null guards + 3 tests
- [x] 134. `InfrastructureDeclarationConnector.DeltaAsync` meaningful object-count delta

---

## §135 — `AlertMetricSnapshotBuilder`: constant-class adoption

**Status:** Done (Mar 2026).

**What was built:**
- Replaced hardcoded `"Critical"` / `"High"` urgency strings with `AlertUrgencies.Critical` / `AlertUrgencies.High`.
- Replaced hardcoded `"Security"` category string with `AlertCategories.Security`.
- No change to runtime behaviour; eliminates two silent magic-string drift sites.

---

## §136 — `AlertMetricSnapshotBuilderTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/AlertMetricSnapshotBuilderTests.cs` — 12 tests:
  - `CriticalRecommendationCount`: null plan → 0; mixed urgencies → counts only Critical + High.
  - `NewComplianceGapCount`: null comparison → 0; three `SecurityDelta` rows → 3.
  - `CostIncreasePercent`: no delta → 0; zero base cost → 0; valid delta → correct percent.
  - `DeferredHighPriorityRecommendationCount`: deferred + low priority → 0; deferred + high priority → 1.
  - `RejectedSecurityRecommendationCount`: rejected non-security → 0; rejected security → 1.
  - `AcceptanceRatePercent`: null profile → 0; zero proposed → 0; typical profile → 40 %.

---

## §137 — `CompositeAlertRuleEvaluatorTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/CompositeAlertRuleEvaluatorTests.cs` — 12 tests:
  - Empty conditions → false; unknown operator → false.
  - AND: all pass → true; one fails → false.
  - OR: any passes → true; all fail → false.
  - Six condition operators (`>=`, `>`, `<=`, `<`, `==`, `!=`) each tested at the exact boundary value.
  - Unknown metric type resolves to 0 (two variants: 0 ≥ 1 → false; 0 == 0 → true).

---

## §138 — `RuleKindConstants` shared class

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning/Alerts/Simulation/RuleKindConstants.cs` — `public const string Simple = "Simple"; Composite = "Composite"`.
- `RuleSimulationService` and `ThresholdRecommendationService` both replaced their private `const string` duplicates with `using static RuleKindConstants`.

---

## §139 — `RecommendationLearningAnalyzerTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/RecommendationLearningAnalyzerTests.cs` — 8 tests:
  - Empty input → empty stats and weights; notes always present.
  - Two categories → one `RecommendationOutcomeStats` bucket each with correct counts.
  - Urgency stats grouped independently of category stats.
  - All-accepted weight clamped at 2.0; all-rejected weight floor at 0.5.
  - Six category→signal-type mappings (Security, Compliance, Requirement, Topology, Cost, Unknown).
  - Notes contain count summary and category-weight phrase.

---

## §140 — `ThresholdRecommendationServiceTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/ThresholdRecommendationServiceTests.cs` — 5 tests:
  - Empty threshold list → `RecommendedCandidate` null, candidates empty, summary note.
  - Simple rule: `SimulateAsync` called once per threshold (3 × verified); `Candidates` count matches.
  - Simple rule: highest scorer is the `RecommendedCandidate`.
  - Composite rule: simulation called with `RuleKind = "Composite"` (verified) 2 ×.
  - Unknown `RuleKind` → `SimulateAsync` never called; candidates empty.

---

## §141 — `AlertService.ApplyActionAsync` tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/AlertServiceApplyActionTests.cs` — 6 tests:
  - Unknown alertId → returns null; repo `UpdateAsync` not called.
  - Unknown action → returns unchanged record; no update or audit.
  - Same-status no-op → no update or audit emitted.
  - `Acknowledge` → `Status = Acknowledged`; repo updated once; `AlertAcknowledged` audit event.
  - `Resolve` → `Status = Resolved`; `AlertResolved` audit event.
  - `Suppress` → `Status = Suppressed`; `AlertSuppressed` audit event.

---

## §142 — `SecurityBaselineFindingEngineTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/SecurityBaselineFindingEngineTests.cs` — 5 tests:
  - Empty graph → no findings.
  - Single `SecurityBaseline` node with no edges → one finding; `RelatedNodeIds` contains only that node.
  - `status = "missing"` → `FindingSeverity.Error`; payload `Impact` mentions "missing".
  - `status = "Present"` (case-insensitive) → `FindingSeverity.Info`.
  - `PROTECTS` edge to resource node → target node ID included in `RelatedNodeIds`; rationale mentions "PROTECTS".

---

## §143 — `DigestDeliveryDispatcher.DeliverToSubscriptionAsync` extraction

**Status:** Done (Mar 2026).

**What was built:**
- `DeliverAsync` now delegates the per-subscription loop body to a new private `DeliverToSubscriptionAsync(digest, subscription, ct)`.
- `DeliverToSubscriptionAsync` owns: attempt creation, channel resolution, `SendAsync`, status update, subscription `LastDeliveredUtc` stamp, and success/failure audit events.
- `OperationCanceledException` is documented as explicitly re-thrown; all other exceptions are caught and audited without propagating.
- No behaviour change; the refactor improves testability and reduces method length.

---

## §144 — `RecommendationFeedbackAnalyzerTests`

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/RecommendationFeedbackAnalyzerTests.cs` — 5 tests:
  - Empty list → empty dictionary.
  - Single bucket (same category+status twice) → key `"Category:Status"` with count 2.
  - Multi-bucket (Security×Proposed, Security×Accepted ×2, Cost×Rejected) → 3 entries with correct counts.
  - Repository called with batch cap of 1 000 (verified via Moq).
  - Key format verified as `"Category:Status"` (no normalisation).

---

## Checklist (§135–144)

- [x] 135. `AlertMetricSnapshotBuilder` — adopt `AlertUrgencies` / `AlertCategories` constants
- [x] 136. `AlertMetricSnapshotBuilderTests` — 12 metric tests (null guards + value paths for all 6 metrics)
- [x] 137. `CompositeAlertRuleEvaluatorTests` — 12 tests (empty, AND, OR, 6 operators, unknown metric)
- [x] 138. `RuleKindConstants` class; wire `RuleSimulationService` + `ThresholdRecommendationService`
- [x] 139. `RecommendationLearningAnalyzerTests` — 8 tests (stats, weights, signal-type inference, notes)
- [x] 140. `ThresholdRecommendationServiceTests` — 5 tests (no-candidates, Simple, highest score, Composite, unknown kind)
- [x] 141. `AlertService.ApplyActionAsync` tests — 6 paths (unknown id, unknown action, same-status, Ack/Resolve/Suppress)
- [x] 142. `SecurityBaselineFindingEngineTests` — 5 scenarios (empty, single, missing/Error, present/Info, PROTECTS edge)
- [x] 143. `DigestDeliveryDispatcher.DeliverToSubscriptionAsync` method extraction
- [x] 144. `RecommendationFeedbackAnalyzerTests` — 5 aggregation tests (empty, single, multi-bucket, cap, key format)

---

## §145 — `DigestDeliveryDispatcher` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/DigestDeliveryDispatcherTests.cs` — null digest, no subscriptions, success path (audit + subscription update), channel failure audited without throwing.

---

## §146 — `RuleSimulationService` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/RuleSimulationServiceTests.cs` — early exit when `UseHistoricalWindow` is false and no `RunId`; empty contexts note; simple-rule match outcome; composite path with suppression decision.

---

## §147 — `AlertGovernanceResolver` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `InternalsVisibleTo("ArchiForge.Decisioning.Tests")` on **ArchiForge.Persistence**.
- `ArchiForge.Decisioning.Tests/AlertGovernanceResolverTests.cs` — preloaded governance skips loader; otherwise loads once by scope.

---

## §148 — `AlertSuppressionPolicy` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/AlertSuppressionPolicyTests.cs` — no prior alert allows create; cooldown; suppression window; `RuleAndRun` dedupe key shape.

---

## §149 — `CompositeAlertService` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/CompositeAlertServiceTests.cs` — no rules + governance load; preloaded governance skips loader; match + allow → create, deliver, `CompositeAlertTriggered` audit; suppressed match → `AlertSuppressedByPolicy` audit.

---

## §150 — `TopologyCoverageFindingEngine` unit tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Decisioning.Tests/TopologyCoverageFindingEngineTests.cs` — zero topology nodes; missing categories payload; full coverage → empty list (mock `IGraphCoverageAnalyzer`).

---

## §151 — `GraphSnapshotStorageMapper` + repository reuse

**Status:** Done (Mar 2026).

**What was built:**
- `GraphSnapshotStorageRow.cs`, `GraphSnapshotStorageMapper.cs`; **SqlGraphSnapshotRepository** delegates deserialization to the mapper (single error message path).
- `ArchiForge.Decisioning.Tests/GraphSnapshotStorageMapperTests.cs` — valid round-trip via `JsonEntitySerializer`, null row, corrupt nodes JSON (wrapped exception chain).

---

## §152 — `DefaultGraphEdgeInferer` connector contract tests

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.KnowledgeGraph.Tests/DefaultGraphEdgeInfererContractTests.cs` — `parentNodeId` → `CONTAINS_RESOURCE` (weight 1); `applicableTopologyNodeIds` → targeted `AppliesTo` only for listed topology ids.

---

## §153 — CA1869: cached audit JSON options

**Status:** Done (Mar 2026).

**What was built:**
- `ArchiForge.Persistence/Serialization/AuditJsonSerializationOptions.cs` — shared `JsonSerializerOptions` instance (`CamelCase`, not indented).
- Wired into **DigestDeliveryDispatcher**, **AdvisoryScanRunner**, **CompositeAlertService**, **AlertService**, **AlertDeliveryDispatcher** (success + failure audit payloads); **AuthorityRunOrchestrator** now uses the same static instance instead of a duplicate field.

---

## §154 — `AdvisoryScanRunnerTests`: digest delivery verification

**Status:** Done (Mar 2026).

**What was built:**
- `RunScheduleAsync_WhenLatestRunHasGoldenManifest_PersistsDigestAndDelivers` — mocks full happy path through plan, alerts, digest build/persist, verifies `IDigestDeliveryDispatcher.DeliverAsync` for the built digest.

---

## Checklist (§145–154)

- [x] 145. `DigestDeliveryDispatcherTests`
- [x] 146. `RuleSimulationServiceTests`
- [x] 147. `AlertGovernanceResolverTests` + `InternalsVisibleTo`
- [x] 148. `AlertSuppressionPolicyTests`
- [x] 149. `CompositeAlertServiceTests`
- [x] 150. `TopologyCoverageFindingEngineTests`
- [x] 151. `GraphSnapshotStorageMapper` / row types + `SqlGraphSnapshotRepository` + mapper tests
- [x] 152. `DefaultGraphEdgeInfererContractTests` (KnowledgeGraph.Tests)
- [x] 153. `AuditJsonSerializationOptions` + persistence audit/result JSON call sites
- [x] 154. `AdvisoryScanRunner` digest persist + `DeliverAsync` verification test

---

## Multi-quarter backlog §155–§254 (proposed Mar 2026)

**Scope:** ~100 follow-on improvements (documentation, tests, observability, security, performance, API, data, UI). They are **not** all implemented in one release; use the checkboxes to track incremental delivery.

### Documentation & ADRs (155–169)

- [x] 155. Finish **METHOD_DOCUMENTATION** piece tracker **12–21** (XML for authority compare/replay through `ArchitectureRunService`) — completed: `<param>`, `<returns>`, `<summary>` on all gap types per audit (pieces 12–21 types documented).
- [x] 156. ADRs: effective governance merge, alert dedupe scopes, digest delivery failure semantics.
- [x] 157. Runbook: advisory scan failures and schedule advance.
- [x] 158. Runbook: comparison replay (light/heavy) + rate limits.
- [x] 159. **API_CONTRACTS:** idempotency / retry for create run + commit.
- [x] 160. **README:** operator quick start (health, `/v1`, correlation id, auth, SMB/storage boundary).
- [x] 161. Terraform variable reference diagram for Azure dependencies.
- [x] 162. File-backed connectors: private network + no public SMB (445).
- [x] 163. **CONTEXT_INGESTION:** connector ordering + warning surface.
- [x] 164. **ALERTS.md:** composite vs simple lifecycle diagram.
- [x] 165. **TEST_STRUCTURE:** map test projects → bounded contexts.
- [x] 166. **Swagger:** tag grouping for Authority, Advisory, Retrieval, Ask, artifacts, Comparison, Analysis, runs, learning, Audit, Diagnostics.
- [x] 167. **Swagger:** examples for alert rule **create** bodies (POST); no PUT/update routes for simple/composite rules in v1.
- [x] 168. **BUILD.md:** SQL Server vs Testcontainers for contributors.
- [x] 169. Provenance indexing: failure modes + retry story.

### Tests — unit (170–194)

- [x] 170. SQL-backed alert/digest repos: strategy (Testcontainers vs abstraction) + tests (`ArchiForge.Persistence.Tests`, DbUp via `DatabaseMigrator`, trait `SqlServerContainer`).
- [x] 171. `DapperArchitectureDigestRepository` / similar round-trips (`DapperArchitectureDigestRepositorySqlIntegrationTests`, `DapperAlertRuleRepositorySqlIntegrationTests`).
- [x] 172. **`AlertDeliveryDispatcherTests`** (null alert, no subs, success audit, failure audited).
- [x] 173. `EffectiveGovernanceLoader` / resolver coverage (`EffectiveGovernanceLoaderTests`, `EffectiveGovernanceResolverTests`).
- [x] 174. `PolicyPackResolver` / assignment resolution layers (`PolicyPackResolverTests`).
- [x] 175. `ImprovementAdvisorService` mapping tests (`ImprovementAdvisorServiceTests`).
- [x] 176. `ArchitectureDigestBuilder` markdown snapshot tests.
- [x] 177. `AuthorityQueryService` DTO mapping tests (`AuthorityRunMapperTests` — shared `RunSummary` / `ManifestSummary` projections).
- [x] 178. `ComparisonService` per-section delta tests (`ComparisonServiceTests`).
- [x] 179. `ExportReplayService` extra formats / errors (`analysis-report-docx` replay + `ExportReplayServiceReplayAsyncTests`).
- [x] 180. **`FindingPayloadValidatorTests`** (envelope guards, compliance + multi-type happy paths).
- [x] 181. `CostConstraintFindingEngine` / `ComplianceFindingEngine` branch coverage.
- [x] 182. `RetrievalQueryService` empty index + ranking (`ArchiForge.Retrieval.Tests`: `RetrievalQueryServiceTests`, `InMemoryVectorIndexTests`).
- [x] 183. `ConversationService` thread lifecycle (`ArchiForge.Api.Tests`: `ConversationServiceTests`).
- [x] 184. `DocxExportService` golden / snapshot tests (`ArchiForge.Coordinator.Tests`: `DocxExportServiceGoldenTests`).
- [x] 185. Coordinator ingestion mapper (`ContextIngestionRequestMapper.FromArchitectureRequest`) — `ContextIngestionRequestMapperTests` in Coordinator.Tests.
- [x] 186. `ContextIngestionService` parser-miss warnings.
- [x] 187. `JsonEntitySerializer` corrupt graph JSON tests.
- [x] 188. Golden audit JSON payload (camelCase) contract test.
- [x] 189. `SimpleScanScheduleCalculator` DST / timezone if behavior defined (UTC-only calculator; see `SimpleScanScheduleCalculatorTests`).
- [x] 190. Advisory scan poll semantics — `AdvisoryDueScheduleProcessor` (sequential due schedules, per-schedule error isolation, OCE propagation) + `AdvisoryDueScheduleProcessorTests`; `AdvisoryScanHostedService` delegates to processor.
- [x] 191. **`PolicyPacksAppServiceTests`** (create + audit; assign miss → no management call).
- [x] 192. `ReplayValidationConstants` extensions.
- [x] 193. `ApiProblemDetailsExceptionFilter` exception-type matrix.
- [x] 194. FluentValidation negative tests per advisory/alert DTO (sample: `AlertRuleBodyValidatorTests`).

### Tests — integration / E2E (195–204)

- [x] 195. Api.Tests: full alert lifecycle (rule → evaluate → list) — <c>AlertLifecycleIntegrationTests</c> + <c>AlertLifecycleWebAppFactory</c> (InMemory storage, seeded authority run, POST rule + schedule run + GET alerts).
- [x] 196. Api.Tests: digest subscription → delivery attempt row (`DigestDeliveryLifecycleIntegrationTests`).
- [x] 197. Api.Tests: governance two-pack merge + effective-content (`EffectiveContent_MergesAdvisoryDefaults_FromTwoAssignedPacks`; compliance union already in `EffectiveContent_UnionsComplianceRuleKeys_FromTwoAssignments`).
- [x] 198. Api.Tests: retrieval index + query smoke (fake vector) — `RetrievalQuerySmokeIntegrationTests` (index documents via DI, query via `GET api/retrieval/search`, empty index, topK clamp, validation).
- [x] 199. Api.Tests: Ask thread + fake LLM — `AskThreadIntegrationTests` (POST `api/ask` with seeded authority run, verify thread, follow-up on same thread, messages list, validation).
- [x] 200. Committed OpenAPI snapshot diff in CI.
  - **`ArchiForge.Api.Tests/Contracts/openapi-v1.contract.snapshot.json`** + **`OpenApiContractSnapshotTests`** (`Suite=Core`): compares **`GET /openapi/v1.json`** to the snapshot (regenerate: **`ARCHIFORGE_UPDATE_OPENAPI_SNAPSHOT=1`**). Runs in **fast core** (Tier 1). See **`docs/TEST_EXECUTION_MODEL.md`**.
- [x] 201. Load test: expensive rate-limit boundary.
  - **`docs/runbooks/LOAD_TEST_RATE_LIMITS.md`** + **`scripts/load/k6-expensive-rate-limit.js`** (configure **`ARCHIFORGE_EXPENSIVE_PATH`** + auth for real 429s).
- [x] 202. Resilience: SQL timeout → health / problem details — `ApplicationProblemMapper.TryMapDatabaseException` maps `SqlException(-2)` / `TimeoutException` → 503 `DatabaseTimeout`, `DbException` → 503 `DatabaseUnavailable`; `SqlConnectionHealthCheck` reports `Degraded` for transient SQL errors (timeout, Azure throttling); `ProblemTypes.DatabaseTimeout` / `DatabaseUnavailable` constants; `ProblemDetailsExtensions.ServiceUnavailableProblem` helper; unit tests in `ApiProblemDetailsExceptionFilterTests` + `SqlConnectionHealthCheckTests`.
- [x] 203. CI: migrate from N−1 schema.
  - **`DatabaseMigrator.RunExcludingTrailingScripts`** (`ArchiForge.Data`) + **`DatabaseMigratorUpgradePathSqlIntegrationTests`** (`SqlServerContainer`): N−1 pass then full **`Run`**.
- [x] 204. UI e2e: policy assign + effective-content.
  - **`archiforge-ui/e2e/policy-packs-journey.spec.ts`** + extended **`mock-archiforge-api-server`** (`v1/policy-packs` POST/GET).

### Observability & reliability (205–214)

- [x] 205. `ActivitySource` for orchestration, advisory scan, retrieval index.
- [x] 206. Metrics: digest delivery by channel.
- [x] 207. Metrics: alert evaluation duration (simple vs composite).
- [x] 208. Metrics: governance resolve + cache hit ratio (if cached) — `governance_resolve_duration_ms`; `governance_pack_content_deserialize_cache_hits` / `_misses` for per-resolve `(packId, version)` JSON dedupe.
- [x] 209. Correlation id → audit fields where missing (activity `correlation.id` chain + advisory/authority/index tags; `AuditService` enrichment).
- [x] 210. Retry / DLQ for background jobs — `InMemoryBackgroundJobQueue` now supports `maxRetries` (exponential backoff, DLQ on exhaustion); `BackgroundJobInfo` tracks `RetryCount`/`MaxRetries`; tests: retry→succeed, retry→exhaust→fail, zero-retry immediate fail.
- [x] 211. Outbox for post-commit indexing.
  - **`019_RetrievalIndexingOutbox.sql`**: table `dbo.RetrievalIndexingOutbox` (`OutboxId`, `RunId`, scope Guids, `CreatedUtc`, `ProcessedUtc`); filtered index on pending rows.
  - **`IRetrievalIndexingOutboxRepository`**: `DapperRetrievalIndexingOutboxRepository` / `InMemoryRetrievalIndexingOutboxRepository`; registered in `ArchiForgeStorageServiceCollectionExtensions` (singleton in-memory, scoped SQL).
  - **`AuthorityRunOrchestrator`**: after successful commit + run-completed audit, enqueues `RunId` + scope; no longer calls `IRetrievalRunCompletionIndexer` inline.
  - **`RetrievalIndexingOutboxProcessor`**: loads `RunDetailDto` via `IAuthorityQueryService`, rebuilds provenance, calls `IRetrievalRunCompletionIndexer`, marks processed.
  - **`RetrievalIndexingOutboxHostedService`**: polls every 2s (API `Hosted/`).
- [x] 212. Circuit breaker for OpenAI / embedding clients.
  - **`CircuitBreakerGate`** (`ArchiForge.Core.Resilience`): closed → open after N consecutive failures → half-open single probe after `DurationOfBreakSeconds` → closed on success; concurrent callers rejected while probe in flight; optional injectable `Func<DateTimeOffset>` clock for tests.
  - **`CircuitBreakerOptions`**: `FailureThreshold` (default 5), `DurationOfBreakSeconds` (default 30), bound from `AzureOpenAI:CircuitBreaker` (`FailureThreshold`, `DurationOfBreakSeconds`).
  - **`CircuitBreakerOpenException`**: thrown when open or probe busy; `RetryAfterUtc` when known.
  - **`CircuitBreakingAgentCompletionClient`** wraps `AzureOpenAiCompletionClient`; **`CircuitBreakingOpenAiEmbeddingClient`** wraps `AzureOpenAiEmbeddingClient`; independent **keyed** `CircuitBreakerGate` instances (`OpenAiCircuitBreakerKeys.Completion` / `Embedding`).
  - **ProblemDetails**: `ApplicationProblemMapper` maps to 503 `ProblemTypes.CircuitBreakerOpen` with `extensions.retryAfterUtc` when set; filter tests in `ApiProblemDetailsExceptionFilterTests`.
  - **Tests**: `CircuitBreakerGateTests`, `CircuitBreakingOpenAiEmbeddingClientTests` (`ArchiForge.Retrieval.Tests`); `CircuitBreakingAgentCompletionClientTests` (`ArchiForge.Api.Tests` + `AgentRuntime` reference).
- [x] 213. Graceful shutdown: advisory poller + host — `AdvisoryScanHostedServiceShutdownTests` (clean exit on cancellation during delay, continues after non-cancellation exception, handles OCE during processing).
- [x] 214. SLO dashboards (Grafana + Prometheus).
  - **`docs/runbooks/SLO_PROMETHEUS_GRAFANA.md`**: enable **`Observability:Prometheus:Enabled`**, metric inventory from **`ArchiForgeInstrumentation`**, example recording rule, Grafana panel outline, burn-rate note.

### Security (215–226)

- [x] 215. Entra ID app roles migration from long-lived API keys.
  - **`infra/terraform-entra/`**: optional **`azuread_application`** with **Admin / Operator / Reader** roles; **`enable_entra_api_app`** default **false**.
  - **`ArchiForge.Api`**: **`ArchiForgeAuth:NameClaimType`** (e.g. **`preferred_username`**); JWT **`RoleClaimType = "roles"`**; **`appsettings.Entra.sample.json`**.
  - **`docs/CUSTOMER_TRUST_AND_ACCESS.md`**: customer narrative and cutover notes (includes OpenAPI auth summary; **238**).
- [x] 216. Key Vault references for all secrets in config samples.
  - **`ArchiForge.Api/appsettings.KeyVault.sample.json`**: example `@Microsoft.KeyVault(...)` values for SQL, Azure OpenAI, API key auth.
  - **`docs/CONFIGURATION_KEY_VAULT.md`**: App Service / Terraform guidance and `__` nested key mapping.
- [x] 217. Private Link for SQL + Blob (Terraform). **AI Search** private endpoint not in this root (future extension).
  - **`infra/terraform-private/`**: VNet, private DNS, private endpoints for **SQL** and **Blob**; **`enable_private_data_plane`** default **false**; operators disable public access after cutover.
- [x] 218. **APIM (Consumption) in front of API** — Terraform `infra/terraform/` with **`enable_api_management`** (default **false** for laptop-only work); **`sku_name = Consumption_0`**; optional OpenAPI import from **`swagger/v1/swagger.json`**; outputs gateway URL + managed identity. **Edge WAF:** optional **`infra/terraform-edge/`** (Front Door Standard + WAF); see **`infra/README.md`**.
- [x] 219. SBOM (CycloneDX) in CI.
  - **`.github/workflows/ci.yml`**: after **`dotnet-fast-core`** build, **`dotnet tool install CycloneDX`** → BOM for **`ArchiForge.Api/ArchiForge.Api.csproj`** → artifact **`sbom-dotnet`**; after **`ui-unit`** **`npm ci`**, **`npx @cyclonedx/cyclonedx-npm@4.2.1`** → artifact **`sbom-npm`**.
  - Local commands: **`docs/BUILD.md`** (SBOM subsection).
- [x] 220. `dotnet list package --vulnerable` gate.
  - **`.github/workflows/ci.yml`**: `dotnet list package --vulnerable --include-transitive` after restore (fails build when the SDK reports vulnerable packages).
  - **`docs/BUILD.md`**: local/CI reminder.
- [x] 221. Secret scanning in CI.
  - **`.github/workflows/ci.yml`**: job **`gitleaks`** (`gitleaks/gitleaks-action@v2.3.9`, **`fetch-depth: 0`**); all other jobs **`needs: gitleaks`**.
  - **`.gitleaks.toml`**: **`[extend] useDefault = true`**; allowlist regexes for documented dev SQL passwords only (**`ArchiForge_Dev_Pass123!`**, **`LocalTesting123!`**).
- [x] 222. Row-level security design for multi-tenant SQL.
  - **`docs/security/MULTI_TENANT_RLS.md`**: SESSION_CONTEXT / policy sketch, defense-in-depth vs app authZ, ops + Terraform alignment.
- [x] 223. PII classification + retention for conversations.
  - **`docs/security/PII_RETENTION_CONVERSATIONS.md`**: classification, retention, backups/indexers, evolution.
- [x] 224. Threat model update for Ask/RAG.
  - **`docs/security/ASK_RAG_THREAT_MODEL.md`**: STRIDE-style table, scope isolation, prompt injection, ops logging.
- [x] 225. CORS regression test (disallowed origin).
- [x] 226. Security headers review (HSTS for non-dev + baseline headers; CSP for Swagger UI remains host-dependent).

### Performance & cost (227–234)

- [x] 227. SQL index review (alerts, runs, graphs, digests).
  - **`020_PerformanceIndexes_HotLists.sql`**: **`IX_Runs_Scope_Project_CreatedUtc`** on **`dbo.Runs`** for **`SqlRunRepository.ListByProjectAsync`** filters (`TenantId`, `WorkspaceId`, `ScopeProjectId`, `ProjectId`, `ORDER BY CreatedUtc DESC`). **AlertRecords**, **ArchitectureDigests**, **ConversationThreads**, **GraphSnapshots** already had scope/time indexes in **`ArchiForge.sql`**; no change required beyond Runs.
  - **`ArchiForge.sql`**: same index on **`Runs`** for greenfield/bootstrap parity.
- [x] 228. Remove N+1 on hot `ListByScope` paths.
  - **`IAlertRecordRepository.ListByScopePagedAsync`** / **`IConversationThreadRepository.ListByScopePagedAsync`**: COUNT + `OFFSET`/`FETCH` (Dapper) or in-memory skip/take; `AlertsController` / `ConversationController` use `PagedResponseBuilder.FromDatabasePage` instead of loading `MaxPageSize * 10` rows.
  - **`DapperAuthorityQueryService` / `InMemoryAuthorityQueryService` `GetRunDetailAsync`**: parallel `Task.WhenAll` for snapshot/manifest/bundle loads (single run hot path).
- [x] 229. Compression for large JSON responses.
- [x] 230. Cache effective governance per HTTP scope (beyond advisory path).
  - **`RequestScopedCachingEffectiveGovernanceLoader`**: scoped decorator over **`EffectiveGovernanceLoader`**; first **`LoadEffectiveContentAsync`** for a `(tenant, workspace, project)` triple wins, subsequent calls on the same request reuse the document.
  - **DI**: `ServiceCollectionExtensions` registers concrete **`EffectiveGovernanceLoader`** + **`IEffectiveGovernanceLoader`** → decorator.
  - **Tests**: `RequestScopedCachingEffectiveGovernanceLoaderTests` (`ArchiForge.Decisioning.Tests`).
- [x] 231. Graph snapshot pagination API design.
  - **`GET /api/graph/runs/{runId}/nodes`**: `page` / `pageSize` (see **`PaginationDefaults`**); response **`GraphNodesPageResponse`** (nodes + edges with both endpoints on the page). **`GraphSnapshotPagination`** + **`GraphSnapshotNodesPage`** in **`ArchiForge.KnowledgeGraph`**.
- [x] 232. Embedding batching cost caps.
  - **`RetrievalEmbeddingCapOptions`** (`Retrieval:EmbeddingCaps`), **`RetrievalIndexingService`** batches **`EmbedManyAsync`** and optional **`MaxChunksPerIndexOperation`**; validation in **`ArchiForgeConfigurationRules`**; tests **`RetrievalIndexingServiceTests`**.
- [x] 233. AI Search SKU guidance (dev vs prod).
  - **`docs/AI_SEARCH_SKU_GUIDANCE.md`**.
- [x] 234. Cold-start profiling + trimming options.
  - **`docs/PERFORMANCE_COLD_START_AND_TRIMMING.md`**.

### API & contracts (235–242)

- [x] 235. Deprecation: `Sunset` + versioned routes policy.
  - **`ApiDeprecationOptions`** + **`ApiDeprecationHeadersMiddleware`**: optional **`Deprecation`**, **`Sunset`**, **`Link`** headers; config **`ApiDeprecation:*`**; validated **`SunsetHttpDate`** when enabled; URL versioning remains **`v{version}`** (see **`Asp.Versioning`** on controllers).
- [x] 236. Standard list pagination (`page`/`pageSize` or cursor) — `PagedResponse<T>`, `PaginationDefaults`, `PagedResponseBuilder` in `ArchiForge.Core.Pagination`; `AlertsController.List` and `ConversationController.ListThreads` accept optional `page`/`pageSize` (backward-compatible with `take`-only).
- [x] 237. ProblemDetails `extensions` machine codes on all 4xx/5xx.
  - **`ProblemErrorCodes`**: stable `UNSPECIFIED`, `CONFLICT`, `RUN_NOT_FOUND`, `DATABASE_TIMEOUT`, `CIRCUIT_BREAKER_OPEN`, etc.; `ResolveFromProblemType` maps `ProblemTypes` URIs.
  - **`ApplicationProblemMapper.CreateProblemResult`**: always sets `extensions.errorCode`; optional post-build `extend` action (e.g. circuit breaker `retryAfterUtc`).
  - **`MapComparisonVerificationFailed`**, **`ProblemDetailsExtensions`** (BadRequest/NotFound/Conflict/503), **`PipelineExtensions`** (500): attach `errorCode`.
  - **Tests**: `ApiProblemDetailsExceptionFilterTests` asserts `errorCode` on conflict, run-not-found, comparison verification, circuit breaker.
- [x] 238. OpenAPI `securitySchemes` for Entra when enabled.
  - **`OpenApiAuthSecurityDocumentFilter`** / **`OpenApiAuthSecurityOperationFilter`**: **`Bearer`** (JWT) when **`ArchiForgeAuth:Mode`** is **`JwtBearer`**; **`ApiKey`** (**`X-Api-Key`**) when **`ApiKey`**; document-level **`security`** + optional **`security: []`** for **`AllowAnonymous`** (explored actions only). Filters read **`IConfiguration` at document generation** so **`WebApplicationFactory`** overrides apply.
  - **`CustomSchemaIds`**: full type name fixes Swashbuckle clash (**`DecisionTrace`** in Decisioning vs Contracts).
  - **Tests**: **`SwaggerOpenApiAuthTests`**, **`SwaggerJsonSecuritySchemesIntegrationTests`**, **`SwaggerDocumentGenerationSmokeTests`**.
- [x] 239. Webhook HMAC for digest/alert channels.
  - **`WebhookDelivery:HmacSha256SharedSecret`**, **`WebhookHmacEnvelopePoster`**, **`HttpWebhookPoster`** (see **`WebhookDeliveryOptions`**).
- [x] 240. Optional `Idempotency-Key` on create run.
  - **`021_ArchitectureRunIdempotency.sql`** + **`ArchitectureRunIdempotency`** in **`ArchiForge.sql`**; **`IArchitectureRunIdempotencyRepository`** / **`ArchitectureRunIdempotencyRepository`**.
  - **`ArchitectureRunService`**: optional **`CreateRunIdempotencyState`**; replay → **`CreateRunResult.IdempotentReplay`**; key + different body → **`ConflictException`** (409).
  - **`RunsController`**: header **`Idempotency-Key`**, **200** + **`Idempotency-Replayed`** on replay; **`ConflictException`** before **`InvalidOperationException`** catch.
  - **`docs/API_CONTRACTS.md`**: contract table + cross-store limitation note.
  - **Tests**: `ArchitectureControllerTests` idempotency cases.
- [x] 241. Bulk endpoint limits + partial success model.
  - **Batch comparison replay:** **`ComparisonReplay:Batch:MaxComparisonRecordIds`**, per-ID try/catch, **`batch-replay-manifest.json`**, **`X-ArchiForge-Batch-Partial`**, **422** when all fail; **`docs/API_CONTRACTS.md`**.
- [x] 242. JSON camelCase audit on public DTOs.
  - **`AddArchiForgeMvc`**: `AddJsonOptions` sets `PropertyNamingPolicy` and `DictionaryKeyPolicy` to **camelCase** for controller JSON.
  - **`docs/JSON_PUBLIC_CONTRACTS.md`**: documents policy and Problem Details extensions.

### Data & persistence (243–249)

- [x] 243. Archival for old runs / digests / conversations.
  - **`ArchivedUtc`** on **`dbo.Runs`**, **`dbo.ArchitectureDigests`**, **`dbo.ConversationThreads`** (migration **`028_ArchivalSoftFlags.sql`**, **`ArchiForge.sql`** CREATE + parity). List/get SQL and in-memory repos exclude archived rows. **`DataArchivalOptions`** (`DataArchival:*`), **`IDataArchivalCoordinator`** / **`DataArchivalCoordinator`**, **`DataArchivalHostedService`** (scoped coordinator + retention days). Default **`DataArchival:Enabled`** false.
- [x] 244. Soft-delete policy for governance assignments.
  - Migration **`029_PolicyPackAssignments_ArchivedUtc.sql`**, **`ArchivedUtc`** on **`PolicyPackAssignment`**, list filter + **`ArchiveAsync`**, **`POST v1/policy-packs/assignments/{id}/archive`**, audit **`PolicyPackAssignmentArchived`**.
- [x] 245. Connection resilience (retry + backoff).
  - **`SqlTransientDetector`** (`ArchiForge.Persistence.Connections`): shared classifier for transient SQL Server error numbers (-2 timeout, 40613 Azure SQL unavailable, 40197 service error, 49918–49920 throttling) and `TimeoutException`. Used by both the health check and the resilient factory.
  - **`ResilientSqlConnectionFactory`** (`ArchiForge.Persistence.Connections`): decorator over `ISqlConnectionFactory` that retries `CreateOpenConnectionAsync` on transient failures with exponential backoff (default 3 retries, 200 ms base, ±25 % jitter). Non-transient exceptions propagate immediately.
  - **DI wiring**: `ArchiForgeStorageServiceCollectionExtensions` now wraps `SqlConnectionFactory` in `ResilientSqlConnectionFactory` for the SQL storage path.
  - **Health check**: `SqlConnectionHealthCheck` delegates to the shared `SqlTransientDetector`.
  - **Tests**: `SqlTransientDetectorTests` (9 cases — all error numbers, null, `TimeoutException`, inner exception), `ResilientSqlConnectionFactoryTests` (8 cases — success, transient retry + success, retry exhaustion, non-transient immediate throw, cancellation, exponential delay range, null guards, zero-retries).
- [x] 246. Read replica routing for heavy authority lists.
  - **`SqlServer:ReadReplica:FailoverGroupReadOnlyListenerConnectionString`** (preferred) and **`AuthorityRunListReadsConnectionString`** (run-list override), **`ReadReplicaRoutedConnectionFactory`** routes **`SqlRunRepository.ListByProjectAsync`**, governance reads (`DapperPolicyPack*`), and **`SqlGoldenManifestRepository.GetByIdAsync`** when configured; Terraform example **`infra/terraform/examples/sql_read_replica_app_settings.tf.example`**.
- [x] 247. Shared test cases InMemory vs Dapper parity.
  - **Contract test pattern**: Abstract base test classes define shared assertions per repository interface; concrete subclasses supply InMemory or Dapper implementations. Both run the exact same test suite.
  - **`AlertRuleRepositoryContractTests`** (7 tests): Create+GetById round-trip, GetById nonexistent → null, Update modifies mutable fields, ListByScope scope filtering, ListByScope ordering (CreatedUtc DESC), ListEnabledByScope excludes disabled, ListEnabledByScope empty when none enabled.
  - **`ArchitectureDigestRepositoryContractTests`** (5 tests): Create+GetById round-trip, GetById nonexistent → null, ListByScope scope filtering, ListByScope ordering (GeneratedUtc DESC), ListByScope respects `take` limit, empty scope → empty list.
  - **InMemory subclasses** (`InMemoryAlertRuleRepositoryContractTests`, `InMemoryArchitectureDigestRepositoryContractTests`): `Category=Unit`, no Docker needed.
  - **Dapper subclasses** (`DapperAlertRuleRepositoryContractTests`, `DapperArchitectureDigestRepositoryContractTests`): `Category=SqlServerContainer`, reuse `SqlServerPersistenceFixture`.
  - All files in `ArchiForge.Persistence.Tests/Contracts/`.
- [x] 248. Single DDL file discipline audit.
  - **`docs/SQL_DDL_DISCIPLINE.md`**: single-file rule for **`ArchiForge.sql`**, DbUp migration pairing, inventory **019–021**.
  - **`ArchiForge.sql`** trailing section aligned with **019** (**`RetrievalIndexingOutbox`**), **020** (Runs index), **021** (idempotency table).
- [x] 249. Migration rollback documentation.
  - **`docs/runbooks/MIGRATION_ROLLBACK.md`**: restore-first, forward-fix vs manual DDL, **028** column-drop note, journal alignment warning.

### UI & developer experience (250–256)

- [x] 250. UI feature flags for experimental advisory panels.
  - **`archiforge-ui/src/lib/feature-flags.ts`** (`NEXT_PUBLIC_EXPERIMENTAL_ADVISORY_PANELS`), optional section on **`advisory`** page.
- [x] 251. Operator UI: Problem Details + **X-Correlation-ID** + Vitest coverage.
  - **ProblemDetails in the shell:** `archiforge-ui/src/lib/api-problem.ts` (`tryParseApiProblemDetails`), `api-problem-copy.ts` (`operatorCopyForProblem` maps `extensions.errorCode` + prefers `supportHint`), `api-request-error.ts` / `api-error.ts` (`buildApiRequestErrorFromParts`, `readApiFailureMessage`), `api-load-failure.ts` (`toApiLoadFailure`, `uiFailureFromMessage`).
  - **Correlation:** `archiforge-ui/src/lib/correlation.ts` (`CORRELATION_ID_HEADER`, `generateCorrelationId`, `isSafeCorrelationId`); browser `api.ts` sends the header; `archiforge-ui/src/app/api/proxy/[...path]/route.ts` forwards safe inbound ids or generates, and `passThrough` echoes upstream **X-Correlation-ID** on responses.
  - **Rendering:** `OperatorApiProblem` on server pages (runs, run detail, manifests, compare) and client operator pages (alerts, advisory, ask, graph, replay, policy packs, etc.).
  - **Tests (Vitest):** `src/lib/api-problem.test.ts`, `api-problem-copy.test.ts`, `correlation.test.ts`, `api-request-error.test.ts`, `api-load-failure.test.ts`, extended `api-error.test.ts`, `src/components/OperatorApiProblem.test.tsx`, `src/app/api/proxy/proxy-route-correlation.test.ts` (plus existing `proxy-route-post-body.test.ts`).
- [x] 252. Dev container (SQL + Azurite + fakes).
  - **`.devcontainer/devcontainer.json`** (.NET 10 + Node 22); **`docs/DEVCONTAINER.md`** + host **`docker compose up -d`**.
- [x] 253. `dotnet new` template for finding engine + tests.
  - **`templates/archiforge-finding-engine`** (`dotnet new install ./templates/archiforge-finding-engine`, shortName **`archiforge-finding-engine`**); see **`docs/BUILD.md`**.
- [x] 254. Contributor onboarding checklist (build, test filters, integration opt-in).
- [x] 255. **Dockerfiles for API + UI** (multi-stage, Alpine, non-root, `HEALTHCHECK`).
  - **`ArchiForge.Api/Dockerfile`**: three-stage (`restore` → `publish` → `runtime`); `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`; `HEALTHCHECK` on `/health/live`; port 8080.
  - **`archiforge-ui/Dockerfile`**: three-stage (`deps` → `build` → `runtime`); `node:22-alpine`; Next.js standalone output (`output: "standalone"` in `next.config.ts`); `HEALTHCHECK` on `/`; port 3000.
  - **`.dockerignore`** (repo root) + **`archiforge-ui/.dockerignore`**: exclude build artifacts, secrets, test data.
  - **`docker-compose.yml`**: `--profile full-stack` adds `api` + `ui` containers alongside SQL/Azurite/Redis; default profile unchanged (dependencies only, hot-reload dev).
  - **`docs/CONTAINERIZATION.md`**: build/run commands, security posture, WAF alignment, layer caching, future Azure notes.
- [x] 256. **Operator UI route boundaries (404 + loading).**
  - **`archiforge-ui/src/app/not-found.tsx`**: custom 404 with **`OperatorEmptyState`**, links to Home / Runs / Compare (WAF RE:05 / SE:01).
  - **Route `loading.tsx`**: advisory, advisory-scheduling, alert-routing, alert-rules, alert-simulation, alert-tuning, alerts, ask, composite-alert-rules, digest-subscriptions, digests, governance-resolution, policy-packs, recommendation-learning, search (context-specific copy; **`OperatorLoadingNotice`**).
  - **`not-found.test.tsx`**: Vitest render gate for 404 copy and links.

### Archival, replay diagnostics, contracts & IaC (257–274)

- [x] 257. **Data archival hosted iteration + health state** — `DataArchivalHostIteration` outcome wiring in `DataArchivalHostedService`; `DataArchivalHostHealthState` records last success/failure.
- [x] 258. **`DataArchivalHostHealthCheck`** — readiness tag **`data_archival`**; **Healthy** when disabled, enabled with no attempt yet, or last success; **Degraded** when enabled and last iteration failed (`failureStatus: Degraded` in `RegisterArchiForgeHealthChecks`).
- [x] 259. **`ReplayDiagnosticsOptions`** — config section, recorder retention for replay diagnostics endpoint.
- [x] 260. **`InMemoryComparisonRecordRepository`** — thread-safe in-memory implementation in **ArchiForge.Data**; parity with Dapper via contract tests.
- [x] 261. **Comparison record contract tests** — abstract base + InMemory + Dapper (`ComparisonRecordRepositoryContractTests`).
- [x] 262. **`TestSqlDbConnectionFactory`** (and SQL integration helpers) for persistence tests against real SQL.
- [x] 263. **RLS / session context** — integration coverage for tenant-scoped SQL access where applicable.
- [x] 264. **Terraform App Service** — private stack / VNet-related alignment (see `infra/terraform-private`).
- [x] 265. **Stryker manifest / workflow** + **`docs/OPENAPI_CONTRACT_DRIFT.md`** (or equivalent contract drift doc) as completed in the same delivery batch.
- [x] 266. **In-memory DI for comparison records** — when **`ArchiForge:StorageProvider=InMemory`**, register **`InMemoryComparisonRecordRepository`** for **`IComparisonRecordRepository`** in **`RegisterComparisonReplayAndDrift`** (singleton; avoids SQL **`ComparisonRecordRepository`** on in-memory hosts).
- [x] 267. **`SessionContextSqlConnectionFactoryTests`** — when **`IRlsSessionContextApplicator.ApplyAsync`** throws, the opened **`SqlConnection`** is disposed and the exception propagates.
- [x] 268. **`DataArchivalHostHealthCheckTests`** — disabled → Healthy; enabled + no attempt → Healthy; enabled + last failure → Degraded; enabled + last success → Healthy.
- [x] 269. **Runbook** — **`docs/runbooks/DATA_ARCHIVAL_HEALTH.md`**: degraded meaning, triage, recovery.
- [x] 270. **CI** — **`.github/workflows/ci.yml`**: job **`terraform-validate-private`** runs **`terraform init -backend=false`** and **`terraform validate`** under **`infra/terraform-private`**.
- [x] 271. **`ComparisonReplayCostEstimator` tests** — **`persistReplay`** score/factor; large payload factor (**`PayloadJson`** > 500k chars).
- [x] 272. **`RunRepositoryContractTests`** — abstract base + **`InMemoryRunRepositoryContractTests`** + **`DapperRunRepositoryContractTests`** (archive case only on in-memory: global SQL `ArchiveRunsCreatedBeforeAsync`).
- [x] 273. **This file** — §257–274 recorded so backlog matches delivery.
- [x] 274. **`docs/ARCHITECTURE_COMPONENTS.md`** — archival readiness, replay diagnostics, cost estimator, in-memory comparison repository.

### Data access clarity & test depth (275–283)

- [x] 275. **Dual `IGoldenManifestRepository` / `IDecisionTraceRepository` contracts** — `ArchiForge.Data.Repositories` (run/commit Dapper: `CreateAsync`, `GetByVersionAsync`, batch traces) vs `ArchiForge.Decisioning.Interfaces` (authority SQL: `SaveAsync`, scoped `GetByIdAsync`). Coordinator registrations use **fully qualified Data interface types** so DI is not mistaken for a duplicate override of the Decisioning contracts registered in **`AddArchiForgeStorage`**.
- [x] 276. **`SqlScopedResolutionDbConnectionFactory`** — singleton **`IDbConnectionFactory`** for SQL storage that opens connections via a **short scope** resolving scoped **`ISqlConnectionFactory`** (resilience + optional RLS) for **`CreateOpenConnectionAsync`**, while **`CreateConnection`** stays an unopened **`SqlConnection`** for readiness probes. **`RegisterDataInfrastructure`** registers **`Data.SqlConnectionFactory`** only when **`StorageProvider=InMemory`**; SQL mode registers this bridge inside **`AddArchiForgeStorage`**.
- [x] 277. **`CorrelationIdMiddlewareTests`** + **`ApiDeprecationHeadersMiddlewareTests`** (Core suite).
- [x] 278. **`RetrievalIndexingOutboxHostedServiceTests`** — clean shutdown and continue-after-failure loop behavior.
- [x] 279. **CI coverage artifacts** — **`dotnet test`** with **`--collect:"XPlat Code Coverage"`** on fast-core and full-regression jobs; upload Cobertura XML artifacts.
- [x] 280. **`terraform fmt -check`** in **`terraform-validate-private`**.
- [x] 281. **Conversation repository contract tests** — thread + message abstract bases with InMemory and Dapper subclasses (archive case skipped on shared SQL for threads).
- [x] 282. **`docs/runbooks/SECRET_AND_CERT_ROTATION.md`** — secrets, SQL, JWT, webhooks, TLS.
- [x] 283. **This file + `docs/ARCHITECTURE_COMPONENTS.md`** — §275–283 recorded; connection bridge and dual-repository note.

### Audit, provenance, rate limits & ops polish (284–292)

- [x] 284. **`AuditRepositoryContractTests`** — abstract base + **`InMemoryAuditRepositoryContractTests`** + **`DapperAuditRepositoryContractTests`** (`AppendAsync`, scoped **`GetByScopeAsync`**, ordering).
- [x] 285. **`ProvenanceSnapshotRepositoryContractTests`** — abstract base + InMemory + Dapper (`SaveAsync`, **`GetByRunIdAsync`**, scope isolation, latest-wins read semantics).
- [x] 286. **Controller rate limiting** — **`[EnableRateLimiting("fixed")]`** on **`JobsController`**, **`ScopeDebugController`**; **`[EnableRateLimiting("expensive")]`** on **`DemoController`**; XML remarks on **`AuthDebugController`** / **`DocsController`** explaining intentional omission.
- [x] 287. **`DataArchivalHostedServiceTests`** — cancellation during delay; coordinator failure records **`DataArchivalHostHealthState`** attempt.
- [x] 288. **`docker-compose.yml`** — Azurite **`healthcheck`** (Node HTTP probe); **`api`** **`depends_on`** Azurite **`service_healthy`** (full-stack profile).
- [x] 289. **`ArchiForgeConfigurationRules.CollectRateLimitingErrors`** — **`PermitLimit` ≥ 1, `WindowMinutes` ≥ 1, `QueueLimit` ≥ 0** for Fixed/Expensive/Replay light & heavy when sections exist; **`ArchiForgeConfigurationRulesTests`** cases.
- [x] 290. **`docs/CONSULTING_DOCX_TEMPLATE.md`** — **`ConsultingDocxTemplate`** / **`ConsultingDocxTemplateProfiles`** reference.
- [x] 291. **CI** — **`dotnet test`** for **`templates/archiforge-finding-engine/...Tests.csproj`** in **`dotnet-fast-core`** (project stays outside main solution).
- [x] 292. **This file + `docs/ARCHITECTURE_COMPONENTS.md`** — §284–292 recorded.

### Synthesis tests, CI Terraform breadth, production safety & ops docs (293–302)

- [x] 293. **`ArchiForge.ArtifactSynthesis.Tests`** — `ArtifactSynthesisService` ordering + empty generators, `MermaidDiagramArtifactGenerator`, `ArtifactPackagingService` (single-file + ZIP); **`Suite=Core`** on test classes.
- [x] 294. **`RealAgentExecutor` + `AgentResultParser` tests** in **`ArchiForge.AgentRuntime.Tests`** (duplicate handler type, dispatch order by enum, missing handler, JSON validation paths).
- [x] 295. **`ArchiForge.Application.Tests`** — **`ArchitectureRunIdempotencyHashing`** (hash stability, null guard, fingerprint equality / inequality).
- [x] 296. **Persistence contract tests** — **`GoldenManifestRepositoryContractTests`** + InMemory + SQL; **`DecisionTraceRepositoryContractTests`** + InMemory + SQL (run FK seed); **`PolicyPackRepositoryContractTests`** + InMemory + Dapper; shared **`AuthorityRunChainTestSeed`** extracted from **`SqlGoldenManifestRepositorySqlIntegrationTests`**.
- [x] 297. **CI** — **`.github/workflows/ci.yml`**: job **`terraform-validate-public-stacks`** matrix **`infra/terraform`**, **`infra/terraform-edge`**, **`infra/terraform-entra`** (`init -backend=false`, `validate`, `fmt -check`).
- [x] 298. **`docker-compose.yml` (full-stack)** — explicit **`healthcheck`** for **`api`** and **`ui`**; **`ui`** **`depends_on`** **`api`** **`condition: service_healthy`**.
- [x] 299. **`CollectProductionSafetyErrors`** — Production CORS allow-list + webhook HMAC when HTTP delivery enabled; **`ArchiForgeConfigurationRulesTests`** (existing Production cases updated with valid CORS/webhook stubs + new failure cases).
- [x] 300. **`docs/ARCHITECTURE_CONTAINERS.md`** — Decisioning, Persistence, KnowledgeGraph, ContextIngestion, Retrieval, ArtifactSynthesis; updated API/Application dependency bullets.
- [x] 301. **Runbooks** — **`docs/runbooks/README.md`**, **`INFRASTRUCTURE_OPS.md`**, **`REDIS_HEALTH.md`**.
- [x] 302. **This file + `docs/ARCHITECTURE_COMPONENTS.md`** — §293–302 recorded; component doc: contract coverage + production safety note.

### Persistence contracts, app idempotency, config, synthesis, CI gate (303–310)

- [x] 303. **Persistence contract tests** — **`AgentResultRepositoryContractTests`** + InMemory + Dapper (FK seed via **`ArchitectureCommitTestSeed`**); **`AdvisoryScanScheduleRepositoryContractTests`** + InMemory + **`DapperAdvisoryScanScheduleRepository`**; **`AlertDeliveryAttemptRepositoryContractTests`** + InMemory + **`DapperAlertDeliveryAttemptRepository`**.
- [x] 304. **`ArchiForge.Application.Tests`** — **`ArchitectureRunServiceCreateRunIdempotencyTests`**: idempotent replay skips **`ICoordinatorService`**; fingerprint mismatch → **`ConflictException`**.
- [x] 305. **`ArchiForgeConfigurationRules`** — **`Retrieval:VectorIndex`** must be **`InMemory`**, **`AzureSearch`**, or omitted; Production webhook HMAC secret minimum length when HTTP delivery is enabled; **`RateLimiting:Replay:Light` / `Heavy`** honor configured **`QueueLimit`**; matching **`ArchiForgeConfigurationRulesTests`**.
- [x] 306. **`ArchiForge.ArtifactSynthesis.Tests`** — **`InventoryArtifactGeneratorTests`**, **`ArtifactBundleValidatorTests`** (duplicate type + empty content).
- [x] 307. **Docs** — **`docs/BUILD.md`** (Application.Tests, Terraform gate on fast-core); **`docs/CONTRIBUTOR_ONBOARDING.md`** (**`Suite=Core`** filter).
- [x] 308. **CI** — **`dotnet-fast-core`** **`needs:`** **`terraform-validate-private`** and **`terraform-validate-public-stacks`** (after **`gitleaks`**).
- [x] 309. **`docs/ARCHITECTURE_COMPONENTS.md`** — contract list extended (idempotency, agent results, advisory schedules, alert attempts, **`ArchitectureCommitTestSeed`**).
- [x] 310. **This file** — §303–310 recorded.

### Application execute/commit, DecisionEngineV2 depth, Data contracts, config & runbooks (311–318)

- [x] 311. **`ArchiForge.Application.Tests`** — **`ArchitectureRunServiceExecuteCommitTests`**: execute happy path + idempotent replay + **`RunNotFoundException`** + terminal **`ConflictException`**; commit happy path + wrong-status **`ConflictException`** + run missing.
- [x] 312. **`DecisionEngineV2Tests`** — empty topology pair; three topics when topology present; security promotion from **strengthen** + “private”; complexity prefers reduce under **caution**.
- [x] 313. **In-memory Data repositories** — **`InMemoryArchitectureRequestRepository`**, **`InMemoryArchitectureRunRepository`** (optional request lookup for **`ListAsync`**), **`InMemoryEvidenceBundleRepository`**, **`InMemoryAgentEvidencePackageRepository`**, **`InMemoryAgentExecutionTraceRepository`**.
- [x] 314. **Persistence contract tests** — request, evidence bundle, architecture run (incl. **`ListAsync`** system name), agent evidence package, agent execution trace (incl. paging); Dapper subclasses + **`ArchitectureCommitTestSeed.InsertArchitectureRequestOnlyAsync`**.
- [x] 315. **`ArchiForgeConfigurationRulesTests`** — ApiKey enabled without keys; embedding caps; **`DataArchival:RunsRetentionDays`**; batch replay **> 500**; empty schema path; Production ApiKey both keys missing.
- [x] 316. **Runbooks** — **`docs/runbooks/AGENT_EXECUTION_FAILURES.md`**, **`ALERT_DELIVERY_FAILURES.md`**; **`docs/runbooks/README.md`** index rows.
- [x] 317. **`docs/ARCHITECTURE_COMPONENTS.md`** — contract list + new in-memory Data repositories note.
- [x] 318. **This file** — §311–318 recorded.

### InMemory coordinator DI, Data contracts, orchestrator & application tests, API test map (319–328)

- [x] 319. **`RegisterCoordinatorDecisionEngineAndRepositories`** — when **`StorageProvider=InMemory`**, register singleton in-memory Data repositories for the full coordinator chain (request/run/idempotency, agent task/result, evaluations, decision nodes, coordinator manifest + decision traces, evidence bundle, agent evidence package, execution traces) so **`ICoordinatorService`** does not depend on SQL for those types.
- [x] 320. **`RegisterRunExportAndArchitectureAnalysis`** — **`IRunExportRecordRepository`** → **`InMemoryRunExportRecordRepository`** when **`StorageProvider=InMemory`**.
- [x] 321. **New in-memory Data repos** — **`InMemoryAgentEvaluationRepository`**, **`InMemoryDecisionNodeRepository`**, **`InMemoryRunExportRecordRepository`**, **`InMemoryCoordinatorGoldenManifestRepository`**, **`InMemoryCoordinatorDecisionTraceRepository`** (names avoid confusion with Decisioning in-memory authority stores).
- [x] 322. **Persistence contract tests** — abstract bases + InMemory + Dapper/SQL: **`CoordinatorGoldenManifestRepositoryContractTests`**, **`CoordinatorDecisionTraceRepositoryContractTests`**, **`AgentEvaluationRepositoryContractTests`**, **`DecisionNodeRepositoryContractTests`**, **`RunExportRecordRepositoryContractTests`** (FK seeds via **`ArchitectureCommitTestSeed`** where required).
- [x] 323. **`AuthorityRunOrchestratorTests`** (**`ArchiForge.Persistence.Tests`**) — happy path (**`CommitAsync`**, retrieval outbox enqueue) and failure path (**`RollbackAsync`**, no commit) with mocked Decisioning/Persistence ports.
- [x] 324. **`ReplayRunServiceTests`** + **`DeterminismCheckServiceTests`** (**`ArchiForge.Application.Tests`**) — not-found / dry-run vs commit / merge success; iteration validation and drift aggregation (Moq **`ReplayAsync`** sequencing).
- [x] 325. **`docs/TEST_STRUCTURE.md`** — **`ArchiForge.Application.Tests`** row; Persistence.Tests row extended; **API controller ↔ tests** mapping subsection (where to find coverage when the test class name does not mirror the controller file).
- [x] 326. **`docs/ARCHITECTURE_COMPONENTS.md`** — InMemory coordinator + run-export registration note.
- [x] 327. **This file** — §319–328 recorded.
- [x] 328. **Follow-up pointer** — per-controller **`*ControllerTests`** are optional where **`docs/TEST_STRUCTURE.md`** mapping shows integration coverage under a different class name. (Governance InMemory + contracts: see **§329–330**.)

### Governance InMemory, contracts, Application tests, ADRs & ops docs (329–338)

- [x] 329. **`RegisterGovernance`** — when **`ArchiForge:StorageProvider=InMemory`**, register singleton **`InMemoryGovernanceApprovalRequestRepository`**, **`InMemoryGovernancePromotionRecordRepository`**, **`InMemoryGovernanceEnvironmentActivationRepository`**; otherwise scoped Dapper implementations.
- [x] 330. **Persistence contract tests** — abstract bases + InMemory + Dapper for the three governance repository interfaces (ordering and activation **`UpdateAsync`** semantics aligned with SQL).
- [x] 331. **`ArchiForge.Application.Tests`** — **`RunDetailQueryServiceApplicationTests`** (`HasBrokenManifestReference`, trace load when manifest present).
- [x] 332. **`ArchiForge.Application.Tests`** — **`AgentResultDiffServiceApplicationTests`** and **`ManifestDiffServiceApplicationTests`** (extra scenarios vs Api.Tests).
- [x] 333. **`ArchiForge.Application.Tests`** — **`DefaultAgentEvaluationServiceTests`** (empty evaluations, validation, cancellation).
- [x] 334. **ADR 0004** — dual manifest/trace repository contracts (Data vs Decisioning).
- [x] 335. **ADR 0005** — InMemory vs Sql storage provider (incl. governance wiring).
- [x] 336. **`docs/DEPLOYMENT.md`** — umbrella deployment/rollback with links to migration and failover runbooks.
- [x] 337. **`docs/runbooks/DATABASE_FAILOVER.md`** — Azure SQL HA/geo-failover, listeners, RPO/RTO framing.
- [x] 338. **This file + `docs/ARCHITECTURE_COMPONENTS.md`** — §329–338 recorded; governance InMemory registration note.

---

## Checklist (items 155–256 progress)

Use the per-item `[x]` / `[ ]` markers in the sections above; this summary rolls up major themes only.

- [x] Documentation & ADRs (155–169): complete (155 XML doc pieces 12–21 done; 156–169 largely addressed via `docs/adr`, runbooks, `API_CONTRACTS`, `ALERTS`, `BUILD`, `TEST_STRUCTURE`, `CONTRIBUTOR_ONBOARDING`, `terraform-azure-variables`, `CONTEXT_INGESTION` SMB note).
- [x] Unit tests (170–194): complete for 170–190, 191–194 (170–171 Persistence.Tests; 183–185, 190 as listed above; 189 UTC calculator documented).
- [x] Integration / E2E (195–204): complete for backlog scope (195–204; **201** k6 doc/script, **203** N−1 migrator test, **204** policy-packs Playwright).
- [x] Observability & reliability (205–214): complete (205–214; **214** SLO runbook).
- [x] Security (215–226): complete for backlog scope (**215–226** including **222–224** design docs; remaining continuous hardening is outside this checklist).
- [x] Performance & cost (227–234): complete for backlog scope (**227–234** including **232–234** embedding caps, AI Search SKU doc, cold-start/trim doc).
- [x] API & contracts (235–242): complete (235 deprecation headers; 236–242 as listed, including **239** webhook HMAC and **241** batch replay partial success).
- [x] Data & persistence (243–249): complete for backlog scope (**243–249** including **244** assignment archival, **246** read-replica list routing).
- [x] UI & DX (250–256): complete for backlog scope (**250** feature flags; **252–253** devcontainer + template; **251**, **254–256** as listed).
- [x] Archival, replay diagnostics, contracts & IaC (257–274): complete (**257–265** prior batch; **266–274** in-repo: in-memory comparison DI, session-context dispose test, archival health unit tests, **`DATA_ARCHIVAL_HEALTH`** runbook, Terraform validate job, cost-estimator tests, run repository contracts, component doc updates).
- [x] Data access clarity & test depth (275–283): complete (**275–276** dual manifest/trace contracts + **`SqlScopedResolutionDbConnectionFactory`**; **277–278** middleware + outbox host tests; **279–280** CI coverage + **`terraform fmt`**, **281** conversation contracts, **282** secret/cert runbook, **283** docs).
- [x] Audit, provenance, rate limits & ops polish (284–292): complete (**284–285** audit + provenance contract tests; **286** controller rate limits + debug/docs remarks; **287** archival host tests; **288** compose Azurite health; **289** rate-limit validation; **290** consulting DOCX doc; **291** template CI test; **292** docs).
- [x] Synthesis tests, CI Terraform breadth, production safety & ops docs (293–302): complete (**293–295** ArtifactSynthesis + AgentRuntime + Application test projects; **296** authority manifest/trace/policy pack contracts + **`AuthorityRunChainTestSeed`**; **297–298** Terraform matrix job + compose API/UI health; **299** production CORS/webhook validation; **300–302** containers doc, runbooks index + infra/redis ops, backlog + components).
- [x] Persistence contracts, app idempotency, config, synthesis, CI gate (303–310): complete (**303** agent result + advisory + alert delivery contracts; **304** **`ArchitectureRunService`** idempotency unit tests; **305** VectorIndex + webhook secret length + replay rate-limit **`QueueLimit`**; **306** inventory generator + bundle validator negatives; **307–308** BUILD/CONTRIBUTOR + CI **`needs`** Terraform; **309–310** components doc + backlog).
- [x] Application execute/commit, DecisionEngineV2 depth, Data contracts, config & runbooks (311–318): complete (**311–312** Application.Tests execute/commit + **`DecisionEngineV2`** edges; **313–314** five in-memory Data repos + matching persistence contracts + request-only SQL seed; **315** configuration rule tests; **316** agent/alert delivery runbooks; **317–318** components doc + backlog).
- [x] InMemory coordinator DI, Data contracts, orchestrator & application tests, API test map (319–328): complete (**319–320** InMemory DI for coordinator + run export; **321–322** new repos + persistence contracts; **323** **`AuthorityRunOrchestratorTests`**; **324** **`ReplayRunService`** / **`DeterminismCheckService`** unit tests; **325–328** **`TEST_STRUCTURE`** mapping + components doc + backlog note).
- [x] Governance InMemory, contracts, Application tests, ADRs & ops docs (329–338): complete (**329–330** governance InMemory repos + persistence contracts; **331–333** Application.Tests for run detail, diffs, default evaluation; **334–335** ADRs 0004–0005; **336–337** **`DEPLOYMENT.md`** + **`DATABASE_FAILOVER.md`**; **338** backlog + **`ARCHITECTURE_COMPONENTS`**).

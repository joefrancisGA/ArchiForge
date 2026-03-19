# Test structure

## Projects

- **ArchiForge.Api.Tests** — API integration tests using `WebApplicationFactory` (full app, in-memory SQLite). Heavier; use for HTTP contracts, comparison replay, exports, run-not-found, 422/409.
- **ArchiForge.DecisionEngine.Tests** — Unit and scenario tests for the decision engine; optional integration tests with real JSON schemas (see `SchemaValidationIntegrationTests`).
- **ArchiForge.Coordinator.Tests**, **ArchiForge.AgentRuntime.Tests**, **ArchiForge.Decisioning.Tests**, etc. — Domain/component tests; no web stack unless explicitly added.

## Categories (optional filtering)

Tests that require the full API or real I/O are tagged with:

```csharp
[Trait("Category", "Integration")]
```

Examples: `ComparisonReplayVerifyDriftIntegrationTests`, `SchemaValidationIntegrationTests` (real-schema tests).

To run only fast/unit tests (exclude integration):

```bash
dotnet test --filter "Category!=Integration"
```

To run only integration tests:

```bash
dotnet test --filter "Category=Integration"
```

## Fixtures and shared setup

- **IntegrationTestBase** (Api.Tests) — provides `HttpClient` and `JsonOptions` from `ArchiForgeApiFactory`.
- **ComparisonReplayTestFixture** (Api.Tests) — static helpers: `CreateRunExecuteCommitReplayAsync`, `PersistEndToEndComparisonAsync` for comparison-replay flows.

No separate “unit-only” test project exists; use `[Trait("Category", "Integration")]` and filter as above if you want to separate runs.

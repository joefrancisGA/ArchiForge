# Step-by-Step Implementation Plan: CLI–API Architecture

Repository-specific plan for **ArchLucid**: CLI calling the ArchLucid API. Includes order of work, files to create or modify, and tests to add.

---

## Current State

| Component | Status | Location |
|-----------|--------|----------|
| Config + URL resolution | Done | `ArchLucid.Cli/ArchLucidProjectScaffolder.cs`, `ArchLucid.Cli/Program.cs` |
| HTTP client (ArchLucidApiClient) | Done | `ArchLucid.Cli/ArchLucidApiClient.cs` |
| CLI commands (run, status, submit, commit, seed, artifacts, health, dev up, new) | Done | `ArchLucid.Cli/Program.cs` |
| CLI test project | Done | `ArchLucid.Cli.Tests/ArchLucid.Cli.Tests.csproj` |
| Config / URL resolution tests | Done | `ArchLucid.Cli.Tests/ArchLucidCliConfigTests.cs` (`ArchLucidConfigTests`), `ArchLucid.Cli.Tests/ArchLucidApiClientTests.cs` |
| API client unit tests (mocked HTTP) | Done | `ArchLucid.Cli.Tests/ArchLucidApiClientHttpTests.cs` |
| Command-line / exit-code tests | Done | `ArchLucid.Cli.Tests/CommandLineTests.cs` |
| Smoke tests | Done | `ArchLucid.Cli.Tests/CliSmokeTests.cs` |
| CLI USAGE docs | Done | `docs/CLI_USAGE.md` |
| README CLI section | Done | `README.md` |
| Scaffold SQL path hardened | Done | `RegisterProject = false` by default; connection string moved to `ScaffoldOptions.ConnectionString` |

---

## Order of Work

Do the phases in this order. Later phases depend on earlier ones.

| Phase | Description |
|-------|-------------|
| **1** | Create CLI test project and add to solution |
| **2** | Config and URL resolution tests |
| **3** | API client unit tests (optional: inject HttpClient) |
| **4** | Command-line / exit-code tests |
| **5** | Documentation and optional cleanup |

---

## Phase 1: Create CLI Test Project

**Goal:** New test project that references the CLI and can run tests without the API.

### Files to create

| File | Purpose |
|------|---------|
| `ArchLucid.Cli.Tests/ArchLucid.Cli.Tests.csproj` | Test project: `net10.0`, xUnit, FluentAssertions, reference `ArchLucid.Cli`. |

**csproj contents (minimal):**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ArchLucid.Cli\ArchLucid.Cli.csproj" />
  </ItemGroup>
</Project>
```

### Files to modify

| File | Change |
|------|--------|
| `ArchLucid.sln` | Add project `ArchLucid.Cli.Tests` and place it under the `tests` solution folder (`{128DBBE7-315A-4005-BED1-BA51A521D8FB}`). |

### Tests to add

- None in this phase (project only).

### Validation

- `dotnet build ArchLucid.Cli.Tests` succeeds.
- `dotnet test ArchLucid.Cli.Tests` runs (no tests yet).

---

## Phase 2: Config and URL Resolution Tests

**Goal:** Unit tests for `LoadConfig` and `ResolveBaseUrl`.

### Files to create

| File | Purpose |
|------|---------|
| `ArchLucid.Cli.Tests/ArchLucidCliConfigTests.cs` | Tests for `ArchLucidProjectScaffolder.LoadConfig` and validation (inline JSON in temp dirs; no checked-in fixtures). |
| `ArchLucid.Cli.Tests/ArchLucidApiClientTests.cs` | Tests for `ArchLucidApiClient.ResolveBaseUrl` (and optionally `GetDefaultBaseUrl` with env). |

### Files to modify

- None (tests only use public API and temp directories).

### Tests to add

**In `ArchLucid.Cli.Tests/ArchLucidCliConfigTests.cs` (`ArchLucidConfigTests`):**

| Test name | Behavior |
|-----------|----------|
| `LoadConfig_ValidJsonAndFilesExist_ReturnsConfig` | Create temp dir with valid `archlucid.json`, `inputs/brief.md`, `plugins/plugin-lock.json`; call `LoadConfig(tempDir)`; assert config.ProjectName and required fields. |
| `LoadConfig_MissingManifestFile_ThrowsFileNotFoundException` | Call `LoadConfig(pathToEmptyDir)` where no `archlucid.json` exists; assert throws `FileNotFoundException`. |
| `LoadConfig_InvalidJson_ThrowsInvalidDataException` | Write invalid JSON to `archlucid.json` in temp dir; assert throws `InvalidDataException` (or `JsonException`). |
| `LoadConfig_MissingBriefFile_Throws` | Valid JSON but `inputs/brief.md` missing under project root; assert throws (per `ValidateConfigOrThrow`). |

**In `ArchLucid.Cli.Tests/ArchLucidApiClientTests.cs`:**

| Test name | Behavior |
|-----------|----------|
| `ResolveBaseUrl_WhenConfigHasApiUrl_ReturnsConfigUrl` | Create config with `ApiUrl = "https://custom:9090"`; assert `ResolveBaseUrl(config)` equals `"https://custom:9090"`. |
| `ResolveBaseUrl_WhenConfigNull_ReturnsDefaultOrEnv` | Call `ResolveBaseUrl(null)`; assert result is default (e.g. `"http://localhost:5128"`) or set `ARCHLUCID_API_URL` and assert then clear in finally. |
| `ResolveBaseUrl_WhenConfigHasApiUrlWithTrailingSlash_TrimsSlash` | Config `ApiUrl = "http://localhost:5128/"`; assert result is `"http://localhost:5128"`. |

### Validation

- `dotnet test ArchLucid.Cli.Tests --filter "FullyQualifiedName~ArchLucidConfigTests|FullyQualifiedName~ArchLucidApiClientTests"` passes.

---

## Phase 3: API Client Unit Tests (Mocked HTTP)

**Goal:** Test `ArchLucidApiClient` against fixed HTTP responses without a real API.

### Option A: Inject HttpClient (recommended)

**Files to modify**

| File | Change |
|------|--------|
| `ArchLucid.Cli/ArchLucidApiClient.cs` | Add a second constructor: `public ArchLucidApiClient(HttpClient httpClient)` that uses the provided client and builds the pipeline the same way (or a no-retry pipeline for tests). Keep existing `ArchLucidApiClient(string baseUrl)` creating an internal `HttpClient`. |

**Files to create**

| File | Purpose |
|------|---------|
| `ArchLucid.Cli.Tests/ArchLucidApiClientHttpTests.cs` | Instantiate client with a custom `HttpMessageHandler` that returns canned responses; assert success/failure and parsed DTOs. |

**Tests to add (in `ArchLucidApiClientHttpTests.cs`)**

| Test name | Behavior |
|-----------|----------|
| `CreateRunAsync_On201_ReturnsSuccessAndRunId` | Handler returns 201 + JSON with `run.runId`; assert `CreateRunAsync` returns `Success` and `Response.Run.RunId` set. |
| `CreateRunAsync_On400_ReturnsFailureWithParsedError` | Handler returns 400 + JSON `{ "detail": "Validation failed" }`; assert `Success == false` and `Error` contains message. |
| `GetRunAsync_On200_ReturnsGetRunResult` | Handler returns 200 + JSON `{ "run": { "runId": "x" }, "tasks": [], "results": [] }`; assert result not null and `Run.RunId == "x"`. |
| `GetRunAsync_On404_ReturnsNull` | Handler returns 404; assert `GetRunAsync` returns null. |
| `CommitRunAsync_On200_ReturnsSuccessAndManifestVersion` | Handler returns 200 + JSON with `manifest.metadata.manifestVersion`; assert success and version in response. |
| `CheckHealthAsync_On200_ReturnsTrue` | Handler returns 200; assert `CheckHealthAsync` returns true. |
| `CheckHealthAsync_On503_ReturnsFalse` | Handler returns 503; assert `CheckHealthAsync` returns false. |

Use a helper that creates `HttpClient` with `new HttpClient(new MockHandler(...))` and pass to `ArchLucidApiClient(httpClient)`.

### Option B: No production code change

Use a minimal in-process test server (e.g. `WebApplicationFactory` from `ArchLucid.Api.Tests` or a simple `HttpListener`) in the test project that responds with fixed JSON. No change to `ArchLucidApiClient.cs`; tests call `new ArchLucidApiClient(serverBaseUrl)`.

### Validation

- `dotnet test ArchLucid.Cli.Tests --filter "FullyQualifiedName~ArchLucidApiClientHttpTests"` passes.

---

## Phase 4: Command-Line / Exit-Code Tests

**Goal:** Assert CLI exit codes and minimal output for important commands.

### Files to create

| File | Purpose |
|------|---------|
| `ArchLucid.Cli.Tests/CommandLineTests.cs` | Run the CLI via `Program.Main(args)` (if accessible) or by invoking the entry point; assert exit code and that output contains expected strings. |

### Files to modify

| File | Change |
|------|--------|
| `ArchLucid.Cli/Program.cs` | Optional: make the async entry logic callable from tests. If `Main` is `private static async Task<int> Main(string[] args)`, you can use `[InternalsVisibleTo("ArchLucid.Cli.Tests")]` and a public `static Task<int> RunAsync(string[] args)` that contains the current main logic, and have `Main` call `RunAsync(args)`. Then tests call `RunAsync` and assert return value. Alternatively, run the CLI as a separate process and parse stdout/stderr. |

### Tests to add

| Test name | Behavior |
|-----------|----------|
| `NoArgs_Returns1_AndPrintsUsage` | Call with `args = []`; assert exit code 1 and console output contains "Please provide a command" or "Available commands". |
| `UnknownCommand_Returns1_AndPrintsUnknown` | Call with `args = ["invalid"]`; assert exit code 1 and output contains "Unknown command". |
| `Health_WhenApiUnreachable_Returns1` | Call with `args = ["health"]` (no API running); assert exit code 1. |
| `New_WithProjectName_Returns0_AndCreatesFiles` | Call with `args = ["new", "TestProject"]` in a temp directory; assert exit code 0 and that `TestProject/archlucid.json`, `TestProject/inputs/brief.md` exist. |

### Validation

- `dotnet test ArchLucid.Cli.Tests --filter "FullyQualifiedName~CommandLineTests"` passes.

---

## Phase 5: Documentation and Optional Cleanup

**Goal:** Document CLI usage; optionally remove or gate non-API behavior in the CLI.

### Files to create (optional)

| File | Purpose |
|------|---------|
| `docs/CLI_USAGE.md` | Reference: all commands, `archlucid.json` fields, `ARCHLUCID_API_URL`, default API URL (`http://localhost:5128`). |

### Files to modify

| File | Change |
|------|--------|
| `README.md` | Add a **CLI** section: how to run the CLI (`dotnet run --project ArchLucid.Cli` or `dotnet tool install` if published), commands `new`, `run`, `status`, `submit`, `commit`, `seed`, `artifacts`, `health`, `dev up`; note that the API must be running (or set `ARCHLUCID_API_URL`). Mention default URL from `ArchLucid.Api` launchSettings (e.g. `http://localhost:5128`). |
| `ArchLucid.Cli/ArchLucidProjectScaffolder.cs` | Optional: remove or gate the direct `SqlConnection` / `INSERT INTO PROJECTS` block (around lines 84–107) so scaffolding does not require SQL Server. Replace with no-op or a config flag "registerProject" default false. |

### Tests to add

- None in this phase.

---

## Summary: Files and Tests at a Glance

| Item | Action |
|------|--------|
| `ArchLucid.Cli.Tests/ArchLucid.Cli.Tests.csproj` | **Create** – test project. |
| `ArchLucid.sln` | **Modify** – add ArchLucid.Cli.Tests under tests folder. |
| `ArchLucid.Cli.Tests/ArchLucidCliConfigTests.cs` | **Create** – 4 tests for LoadConfig. |
| `ArchLucid.Cli.Tests/ArchLucidApiClientTests.cs` | **Create** – 3 tests for ResolveBaseUrl. |
| `ArchLucid.Cli.Tests/ArchLucidApiClientHttpTests.cs` | **Create** – 7 tests for client with mocked HTTP. |
| `ArchLucid.Cli.Tests/CommandLineTests.cs` | **Create** – 4 tests for exit codes/output. |
| `ArchLucid.Cli/ArchLucidApiClient.cs` | **Modify** (optional) – add constructor taking `HttpClient` for tests. |
| `ArchLucid.Cli/Program.cs` | **Modify** (optional) – expose `RunAsync(args)` for tests. |
| `README.md` | **Modify** – add CLI section. |
| `docs/CLI_USAGE.md` | **Create** (optional) – full CLI reference. |
| `ArchLucid.Cli/ArchLucidProjectScaffolder.cs` | **Modify** (optional) – gate or remove direct SQL in scaffolder. |

---

## API Endpoints Reference (for client and tests)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/health` | Health check. |
| POST | `/v1/architecture/request` | Create run; body: `ArchitectureRequest`. |
| GET | `/v1/architecture/run/{runId}` | Get run, tasks, results. |
| POST | `/v1/architecture/run/{runId}/result` | Submit agent result. |
| POST | `/v1/architecture/run/{runId}/commit` | Merge results and produce manifest. |
| GET | `/v1/architecture/manifest/{version}` | Get manifest by version. |
| POST | `/v1/architecture/run/{runId}/seed-fake-results` | Dev only; seed fake results. |

Base URL default: `http://localhost:5128` (from `ARCHLUCID_API_URL` or `archlucid.json` `apiUrl`). All JSON camelCase; errors use RFC 9457 Problem Details-style `detail`/`title` or legacy `error`/`errors`.

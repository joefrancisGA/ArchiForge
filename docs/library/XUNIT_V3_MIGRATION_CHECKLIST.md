> **Scope:** For ArchLucid engineers migrating test projects from xUnit v2 to v3; a sequential work checklist, not a substitute for the official xUnit migration guide or general unit-testing policy.

# xUnit.net v3 migration checklist (ArchLucid)

Use this as a sequential work list when moving test projects from **xUnit v2** (`xunit`) to **xUnit v3** (`xunit.v3.*`). Official references: [What’s new in v3](https://xunit.net/docs/getting-started/v3/whats-new), [Migrating from v2 to v3](https://xunit.net/docs/getting-started/v3/migration).

---

## Phase 0 — Decide and spike (blocking)

- [ ] **Read** the official migration guide (packages, executables, analyzer rule changes).
- [ ] **Confirm ecosystem** on your target frameworks (`net10.0` etc.):
  - [ ] **FsCheck** + **`FsCheck.Xunit`** — version that supports **xUnit v3** for all property-test projects (`Decisioning.Tests`, `Application.Tests`, `Contracts.Tests`).
  - [ ] **`Xunit.SkippableFact`** — v3-compatible package/version (used heavily in persistence/SQL integration tests).
  - [ ] **`Moq`, `FluentAssertions`, `coverlet.collector`** — no regression with v3 + `Microsoft.NET.Test.Sdk` layout you choose.
- [ ] **Spike**: migrate **one** high-value project first (recommend **`ArchLucid.Decisioning.Tests`** — FsCheck-heavy) until `dotnet test` is green locally and in CI-style conditions.
- [ ] Document **runner choice**: classic VSTest vs **Microsoft Testing Platform** (optional follow-up); note impact on VS / `dotnet test` flags and `.runsettings` if any.

## Phase 1 — Central package versions (`Directory.Packages.props`)

- [ ] Add **v3 package versions** (`xunit.v3`, `xunit.v3.assert`, runners as required by docs — do not blindly map 1:1 from v2).
- [ ] Align **`xunit.analyzers`** / analyzer suppressions (`xUnit3003` and others called out in migration doc).
- [ ] Bump or remove **`xunit.runner.visualstudio`** per v3 guidance (may differ by IDE/SDK).
- [ ] Remove obsolete v2 **`PackageVersion`** rows only after **no** project references them.

## Phase 2 — Every test `.csproj` (bulk edit)

Apply to **all** `*.Tests.csproj` (and **`ArchLucid.Architecture.Tests`**, **`ArchLucid.TestSupport`** if it references xUnit), plus **`templates/archlucid-finding-engine/ArchLucidFindingEngine.Tests`** (bring under CPVM or align versions).

For each project:

- [ ] Replace **`PackageReference Include="xunit"`** → v3 meta/core packages per migration doc.
- [ ] Set **`<OutputType>Exe</OutputType>`** where required for v3 test executables (per guidance for your SDK).
- [ ] Resolve **duplicate entry points**: remove or adjust any **custom `Program.cs`** / `Main` that conflicts with generated xUnit v3 host (migration doc describes this).
- [ ] Preserve **`InternalsVisibleTo`** and **test-specific `PropertyGroup`** (`IsPackable`, `RootNamespace`, etc.).

## Phase 3 — Source and API updates

Repo-specific hotspots (grep-driven):

- [ ] **`using Xunit.Abstractions`** / **`ITestOutputHelper`** — update to v3 patterns (minimal files today: Api/Application performance/contract tests).
- [ ] **`IAsyncLifetime`**, **`IClassFixture`**, **`ICollectionFixture`** — compile and behave under v3; fix any fixture teardown ordering regressions.
- [ ] **`[SkippableFact]`** usages — compile and skip correctly under v3 runner.
- [ ] **`[Property]`** / **FsCheck** tests — discovery and `[Trait]`/`[Fact]` interplay; resolve **xUnit1031 / sync-async** pragmas where APIs change.
- [ ] **`Microsoft.AspNetCore.Mvc.Testing`** / **`WebApplicationFactory`** (**`ArchLucid.Api.Tests`**) — full integration pass (startup, auth, parallelism).
- [ ] **`MemberData`** / **`Theory`** / **`InlineData`** — fix any analyzer or serialization behavior changes cited in migration notes.
- [ ] Third-party **`[Trait]`** filtering in CI scripts — verify filters still match (`Category`, `Suite`, `Slow`, etc.).

## Phase 4 — CI / developer workflow

- [ ] Update **`dotnet test`** invocation(s) (scripts, pipelines, README) if MTP or runner flags change.
- [ ] Run **container / SQL** tests that use **SkippableFact** (same skip conditions, environment variables).
- [ ] **Code coverage** (`coverlet`) — reports still generated and merged.
- [ ] **Parallelism / timeouts** — watch for flakiness from v3 process model; tune if needed.

## Phase 5 — Completion

- [ ] Full solution: **`dotnet test ArchLucid.sln`** (or split legs matching CI) green on **Release** where applicable.
- [ ] **`templates/archlucid-finding-engine`** uses same xUnit generation as repo (prefer CPVM-import from parent if template allows).
- [ ] Short **internal note** (PR description or README): v3 rationale, MTP status, known caveats.

## Rollback

- [ ] Keep migration in **one branch/PR series** per phase if possible so reverting **`Directory.Packages.props` + `.csproj` + critical source** restores v2 behavior.

---

## Inventory reminder (when prioritizing fixes)

High-risk projects for ArchLucid: **`ArchLucid.Decisioning.Tests`** (many FsCheck files), **`ArchLucid.Application.Tests`**, **`ArchLucid.Contracts.Tests`**, **`ArchLucid.Persistence.Tests` / Api integration tests** (Skippable + containers), **`ArchLucid.Api.Tests`** (`WebApplicationFactory`, `ITestOutputHelper`).

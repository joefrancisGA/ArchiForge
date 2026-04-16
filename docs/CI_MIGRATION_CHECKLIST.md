# CI migration and demo seeding regression loop

This document describes the minimum checks that must pass whenever a SQL migration,
a demo seeding change, or a `DemoSeedService` / `IDemoSeedService` change lands.
Run these locally before pushing; they must also run in CI on every PR.

---

## Why this matters

- `DemoSeedService` is idempotent by design — running it twice must not throw or duplicate data.
- Every DbUp migration must be idempotent (`IF NOT EXISTS`, `IF OBJECT_ID IS NULL`, etc.).
- `ArchLucid.sql` (greenfield SQL Server) must stay in sync with migration `0NN_*.sql` files, or greenfield bootstrap drifts from DbUp-upgraded databases.

---

## Local pre-push loop

Run these commands from the repo root before every push that touches SQL or seeding:

```powershell
# 1. Build everything — catches CS errors in seeding or repo changes
dotnet build ArchLucid.sln --configuration Debug

# 2. Run the DemoSeedService idempotency test
dotnet test ArchLucid.Api.Tests\ArchLucid.Api.Tests.csproj `
  --filter "FullyQualifiedName~DemoSeedServiceTests" `
  --no-build

# 3. Run the migration ordering / content tests
dotnet test ArchLucid.Api.Tests\ArchLucid.Api.Tests.csproj `
  --filter "FullyQualifiedName~DatabaseMigrationScriptTests" `
  --no-build

# 4. Run all unit-category tests (fast, no API stack)
dotnet test ArchLucid.sln --filter "Category=Unit" --no-build

# 5. (Optional, slower) Run full integration suite
dotnet test ArchLucid.sln --filter "Category=Integration" --no-build
```

If any of steps 1–4 fail, do not push. Step 5 should pass on the PR branch before merging.

---

## What the tests validate

| Test | File | What is asserted |
|---|---|---|
| `SeedAsync_twice_does_not_throw_and_remains_idempotent` | `DemoSeedServiceTests.cs` | Calling seed twice in the same DB session produces no exception and yields the same state |
| `DatabaseMigrationScriptTests` | `DatabaseMigrationScriptTests.cs` | Scripts exist in the expected order; no gaps or duplicate numbers; each script name parses |

---

## Adding a new migration (017+)

Follow this checklist **before** opening a PR:

- [ ] Created `ArchLucid.Persistence/Migrations/017_YourChange.sql` — idempotent DDL only.
- [ ] Updated `ArchLucid.Persistence/Scripts/ArchLucid.sql` with the same objects/columns.
- [ ] Extended `DatabaseMigrationScriptTests` if new ordering rules apply.
- [ ] Updated `docs/SQL_SCRIPTS.md` migration catalog (§4.2) with the new entry.
- [ ] Updated `docs/DATA_MODEL.md` if the conceptual data model changed.
- [ ] All five steps of the local pre-push loop pass.

---

## Adding or changing DemoSeedService

- [ ] `DemoSeedService` still calls every `EnsureXxxAsync` with idempotent semantics (MERGE or INSERT-if-not-exists).
- [ ] Demo keys come from `ContosoRetailDemoIds.ForTenant(tenantId)` (or the canonical path inside it for `ScopeIds.DefaultTenant`), not ad-hoc literals.
- [ ] `DemoSeedServiceTests.SeedAsync_twice_does_not_throw_and_remains_idempotent` still passes.
- [ ] If a new `EnsureXxxAsync` block is added, a corresponding test case or assertion is added for it.

---

## CI pipeline integration (recommended)

Add the following to your CI YAML (Azure DevOps / GitHub Actions):

```yaml
- name: Build
  run: dotnet build ArchLucid.sln --configuration Release

- name: Unit tests
  run: dotnet test ArchLucid.sln --filter "Category=Unit" --no-build --logger trx

- name: Integration tests (includes DemoSeedService + migration tests)
  run: dotnet test ArchLucid.sln --filter "Category=Integration" --no-build --logger trx
```

**ArchLucid.Api.Tests** integration tests require a reachable **SQL Server** (default **`localhost`**); factories create temporary databases and run **DbUp**. `DemoSeedServiceTests` and `DatabaseMigrationScriptTests` run in this phase when included in the filter.

---

## Security notes

- Connection strings must never appear in migration `.sql` files.
- `RegisterProject = false` is the default in `ScaffoldOptions`; enabling it requires an explicit `ConnectionString` on `ScaffoldOptions` (no hardcoded default).
- API keys, secrets, and passwords must be sourced from User Secrets, environment variables, or Azure Key Vault — never committed.

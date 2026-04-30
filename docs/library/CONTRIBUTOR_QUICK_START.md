> **Scope:** Contributor fast path — clone, build, tests, optional SQL, run API/UI; links to deeper maps (no shaping/commercial depth).

# Contributor quick start

**Time box:** one sitting to a green local loop on Windows (PowerShell). For product scope and gates, read **[V1_SCOPE.md](V1_SCOPE.md)** after you can build.

## 1. Clone and restore

```powershell
git clone <your-fork-or-upstream-url> ArchLucid
Set-Location ArchLucid
dotnet restore
```

## 2. Build

```powershell
dotnet build ArchLucid.sln -c Release
```

## 3. Docker Compose (API + dependencies)

Use the repository `compose` / dev profile if present in your branch (SQL, Azurite, etc.). From repo root:

```powershell
docker compose --profile dev up -d
```

If Compose is not configured locally, rely on **optional SQL** (below) or InMemory-only tests.

## 4. Fast tests (no SQL)

```powershell
dotnet test ArchLucid.Core.Tests/ArchLucid.Core.Tests.csproj -c Release
dotnet test ArchLucid.Application.Tests/ArchLucid.Application.Tests.csproj -c Release --filter "Category!=SqlIntegration"
```

Full strict coverage and SQL-backed suites need **`ARCHLUCID_SQL_TEST`** — see **[CODE_COVERAGE.md](CODE_COVERAGE.md)**.

## 5. Optional SQL-backed tests

Set **`ARCHLUCID_SQL_TEST`** to a SQL Server connection string (same pattern as CI; trust/cert options as in **[BUILD.md](BUILD.md)**). Then run the Persistence or full-regression profile you need.

## 6. Run the API locally

```powershell
dotnet run --project ArchLucid.Api/ArchLucid.Api.csproj
```

Verify **`GET /version`** and use the OpenAPI doc route your environment exposes.

## 7. Run the operator UI

From **`archlucid-ui`** (Node 22 per repo tooling):

```powershell
Set-Location archlucid-ui
npm install
npm run dev
```

Point the UI at your local API base URL as documented in UI **`.env.example`**.

## 8. First change

- Pick a small issue (test, bugfix, or doc typo). **Keep diffs focused.**
- Match **`.editorconfig`** and C# house style in **`docs/CSHARP_HOUSE_STYLE.md`** (guard clauses, null patterns, primary constructors where appropriate).

## Deeper maps

- **[CODE_MAP.md](CODE_MAP.md)** — where major subsystems live.
- **[ARCHITECTURE_ON_ONE_PAGE.md](../ARCHITECTURE_ON_ONE_PAGE.md)** — single-screen system overview.
